using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ZimmetTakip.Web.ViewModels;

// Varlık ekleme ve düzenleme formlarında kullanılan model.
// Alan uzunlukları assets tablosundaki kolon uzunluklarıyla aynıdır.
public class AssetFormViewModel
{
    // Düzenlemede hangi varlığın güncelleneceğini belirtir (eklemede 0)
    public int AssetId { get; set; }

    // Seçilen kategori (int? kullanıldı: boş seçim "zorunlu" hatası verebilsin diye)
    [Required(ErrorMessage = "Kategori seçiniz.")]
    [Display(Name = "Kategori")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Varlık adı zorunludur.")]
    [StringLength(150, ErrorMessage = "Varlık adı en fazla 150 karakter olabilir.")]
    [Display(Name = "Varlık Adı")]
    public string AssetName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Marka en fazla 100 karakter olabilir.")]
    [Display(Name = "Marka")]
    public string? Brand { get; set; }

    // assets.model kolonu. Razor'daki "Model" anahtar kelimesiyle karışmasın diye ModelName adı verildi.
    [StringLength(100, ErrorMessage = "Model en fazla 100 karakter olabilir.")]
    [Display(Name = "Model")]
    public string? ModelName { get; set; }

    // Seri numarası (assets.serial_no - benzersiz)
    [Required(ErrorMessage = "Seri numarası zorunludur.")]
    [StringLength(100, ErrorMessage = "Seri numarası en fazla 100 karakter olabilir.")]
    [Display(Name = "Seri No")]
    public string SerialNo { get; set; } = string.Empty;

    // Durum: veritabanı değeri tutulur (in_stock, faulty, retired ...)
    [Required(ErrorMessage = "Durum seçiniz.")]
    [Display(Name = "Durum")]
    public string Status { get; set; } = string.Empty;

    // Alım tarihi: formda tarih seçici (input type="date") olarak gösterilir
    [DataType(DataType.Date)]
    [Display(Name = "Alım Tarihi")]
    public DateTime? PurchaseDate { get; set; }

    [StringLength(250, ErrorMessage = "Açıklama en fazla 250 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    // Varlık şu an zimmetliyse durumu formdan değiştirilemez (iade işlemiyle değişir)
    public bool IsAssigned { get; set; }

    // Açılır liste seçenekleri: sadece ekranda gösterim içindir, formdan gelmez ve doğrulanmaz
    [ValidateNever]
    public List<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();

    [ValidateNever]
    public List<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
}
