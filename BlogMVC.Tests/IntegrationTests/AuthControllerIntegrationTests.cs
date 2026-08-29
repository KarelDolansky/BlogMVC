using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using BlogMVC.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BlogMVC.Tests.IntegrationTests;

/// <summary>
///     Integration tests for <see cref="AuthController" /> against the real app (via an HTTP client
///     from <see cref="WebApplicationFactory{Program}" />), real ASP.NET Core Identity (in-memory
///     SQLite) and the real JWT signing configuration from appsettings.Testing.json.
/// </summary>
[Collection("BlogController")]
public class AuthControllerIntegrationTests(WebApplicationFactory<Program> factory)
    : BlogControllerTestBase(factory)
{
    /// <summary>Registers a confirmed Identity user directly, bypassing the (non-existent) registration endpoint.</summary>
    private async Task<string> RegisterUserAsync(string email, string password)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, password);
        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(e => e.Description)));
        return user.Id;
    }

    /// <summary>Verifies that Login with valid credentials returns 200 OK with a JWT token.</summary>
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        const string password = "Password123!";
        var email = $"login-{Guid.NewGuid():N}@example.com";
        await RegisterUserAsync(email, password);
        var loginDto = new LoginDtoFactory().WithEmail(email).WithPassword(password).Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.False(string.IsNullOrEmpty(body!["token"]));
    }

    /// <summary>Verifies that the issued JWT carries the user's Id and email as claims.</summary>
    [Fact]
    public async Task Login_WithValidCredentials_TokenContainsUserClaims()
    {
        // Arrange
        const string password = "Password123!";
        var email = $"claims-{Guid.NewGuid():N}@example.com";
        var userId = await RegisterUserAsync(email, password);
        var loginDto = new LoginDtoFactory().WithEmail(email).WithPassword(password).Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body!["token"]);
        Assert.Equal(userId, jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(email, jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value);
    }

    /// <summary>Verifies that Login with a non-existing email returns 401 Unauthorized.</summary>
    [Fact]
    public async Task Login_WithNonExistingEmail_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDtoFactory().WithEmail($"missing-{Guid.NewGuid():N}@example.com").Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Verifies that Login with a wrong password returns 401 Unauthorized.</summary>
    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var email = $"wrongpw-{Guid.NewGuid():N}@example.com";
        await RegisterUserAsync(email, "Password123!");
        var loginDto = new LoginDtoFactory().WithEmail(email).WithPassword("SomethingElse123!").Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Register ----------

    /// <summary>Verifies that Register with valid data returns 200 OK and actually creates an Identity user.</summary>
    [Fact]
    public async Task Register_WithValidData_ReturnsOkAndCreatesUser()
    {
        // Arrange
        var email = $"register-{Guid.NewGuid():N}@example.com";
        var registerDto = new RegisterDtoFactory().WithEmail(email).WithPassword("Password123!").Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
    }

    /// <summary>
    ///     Verifies that a freshly registered account is created locked out (login fails as LockedOut, not
    ///     InvalidCredentials).
    /// </summary>
    [Fact]
    public async Task Register_WithValidData_CreatesAccountLockedOut()
    {
        // Arrange
        var email = $"locked-{Guid.NewGuid():N}@example.com";
        const string password = "Password123!";
        var registerDto = new RegisterDtoFactory().WithEmail(email).WithPassword(password).Build();
        await Client.PostAsJsonAsync("/api/auth/register", registerDto);
        var loginDto = new LoginDtoFactory().WithEmail(email).WithPassword(password).Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("locked", body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Verifies that once an administrator clears the lockout directly (simulated via UserManager), the account can
    ///     log in.
    /// </summary>
    [Fact]
    public async Task Register_ThenUnlockedByAdmin_CanLogIn()
    {
        // Arrange
        var email = $"unlocked-{Guid.NewGuid():N}@example.com";
        const string password = "Password123!";
        var registerDto = new RegisterDtoFactory().WithEmail(email).WithPassword(password).Build();
        await Client.PostAsJsonAsync("/api/auth/register", registerDto);

        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await userManager.FindByEmailAsync(email);
            await userManager.SetLockoutEndDateAsync(user!, null);
        }

        var loginDto = new LoginDtoFactory().WithEmail(email).WithPassword(password).Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Verifies that Register with an already-registered email returns 400 Bad Request.</summary>
    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var email = $"duplicate-{Guid.NewGuid():N}@example.com";
        await RegisterUserAsync(email, "Password123!");
        var registerDto = new RegisterDtoFactory().WithEmail(email).WithPassword("Password123!").Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}