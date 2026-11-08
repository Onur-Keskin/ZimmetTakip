using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ZimmetTakip.Web.Helpers;
using ZimmetTakip.Web.Models;
using ZimmetTakip.Web.ViewModels;

namespace ZimmetTakip.Web.Controllers;

// Zimmet yönetimi: zimmet listesi, yeni zimmet atama ve iade alma. Sadece Admin rolü erişebilir.
// Zimmet kaydı ile varlık durumu her zaman birlikte değişir; bu yüzden ikisi tek transaction içinde yapılır.
[Authorize(Roles = "Admin")]
public class AssignmentController : Controller
{
    private readonly ZimmetDbContext _context;

    // Veritabanı bağlamı (DbContext) dependency injection ile alınır
    public AssignmentController(ZimmetDbContext context)
    {
        _context = context;
    }

    // GET /Assignment
    // Aktif zimmetleri ve zimmet geçmişini (iade edilmişler) ayrı listeler halinde gösterir.
    public async Task<IActionResult> Index()
    {
        // Varlık ve kullanıcı bilgileriyle birlikte tüm zimmet kayıtlarını al
        var assignments = await _context.Assignments
            .Include(a => a.Asset)
            .Include(a => a.User)
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync();

        // returned_at boşsa aktif zimmet, doluysa geçmiş kaydıdır
        var model = new AssignmentListViewModel
        {
            ActiveAssignments = assignments
                .Where(a => a.ReturnedAt == null)
                .ToList(),
            HistoryAssignments = assignments
                .Where(a => a.ReturnedAt != null)
                .OrderByDescending(a => a.ReturnedAt)
                .ToList()
        };

        return View(model);
    }

    // GET /Assignment/Create
    // Yeni zimmet formunu gösterir (sadece aktif kullanıcılar ve depodaki varlıklar listelenir).
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new AssignmentCreateViewModel();
        await FillOptionsAsync(model);

