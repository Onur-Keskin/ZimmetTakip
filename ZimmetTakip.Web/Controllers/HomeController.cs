using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZimmetTakip.Web.Helpers;
using ZimmetTakip.Web.Models;
using ZimmetTakip.Web.ViewModels;

namespace ZimmetTakip.Web.Controllers;

// Ana sayfa (özet paneli) ve genel hata sayfası.
// Global AuthorizeFilter nedeniyle ana sayfa giriş ister.
public class HomeController : Controller
{
    private readonly ZimmetDbContext _context;

    // Veritabanı bağlamı (DbContext) dependency injection ile alınır
    public HomeController(ZimmetDbContext context)
    {
        _context = context;
    }

    // GET / veya /Home/Index
    // Giriş yapan kullanıcıya özet panelini gösterir:
    // Admin → envanter ve zimmet sayıları, User → kendi üzerindeki aktif zimmet sayısı.
    public async Task<IActionResult> Index()
    {
        var model = new HomeIndexViewModel
        {
            IsAdmin = User.IsInRole("Admin")
        };

        if (model.IsAdmin)
        {
            // Admin kartları: her sayı ayrı bir basit COUNT sorgusuyla hesaplanır
            model.TotalAssetCount = await _context.Assets.CountAsync();
            model.InStockCount = await _context.Assets.CountAsync(a => a.Status == StatusHelper.InStock);
            model.AssignedCount = await _context.Assets.CountAsync(a => a.Status == StatusHelper.Assigned);
            model.FaultyCount = await _context.Assets.CountAsync(a => a.Status == StatusHelper.Faulty);
            model.ActiveAssignmentCount = await _context.Assignments.CountAsync(a => a.ReturnedAt == null);
        }
        else
        {
            // User kartı: kullanıcı id'si URL'den değil, cookie'deki NameIdentifier claim'inden okunur
            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            model.MyActiveAssignmentCount = await _context.Assignments
                .CountAsync(a => a.UserId == currentUserId && a.ReturnedAt == null);
        }

        return View(model);
    }

    // /Home/Error
    // Beklenmeyen hata olduğunda (geliştirme ortamı dışında) gösterilen genel hata sayfası.
    // [AllowAnonymous]: giriş yapmamış kullanıcı hata alırsa Login'e yönlendirilmek yerine hata sayfasını görsün.
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
