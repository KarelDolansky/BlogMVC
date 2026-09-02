using System.ComponentModel.DataAnnotations;

namespace BlogMVC.Dto;

/// <summary>DTO for changing a user's role via PUT api/users/{id}/role.</summary>
public class UpdateUserRoleDto
{
    /// <summary>The role to assign. Must be one of <see cref="Data.Roles.All" />; replaces any role the user currently holds.</summary>
    [Required]
    public required string Role { get; set; }
}