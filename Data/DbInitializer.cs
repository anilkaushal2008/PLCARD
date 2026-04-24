    using Microsoft.AspNetCore.Identity;

    namespace PLCARD.Data
    {
        public static class DbInitializer
        {
            public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
            {
                var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

                // 1. Create Roles if they do not exist
                string[] roleNames = { "Admin", "Manager", "User" };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // 2. Create a default Admin user for yourself           
                var adminEmail = "admin@indushealthcare.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);

                if (adminUser == null)
                {
                    var newAdmin = new IdentityUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true
                    };

                    // Use a strong password
                    var result = await userManager.CreateAsync(newAdmin, "Admin@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newAdmin, "Admin");
                    }
                }
            }
        }
    }