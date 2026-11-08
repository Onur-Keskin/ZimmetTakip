using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ZimmetTakip.Web.Helpers;
using ZimmetTakip.Web.Models;
using ZimmetTakip.Web.ViewModels;

namespace ZimmetTakip.Web.Controllers;

// Kullanıcı yönetimi: listeleme ve rol / aktiflik düzenleme. Sadece Admin rolü erişebilir.
// Kullanıcılar silinmez; is_active = 0 yapılarak pasife alınır (zimmet geçmişi korunur).
[Authorize(Roles = "Admin")]
public class UserController : Controller
{
    private readonly ZimmetDbContext _context;

    // Veritabanı bağlamı (DbContext) dependency injection ile alınır
    public UserController(ZimmetDbContext context)
    {
        _context = context;
    }

    // GET /User
    // Tüm kullanıcıları rolleriyle birlikte, ada göre sıralı listeler.
    public async Task<IActionResult> Index()
    {
        var users = await _context.Users
            .Include(u => u.Role)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        return View(users);
    }

    // GET /User/Edit/5
    // Kullanıcının rolünü ve aktiflik durumunu düzenleme formunu gösterir.
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Entity'den form modeline aktar
        var model = new UserEditViewModel
        {
            UserId = user.UserId,
            RoleId = user.RoleId,
            IsActive = user.IsActive
        };
        FillDisplayFields(model, user);
        await FillRoleOptionsAsync(model);

        return View(model);
    }

    // POST /User/Edit/5
    // Kullanıcının rolünü ve aktiflik durumunu günceller.
    // Admin kendi hesabını pasife alamaz.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UserEditViewModel model)
    {
        // Adresteki id ile formdaki id aynı olmalı
        if (id != model.UserId)
        {
            return NotFound();
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Ad, e-posta gibi gösterim alanları formdan değil veritabanından doldurulur
        FillDisplayFields(model, user);

        if (!ModelState.IsValid)
        {
            await FillRoleOptionsAsync(model);
            return View(model);
        }

        // Admin kendi hesabını pasife alırsa sisteme bir daha giriş yapamaz; bunu engelle
        if (model.IsCurrentUser && !model.IsActive)
        {
            ModelState.AddModelError(nameof(model.IsActive), "Kendi hesabınızı pasife alamazsınız.");
        }

        // Seçilen rol veritabanında gerçekten var mı?
        bool roleExists = await _context.Roles.AnyAsync(r => r.RoleId == model.RoleId);
        if (!roleExists)
        {
            ModelState.AddModelError(nameof(model.RoleId), "Geçerli bir rol seçiniz.");
        }

        if (!ModelState.IsValid)
        {
            await FillRoleOptionsAsync(model);
            return View(model);
        }

        // Sadece rol ve aktiflik bilgisi güncellenir
        user.RoleId = model.RoleId!.Value;
        user.IsActive = model.IsActive;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"\"{user.FullName}\" kullanıcısı güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    // Formda sadece gösterilen alanları (ad, e-posta, departman) ve "kendisi mi" bilgisini doldurur
    private void FillDisplayFields(UserEditViewModel model, User user)
    {
        model.FullName = user.FullName;
        model.Email = user.Email;
        model.Department = user.Department;

        // Giriş yapan kullanıcının id'si cookie'deki NameIdentifier claim'inden okunur
        string? currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        model.IsCurrentUser = currentUserId == user.UserId.ToString();
    }

    // Rol açılır listesinin seçeneklerini doldurur (rol adları Türkçe gösterilir)
    private async Task FillRoleOptionsAsync(UserEditViewModel model)
    {
        var roles = await _context.Roles
            .OrderBy(r => r.RoleName)
            .ToListAsync();

        model.RoleOptions = new List<SelectListItem>();
        foreach (var role in roles)
        {
            model.RoleOptions.Add(new SelectListItem
            {
                Value = role.RoleId.ToString(),
                Text = RoleHelper.GetDisplayName(role.RoleName)
            });
        }
    }
}
