# ZimmetTakip — Proje Bağlamı

> BLM5227 Web Programlama dönem projesi (YTÜ YL). Bu dosya proje kökünde durur;
> Claude Code promptları şema ve kurallar için bu dosyaya referans verir.

## Amaç

Şirketteki BT varlıklarının (laptop, monitör, yazılım lisansı vb.) envanterini tutan
ve bu varlıkların personele zimmetlenmesini yöneten küçük ölçekli bir bilgi sistemi.

## Kesin kurallar (ihlal edilemez)

1. Klasik **ASP.NET Core 10 MVC** (Razor view). SPA yok.
2. **EF Core DATABASE-FIRST.** `dotnet ef migrations` ve code-first ASLA kullanılmaz.
   Şema değişikliği gerekirse T-SQL script önerilir, kullanıcı SSMS'te çalıştırır,
   ardından scaffold yenilenir. Scaffold edilen `Models/` elle değiştirilmez.
3. **Adlandırma:** tüm tablo ve kolon adları İngilizce **snake_case** (`users`,
   `asset_name` gibi). Kod tarafı İngilizce (controller, model, değişken);
   ekrandaki tüm metinler, başlıklar ve mesajlar Türkçe.
4. **ASP.NET Identity kullanılmaz.** Cookie authentication + aşağıdaki `users` tablosu.
5. Parolalar **BCrypt.Net-Next** ile hashlenir, asla düz metin saklanmaz.
6. JavaScript tarafında **jQuery 3.x + `$.ajax`** kullanılır; `fetch` kullanılmaz.
   AJAX POST istekleri anti-forgery token gönderir.
7. **Kod stili:** basit ve okunabilir kod — clever çözüm yok. Her anlamlı bloğun
   üstüne Türkçe açıklayıcı yorum satırı; her controller action'ının üstünde ne
   yaptığını anlatan kısa açıklama. Kod sözlü sınavda sunulup anlatılacak.
8. UI tasarımı Bootstrap 5.
9. Her fazda yalnızca istenen iş yapılır; kapsam genişletilmez.

## Roller ve yetkiler

| Rol   | Yetkiler                                                                 |
|-------|--------------------------------------------------------------------------|
| Admin | Kategori/varlık/kullanıcı CRUD; zimmet atama ve iade; tüm zimmet geçmişi |
| User  | Kayıt/giriş; yalnızca KENDİ zimmetleri ve geçmişi                        |

Tüm action'lar varsayılan `[Authorize]`; Login/Register/AccessDenied `[AllowAnonymous]`;
admin controller'ları `[Authorize(Roles="Admin")]`. Yetkisiz erişim → AccessDenied sayfası.

## Veritabanı şeması (ZimmetTakipDB)

### roles
| Kolon     | Tip               | Kısıt            |
|-----------|-------------------|------------------|
| role_id   | INT IDENTITY(1,1) | PK               |
| role_name | NVARCHAR(50)      | NOT NULL, UNIQUE |

### users
| Kolon         | Tip               | Kısıt                           |
|---------------|-------------------|----------------------------------|
| user_id       | INT IDENTITY(1,1) | PK                               |
| full_name     | NVARCHAR(100)     | NOT NULL                         |
| email         | NVARCHAR(150)     | NOT NULL, UNIQUE                 |
| password_hash | NVARCHAR(100)     | NOT NULL                         |
| role_id       | INT               | NOT NULL, FK → roles(role_id)    |
| department    | NVARCHAR(100)     | NULL                             |
| is_active     | BIT               | NOT NULL, DEFAULT 1              |
| created_at    | DATETIME2         | NOT NULL, DEFAULT SYSDATETIME()  |

### categories
| Kolon         | Tip               | Kısıt            |
|---------------|-------------------|------------------|
| category_id   | INT IDENTITY(1,1) | PK               |
| category_name | NVARCHAR(100)     | NOT NULL, UNIQUE |
| description   | NVARCHAR(250)     | NULL             |

### assets
| Kolon         | Tip               | Kısıt                                                                  |
|---------------|-------------------|-------------------------------------------------------------------------|
| asset_id      | INT IDENTITY(1,1) | PK                                                                      |
| category_id   | INT               | NOT NULL, FK → categories(category_id)                                  |
| asset_name    | NVARCHAR(150)     | NOT NULL                                                                |
| brand         | NVARCHAR(100)     | NULL                                                                    |
| model         | NVARCHAR(100)     | NULL                                                                    |
| serial_no     | NVARCHAR(100)     | NOT NULL, UNIQUE                                                        |
| status        | NVARCHAR(20)      | NOT NULL, DEFAULT 'in_stock', CHECK ('in_stock','assigned','faulty','retired') |
| purchase_date | DATE              | NULL                                                                    |
| description   | NVARCHAR(250)     | NULL                                                                    |

