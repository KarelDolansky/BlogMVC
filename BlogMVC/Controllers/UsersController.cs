using BlogMVC.Data;
using BlogMVC.Dto;
using BlogMVC.Responses;
using BlogMVC.Results;
using BlogMVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogMVC.Controllers;

/// <summary>User administration API at "api/users". Lists users and handles role assignment.</summary>
/// <param name="userService">Service handling Identity user lookups and role assignments.</param>
[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : BaseApiController
{
    /// <summary>
    ///     GET api/users – lists every user with their id, username, and current role. Requires
    ///     <see cref="Permissions.Users.ManageRoles" />; feeds a frontend role-management UI.
    /// </summary>
    /// <returns>200 with every user.</returns>
    [HttpGet]
    [Authorize(Policy = Permissions.Users.ManageRoles)]
    public async Task<ActionResult<IReadOnlyList<UserSummaryResponse>>> GetUsers()
    {
        var users = await userService.GetUsersAsync();
        return Ok(users.Select(UserSummaryResponse.FromUserSummary).ToList());
    }

    /// <summary>
    ///     PUT api/users/{id}/role – replaces the target user's role. Requires
    ///     <see cref="Permissions.Users.ManageRoles" />.
    /// </summary>
    /// <param name="id">Identity id of the user to update.</param>
    /// <param name="dto">The role to assign.</param>
    /// <returns>
    ///     200 with a <see cref="UserRoleResponse" /> on success; 404 if no such user exists; 400 if the role
    ///     isn't recognized.
    /// </returns>
    [HttpPut("{id}/role")]
    [Authorize(Policy = Permissions.Users.ManageRoles)]
    public async Task<ActionResult<UserRoleResponse>> UpdateUserRole(string id, UpdateUserRoleDto dto)
    {
        var result = await userService.UpdateUserRoleAsync(id, dto.Role);

        if (!result.Succeeded)
            return result.FailureReason == UpdateUserRoleFailureReason.UserNotFound
                ? NotFound()
                : BadRequest($"'{dto.Role}' is not a recognized role.");

        return Ok(new UserRoleResponse { UserId = id, Role = dto.Role });
    }
}