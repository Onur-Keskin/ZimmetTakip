# ZimmetTakip

## Proje amacı
BT envanter ve zimmet takip sistemi. BLM5227 dönem projesi.
Şirketteki BT varlıklarının (bilgisayar, monitör, telefon vb.) envanterini tutmak ve hangi varlığın hangi kullanıcıya zimmetlendiğini takip etmek.

## Yapı
- Solution: `ZimmetTakip.slnx`
- Proje: `ZimmetTakip.Web` — ASP.NET Core 10 MVC
- UI: Bootstrap 5 + jQuery (`wwwroot/lib` altında yerel kopyalar, `Views/Shared/_Layout.cshtml` içinde yüklenir)

## Mimari kurallar
- Klasik MVC: Controller → View. Ek katman/pattern (repository, CQRS, servis katmanı vb.) gerekmedikçe ekleme.
- **EF Core DATABASE-FIRST.** Migration / code-first **ASLA** kullanma (`dotnet ef migrations`, `Database.EnsureCreated` vb. yok).
- Modeller veritabanından scaffold edilir (`dotnet ef dbcontext scaffold`). Scaffold edilen `Models` klasörünü **elle değiştirme**; ek davranış gerekiyorsa partial class veya ayrı ViewModel kullan.
- Şema değişikliği gerekirse kodu değiştirmeden önce kullanıcıya **T-SQL script öner**; script kullanıcı tarafından çalıştırıldıktan sonra yeniden scaffold edilir.

## Veritabanı adlandırma
- Tablo ve kolon adları **İngilizce snake_case**: `users`, `assets`, `assignment_note` gibi.

## Kimlik doğrulama
- **Cookie authentication** kullanılır.
- **ASP.NET Identity kullanılmaz.**

## Kod stili
- **BASİT ve okunabilir** kod. Gereksiz soyutlama, ileri seviye dil hileleri ve "akıllıca" kısaltmalardan kaçın.
- Her anlamlı kod bloğunun üstüne **Türkçe açıklayıcı yorum satırı** yaz. Kod sözlü sınavda sunulup anlatılacak; yorumlar bloğun ne yaptığını ve neden yaptığını açıklamalı.

## UI dili
- Kullanıcıya görünen tüm metinler (menü, başlık, buton, mesaj, doğrulama hataları) **Türkçe**.

## Çalışma şekli
- Her fazda **SADECE istenen işi yap**. Kapsamı genişletme; istenmeyen sayfa, özellik, paket veya refactor ekleme.
