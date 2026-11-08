using System.ComponentModel.DataAnnotations;

namespace ZimmetTakip.Web.ViewModels;

// Giriş formunun verilerini taşıyan model.
// Scaffold edilen User entity'si yerine ayrı bir ViewModel kullanılır;
// böylece formda sadece gereken alanlar olur ve doğrulama kuralları burada tanımlanır.
public class LoginViewModel
{
    // Kullanıcının e-posta adresi (giriş için kullanıcı adı yerine geçer)
    [Required(ErrorMessage = "E-posta alanı zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    // Kullanıcının parolası (düz metin olarak gelir, veritabanındaki hash ile karşılaştırılır)
    [Required(ErrorMessage = "Parola alanı zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parola")]
    public string Password { get; set; } = string.Empty;
}
