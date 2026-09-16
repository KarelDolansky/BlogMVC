using System.ComponentModel.DataAnnotations;

namespace BlogMVC.Dto;

/// <summary>DTO for replacing a role's permission set via PUT api/roles/{name}/permissions.</summary>
public class UpdateRolePermissionsDto
{
    /// <summary>
    ///     Permission claim values (see <see cref="Data.Permissions.All" />) the role should grant, replacing
    ///     whatever it currently grants. May be an empty array to revoke every permission from the role, but
    ///     must be present (not JSON <c>null</c>).
    /// </summary>
    [Required]
    public required IReadOnlyList<string> Permissions { get; set; }
}