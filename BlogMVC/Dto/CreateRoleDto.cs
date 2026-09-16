using System.ComponentModel.DataAnnotations;

namespace BlogMVC.Dto;

/// <summary>DTO for creating a new role via POST api/roles.</summary>
public class CreateRoleDto
{
    /// <summary>Name of the new role. Must not already exist.</summary>
    [Required]
    public required string Name { get; set; }

    /// <summary>
    ///     Permission claim values (see <see cref="Data.Permissions.All" />) to grant the role. May be an empty
    ///     array — a role with no permissions yet is valid — but must be present (not JSON <c>null</c>).
    /// </summary>
    [Required]
    public required IReadOnlyList<string> Permissions { get; set; }
}