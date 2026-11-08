using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZimmetTakip.Web.Models;
using ZimmetTakip.Web.ViewModels;

namespace ZimmetTakip.Web.Controllers;

// Giriş, kayıt, çıkış ve yetkisiz erişim işlemlerini yöneten controller.
// Kimlik doğrulama cookie ile yapılır; ASP.NET Identity kullanılmaz.
public class AccountController : Controller
{
    private readonly ZimmetDbContext _context;

    // Veritabanı bağlamı (DbContext) dependency injection ile alınır
    public AccountController(ZimmetDbContext context)
    {
        _context = context;
    }

    // GET /Account/Login
    // Giriş formunu gösterir. returnUrl: girişten sonra dönülecek sayfa (örn. yetki istenen sayfa).
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST /Account/Login
    // E-posta ve parolayı doğrular; başarılıysa kullanıcı bilgileriyle cookie oluşturup giriş yapar.
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        // Form kurallarına uymayan veri geldiyse formu hatalarla birlikte geri göster
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Kullanıcıyı e-posta ile bul; rol adını da almak için Role tablosunu dahil et
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == model.Email.Trim());

        // Kullanıcı yoksa veya parola hash ile eşleşmiyorsa hata ver.
        // Güvenlik için hangisinin yanlış olduğu söylenmez.
        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "E-posta veya parola hatalı.");
            return View(model);
        }

        // Pasif (is_active = 0) kullanıcı giriş yapamaz
        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Hesabınız pasif durumdadır. Lütfen sistem yöneticisi ile iletişime geçin.");
            return View(model);
        }

        // Cookie içinde saklanacak kullanıcı bilgileri (claim'ler):
        // NameIdentifier = user_id, Name = full_name, Role = role_name
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.RoleName)
        };

        // Claim'lerden kimlik ve kullanıcı (principal) nesnesini oluştur, cookie ile giriş yap
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        // Girişten önce bir sayfaya gitmek istiyorsa oraya dön.
        // Url.IsLocalUrl kontrolü, başka bir siteye yönlendirme (open redirect) açığını engeller.
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    // GET /Account/Register
    // Kayıt formunu gösterir.
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // POST /Account/Register
    // Yeni kullanıcıyı "User" rolüyle kaydeder; parola BCrypt ile hash'lenerek saklanır.
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        // Form kurallarına uymayan veri geldiyse formu hatalarla birlikte geri göster
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string email = model.Email.Trim();

        // Aynı e-posta ile kayıtlı kullanıcı varsa hata ver (users.email benzersizdir)
        bool emailExists = await _context.Users.AnyAsync(u => u.Email == email);
        if (emailExists)
        {
            ModelState.AddModelError(nameof(model.Email), "Bu e-posta adresi ile kayıtlı bir kullanıcı zaten var.");
            return View(model);
        }

        // Yeni kayıtlar her zaman "User" rolünü alır
        var userRole = await _context.Roles.FirstAsync(r => r.RoleName == "User");

        // Yeni kullanıcı nesnesi: parola düz metin değil, BCrypt hash'i olarak saklanır
        var user = new User
        {
            FullName = model.FullName.Trim(),
            Email = email,
            Department = string.IsNullOrWhiteSpace(model.Department) ? null : model.Department.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            RoleId = userRole.RoleId,
            IsActive = true
        };

        // Kullanıcıyı veritabanına kaydet (created_at veritabanında otomatik dolar)
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Başarı mesajını bir sonraki sayfaya taşı ve giriş sayfasına yönlendir
        TempData["SuccessMessage"] = "Kayıt başarılı. Şimdi giriş yapabilirsiniz.";
        return RedirectToAction(nameof(Login));
    }

    // POST /Account/Logout
    // Kullanıcının cookie'sini silerek çıkış yapar ve giriş sayfasına yönlendirir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    // GET /Account/AccessDenied
    // Kullanıcının rolü bir sayfaya yetmediğinde gösterilen "erişim engellendi" sayfası.
    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
