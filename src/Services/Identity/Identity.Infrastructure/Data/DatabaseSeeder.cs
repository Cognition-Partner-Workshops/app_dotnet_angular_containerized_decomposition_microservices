using Identity.Domain.Authorization;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using Identity.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Data;

public class DatabaseSeeder(IdentityDbContext dbContext, ILogger<DatabaseSeeder> logger,
    IUserAccountService userAccountService, IUserRoleService userRoleService) : IDatabaseSeeder
{
    private const int MigrationAttempts = 10;
    private static readonly TimeSpan MigrationRetryDelay = TimeSpan.FromSeconds(5);

    public async Task SeedAsync()
    {
        if (dbContext.Database.IsNpgsql())
            await MigrateWithRetryAsync();
        else
            await dbContext.Database.EnsureCreatedAsync();

        await SeedDefaultUsersAsync();
    }

    private async Task MigrateWithRetryAsync()
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await dbContext.Database.MigrateAsync();
                return;
            }
            catch (Exception ex) when (attempt < MigrationAttempts)
            {
                logger.LogWarning(ex, "Database not reachable (attempt {Attempt} of {Attempts}), retrying in {Delay}",
                    attempt, MigrationAttempts, MigrationRetryDelay);

                await Task.Delay(MigrationRetryDelay);
            }
        }
    }

    /************ DEFAULT USERS **************/

    private async Task SeedDefaultUsersAsync()
    {
        if (!await dbContext.Users.AnyAsync())
        {
            logger.LogInformation("Generating inbuilt accounts");

            const string adminRoleName = "administrator";
            const string userRoleName = "user";

            await EnsureRoleAsync(adminRoleName, "Default administrator",
                ApplicationPermissions.GetAllPermissionValues());

            await EnsureRoleAsync(userRoleName, "Default user", []);

            await CreateUserAsync("admin",
                                  "tempP@ss123",
                                  "Inbuilt Administrator",
                                  "admin@ebenmonney.com",
                                  "+1 (123) 000-0000",
                                  [adminRoleName]);

            await CreateUserAsync("user",
                                  "tempP@ss123",
                                  "Inbuilt Standard User",
                                  "user@ebenmonney.com",
                                  "+1 (123) 000-0001",
                                  [userRoleName]);

            logger.LogInformation("Inbuilt account generation completed");
        }
    }

    private async Task EnsureRoleAsync(string roleName, string description, string[] claims)
    {
        if (await userRoleService.GetRoleByNameAsync(roleName) == null)
        {
            logger.LogInformation("Generating default role: {roleName}", roleName);

            var applicationRole = new ApplicationRole(roleName, description);

            var result = await userRoleService.CreateRoleAsync(applicationRole, claims);

            if (!result.Succeeded)
            {
                throw new UserRoleException($"Seeding \"{description}\" role failed. Errors: " +
                    $"{string.Join(Environment.NewLine, result.Errors)}");
            }
        }
    }

    private async Task<ApplicationUser> CreateUserAsync(
        string userName, string password, string fullName, string email, string phoneNumber, string[] roles)
    {
        logger.LogInformation("Generating default user: {userName}", userName);

        var applicationUser = new ApplicationUser
        {
            UserName = userName,
            FullName = fullName,
            Email = email,
            PhoneNumber = phoneNumber,
            EmailConfirmed = true,
            IsEnabled = true
        };

        var result = await userAccountService.CreateUserAsync(applicationUser, roles, password);

        if (!result.Succeeded)
        {
            throw new UserAccountException($"Seeding \"{userName}\" user failed. Errors: " +
                $"{string.Join(Environment.NewLine, result.Errors)}");
        }

        return applicationUser;
    }
}
