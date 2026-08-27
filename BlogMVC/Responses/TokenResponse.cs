namespace BlogMVC.Responses;

/// <summary>Response body for api/auth/login on success: carries the issued JWT access token.</summary>
public class TokenResponse
{
    /// <summary>The signed JWT, to be sent as an "Authorization: Bearer {token}" header on subsequent requests.</summary>
    public required string Token { get; init; }
}