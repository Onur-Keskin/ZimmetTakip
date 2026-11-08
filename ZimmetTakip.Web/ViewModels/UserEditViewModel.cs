using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ZimmetTakip.Web.ViewModels;

// Admin'in kullanıcı düzenleme formu. Sadece rol ve aktiflik durumu değiştirilebilir;
// ad, e-posta ve departman bilgi amaçlı gösterilir.
public class UserEditViewModel
{
    public int UserId { get; set; }

    // Sadece gösterim alanları: formdan gelmez, her seferinde veritabanından doldurulur
    [ValidateNever]
    public string FullName { get; set; } = string.Empty;

    [ValidateNever]
    public string Email { get; set; } = string.Empty;

    [ValidateNever]
    public string? Department { get; set; }

    // Seçilen rol (roles.role_id)
    [Required(ErrorMessage = "Rol seçiniz.")]
    [Display(Name = "Rol")]
    public int? RoleId { get; set; }

    // Aktif mi? (false = pasif, kullanıcı giriş yapamaz)
    [Display(Name = "Hesap aktif")]
    public bool IsActive { get; set; }

    // Düzenlenen kullanıcı, giriş yapmış admin'in kendisi mi? (kendini pasife alamaz)
    public bool IsCurrentUser { get; set; }

    // Rol açılır listesi seçenekleri (sadece gösterim)
    [ValidateNever]
    public List<SelectListItem> RoleOptions { get; set; } = new List<SelectListItem>();
}
