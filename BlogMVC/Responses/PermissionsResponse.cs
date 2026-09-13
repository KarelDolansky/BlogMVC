namespace BlogMVC.Responses;

/// <summary>Response for GET api/roles/permissions – every permission claim value a role can be granted.</summary>
public class PermissionsResponse
{
    /// <summary>All known permission claim values (see <see cref="Data.Permissions.All" />).</summary>
    public required IReadOnlyList<string> Permissions { get; init; }
}