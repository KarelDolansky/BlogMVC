using BlogMVC.Models;

namespace BlogMVC.Responses;

/// <summary>Response item for the role administration endpoints on <see cref="Controllers.RolesController" />.</summary>
public class RoleResponse
{
    /// <summary>The role's name.</summary>
    public required string Name { get; init; }

    /// <summary>The permission claim values (see <see cref="Data.Permissions" />) this role currently grants.</summary>
    public required IReadOnlyCollection<string> Permissions { get; init; }

    /// <summary>Maps a <see cref="RoleSummary" /> to its wire representation.</summary>
    /// <param name="role">The domain read-model to map.</param>
    /// <returns>The equivalent <see cref="RoleResponse" />.</returns>
    public static RoleResponse FromRoleSummary(RoleSummary role)
    {
        return new RoleResponse { Name = role.Name, Permissions = role.Permissions };
    }
}