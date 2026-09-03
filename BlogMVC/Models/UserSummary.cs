namespace BlogMVC.Models;

/// <summary>An Identity user's id, sign-in name, and current single-role assignment, for role administration.</summary>
public class UserSummary
{
    /// <summary>Identity id of the user.</summary>
    public required string Id { get; init; }

    /// <summary>The user's sign-in name (email).</summary>
    public required string UserName { get; init; }

    /// <summary>The user's currently assigned role, or null if they hold none.</summary>
    public string? Role { get; init; }
}