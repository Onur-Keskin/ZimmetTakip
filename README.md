# ZimmetTakip — BT Envanter ve Zimmet Takip Sistemi

BLM5227 Web Programlama dönem projesi.

Şirketteki BT varlıklarının (laptop, monitör, yazılım lisansı, aksesuar vb.) envanterini tutan
ve bu varlıkların personele zimmetlenmesini / iadesini yöneten küçük ölçekli bir web uygulaması.

## Kullanılan teknolojiler

- **ASP.NET Core 10 MVC** (Razor view)
- **Entity Framework Core 10** — *database-first* (veritabanı SQL script ile kurulur, modeller scaffold edilir; migration kullanılmaz)
- **SQL Server** (geliştirmede LocalDB)
- **Cookie authentication** (ASP.NET Identity kullanılmaz) + **BCrypt.Net-Next** ile parola hash'leme
- **Bootstrap 5** (arayüz), **jQuery 3** + `$.ajax` (AJAX işlemleri), jQuery Validation (istemci tarafı doğrulama)

## Kurulum

Gereksinimler: .NET 10 SDK, SQL Server veya LocalDB, SQL Server Management Studio (SSMS).

1. **Veritabanını oluşturun:** `Database/ZimmetTakipDB.sql` dosyasını SSMS'te açıp tamamını çalıştırın (F5).
   Script `ZimmetTakipDB` veritabanını, tabloları, kısıtları ve örnek verileri (roller, admin kullanıcı,
   kategoriler, varlıklar) oluşturur.

2. **Bağlantı cümlesini kontrol edin:** `ZimmetTakip.Web/appsettings.json` içindeki `ConnectionStrings:ZimmetDb`
   değeri varsayılan olarak LocalDB'yi gösterir. Farklı bir SQL Server kullanıyorsanız `Server=` kısmını değiştirin.

   ```json
   "ConnectionStrings": {
     "ZimmetDb": "Server=(localdb)\\MSSQLLocalDB;Database=ZimmetTakipDB;Trusted_Connection=True;TrustServerCertificate=True"
   }
   ```

3. **Araçları geri yükleyin** (proje kökünde; `dotnet-ef` yerel aracı):

   ```bash
   dotnet tool restore
   ```

4. **Uygulamayı çalıştırın** (proje kökünde):

   ```bash
   dotnet run --project ZimmetTakip.Web
   ```

   Ardından tarayıcıda `http://localhost:5279` adresini açın.

## Varsayılan admin hesabı

| E-posta                   | Parola     |
|---------------------------|------------|
| `admin@zimmettakip.local` | `Admin123!` |

## Roller

| Rol                  | Yetkiler |
|----------------------|----------|
| **Admin** (Yönetici) | Kategori, varlık ve kullanıcı yönetimi; zimmet atama ve iade alma; tüm zimmet geçmişini görme. Kendi hesabını pasife alamaz ve kendi rolünü değiştiremez. |
| **User** (Kullanıcı) | Kayıt olma ve giriş yapma; yalnızca **kendi** zimmetlerini ve zimmet geçmişini görme ("Zimmetlerim"). |

- Kayıt sayfasından gelen yeni kullanıcılar otomatik olarak **User** rolünü alır.
- Kullanıcılar silinmez; pasife alınan (`is_active = 0`) kullanıcı giriş yapamaz.
- Yetkisiz bir sayfaya erişilmeye çalışılırsa "Erişim Engellendi" sayfası gösterilir.

## AJAX / jQuery senaryoları

1. **Canlı arama** (Varlıklar): arama kutusuna yazdıkça (300 ms debounce) JSON endpoint'ten sonuç alınır,
   tablo jQuery ile yeniden çizilir.
2. **Cascade dropdown** (Yeni Zimmet): kategori seçilince o kategorideki yalnızca "Depoda" varlıklar
   AJAX ile varlık listesine doldurulur.
3. **AJAX iade** (Zimmetler): "İade Al" butonu Bootstrap modal açar; onaylanınca anti-forgery token'lı
   AJAX POST yapılır, satır aktif sekmeden kaldırılıp geçmişe eklenir ve Bootstrap toast ile sonuç gösterilir.
4. **DOM efektleri** (Varlıklar): satıra tıklayınca detay satırı `slideToggle` ile açılır/kapanır;
   "Arızalı" durumundaki satırlar sayfa yüklenince vurgulanır.
