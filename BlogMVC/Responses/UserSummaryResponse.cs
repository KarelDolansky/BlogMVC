using BlogMVC.Models;

namespace BlogMVC.Responses;

/// <summary>Response item for GET api/users – one user's id, username, and current role.</summary>
public class UserSummaryResponse
{
    /// <summary>Identity id of the user.</summary>
    public required string Id { get; init; }

    /// <summary>The user's sign-in name (email).</summary>
    public required string UserName { get; init; }

    /// <summary>The user's currently assigned role, or null if they hold none.</summary>
    public string? Role { get; init; }

    /// <summary>Maps a <see cref="UserSummary" /> to its wire representation.</summary>
    /// <param name="user">The domain read-model to map.</param>
    /// <returns>The equivalent <see cref="UserSummaryResponse" />.</returns>
    public static UserSummaryResponse FromUserSummary(UserSummary user)
    {
        return new UserSummaryResponse { Id = user.Id, UserName = user.UserName, Role = user.Role };
    }
}