        return View(model);
    }

    // POST /Assignment/Create
    // Seçilen varlığı seçilen kullanıcıya zimmetler ve varlığın durumunu "assigned" yapar.
    // Dropdown'a güvenilmez: tüm kurallar sunucu tarafında tekrar kontrol edilir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssignmentCreateViewModel model)
    {
        // Form kurallarına uymayan veri geldiyse formu hatalarla geri göster
        if (!ModelState.IsValid)
        {
            await FillOptionsAsync(model);
            return View(model);
        }

        // Transaction başlat: kontroller, zimmet ekleme ve varlık güncelleme tek bir bütün olarak yapılır.
        // CommitAsync çağrılmadan metottan çıkılırsa (hata veya erken dönüş) hiçbir değişiklik kaydedilmez.
        await using var transaction = await _context.Database.BeginTransactionAsync();

        // Kullanıcı var mı ve aktif mi?
        var user = await _context.Users.FindAsync(model.UserId!.Value);
        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError(nameof(model.UserId), "Seçilen kullanıcı bulunamadı veya pasif durumda.");
        }

        // Varlık var mı ve depoda mı?
        var asset = await _context.Assets.FindAsync(model.AssetId!.Value);
        if (asset == null)
        {
            ModelState.AddModelError(nameof(model.AssetId), "Seçilen varlık bulunamadı.");
        }
        else if (asset.Status != StatusHelper.InStock)
        {
            ModelState.AddModelError(nameof(model.AssetId),
                $"Sadece \"Depoda\" durumundaki varlıklar zimmetlenebilir. Bu varlığın durumu: {StatusHelper.GetDisplayName(asset.Status)}.");
        }
        else if (model.CategoryId != null && asset.CategoryId != model.CategoryId)
        {
            // Kategori seçildiyse varlık o kategoriye ait olmalı (form dışından gelen isteklere karşı)
            ModelState.AddModelError(nameof(model.AssetId), "Seçilen varlık, seçilen kategoriye ait değil.");
        }

        // İş kuralı: bir varlığın aynı anda tek aktif zimmeti olabilir
        bool hasActiveAssignment = await _context.Assignments
            .AnyAsync(a => a.AssetId == model.AssetId && a.ReturnedAt == null);
        if (hasActiveAssignment)
        {
            ModelState.AddModelError(nameof(model.AssetId), "Bu varlığın zaten aktif bir zimmeti var.");
        }

        if (!ModelState.IsValid)
        {
            await FillOptionsAsync(model);
            return View(model);
        }

        // Yeni zimmet kaydını oluştur
        var assignment = new Assignment
        {
            AssetId = asset!.AssetId,
            UserId = user!.UserId,
            AssignedAt = DateTime.Now,
            AssignmentNote = model.AssignmentNote?.Trim()
        };
        _context.Assignments.Add(assignment);

        // Varlığın durumunu "Zimmetli" yap
        asset.Status = StatusHelper.Assigned;

        try
        {
            // Önce değişiklikleri veritabanına gönder, sonra transaction'ı onayla
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            // Yukarıdaki kontrollerden geçtikten sonra başka bir admin aynı varlığı
            // bizden hemen önce zimmetlemiş olabilir. Bu durumda veritabanındaki
            // ux_assignments_active_asset index'i ikinci aktif zimmeti reddeder.
            // Hata sayfası yerine işlemi geri alıp formu Türkçe mesajla tekrar gösteriyoruz.
            await transaction.RollbackAsync();

            ModelState.AddModelError(nameof(model.AssetId), "Bu varlık az önce başka bir kullanıcıya zimmetlendi. Lütfen başka bir varlık seçiniz.");
            await FillOptionsAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = $"\"{asset.AssetName}\" varlığı {user.FullName} kullanıcısına zimmetlendi.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Assignment/Return/5
    // İade formunu gösterir (zimmet bilgileri + iade notu alanı).
    [HttpGet]
    public async Task<IActionResult> Return(int id)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Asset)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.AssignmentId == id);
        if (assignment == null)
        {
            return NotFound();
        }

        // Zaten iade edilmiş zimmet tekrar iade edilemez
        if (assignment.ReturnedAt != null)
        {
            TempData["ErrorMessage"] = "Bu zimmet zaten iade edilmiş.";
            return RedirectToAction(nameof(Index));
        }

        var model = new AssignmentReturnViewModel
        {
            AssignmentId = assignment.AssignmentId
        };
        FillReturnDisplayFields(model, assignment);

        return View(model);
    }

    // POST /Assignment/Return/5
    // Zimmeti kapatır (returned_at = şimdi, iade notu kaydedilir) ve varlığı tekrar "in_stock" yapar.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(int id, AssignmentReturnViewModel model)
    {
        // Adresteki id ile formdaki id aynı olmalı
        if (id != model.AssignmentId)
        {
            return NotFound();
        }

        // Transaction başlat: zimmet güncelleme ve varlık güncelleme tek bir bütün olarak yapılır
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var assignment = await _context.Assignments
            .Include(a => a.Asset)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.AssignmentId == id);
        if (assignment == null)
        {
            return NotFound();
        }

        // Zaten iade edilmiş zimmet tekrar iade edilemez (örn. iki sekmeden aynı anda iade)
        if (assignment.ReturnedAt != null)
        {
            TempData["ErrorMessage"] = "Bu zimmet zaten iade edilmiş.";
            return RedirectToAction(nameof(Index));
        }

        // İade notu kurallara uymuyorsa formu bilgilerle birlikte geri göster
        if (!ModelState.IsValid)
        {
            FillReturnDisplayFields(model, assignment);
            return View(model);
        }

        // Zimmeti kapat ve varlığı tekrar "Depoda" yap (ortak metot)
        MarkAsReturned(assignment, model.ReturnNote);

        // Önce değişiklikleri veritabanına gönder, sonra transaction'ı onayla
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = $"\"{assignment.Asset.AssetName}\" varlığı {assignment.User.FullName} kullanıcısından iade alındı.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Assignment/ReturnAjax
    // Zimmetler sayfasındaki modal penceresinden jQuery $.ajax ile çağrılır.
    // Return (POST) ile aynı işi yapar ama sayfaya yönlendirmek yerine JSON döner.
    // Anti-forgery token, modal içindeki formla birlikte gönderilir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReturnAjax(int id, string? returnNote)
    {
        // İade notu uzunluk kontrolü (assignments.return_note en fazla 250 karakter)
        if (returnNote != null && returnNote.Trim().Length > 250)
        {
            return Json(new { success = false, message = "İade notu en fazla 250 karakter olabilir." });
        }

        // Transaction başlat: zimmet güncelleme ve varlık güncelleme tek bir bütün olarak yapılır
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var assignment = await _context.Assignments
            .Include(a => a.Asset)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.AssignmentId == id);
        if (assignment == null)
        {
            return Json(new { success = false, message = "Zimmet kaydı bulunamadı." });
        }

        // Zaten iade edilmiş zimmet tekrar iade edilemez
        if (assignment.ReturnedAt != null)
        {
            return Json(new { success = false, message = "Bu zimmet zaten iade edilmiş." });
        }

        // Zimmeti kapat ve varlığı tekrar "Depoda" yap (ortak metot)
        MarkAsReturned(assignment, returnNote);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        // JS'nin satırı "Zimmet Geçmişi" sekmesine ekleyebilmesi için gereken bilgileri döndür
        return Json(new
        {
            success = true,
            message = $"İade alındı: \"{assignment.Asset.AssetName}\" ({assignment.User.FullName})",
            assignmentId = assignment.AssignmentId,
            assetName = assignment.Asset.AssetName,
            serialNo = assignment.Asset.SerialNo,
            userFullName = assignment.User.FullName,
            assignedAt = assignment.AssignedAt.ToString("dd.MM.yyyy HH:mm"),
            returnedAt = assignment.ReturnedAt!.Value.ToString("dd.MM.yyyy HH:mm"),
            assignmentNote = assignment.AssignmentNote,
            returnNote = assignment.ReturnNote
        });
    }

    // İade işleminin ortak kısmı (hem normal formdan hem AJAX'tan kullanılır):
    // zimmet kapatılır (iade tarihi + notu) ve varlık tekrar "Depoda" durumuna alınır.
    // Kaydetme (SaveChanges) ve transaction çağıran metotta yapılır.
    private static void MarkAsReturned(Assignment assignment, string? returnNote)
    {
        assignment.ReturnedAt = DateTime.Now;
        assignment.ReturnNote = returnNote?.Trim();
        assignment.Asset.Status = StatusHelper.InStock;
    }

    // GET /Assignment/AssetsByCategory?categoryId=3
    // Cascade dropdown için JSON döner (Assignment/Create sayfasındaki jQuery $.ajax isteği çağırır).
    // Seçilen kategorideki yalnızca "Depoda" (in_stock) varlıkları listeler.
    [HttpGet]
    public async Task<IActionResult> AssetsByCategory(int categoryId)
    {
        var assets = await _context.Assets
            .Where(a => a.CategoryId == categoryId && a.Status == StatusHelper.InStock)
            .OrderBy(a => a.AssetName)
            .Select(a => new
            {
                value = a.AssetId,
                text = a.AssetName + " - " + a.SerialNo
            })
            .ToListAsync();

        return Json(assets);
    }

    // Formdaki kullanıcı, kategori ve varlık açılır listelerini doldurur.
    // Sadece aktif kullanıcılar ve "Depoda" durumundaki varlıklar listelenir.
    // (JS açıkken varlık listesi kategoriye göre AJAX ile yeniden doldurulur; JS kapalıyken bu tam liste kullanılır.)
    private async Task FillOptionsAsync(AssignmentCreateViewModel model)
    {
        model.CategoryOptions = await _context.Categories
            .OrderBy(c => c.CategoryName)
            .Select(c => new SelectListItem
            {
                Value = c.CategoryId.ToString(),
                Text = c.CategoryName
            })
            .ToListAsync();

        model.UserOptions = await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(u => new SelectListItem
            {
                Value = u.UserId.ToString(),
                Text = u.FullName + " (" + u.Email + ")"
            })
            .ToListAsync();

        model.AssetOptions = await _context.Assets
            .Where(a => a.Status == StatusHelper.InStock)
            .OrderBy(a => a.AssetName)
            .Select(a => new SelectListItem
            {
                Value = a.AssetId.ToString(),
                Text = a.AssetName + " - " + a.SerialNo
            })
            .ToListAsync();
    }

    // İade formunda gösterilen zimmet bilgilerini (varlık, kullanıcı, tarih, not) doldurur
    private static void FillReturnDisplayFields(AssignmentReturnViewModel model, Assignment assignment)
    {
        model.AssetName = assignment.Asset.AssetName;
        model.SerialNo = assignment.Asset.SerialNo;
        model.UserFullName = assignment.User.FullName;
        model.AssignedAt = assignment.AssignedAt;
        model.AssignmentNote = assignment.AssignmentNote;
    }
}
