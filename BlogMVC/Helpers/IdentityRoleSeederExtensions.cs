using BlogMVC.Data;
using Microsoft.AspNetCore.Identity;

namespace BlogMVC.Helpers;

/// <summary>
///     Startup extension that seeds the predefined Identity roles (<see cref="Roles.All" />) into the
///     <c>AspNetRoles</c> table. Called once from <see cref="Program" /> right after the app is built.
/// </summary>
public static class IdentityRoleSeederExtensions
{
    /// <summary>Creates any predefined role that doesn't already exist yet. Safe to call on every startup.</summary>
    public static async Task SeedIdentityRolesAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var roleName in Roles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded)
                app.Logger.LogWarning("Failed to seed role '{RoleName}': {Errors}", roleName,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}