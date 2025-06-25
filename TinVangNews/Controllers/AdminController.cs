// Controllers/AdminController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;      // *** THÊM USING CHO SIGNALR ***
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TinVangNews.Data;
using TinVangNews.Hubs;                 // *** THÊM USING CHO HUBS ***
using TinVangNews.ViewModels.Admin;

namespace TinVangNews.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<DashboardHub> _dashboardHubContext; // *** KHAI BÁO HUB CONTEXT ***

        // Cập nhật constructor để inject HubContext
        public AdminController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IHubContext<DashboardHub> dashboardHubContext) // *** INJECT HUB CONTEXT ***
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _dashboardHubContext = dashboardHubContext; // *** GÁN HUB CONTEXT ***
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Bảng điều khiển Quản trị";
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var latestUsers = await _userManager.Users
                                            .OrderByDescending(u => u.Id)
                                            .Take(5)
                                            .Select(u => new LatestUserViewModel { Email = u.Email })
                                            .ToListAsync();

            var dashboardViewModel = new DashboardViewModel
            {
                TotalUsers = await _userManager.Users.CountAsync(),
                TotalAdmins = admins.Count,
                TotalFavoriteArticles = await _context.FavoriteArticles.CountAsync(),
                LatestUsers = latestUsers
            };
            return View(dashboardViewModel);
        }

        public async Task<IActionResult> ManageUsers()
        {
            ViewData["Title"] = "Quản lý Người dùng";
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<UserViewModel>();

            foreach (var user in users)
            {
                var viewModel = new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Roles = await _userManager.GetRolesAsync(user)
                };
                userViewModels.Add(viewModel);
            }
            return View(userViewModels);
        }

        public async Task<IActionResult> EditUserRoles(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            ViewData["Title"] = $"Chỉnh sửa vai trò cho: {user.Email}";
            var allRoles = await _roleManager.Roles.ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new EditUserRolesViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                Roles = allRoles.Select(role => new RoleSelectionViewModel
                {
                    RoleName = role.Name,
                    IsSelected = userRoles.Contains(role.Name)
                }).ToList()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUserRoles(EditUserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var roleSelection in model.Roles)
            {
                if (roleSelection.IsSelected && !userRoles.Contains(roleSelection.RoleName))
                {
                    await _userManager.AddToRoleAsync(user, roleSelection.RoleName);
                }
                else if (!roleSelection.IsSelected && userRoles.Contains(roleSelection.RoleName))
                {
                    await _userManager.RemoveFromRoleAsync(user, roleSelection.RoleName);
                }
            }

            // *** GỬI TÍN HIỆU REAL-TIME ĐẾN ADMIN DASHBOARD ***
            await _dashboardHubContext.Clients.Group("Admins").SendAsync("UpdateDashboardStats");

            TempData["SuccessMessage"] = $"Đã cập nhật vai trò cho người dùng {user.Email} thành công.";
            return RedirectToAction(nameof(ManageUsers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (id == currentUserId)
            {
                TempData["ErrorMessage"] = "Bạn không thể xóa tài khoản của chính mình.";
                return RedirectToAction(nameof(ManageUsers));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                // *** GỬI TÍN HIỆU REAL-TIME ĐẾN ADMIN DASHBOARD ***
                await _dashboardHubContext.Clients.Group("Admins").SendAsync("UpdateDashboardStats");
                TempData["SuccessMessage"] = $"Đã xóa người dùng {user.Email} thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Lỗi khi xóa người dùng {user.Email}.";
            }
            return RedirectToAction(nameof(ManageUsers));
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var stats = new
            {
                totalUsers = await _userManager.Users.CountAsync(),
                totalAdmins = admins.Count,
                totalFavoriteArticles = await _context.FavoriteArticles.CountAsync()
            };
            return Json(stats);
        }
    }
}