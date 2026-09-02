namespace BlogMVC.Responses;

/// <summary>Response for a successful api/users/{id}/role update.</summary>
public class UserRoleResponse
{
    /// <summary>Id of the updated user.</summary>
    public required string UserId { get; init; }

    /// <summary>The user's newly assigned role.</summary>
    public required string Role { get; init; }
}