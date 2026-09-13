using BlogMVC.Data;
using Microsoft.AspNetCore.Identity;

namespace BlogMVC.Helpers;

/// <summary>Extension helpers for resolving permission claims from Identity roles via <see cref="RoleManager{TRole}" />.</summary>
public static class RoleManagerExtensions
{
    /// <summary>
    ///     Resolves the distinct union of permission claims (<see cref="Permissions.ClaimType" />) granted by the
    ///     given role names, reading each role's claims from <c>AspNetRoleClaims</c> via
    ///     <see cref="RoleManager{TRole}.GetClaimsAsync" />.
    /// </summary>
    /// <param name="roleManager">Identity's role store.</param>
    /// <param name="roleNames">Role names held by a user (e.g. from Identity role claims).</param>
    /// <returns>
    ///     Distinct permission claim values granted by any of <paramref name="roleNames" />; unknown role names
    ///     contribute nothing.
    /// </returns>
    public static async Task<IReadOnlyCollection<string>> GetPermissionsAsync(
        this RoleManager<IdentityRole> roleManager, IEnumerable<string> roleNames)
    {
        var permissions = new HashSet<string>();

        foreach (var roleName in roleNames)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null)
                continue;

            var claims = await roleManager.GetClaimsAsync(role);
            foreach (var claim in claims)
                if (claim.Type == Permissions.ClaimType)
                    permissions.Add(claim.Value);
        }

        return permissions;
    }
}