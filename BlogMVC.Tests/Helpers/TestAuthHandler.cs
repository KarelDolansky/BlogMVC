using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlogMVC.Tests.Helpers;

/// <summary>
/// Test authentication handler that replaces real ASP.NET Core Identity sign-in
/// in integration tests. It reads the user's identity from HTTP headers
/// (<see cref="UserIdHeader"/>, <see cref="UserNameHeader"/>) instead of cookies/tokens.
/// </summary>
public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>Name of this authentication scheme, registered in the test DI container.</summary>
    public const string SchemeName = "Test";

    /// <summary>Name of the HTTP header carrying the Id of the user to treat as logged in.</summary>
    public const string UserIdHeader = "Test-UserId";

    /// <summary>Name of the HTTP header carrying the user's name (optional, defaults to "TestUser").</summary>
    public const string UserNameHeader = "Test-UserName";

    /// <summary>
    /// Attempts to authenticate the request based on headers. If <see cref="UserIdHeader"/>
    /// is missing, returns NoResult (the request stays anonymous).
    /// </summary>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var userId) ||
            string.IsNullOrEmpty(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userName = Request.Headers.TryGetValue(UserNameHeader, out var name)
            ? name.ToString()
            : "TestUser";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, userName)
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>Simulates the response to a failed authentication challenge – returns 401 Unauthorized.</summary>
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    /// <summary>Simulates the response to insufficient permissions – returns 403 Forbidden.</summary>
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}