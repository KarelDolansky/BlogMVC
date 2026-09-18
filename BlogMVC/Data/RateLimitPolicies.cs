namespace BlogMVC.Data;

/// <summary>Names of the rate-limiting policies registered in <c>Program.cs</c>.</summary>
public static class RateLimitPolicies
{
    /// <summary>Fixed-window limit per client IP on the auth endpoints (see <see cref="Models.AuthRateLimitSettings" />).</summary>
    public const string Auth = "Auth";
}