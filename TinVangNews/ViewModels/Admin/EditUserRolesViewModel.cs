namespace TinVangNews.ViewModels.Admin
{
    public class EditUserRolesViewModel
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public List<RoleSelectionViewModel> Roles { get; set; }
    }
}