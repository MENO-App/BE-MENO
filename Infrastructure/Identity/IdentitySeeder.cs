using Microsoft.AspNetCore.Identity;
using System.Linq;

namespace Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        string[] roles = ["ADMIN", "KITCHEN", "STUDENT", "STAFF"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
            }
        }
    }
    public static async Task SeedInitialAdminAsync(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    string email,
    string password)
    {
        // Ensure ADMIN role exists
        if (!await roleManager.RoleExistsAsync("ADMIN"))
        {
            await roleManager.CreateAsync(new ApplicationRole { Name = "ADMIN" });
        }

        // Ensure admin user exists
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new Exception($"Failed to create initial admin user: {errors}");
            }
        }

        // Ensure the user is in ADMIN role
        if (!await userManager.IsInRoleAsync(user, "ADMIN"))
        {
            await userManager.AddToRoleAsync(user, "ADMIN");
        }
    }
}
