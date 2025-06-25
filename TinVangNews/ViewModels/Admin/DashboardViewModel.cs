// ViewModels/Admin/DashboardViewModel.cs
namespace TinVangNews.ViewModels.Admin
{
    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalFavoriteArticles { get; set; }
        public List<LatestUserViewModel> LatestUsers { get; set; }
    }

    public class LatestUserViewModel
    {
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; } 
    }
}