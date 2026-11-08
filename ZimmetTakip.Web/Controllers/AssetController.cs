using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ZimmetTakip.Web.Helpers;
using ZimmetTakip.Web.Models;
using ZimmetTakip.Web.ViewModels;

namespace ZimmetTakip.Web.Controllers;

// Varlık (envanter) yönetimi: listele, ekle, düzenle, sil. Sadece Admin rolü erişebilir.
// Formlar Post-Redirect-Get düzeniyle çalışır: başarılı POST sonrası listeye yönlendirilir.
[Authorize(Roles = "Admin")]
public class AssetController : Controller
{
    private readonly ZimmetDbContext _context;

    // Veritabanı bağlamı (DbContext) dependency injection ile alınır
    public AssetController(ZimmetDbContext context)
    {
        _context = context;
    }

    // GET /Asset
    // Tüm varlıkları kategori adlarıyla birlikte, ada göre sıralı listeler.
    public async Task<IActionResult> Index()
    {
        var assets = await _context.Assets
            .Include(a => a.Category)
            .OrderBy(a => a.AssetName)
            .ToListAsync();

        return View(assets);
    }

    // GET /Asset/Search?term=dell
    // Canlı arama için JSON döner (Asset/Index sayfasındaki jQuery $.ajax isteği çağırır).
    // Varlık adı, marka veya seri numarasında aranan metni içeren varlıkları getirir; boş aramada hepsini.
    [HttpGet]
    public async Task<IActionResult> Search(string? term)
    {
        var query = _context.Assets
            .Include(a => a.Category)
            .AsQueryable();

        // Arama metni girildiyse filtrele (SQL tarafında LIKE '%metin%' olarak çalışır)
        if (!string.IsNullOrWhiteSpace(term))
        {
            string searchText = term.Trim();
            query = query.Where(a =>
                a.AssetName.Contains(searchText) ||
                (a.Brand != null && a.Brand.Contains(searchText)) ||
                a.SerialNo.Contains(searchText));
        }

        var assets = await query
            .OrderBy(a => a.AssetName)
            .ToListAsync();

        // Tabloyu çizmek için gereken alanları JSON'a uygun basit nesnelere dönüştür.
        // Durum metni ve rozet rengi StatusHelper'dan alınır; böylece sunucuda çizilen tabloyla aynı görünür.
        var result = assets.Select(a => new
        {
            assetId = a.AssetId,
            assetName = a.AssetName,
            categoryName = a.Category.CategoryName,
            brand = a.Brand,
            model = a.Model,
            serialNo = a.SerialNo,
            status = a.Status,
            statusText = StatusHelper.GetDisplayName(a.Status),
            statusBadgeClass = StatusHelper.GetBadgeClass(a.Status),
            purchaseDate = a.PurchaseDate?.ToString("dd.MM.yyyy"),
            description = a.Description
        });

        return Json(result);
    }

    // GET /Asset/Create
    // Boş varlık ekleme formunu gösterir (varsayılan durum: Depoda).
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new AssetFormViewModel
        {
            Status = StatusHelper.InStock
        };
        await FillOptionsAsync(model);

