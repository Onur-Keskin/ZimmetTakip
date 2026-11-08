using ZimmetTakip.Web.Models;

namespace ZimmetTakip.Web.ViewModels;

// Zimmet listesi sayfalarında (Zimmetler ve Zimmetlerim) kullanılan model.
// Aktif zimmetler ve geçmiş ayrı listelerde tutulur, ekranda ayrı sekmelerde gösterilir.
public class AssignmentListViewModel
{
    // Aktif zimmetler: returned_at alanı boş olanlar
    public List<Assignment> ActiveAssignments { get; set; } = new List<Assignment>();

    // Zimmet geçmişi: returned_at alanı dolu olanlar (iade edilmiş zimmetler)
    public List<Assignment> HistoryAssignments { get; set; } = new List<Assignment>();
}
