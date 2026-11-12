namespace ZimmetTakip.Web.ViewModels;

// Ana sayfadaki özet panelinin sayıları.
// Admin için genel envanter sayıları, User için sadece kendi aktif zimmet sayısı doldurulur.
public class HomeIndexViewModel
{
    // Giriş yapan kullanıcı Admin mi? (view hangi kartları göstereceğine buna göre karar verir)
    public bool IsAdmin { get; set; }

    // --- Admin kartları ---

    // Toplam varlık sayısı
    public int TotalAssetCount { get; set; }

    // Durumu "Depoda" (in_stock) olan varlık sayısı
    public int InStockCount { get; set; }

    // Durumu "Zimmetli" (assigned) olan varlık sayısı
    public int AssignedCount { get; set; }

    // Durumu "Arızalı" (faulty) olan varlık sayısı
    public int FaultyCount { get; set; }

    // Aktif zimmet sayısı (returned_at boş olan zimmet kayıtları)
    public int ActiveAssignmentCount { get; set; }

    // --- User kartı ---

    // Giriş yapan kullanıcının üzerindeki aktif zimmet sayısı
    public int MyActiveAssignmentCount { get; set; }
}
