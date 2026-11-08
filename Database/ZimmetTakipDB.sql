/* =====================================================================
   ZimmetTakipDB - Veritabanı oluşturma ve örnek veri (seed) script'i
   BLM5227 dönem projesi - BT envanter ve zimmet takip sistemi
   Şema kaynağı: proje kökündeki proje-plan.md

   Çalıştırma: SSMS'te bu dosyanın tamamını tek seferde çalıştırın (F5).
   Not: ZimmetTakipDB zaten varsa CREATE DATABASE hata verir;
        yeniden kurmak için önce mevcut veritabanını silin.
   ===================================================================== */

-- Veritabanını oluştur
CREATE DATABASE ZimmetTakipDB;
GO

-- Sonraki tüm komutlar yeni veritabanında çalışsın
USE ZimmetTakipDB;
GO

/* ---------------------------------------------------------------------
   1) roles - Kullanıcı rolleri (Admin, User)
   - role_name benzersizdir (aynı rol iki kez tanımlanamaz)
   --------------------------------------------------------------------- */
CREATE TABLE roles (
    role_id   INT IDENTITY(1,1) NOT NULL,
    role_name NVARCHAR(50)      NOT NULL,

    CONSTRAINT pk_roles           PRIMARY KEY (role_id),
    CONSTRAINT uq_roles_role_name UNIQUE (role_name)
);
GO

/* ---------------------------------------------------------------------
   2) users - Sisteme giriş yapan ve zimmet alan kullanıcılar
   - email benzersizdir (giriş için kullanılır)
   - password_hash BCrypt hash'idir (60 karakter), düz parola tutulmaz
   - Kullanıcı silinmez; is_active = 0 yapılarak pasife alınır
   --------------------------------------------------------------------- */
CREATE TABLE users (
    user_id       INT IDENTITY(1,1) NOT NULL,
    full_name     NVARCHAR(100)     NOT NULL,
    email         NVARCHAR(150)     NOT NULL,
    password_hash NVARCHAR(100)     NOT NULL,
    role_id       INT               NOT NULL,
    department    NVARCHAR(100)     NULL,
    is_active     BIT               NOT NULL CONSTRAINT df_users_is_active  DEFAULT (1),
    created_at    DATETIME2         NOT NULL CONSTRAINT df_users_created_at DEFAULT (SYSDATETIME()),

    CONSTRAINT pk_users       PRIMARY KEY (user_id),
    CONSTRAINT uq_users_email UNIQUE (email),
    CONSTRAINT fk_users_roles FOREIGN KEY (role_id) REFERENCES roles (role_id)
);
GO

/* ---------------------------------------------------------------------
   3) categories - Varlık kategorileri (Laptop, Monitör vb.)
   - category_name benzersizdir (aynı kategori iki kez tanımlanamaz)
   --------------------------------------------------------------------- */
CREATE TABLE categories (
    category_id   INT IDENTITY(1,1) NOT NULL,
    category_name NVARCHAR(100)     NOT NULL,
    description   NVARCHAR(250)     NULL,

    CONSTRAINT pk_categories               PRIMARY KEY (category_id),
    CONSTRAINT uq_categories_category_name UNIQUE (category_name)
);
GO

/* ---------------------------------------------------------------------
   4) assets - BT envanterindeki varlıklar
   - serial_no benzersizdir (aynı cihaz/lisans iki kez kaydedilemez)
   - status yalnızca şu değerleri alabilir (ekranda Türkçe karşılığı):
       in_stock : Depoda   - zimmetlenebilir
       assigned : Zimmetli - bir kullanıcıda
       faulty   : Arızalı
       retired  : Hurda    - kullanım dışı
   --------------------------------------------------------------------- */
CREATE TABLE assets (
    asset_id      INT IDENTITY(1,1) NOT NULL,
    category_id   INT               NOT NULL,
    asset_name    NVARCHAR(150)     NOT NULL,
    brand         NVARCHAR(100)     NULL,
    model         NVARCHAR(100)     NULL,
    serial_no     NVARCHAR(100)     NOT NULL,
    status        NVARCHAR(20)      NOT NULL CONSTRAINT df_assets_status DEFAULT ('in_stock'),
    purchase_date DATE              NULL,
    description   NVARCHAR(250)     NULL,

    CONSTRAINT pk_assets            PRIMARY KEY (asset_id),
    CONSTRAINT uq_assets_serial_no  UNIQUE (serial_no),
    CONSTRAINT fk_assets_categories FOREIGN KEY (category_id) REFERENCES categories (category_id),
    CONSTRAINT ck_assets_status     CHECK (status IN ('in_stock', 'assigned', 'faulty', 'retired'))
);
GO

/* ---------------------------------------------------------------------
   5) assignments - Zimmet kayıtları (hangi varlık, kime, ne zaman)
   - returned_at NULL ise zimmet hâlâ aktiftir
   - İade edilince returned_at ve return_note doldurulur;
     returned_at dolu satırlar zimmet geçmişidir (ayrı geçmiş tablosu yok)
   --------------------------------------------------------------------- */
