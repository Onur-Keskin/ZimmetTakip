using System.ComponentModel.DataAnnotations;

namespace ZimmetTakip.Web.ViewModels;

// Kategori ekleme ve düzenleme formlarında kullanılan model.
// Alan uzunlukları categories tablosundaki kolon uzunluklarıyla aynıdır.
public class CategoryFormViewModel
{
    // Düzenlemede hangi kategorinin güncelleneceğini belirtir (eklemede 0)
    public int CategoryId { get; set; }

    // Kategori adı (categories.category_name - benzersiz)
    [Required(ErrorMessage = "Kategori adı zorunludur.")]
    [StringLength(100, ErrorMessage = "Kategori adı en fazla 100 karakter olabilir.")]
    [Display(Name = "Kategori Adı")]
    public string CategoryName { get; set; } = string.Empty;

    // Açıklama (isteğe bağlı)
    [StringLength(250, ErrorMessage = "Açıklama en fazla 250 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }
}
