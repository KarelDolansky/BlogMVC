using System.Security.Claims;
using BlogMVC.Data;
using BlogMVC.Models;
using BlogMVC.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlogMVC.Services;

/// <summary>
///     Default <see cref="IRoleService" />: manages roles and their permission claims via
///     <see cref="RoleManager{TRole}" />, and checks role usage via <see cref="UserManager{TUser}" /> before
///     allowing deletion. Multi-step writes run inside an explicit <see cref="ApplicationDbContext" />
///     transaction, shared with the Identity stores via DI scope.
/// </summary>
/// <param name="roleManager">Identity's role store, used to create/delete roles and manage their permission claims.</param>
/// <param name="userManager">Identity's user store, used to check whether any user still holds a role before deleting it.</param>
/// <param name="dbContext">
///     The same scoped <see cref="ApplicationDbContext" /> instance backing <paramref name="roleManager" />/
///     <paramref name="userManager" />'s stores, used only to open an explicit transaction around multi-step writes.
/// </param>
public class RoleService(
    RoleManager<IdentityRole> roleManager,
    UserManager<IdentityUser> userManager,
    ApplicationDbContext dbContext) : IRoleService
{
    /// <summary>Loads every role from <c>AspNetRoles</c> (Identity exposes no async equivalent) and its permission claims.</summary>
    /// <returns>Every role as a <see cref="RoleSummary" />.</returns>
    public async Task<IReadOnlyList<RoleSummary>> GetRolesAsync()
    {
        var roles = roleManager.Roles.ToList();
        var summaries = new List<RoleSummary>(roles.Count);

        foreach (var role in roles)
            summaries.Add(await ToSummaryAsync(role));

        return summaries;
    }

    /// <summary>Looks up the role by name (<c>RoleManager.FindByNameAsync</c>) and maps it to a <see cref="RoleSummary" />.</summary>
    /// <param name="name">Name of the role to look up.</param>
    /// <returns>The role, or <c>null</c> if no role with that name exists.</returns>
    public async Task<RoleSummary?> GetRoleAsync(string name)
    {
        var role = await roleManager.FindByNameAsync(name);
        return role == null ? null : await ToSummaryAsync(role);
    }

    /// <summary>
    ///     Validates that <paramref name="name" /> (trimmed) isn't already taken and every entry in
    ///     <paramref name="permissions" /> is a known permission value, then — inside one transaction — creates
    ///     the role (<c>RoleManager.CreateAsync</c>) and grants it each permission as a claim (<c>AddClaimAsync</c>).
    /// </summary>
    /// <param name="name">Name of the new role. Leading/trailing whitespace is trimmed before use.</param>
    /// <param name="permissions">Permission claim values to grant it.</param>
    /// <returns>
    ///     <see cref="CreateRoleResult.Success" /> carrying the created role; a failure carrying
    ///     <see cref="CreateRoleFailureReason.DuplicateName" /> if the name is already taken, or
    ///     <see cref="CreateRoleFailureReason.InvalidPermission" /> if any permission isn't recognized.
    /// </returns>
    public async Task<CreateRoleResult> CreateRoleAsync(string name, IReadOnlyCollection<string> permissions)
    {
        name = name.Trim();

        if (await roleManager.RoleExistsAsync(name))
            return CreateRoleResult.Failure(CreateRoleFailureReason.DuplicateName);

        if (permissions.Any(p => !Permissions.All.Contains(p)))
            return CreateRoleResult.Failure(CreateRoleFailureReason.InvalidPermission);

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var role = new IdentityRole(name);
            var createResult = await roleManager.CreateAsync(role);
            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return CreateRoleResult.Failure(CreateRoleFailureReason.DuplicateName);
            }

            foreach (var permission in permissions)
                await roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission));

            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            // Another request created a role with the same normalized name between our check above and
            // CreateAsync (or AddClaimAsync failed for an unrelated DB-level reason) — the transaction's
            // rollback (via disposal) discards the partial insert either way.
            return CreateRoleResult.Failure(CreateRoleFailureReason.DuplicateName);
        }

        return CreateRoleResult.Success(new RoleSummary { Name = name, Permissions = permissions });
    }

    /// <summary>
    ///     Looks up the role by name, validates every entry in <paramref name="permissions" />, then — inside
    ///     one transaction — replaces its permission claims wholesale: removes every existing
    ///     <see cref="Permissions.ClaimType" /> claim (<c>GetClaimsAsync</c>/<c>RemoveClaimAsync</c>) and adds
    ///     the new set (<c>AddClaimAsync</c>).
    /// </summary>
    /// <param name="name">Name of the role to update.</param>
    /// <param name="permissions">Permission claim values the role should grant.</param>
    /// <returns>
    ///     <see cref="UpdateRolePermissionsResult.Success" /> carrying the role's new permissions; a failure
    ///     carrying <see cref="UpdateRolePermissionsFailureReason.RoleNotFound" /> if no such role exists, or
    ///     <see cref="UpdateRolePermissionsFailureReason.InvalidPermission" /> if any permission isn't recognized.
    /// </returns>
    public async Task<UpdateRolePermissionsResult> UpdateRolePermissionsAsync(string name,
        IReadOnlyCollection<string> permissions)
    {
        var role = await roleManager.FindByNameAsync(name);
        if (role == null)
            return UpdateRolePermissionsResult.Failure(UpdateRolePermissionsFailureReason.RoleNotFound);

        if (permissions.Any(p => !Permissions.All.Contains(p)))
            return UpdateRolePermissionsResult.Failure(UpdateRolePermissionsFailureReason.InvalidPermission);

        // Makes the remove+add sequence atomic: a failure partway through rolls back to the role's previous
        // permission set instead of leaving a mix of old and new claims.
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var existingClaims = await roleManager.GetClaimsAsync(role);
        foreach (var claim in existingClaims.Where(c => c.Type == Permissions.ClaimType))
            await roleManager.RemoveClaimAsync(role, claim);

        foreach (var permission in permissions)
            await roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission));

        await transaction.CommitAsync();

        return UpdateRolePermissionsResult.Success(new RoleSummary { Name = name, Permissions = permissions });
    }

    /// <summary>
    ///     Looks up the role by name, refuses to delete it while any user still holds it
    ///     (<c>UserManager.GetUsersInRoleAsync</c>), otherwise deletes it (<c>RoleManager.DeleteAsync</c>).
    /// </summary>
    /// <param name="name">Name of the role to delete.</param>
    /// <returns>
    ///     <see cref="DeleteRoleResult.Success" /> on success; a failure carrying
    ///     <see cref="DeleteRoleFailureReason.RoleNotFound" /> if no such role exists, or
    ///     <see cref="DeleteRoleFailureReason.RoleInUse" /> if at least one user still holds it.
    /// </returns>
    public async Task<DeleteRoleResult> DeleteRoleAsync(string name)
    {
        var role = await roleManager.FindByNameAsync(name);
        if (role == null)
            return DeleteRoleResult.Failure(DeleteRoleFailureReason.RoleNotFound);

        // Runs the "still in use" check and the delete in one transaction, narrowing (but — being a
        // check-then-act pair — not perfectly closing) the race against a concurrent UserService.UpdateUserRoleAsync
        // assigning this role to a user at the same time.
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var usersInRole = await userManager.GetUsersInRoleAsync(name);
        if (usersInRole.Count > 0)
        {
            await transaction.RollbackAsync();
            return DeleteRoleResult.Failure(DeleteRoleFailureReason.RoleInUse);
        }

        await roleManager.DeleteAsync(role);
        await transaction.CommitAsync();

        return DeleteRoleResult.Success();
    }

    /// <summary>Maps an <see cref="IdentityRole" /> to a <see cref="RoleSummary" /> by reading its permission claims.</summary>
    /// <param name="role">The Identity role to summarize.</param>
    /// <returns>The role's name and the <see cref="Permissions.ClaimType" /> claim values it currently holds.</returns>
    private async Task<RoleSummary> ToSummaryAsync(IdentityRole role)
    {
        var claims = await roleManager.GetClaimsAsync(role);
        var permissions = claims.Where(c => c.Type == Permissions.ClaimType).Select(c => c.Value).ToList();
        return new RoleSummary { Name = role.Name!, Permissions = permissions };
    }
}