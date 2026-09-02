using BlogMVC.Results;

namespace BlogMVC.Services;

/// <summary>
///     Application (business) layer for user account administration, sitting between
///     <see cref="Controllers.UsersController" /> and Identity's user store.
/// </summary>
public interface IUserService
{
    /// <summary>
    ///     Replaces the given user's role with <paramref name="role" /> — removes every role the user currently
    ///     holds and assigns only the new one.
    /// </summary>
    /// <param name="userId">Identity id of the user to update.</param>
    /// <param name="role">The role to assign. Must be one of <see cref="Data.Roles.All" />.</param>
    /// <returns>An <see cref="UpdateUserRoleResult" /> indicating success, or why it failed.</returns>
    Task<UpdateUserRoleResult> UpdateUserRoleAsync(string userId, string role);
}