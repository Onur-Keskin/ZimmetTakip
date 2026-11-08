using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using ZimmetTakip.Web.Models;

var builder = WebApplication.CreateBuilder(args);

// MVC servislerini ekle (Controller + View).
// AuthorizeFilter global olarak eklendiği için tüm action'lar varsayılan olarak giriş ister;
// giriş gerektirmeyen action'lar [AllowAnonymous] ile işaretlenir.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter());
});

// Veritabanı bağlantısı: scaffold edilen ZimmetDbContext'i SQL Server ile kaydet.
// Bağlantı cümlesi appsettings.json içindeki "ZimmetDb" anahtarından okunur.
builder.Services.AddDbContext<ZimmetDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ZimmetDb")));

// Cookie tabanlı kimlik doğrulama (ASP.NET Identity kullanılmıyor).
// Giriş yapmamış kullanıcı Login sayfasına, yetkisi olmayan kullanıcı AccessDenied sayfasına yönlendirilir.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// Yetkilendirme servisleri ([Authorize], rol kontrolü)
builder.Services.AddAuthorization();

var app = builder.Build();

// Geliştirme ortamı dışında hataları genel hata sayfasına yönlendir
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// İstek işleme sırası: HTTPS yönlendirme → yönlendirme (routing)
app.UseHttpsRedirection();
app.UseRouting();

// Önce kimlik doğrulama (kullanıcı kim?), sonra yetkilendirme (bu sayfaya girebilir mi?)
app.UseAuthentication();
app.UseAuthorization();

// wwwroot altındaki statik dosyalar (css, js, lib)
app.MapStaticAssets();

// Varsayılan rota: /Controller/Action/id (varsayılan Home/Index)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
