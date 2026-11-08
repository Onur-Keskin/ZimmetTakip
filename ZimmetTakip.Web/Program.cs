using Microsoft.EntityFrameworkCore;
using ZimmetTakip.Web.Models;

var builder = WebApplication.CreateBuilder(args);

// MVC servislerini ekle (Controller + View)
builder.Services.AddControllersWithViews();

// Veritabanı bağlantısı: scaffold edilen ZimmetDbContext'i SQL Server ile kaydet.
// Bağlantı cümlesi appsettings.json içindeki "ZimmetDb" anahtarından okunur.
builder.Services.AddDbContext<ZimmetDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ZimmetDb")));

var app = builder.Build();

// Geliştirme ortamı dışında hataları genel hata sayfasına yönlendir
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// İstek işleme sırası: HTTPS yönlendirme → yönlendirme (routing) → yetkilendirme
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

// wwwroot altındaki statik dosyalar (css, js, lib)
app.MapStaticAssets();

// Varsayılan rota: /Controller/Action/id (varsayılan Home/Index)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
