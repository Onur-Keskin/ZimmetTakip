using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ZimmetTakip.Web.ViewModels;

// Zimmet iade formunda kullanılan model.
// Formdan sadece iade notu gelir; diğer alanlar bilgi amaçlı gösterilir.
public class AssignmentReturnViewModel
{
    public int AssignmentId { get; set; }

    // Sadece gösterim alanları: formdan gelmez, her seferinde veritabanından doldurulur
    [ValidateNever]
    public string AssetName { get; set; } = string.Empty;

    [ValidateNever]
    public string SerialNo { get; set; } = string.Empty;

    [ValidateNever]
    public string UserFullName { get; set; } = string.Empty;

    [ValidateNever]
    public DateTime AssignedAt { get; set; }

    [ValidateNever]
    public string? AssignmentNote { get; set; }

    // İade notu (assignments.return_note - isteğe bağlı)
    [StringLength(250, ErrorMessage = "İade notu en fazla 250 karakter olabilir.")]
    [Display(Name = "İade Notu")]
    public string? ReturnNote { get; set; }
}