CREATE TABLE assignments (
    assignment_id   INT IDENTITY(1,1) NOT NULL,
    asset_id        INT               NOT NULL,
    user_id         INT               NOT NULL,
    assigned_at     DATETIME2         NOT NULL CONSTRAINT df_assignments_assigned_at DEFAULT (SYSDATETIME()),
    returned_at     DATETIME2         NULL,
    assignment_note NVARCHAR(250)     NULL,
    return_note     NVARCHAR(250)     NULL,

    CONSTRAINT pk_assignments        PRIMARY KEY (assignment_id),
    CONSTRAINT fk_assignments_assets FOREIGN KEY (asset_id) REFERENCES assets (asset_id),
    CONSTRAINT fk_assignments_users  FOREIGN KEY (user_id)  REFERENCES users (user_id)
);
GO

/* ---------------------------------------------------------------------
   ux_assignments_active_asset - Bir varlığın aynı anda tek aktif zimmeti olabilir
   - Filtreli (filtered) UNIQUE index: sadece returned_at IS NULL olan satırlara uygulanır
   - Aynı asset_id için ikinci bir aktif zimmet eklenmek istenirse veritabanı hata verir
   - İade edilmiş satırlar (returned_at dolu) filtre dışında kalır; böylece bir varlığın
     zimmet geçmişinde birden fazla kayıt olabilir
   - Uygulama tarafındaki kontrolü tamamlar: iki kullanıcı aynı anda aynı varlığı
     zimmetlemeye çalışsa bile ikinci kayıt veritabanı tarafından engellenir
   --------------------------------------------------------------------- */
CREATE UNIQUE INDEX ux_assignments_active_asset
    ON assignments (asset_id)
    WHERE returned_at IS NULL;
GO

/* =====================================================================
   SEED - Örnek veriler
   Boş veritabanında IDENTITY değerleri 1'den başladığı için
   aşağıdaki FK değerleri (role_id, category_id) sabit verilmiştir.
   ===================================================================== */

-- Roller: role_id=1 Admin, role_id=2 User
INSERT INTO roles (role_name) VALUES
    (N'Admin'),
    (N'User');
GO

-- Admin kullanıcı
-- ÖNEMLİ: Aşağıdaki '<BCRYPT_HASH>' yerine admin parolasının BCrypt hash'ini yapıştırın.
--         Hash "$2a$11$..." veya "$2b$11$..." ile başlayan 60 karakterlik bir metindir.
INSERT INTO users (full_name, email, password_hash, role_id, department) VALUES
    (N'Sistem Yöneticisi', N'admin@zimmettakip.local', N'$2a$11$RJSGSuKJXSur5RydFP82V.azCM9b4ChtxtZO33q0TFik7op2NhUsq', 1, N'Bilgi Teknolojileri');
GO

-- Kategoriler: category_id=1 Laptop, 2 Monitör, 3 Yazılım Lisansı, 4 Aksesuar
INSERT INTO categories (category_name, description) VALUES
    (N'Laptop',          N'Dizüstü bilgisayarlar'),
    (N'Monitör',         N'Harici ekranlar'),
    (N'Yazılım Lisansı', N'Kullanıcıya atanan yazılım lisansları'),
    (N'Aksesuar',        N'Klavye, mouse, kulaklık, dock gibi çevre birimleri');
GO

-- 10 örnek varlık (farklı kategori ve durumlarda)
-- Not: Zimmet kaydı olmadığı için hiçbir varlık 'assigned' yapılmadı;
--      'assigned' durumu uygulamadan zimmet atanınca oluşacak.
INSERT INTO assets (category_id, asset_name, brand, model, serial_no, status, purchase_date, description) VALUES
    (1, N'Dell Latitude 5540',      N'Dell',      N'Latitude 5540',     N'DL5540-0001',    'in_stock', '2024-02-15', NULL),
    (1, N'Lenovo ThinkPad T14',     N'Lenovo',    N'ThinkPad T14 G4',   N'LNT14-0002',     'in_stock', '2024-03-10', NULL),
    (1, N'HP EliteBook 840',        N'HP',        N'EliteBook 840 G10', N'HPEB840-0003',   'faulty',   '2023-09-01', N'Klavye arızası, serviste'),
    (2, N'Dell P2422H Monitör',     N'Dell',      N'P2422H',            N'DLP2422-0004',   'in_stock', '2024-01-08', NULL),
    (2, N'LG 27UL500 Monitör',      N'LG',        N'27UL500',           N'LG27UL-0005',    'in_stock', '2024-01-08', NULL),
    (2, N'Samsung S24 Monitör',     N'Samsung',   N'S24C310',           N'SMS24-0006',     'retired',  '2018-06-30', N'Ekranda ölü pikseller, kullanım dışı'),
    (3, N'Microsoft Office 365',    N'Microsoft', N'Business Standard', N'MSO365-0007',    'in_stock', '2024-05-01', N'Yıllık abonelik'),
    (3, N'JetBrains Rider',         N'JetBrains', N'Rider',             N'JBRIDER-0008',   'in_stock', '2024-06-12', N'Yıllık abonelik'),
    (4, N'Logitech MX Keys Klavye', N'Logitech',  N'MX Keys',           N'LGMXK-0009',     'in_stock', '2024-04-22', NULL),
    (4, N'Dell WD19 Dock',          N'Dell',      N'WD19S',             N'DLWD19-0010',    'faulty',   '2023-11-20', N'USB-C bağlantısı kopuyor');
GO
