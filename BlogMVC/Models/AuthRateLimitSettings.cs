namespace BlogMVC.Models;

/// <summary>
///     Configuration model for the per-client rate limit on the auth endpoints, bound from the
///     "RateLimiting:Auth" section via IOptions&lt;AuthRateLimitSettings&gt;.
/// </summary>
public class AuthRateLimitSettings
{
    /// <summary>Maximum number of requests one client IP may make within <see cref="WindowSeconds" />.</summary>
    public int PermitLimit { get; set; } = 10;

    /// <summary>Length of the fixed rate-limit window in seconds.</summary>
    public int WindowSeconds { get; set; } = 60;
}