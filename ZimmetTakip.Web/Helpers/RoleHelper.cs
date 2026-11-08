namespace ZimmetTakip.Web.Helpers;

// Rol adlarının (roles.role_name) ekrandaki Türkçe karşılıklarını veren yardımcı sınıf.
public static class RoleHelper
{
    // Veritabanı rol adını Türkçe metne çevirir (Admin → Yönetici, User → Kullanıcı)
    public static string GetDisplayName(string roleName)
    {
        switch (roleName)
        {
            case "Admin": return "Yönetici";
            case "User": return "Kullanıcı";
            default: return roleName;
        }
    }
}
