using BlogMVC.Data;
using BlogMVC.Dto;
using BlogMVC.Responses;
using BlogMVC.Results;
using BlogMVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogMVC.Controllers;

/// <summary>
///     Role administration API at "api/roles". Lets an administrator create roles and edit the permissions
///     each one grants. Requires <see cref="Permissions.Roles.Manage" /> on every endpoint.
/// </summary>
/// <param name="roleService">Service handling Identity role lookups, creation, permission edits, and deletion.</param>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Permissions.Roles.Manage)]
public class RolesController(IRoleService roleService) : BaseApiController
{
    /// <summary>GET api/roles – lists every role with the permissions it currently grants.</summary>
    /// <returns>200 with every role.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleResponse>>> GetRoles()
    {
        var roles = await roleService.GetRolesAsync();
        return Ok(roles.Select(RoleResponse.FromRoleSummary).ToList());
    }

    /// <summary>GET api/roles/permissions – lists every permission claim value a role can be granted.</summary>
    /// <returns>200 with the full permission catalog.</returns>
    [HttpGet("permissions")]
    public ActionResult<PermissionsResponse> GetPermissions()
    {
        return Ok(new PermissionsResponse { Permissions = Permissions.All });
    }

    /// <summary>GET api/roles/{name} – looks up a single role by name.</summary>
    /// <param name="name">Name of the role to look up.</param>
    /// <returns>200 with the <see cref="RoleResponse" />; 404 if no such role exists.</returns>
    [HttpGet("{name}", Name = "GetRole")]
    public async Task<ActionResult<RoleResponse>> GetRole(string name)
    {
        var role = await roleService.GetRoleAsync(name);
        return role == null ? NotFound() : Ok(RoleResponse.FromRoleSummary(role));
    }

    /// <summary>POST api/roles – creates a new role with an initial permission set.</summary>
    /// <param name="dto">The role's name and initial permissions.</param>
    /// <returns>
    ///     201 with the created <see cref="RoleResponse" />; 409 if a role with that name already exists; 400 if
    ///     any requested permission isn't recognized.
    /// </returns>
    [HttpPost]
    public async Task<ActionResult<RoleResponse>> CreateRole(CreateRoleDto dto)
    {
        var result = await roleService.CreateRoleAsync(dto.Name, dto.Permissions);

        if (!result.Succeeded)
            return result.FailureReason == CreateRoleFailureReason.DuplicateName
                ? Conflict($"A role named '{dto.Name}' already exists.")
                : BadRequest("One or more requested permissions are not recognized.");

        var response = RoleResponse.FromRoleSummary(result.Role!);
        return CreatedAtRoute("GetRole", new { name = response.Name }, response);
    }

    /// <summary>PUT api/roles/{name}/permissions – replaces a role's entire permission set.</summary>
    /// <param name="name">Name of the role to update.</param>
    /// <param name="dto">The permissions the role should grant, replacing whatever it currently grants.</param>
    /// <returns>
    ///     200 with the updated <see cref="RoleResponse" />; 404 if no such role exists; 400 if any requested
    ///     permission isn't recognized.
    /// </returns>
    [HttpPut("{name}/permissions")]
    public async Task<ActionResult<RoleResponse>> UpdateRolePermissions(string name, UpdateRolePermissionsDto dto)
    {
        var result = await roleService.UpdateRolePermissionsAsync(name, dto.Permissions);

        if (!result.Succeeded)
            return result.FailureReason == UpdateRolePermissionsFailureReason.RoleNotFound
                ? NotFound()
                : BadRequest("One or more requested permissions are not recognized.");

        return Ok(RoleResponse.FromRoleSummary(result.Role!));
    }

    /// <summary>DELETE api/roles/{name} – deletes a role.</summary>
    /// <param name="name">Name of the role to delete.</param>
    /// <returns>
    ///     204 on success; 404 if no such role exists; 409 if at least one user still holds the role.
    /// </returns>
    [HttpDelete("{name}")]
    public async Task<IActionResult> DeleteRole(string name)
    {
        var result = await roleService.DeleteRoleAsync(name);

        if (!result.Succeeded)
            return result.FailureReason == DeleteRoleFailureReason.RoleNotFound
                ? NotFound()
                : Conflict($"Role '{name}' is still assigned to one or more users.");

        return NoContent();
    }
}