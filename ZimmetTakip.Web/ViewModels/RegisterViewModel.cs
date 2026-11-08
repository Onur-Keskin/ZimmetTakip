using System.ComponentModel.DataAnnotations;

namespace ZimmetTakip.Web.ViewModels;

// Kayıt formunun verilerini taşıyan model.
// Alan uzunlukları users tablosundaki kolon uzunluklarıyla aynı tutulmuştur.
public class RegisterViewModel
{
    // Ad soyad (users.full_name - en fazla 100 karakter)
    [Required(ErrorMessage = "Ad Soyad alanı zorunludur.")]
    [StringLength(100, ErrorMessage = "Ad Soyad en fazla 100 karakter olabilir.")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    // E-posta (users.email - en fazla 150 karakter, benzersiz olmalı)
    [Required(ErrorMessage = "E-posta alanı zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [StringLength(150, ErrorMessage = "E-posta en fazla 150 karakter olabilir.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    // Departman (users.department - isteğe bağlı)
    [StringLength(100, ErrorMessage = "Departman en fazla 100 karakter olabilir.")]
    [Display(Name = "Departman")]
    public string? Department { get; set; }

    // Parola (en az 6 karakter; veritabanına BCrypt hash'i kaydedilir)
    [Required(ErrorMessage = "Parola alanı zorunludur.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Parola en az 6 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parola")]
    public string Password { get; set; } = string.Empty;

    // Parola tekrarı (Password alanıyla aynı olmalı)
    [Required(ErrorMessage = "Parola tekrarı zorunludur.")]
    [Compare(nameof(Password), ErrorMessage = "Parolalar eşleşmiyor.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parola (Tekrar)")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
