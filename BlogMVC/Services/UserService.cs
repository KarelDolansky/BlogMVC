using BlogMVC.Data;
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
}