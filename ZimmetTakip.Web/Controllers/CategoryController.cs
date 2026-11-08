using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZimmetTakip.Web.Models;
using ZimmetTakip.Web.ViewModels;

namespace ZimmetTakip.Web.Controllers;

// Kategori yönetimi (listele, ekle, düzenle, sil). Sadece Admin rolü erişebilir.
// Formlar Post-Redirect-Get düzeniyle çalışır: başarılı POST sonrası listeye yönlendirilir.
[Authorize(Roles = "Admin")]
public class CategoryController : Controller
{
    private readonly ZimmetDbContext _context;

    // Veritabanı bağlamı (DbContext) dependency injection ile alınır
    public CategoryController(ZimmetDbContext context)
    {
        _context = context;
    }

    // GET /Category
    // Tüm kategorileri ada göre sıralı listeler.
    public async Task<IActionResult> Index()
    {
        var categories = await _context.Categories
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        return View(categories);
    }

    // GET /Category/Create
    // Boş kategori ekleme formunu gösterir.
    [HttpGet]
    public IActionResult Create()
    {
        return View(new CategoryFormViewModel());
    }

    // POST /Category/Create
    // Formdan gelen bilgilerle yeni kategori ekler.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        // Form kurallarına uymayan veri geldiyse formu hatalarla geri göster
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string categoryName = model.CategoryName.Trim();

        // Aynı isimde kategori varsa ekleme (category_name benzersizdir)
        bool nameExists = await _context.Categories.AnyAsync(c => c.CategoryName == categoryName);
        if (nameExists)
        {
            ModelState.AddModelError(nameof(model.CategoryName), "Bu isimde bir kategori zaten var.");
            return View(model);
        }

        // Yeni kategoriyi oluştur ve kaydet
        var category = new Category
        {
            CategoryName = categoryName,
            Description = model.Description?.Trim()
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"\"{categoryName}\" kategorisi eklendi.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Category/Edit/5
    // Seçilen kategorinin bilgileriyle dolu düzenleme formunu gösterir.
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        // Entity'den form modeline aktar
        var model = new CategoryFormViewModel
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Description = category.Description
        };

        return View(model);
    }

    // POST /Category/Edit/5
    // Formdan gelen bilgilerle kategoriyi günceller.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
    {
        // Adresteki id ile formdaki id aynı olmalı
        if (id != model.CategoryId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        string categoryName = model.CategoryName.Trim();

        // Aynı isim başka bir kategoride kullanılıyor mu? (kategorinin kendisi hariç)
        bool nameExists = await _context.Categories
            .AnyAsync(c => c.CategoryName == categoryName && c.CategoryId != id);
        if (nameExists)
        {
            ModelState.AddModelError(nameof(model.CategoryName), "Bu isimde bir kategori zaten var.");
            return View(model);
        }

        // Değişiklikleri entity'ye aktar ve kaydet
        category.CategoryName = categoryName;
        category.Description = model.Description?.Trim();
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"\"{categoryName}\" kategorisi güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Category/Delete/5
    // Silme onay sayfasını gösterir.
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        return View(category);
    }

    // POST /Category/Delete/5
    // Kategoriyi siler. Kategoriye bağlı varlık varsa silme engellenir.
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        // Bu kategoride kayıtlı varlık varsa silme (önce varlıklar taşınmalı veya silinmeli)
        bool hasAssets = await _context.Assets.AnyAsync(a => a.CategoryId == id);
        if (hasAssets)
        {
            TempData["ErrorMessage"] = $"\"{category.CategoryName}\" kategorisine bağlı varlıklar olduğu için silinemez.";
            return RedirectToAction(nameof(Index));
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"\"{category.CategoryName}\" kategorisi silindi.";
        return RedirectToAction(nameof(Index));
    }
}
