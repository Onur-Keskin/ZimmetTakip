using Microsoft.AspNetCore.Mvc.Rendering;

namespace ZimmetTakip.Web.Helpers;

// Varlık durumlarının (assets.status) tek merkezden yönetildiği yardımcı sınıf.
// Veritabanında İngilizce değerler tutulur, ekranda Türkçe karşılıkları gösterilir.
// Dönüşüm burada toplandığı için view'larda tekrar yazılmaz.
public static class StatusHelper
{
    // Veritabanındaki status değerleri (CHECK kısıtındaki değerlerle aynı)
    public const string InStock = "in_stock";
    public const string Assigned = "assigned";
    public const string Faulty = "faulty";
    public const string Retired = "retired";

    // Veritabanı değerini ekranda gösterilecek Türkçe metne çevirir
    public static string GetDisplayName(string status)
    {
        switch (status)
        {
            case InStock: return "Depoda";
            case Assigned: return "Zimmetli";
            case Faulty: return "Arızalı";
            case Retired: return "Hurda";
            default: return status;
        }
    }

    // Listelerde durumu renkli rozet (badge) ile göstermek için Bootstrap sınıfı döner
    public static string GetBadgeClass(string status)
    {
        switch (status)
        {
            case InStock: return "text-bg-success";
            case Assigned: return "text-bg-primary";
            case Faulty: return "text-bg-danger";
            case Retired: return "text-bg-secondary";
            default: return "text-bg-light";
        }
    }

    // Formda elle seçilebilen durumlar.
    // "Zimmetli" listede yoktur: bu durum yalnızca zimmet atama işlemiyle oluşur.
    public static List<SelectListItem> GetManualStatusOptions()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Value = InStock, Text = GetDisplayName(InStock) },
            new SelectListItem { Value = Faulty, Text = GetDisplayName(Faulty) },
            new SelectListItem { Value = Retired, Text = GetDisplayName(Retired) }
        };
    }

    // Formdan gelen durumun elle seçilebilen durumlardan biri olup olmadığını kontrol eder
    public static bool IsManualStatus(string? status)
    {
        return status == InStock || status == Faulty || status == Retired;
    }
}
