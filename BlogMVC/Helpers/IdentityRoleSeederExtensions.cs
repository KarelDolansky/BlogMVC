using System.Security.Claims;
using BlogMVC.Data;
using Microsoft.AspNetCore.Identity;

namespace BlogMVC.Helpers;

/// <summary>
///     Startup extension that seeds the predefined Identity roles (<see cref="Roles.All" />) into the
///     <c>AspNetRoles</c> table, together with their default permission claims. Called once from
///     <see cref="Program" /> right after the app is built.
/// </summary>
public static class IdentityRoleSeederExtensions
{
    /// <summary>
    ///     Default permission claims granted to each predefined role the first time it's created. Only consulted
    ///     for roles that don't already exist — an existing role's permissions may have since been edited by an
    ///     administrator via <see cref="Services.IRoleService" /> and must not be overwritten on every startup.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> DefaultPermissions =
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            [Roles.Administrator] =
            [
                Permissions.Posts.Create, Permissions.Posts.CreateBulk,
                Permissions.Posts.EditAny, Permissions.Posts.DeleteAny,
                Permissions.Users.ManageRoles, Permissions.Roles.Manage
            ],
            [Roles.Editor] =
            [
                Permissions.Posts.Create, Permissions.Posts.CreateBulk,
                Permissions.Posts.EditOwn, Permissions.Posts.DeleteOwn
            ],
            [Roles.Author] = [Permissions.Posts.Create, Permissions.Posts.EditOwn, Permissions.Posts.DeleteOwn],
            [Roles.Commentator] = []
        };

    /// <summary>
    ///     Creates any predefined role that doesn't already exist yet, granting it its default permission claims.
    ///     Safe to call on every startup — existing roles (and any permissions an administrator has since edited
    ///     on them) are left untouched.
    /// </summary>
    public static async Task SeedIdentityRolesAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var roleName in Roles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            var role = new IdentityRole(roleName);
            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                app.Logger.LogWarning("Failed to seed role '{RoleName}': {Errors}", roleName,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                continue;
            }

            foreach (var permission in DefaultPermissions.GetValueOrDefault(roleName, []))
                await roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission));
        }
    }
}