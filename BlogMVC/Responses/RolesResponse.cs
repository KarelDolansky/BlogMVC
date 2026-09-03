namespace BlogMVC.Responses;

/// <summary>Response for GET api/users/roles – every role a user can be assigned.</summary>
public class RolesResponse
{
    /// <summary>All predefined role names, in seed order (see <see cref="BlogMVC.Data.Roles.All" />).</summary>
    public required IReadOnlyList<string> Roles { get; init; }
}