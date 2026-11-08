using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZimmetTakip.Web.Models;

namespace ZimmetTakip.Web.Controllers;

// Ana sayfa ve genel hata sayfası.
// Global AuthorizeFilter nedeniyle ana sayfa giriş ister.
public class HomeController : Controller
{
    // GET / veya /Home/Index
    // Giriş yapmış kullanıcıya karşılama sayfasını gösterir.
    public IActionResult Index()
    {
        return View();
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
