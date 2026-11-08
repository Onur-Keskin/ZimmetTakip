using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ZimmetTakip.Web.ViewModels;

// Yeni zimmet atama formunda kullanılan model.
public class AssignmentCreateViewModel
{
    // Zimmet alacak kullanıcı (int? kullanıldı: boş seçim "zorunlu" hatası verebilsin diye)
    [Required(ErrorMessage = "Kullanıcı seçiniz.")]
    [Display(Name = "Kullanıcı")]
    public int? UserId { get; set; }

    // Kategori: varlık listesini daraltmak için kullanılır (seçilince varlıklar AJAX ile yüklenir).
    // Veritabanına kaydedilmez; zorunlu değildir.
    [Display(Name = "Kategori")]
    public int? CategoryId { get; set; }

    // Zimmetlenecek varlık
    [Required(ErrorMessage = "Varlık seçiniz.")]
    [Display(Name = "Varlık")]
    public int? AssetId { get; set; }

    // Zimmet notu (assignments.assignment_note - isteğe bağlı)
    [StringLength(250, ErrorMessage = "Zimmet notu en fazla 250 karakter olabilir.")]
    [Display(Name = "Zimmet Notu")]
    public string? AssignmentNote { get; set; }

    // Açılır liste seçenekleri: sadece ekranda gösterim içindir, formdan gelmez ve doğrulanmaz
    [ValidateNever]
    public List<SelectListItem> UserOptions { get; set; } = new List<SelectListItem>();

    [ValidateNever]
    public List<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();

    [ValidateNever]
    public List<SelectListItem> AssetOptions { get; set; } = new List<SelectListItem>();
}
