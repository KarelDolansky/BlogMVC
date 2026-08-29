namespace BlogMVC.Responses;

/// <summary>Response body for api/auth/register on success.</summary>
public class RegisterResponse
{
    /// <summary>Human-readable confirmation, e.g. that the account still needs administrator approval.</summary>
    public required string Message { get; init; }
}