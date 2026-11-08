using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZimmetTakip.Web.Models;
using ZimmetTakip.Web.ViewModels;

namespace ZimmetTakip.Web.Controllers;

// "Zimmetlerim" sayfası: giriş yapan kullanıcı yalnızca KENDİ zimmetlerini görür.
// Tüm giriş yapmış kullanıcılar (Admin ve User) erişebilir.
[Authorize]
public class MyAssignmentsController : Controller
{
    private readonly ZimmetDbContext _context;

    // Veritabanı bağlamı (DbContext) dependency injection ile alınır
    public MyAssignmentsController(ZimmetDbContext context)
    {
        _context = context;
    }

    // GET /MyAssignments
    // Giriş yapan kullanıcının aktif zimmetlerini ve zimmet geçmişini listeler.
    // Güvenlik: kullanıcı id'si URL'den alınmaz; giriş sırasında cookie'ye yazılan
    // NameIdentifier claim'inden okunur. Böylece başka birinin zimmetleri görüntülenemez.
    public async Task<IActionResult> Index()
    {
        int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Sadece bu kullanıcıya ait zimmetleri, varlık ve kategori bilgileriyle al
        var assignments = await _context.Assignments
            .Include(a => a.Asset)
            .ThenInclude(asset => asset.Category)
            .Where(a => a.UserId == currentUserId)
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
}
