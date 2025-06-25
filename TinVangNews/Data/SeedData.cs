// Data/SeedData.cs
using Microsoft.AspNetCore.Identity;

namespace TinVangNews.Data
{
    public static class SeedData
    {
        // Phương thức khởi tạo bất đồng bộ để seed dữ liệu
        public static async Task Initialize(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // --- Seed Roles ---
            string[] roleNames = { "Admin", "User" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    // Tạo role nếu nó chưa tồn tại
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // --- Seed Admin User ---
            // Lấy thông tin admin từ file appsettings.json
            var adminEmail = configuration["AdminUser:Email"];
            var adminPassword = configuration["AdminUser:Password"];

            if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
            {
                Console.WriteLine("Admin user credentials not found in appsettings.json. Skipping admin seed.");
                return;
            }

            // Kiểm tra xem admin user đã tồn tại chưa
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                // Nếu chưa tồn tại, tạo user mới
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true // Tự động xác nhận email cho admin
                };
                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    // Gán role "Admin" cho user vừa tạo
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine("Admin user created and assigned to Admin role.");
                }
                else
                {
                    Console.WriteLine("Failed to create admin user.");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine(error.Description);
                    }
                }
            }
        }
    }
}