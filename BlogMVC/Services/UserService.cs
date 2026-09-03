using BlogMVC.Data;
using BlogMVC.Models;
using BlogMVC.Results;
using Microsoft.AspNetCore.Identity;

namespace BlogMVC.Services;

/// <summary>Default <see cref="IUserService" />: manages Identity role assignments via <see cref="UserManager{TUser}" />.</summary>
/// <param name="userManager">Identity's user store, used to look up users and manage their role assignments.</param>
public class UserService(UserManager<IdentityUser> userManager) : IUserService
{
    /// <summary>
    ///     Looks up the user by id, validates <paramref name="role" /> against <see cref="Roles.All" />, then
    ///     removes every role the user currently holds (<c>GetRolesAsync</c>/<c>RemoveFromRolesAsync</c>) and
    ///     assigns the new one (<c>AddToRoleAsync</c>).
    /// </summary>
    /// <param name="userId">Identity id of the user to update.</param>
    /// <param name="role">The role to assign.</param>
    /// <returns>
    ///     <see cref="UpdateUserRoleResult.Success" /> on success; a failure carrying
    ///     <see cref="UpdateUserRoleFailureReason.UserNotFound" /> if no such user exists, or
    ///     <see cref="UpdateUserRoleFailureReason.InvalidRole" /> if <paramref name="role" /> isn't recognized.
    /// </returns>
    public async Task<UpdateUserRoleResult> UpdateUserRoleAsync(string userId, string role)
    {
        if (!Roles.All.Contains(role))
            return UpdateUserRoleResult.Failure(UpdateUserRoleFailureReason.InvalidRole);

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return UpdateUserRoleResult.Failure(UpdateUserRoleFailureReason.UserNotFound);

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
            await userManager.RemoveFromRolesAsync(user, currentRoles);

        await userManager.AddToRoleAsync(user, role);

        return UpdateUserRoleResult.Success();
    }

    /// <summary>
    ///     Loads every user (<c>UserManager.Users</c> — Identity exposes no async equivalent) and, for each, its
    ///     current role via <c>GetRolesAsync</c> (Identity has no single "role" column — roles are a separate
    ///     join table).
    /// </summary>
    /// <returns>Every user as a <see cref="UserSummary" />, first-assigned role only (there should be at most one).</returns>
    public async Task<IReadOnlyList<UserSummary>> GetUsersAsync()
    {
        var users = userManager.Users.ToList();
        var summaries = new List<UserSummary>(users.Count);

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            summaries.Add(new UserSummary { Id = user.Id, UserName = user.UserName!, Role = roles.FirstOrDefault() });
        }

        return summaries;
    }
}