        return View(model);
    }

    // POST /Asset/Create
    // Formdan gelen bilgilerle yeni varlık ekler.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssetFormViewModel model)
    {
        // Form kurallarına uymayan veri geldiyse formu hatalarla geri göster
        if (!ModelState.IsValid)
        {
            await FillOptionsAsync(model);
            return View(model);
        }

        // Elle sadece Depoda / Arızalı / Hurda seçilebilir; "Zimmetli" durumu zimmet atamayla oluşur
        if (!StatusHelper.IsManualStatus(model.Status))
        {
            ModelState.AddModelError(nameof(model.Status), "Geçersiz durum seçimi.");
        }

        // Kategori ve seri numarası kontrolleri (eklemede mevcut varlık yok: id = 0)
        await ValidateAssetAsync(model, 0);

        if (!ModelState.IsValid)
        {
            await FillOptionsAsync(model);
            return View(model);
        }

        // Yeni varlığı oluştur, form bilgilerini aktar ve kaydet
        var asset = new Asset();
        CopyFormToAsset(model, asset);
        asset.Status = model.Status;

        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"\"{asset.AssetName}\" varlığı eklendi.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Asset/Edit/5
    // Seçilen varlığın bilgileriyle dolu düzenleme formunu gösterir.
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
        {
            return NotFound();
        }

        // Entity'den form modeline aktar
        var model = new AssetFormViewModel
        {
            AssetId = asset.AssetId,
            CategoryId = asset.CategoryId,
            AssetName = asset.AssetName,
            Brand = asset.Brand,
            ModelName = asset.Model,
            SerialNo = asset.SerialNo,
            Status = asset.Status,
            Description = asset.Description,
            IsAssigned = asset.Status == StatusHelper.Assigned
        };

        // Veritabanında DateOnly olan tarihi formdaki DateTime alanına çevir
        if (asset.PurchaseDate.HasValue)
        {
            model.PurchaseDate = asset.PurchaseDate.Value.ToDateTime(TimeOnly.MinValue);
        }

        await FillOptionsAsync(model);
        return View(model);
    }

    // POST /Asset/Edit/5
    // Formdan gelen bilgilerle varlığı günceller.
    // Zimmetli bir varlığın durumu buradan değiştirilemez (iade işlemiyle değişir).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AssetFormViewModel model)
    {
        // Adresteki id ile formdaki id aynı olmalı
        if (id != model.AssetId)
        {
            return NotFound();
        }

        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
        {
            return NotFound();
        }

        // Zimmetli olup olmadığı formdan değil, veritabanındaki güncel durumdan belirlenir
        model.IsAssigned = asset.Status == StatusHelper.Assigned;

        if (!ModelState.IsValid)
        {
            await FillOptionsAsync(model);
            return View(model);
        }

        // Zimmetli olmayan varlıkta sadece elle seçilebilen durumlar kabul edilir
        if (!model.IsAssigned && !StatusHelper.IsManualStatus(model.Status))
        {
            ModelState.AddModelError(nameof(model.Status), "Geçersiz durum seçimi.");
        }

        // Kategori ve seri numarası kontrolleri (varlığın kendisi hariç)
        await ValidateAssetAsync(model, id);

        if (!ModelState.IsValid)
        {
            await FillOptionsAsync(model);
            return View(model);
        }

        // Form bilgilerini entity'ye aktar; zimmetli varlığın durumuna dokunma
        CopyFormToAsset(model, asset);
        if (!model.IsAssigned)
        {
            asset.Status = model.Status;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"\"{asset.AssetName}\" varlığı güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Asset/Delete/5
    // Silme onay sayfasını gösterir.
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var asset = await _context.Assets
            .Include(a => a.Category)
            .FirstOrDefaultAsync(a => a.AssetId == id);
        if (asset == null)
        {
            return NotFound();
        }

        return View(asset);
    }

    // POST /Asset/Delete/5
    // Varlığı siler. Aktif zimmeti veya zimmet geçmişi olan varlık silinemez.
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
        {
            return NotFound();
        }

        // Aktif zimmet: returned_at alanı boş olan zimmet kaydı
        bool hasActiveAssignment = await _context.Assignments
            .AnyAsync(a => a.AssetId == id && a.ReturnedAt == null);
        if (hasActiveAssignment)
        {
            TempData["ErrorMessage"] = $"\"{asset.AssetName}\" varlığının aktif zimmeti olduğu için silinemez. Önce iade alınmalıdır.";
            return RedirectToAction(nameof(Index));
        }

        // İade edilmiş eski zimmet kayıtları da varsa silinemez:
        // zimmet geçmişi korunur (assignments tablosundaki FK de silmeye izin vermez).
        bool hasAssignmentHistory = await _context.Assignments.AnyAsync(a => a.AssetId == id);
        if (hasAssignmentHistory)
        {
            TempData["ErrorMessage"] = $"\"{asset.AssetName}\" varlığının zimmet geçmişi olduğu için silinemez. Kullanım dışıysa durumunu \"Hurda\" yapabilirsiniz.";
            return RedirectToAction(nameof(Index));
        }

        _context.Assets.Remove(asset);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"\"{asset.AssetName}\" varlığı silindi.";
        return RedirectToAction(nameof(Index));
    }

    // Formdaki kategori ve durum açılır listelerinin seçeneklerini doldurur
    private async Task FillOptionsAsync(AssetFormViewModel model)
    {
        model.CategoryOptions = await _context.Categories
            .OrderBy(c => c.CategoryName)
            .Select(c => new SelectListItem
            {
                Value = c.CategoryId.ToString(),
                Text = c.CategoryName
            })
            .ToListAsync();

        model.StatusOptions = StatusHelper.GetManualStatusOptions();
    }

    // Veritabanı gerektiren doğrulamalar: kategori var mı, seri numarası benzersiz mi?
    // currentAssetId: düzenlenen varlığın id'si (eklemede 0), kendi seri numarasıyla çakışmasın diye
    private async Task ValidateAssetAsync(AssetFormViewModel model, int currentAssetId)
    {
        bool categoryExists = await _context.Categories.AnyAsync(c => c.CategoryId == model.CategoryId);
        if (!categoryExists)
        {
            ModelState.AddModelError(nameof(model.CategoryId), "Geçerli bir kategori seçiniz.");
        }

        string serialNo = model.SerialNo.Trim();
        bool serialExists = await _context.Assets
            .AnyAsync(a => a.SerialNo == serialNo && a.AssetId != currentAssetId);
        if (serialExists)
        {
            ModelState.AddModelError(nameof(model.SerialNo), "Bu seri numarası ile kayıtlı başka bir varlık var.");
        }
    }

    // Formdaki alanları varlık entity'sine kopyalar (durum hariç, durum ayrıca ele alınır)
    private static void CopyFormToAsset(AssetFormViewModel model, Asset asset)
    {
        asset.CategoryId = model.CategoryId!.Value;
        asset.AssetName = model.AssetName.Trim();
        asset.Brand = model.Brand?.Trim();
        asset.Model = model.ModelName?.Trim();
        asset.SerialNo = model.SerialNo.Trim();
        asset.Description = model.Description?.Trim();

        // Formdaki DateTime tarihi veritabanındaki DateOnly tipine çevir
        asset.PurchaseDate = null;
        if (model.PurchaseDate.HasValue)
        {
            asset.PurchaseDate = DateOnly.FromDateTime(model.PurchaseDate.Value);
        }
    }
}
