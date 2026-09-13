namespace BlogMVC.Models;

/// <summary>A role's name and the permissions it currently grants, for role administration.</summary>
public class RoleSummary
{
    /// <summary>The role's name.</summary>
    public required string Name { get; init; }

    /// <summary>The permission claim values (see <see cref="Data.Permissions" />) this role currently grants.</summary>
    public required IReadOnlyCollection<string> Permissions { get; init; }
}