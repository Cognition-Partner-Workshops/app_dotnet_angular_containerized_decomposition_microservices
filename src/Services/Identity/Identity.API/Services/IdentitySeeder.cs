using Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Identity.API.Services;

/// <summary>
/// Seeds a demo administrator account on startup so a token can be obtained out of the box.
/// </summary>
public static class IdentitySeeder
{
    public const string AdminEmail = "admin@quickapp.local";
    public const string AdminPassword = "Pa$$w0rd!";
    public const string AdminRole = "Administrator";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        if (!await roleManager.RoleExistsAsync(AdminRole))
            await roleManager.CreateAsync(new ApplicationRole(AdminRole) { Description = "System administrator" });

        var admin = await userManager.FindByEmailAsync(AdminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                EmailConfirmed = true,
                FullName = "QuickApp Administrator",
                JobTitle = "Administrator",
                IsEnabled = true,
            };

            var result = await userManager.CreateAsync(admin, AdminPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to seed demo admin user: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(admin, AdminRole))
            await userManager.AddToRoleAsync(admin, AdminRole);
    }
}