### assignments
| Kolon           | Tip               | Kısıt                              |
|-----------------|-------------------|-------------------------------------|
| assignment_id   | INT IDENTITY(1,1) | PK                                  |
| asset_id        | INT               | NOT NULL, FK → assets(asset_id)     |
| user_id         | INT               | NOT NULL, FK → users(user_id)       |
| assigned_at     | DATETIME2         | NOT NULL, DEFAULT SYSDATETIME()     |
| returned_at     | DATETIME2         | NULL (NULL = aktif zimmet)          |
| assignment_note | NVARCHAR(250)     | NULL                                |
| return_note     | NVARCHAR(250)     | NULL                                |

> Not: `ux_assignments_active_asset` filtreli unique index'i yalnızca veritabanında tanımlıdır.
> EF Core scaffold bu index'i görünce assets-assignments ilişkisini yanlışlıkla bire bir (`WithOne`) üretir.
> Bu yüzden scaffold `--force` ile yenilenirse `Models/Asset.cs` ve `Models/ZimmetDbContext.cs`'teki
> bu değişiklik geri alınmalıdır (`Asset.Assignments` koleksiyon olarak kalmalı).

> Not: EF Core scaffold snake_case adları C# tarafında otomatik PascalCase'e çevirir
> (`full_name` → `FullName`, `assignments` → `Assignment`), yani kod tarafı temiz kalır.

### Status / rol değerleri ↔ ekran karşılıkları

| DB değeri | Ekranda   |
|-----------|-----------|
| in_stock  | Depoda    |
| assigned  | Zimmetli  |
| faulty    | Arızalı   |
| retired   | Hurda     |
| Admin     | Yönetici  |
| User      | Kullanıcı |

### Seed verisi
- roles: `Admin`, `User`
- 1 admin kullanıcı (parola BCrypt hash'li)
- 4 kategori: Laptop, Monitör, Yazılım Lisansı, Aksesuar
- ~10 örnek varlık (farklı kategori ve durumlarda; en az 1 tanesi `faulty`)

## İş kuralları

1. Bir varlığın aynı anda **tek aktif zimmeti** olabilir (`returned_at IS NULL` olan tek satır).
   Yeni zimmet açılırken kontrol edilir.
2. Zimmet atama: yalnızca `status='in_stock'` varlıklar seçilebilir; kayıtla birlikte
   varlık `status='assigned'` yapılır. Zimmet insert + varlık update **tek transaction**.
3. İade: `returned_at` ve `return_note` doldurulur, varlık `status='in_stock'` yapılır.
4. Zimmet geçmişi ayrı tablo değildir: `returned_at` dolu satırlar geçmiştir.
5. Kullanıcı silinmez, `is_active=0` ile pasife alınır.
6. Bağlı varlığı olan kategori ve aktif zimmeti olan varlık silinemez (engelle + mesaj).

## Fazlar (sıra önemli)

| Faz | Kapsam                                                                 |
|-----|------------------------------------------------------------------------|
| 0   | MVC iskeleti, layout, Git, CLAUDE.md                                   |
| 1   | T-SQL script (kullanıcı SSMS'te çalıştırır) + EF scaffold              |
| 2   | Cookie auth: Login/Register/Logout/AccessDenied, roller                |
| 3   | Admin CRUD: CategoryController, AssetController, UserController        |
| 4   | AssignmentController (atama/iade) + MyAssignmentsController (Zimmetlerim) |
| 5   | jQuery/AJAX: canlı arama, cascade dropdown, AJAX iade, DOM efektleri   |
| 6   | [Authorize] taraması, rol bazlı menü, yorum geçişi, README             |

## AJAX senaryoları (madde 7 kanıtı)

1. `Asset/Index` — keyup + 300ms debounce ile canlı arama (JSON endpoint, tabloyu jQuery çizer)
2. `Assignment/Create` — kategori → varlık cascade dropdown (yalnızca in_stock)
3. İade Al butonu — AJAX POST, satır DOM'da güncellenir, Bootstrap toast
4. Saf DOM/jQuery — satır detayı `slideToggle`, `faulty` satırların sayfa yüklenince vurgulanması
