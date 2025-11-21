using Microsoft.AspNetCore.Identity;
using claimSystem3.Models;

namespace claimSystem3.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roleNames = { "Lecturer", "Coordinator", "Manager", "HR" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create default admin user for each role
            await CreateUser(userManager, "lecturer@cmcs.com", "Lecturer", "John", "Doe", "lecturer123", "Lecturer");
            await CreateUser(userManager, "coordinator@cmcs.com", "Coordinator", "Jane", "Smith", "coordinator123", "Coordinator");
            await CreateUser(userManager, "manager@cmcs.com", "Manager", "Mike", "Johnson", "manager123", "Manager");
            await CreateUser(userManager, "hr@cmcs.com", "HR", "Sarah", "Wilson", "hr123", "HR");
        }

        private static async Task CreateUser(UserManager<ApplicationUser> userManager,
            string email, string userName, string firstName, string lastName, string password, string role)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = userName,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Role = role
                };

                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }
        }
    }
}