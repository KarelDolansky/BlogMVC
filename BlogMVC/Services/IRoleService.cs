using BlogMVC.Models;
using BlogMVC.Results;

namespace BlogMVC.Services;

/// <summary>
///     Application (business) layer for role administration — creating roles and editing the permissions each
///     one grants — sitting between <see cref="Controllers.RolesController" /> and Identity's role store.
/// </summary>
public interface IRoleService
{
    /// <summary>Lists every role together with the permissions it currently grants.</summary>
    /// <returns>All roles, in no particular order.</returns>
    Task<IReadOnlyList<RoleSummary>> GetRolesAsync();

    /// <summary>Looks up a single role by name.</summary>
    /// <param name="name">Name of the role to look up.</param>
    /// <returns>The role, or <c>null</c> if no role with that name exists.</returns>
    Task<RoleSummary?> GetRoleAsync(string name);

    /// <summary>Creates a new role with the given initial permission set.</summary>
    /// <param name="name">Name of the new role. Leading/trailing whitespace is trimmed before use. Must not already exist.</param>
    /// <param name="permissions">Permission claim values to grant it. Must each be one of <see cref="Data.Permissions.All" />.</param>
    /// <returns>A <see cref="CreateRoleResult" /> carrying the created role, or why creation failed.</returns>
    Task<CreateRoleResult> CreateRoleAsync(string name, IReadOnlyCollection<string> permissions);

    /// <summary>Replaces the given role's entire permission set.</summary>
    /// <param name="name">Name of the role to update.</param>
    /// <param name="permissions">
    ///     Permission claim values the role should grant. Must each be one of
    ///     <see cref="Data.Permissions.All" />.
    /// </param>
    /// <returns>An <see cref="UpdateRolePermissionsResult" /> carrying the role's new permissions, or why the update failed.</returns>
    Task<UpdateRolePermissionsResult> UpdateRolePermissionsAsync(string name, IReadOnlyCollection<string> permissions);

    /// <summary>Deletes a role, refusing to do so while any user still holds it.</summary>
    /// <param name="name">Name of the role to delete.</param>
    /// <returns>A <see cref="DeleteRoleResult" /> indicating success, or why deletion failed.</returns>
    Task<DeleteRoleResult> DeleteRoleAsync(string name);
}