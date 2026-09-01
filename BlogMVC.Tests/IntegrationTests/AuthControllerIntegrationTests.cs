using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using BlogMVC.Data;
using BlogMVC.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BlogMVC.Tests.IntegrationTests;

/// <summary>
///     Integration tests for <see cref="AuthController" /> against the real app — real in-memory SQLite
///     Identity and real JWT signing from appsettings.Testing.json.
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

    /// <summary>Verifies that Login with a malformed email address returns 400 Bad Request (DTO validation, not 401).</summary>
    [Fact]
    public async Task Login_WithInvalidEmailFormat_ReturnsBadRequest()
    {
        // Arrange
        var loginDto = new LoginDtoFactory().WithEmail("not-an-email").Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Verifies that Login with an empty password returns 400 Bad Request (DTO validation, not 401).</summary>
    [Fact]
    public async Task Login_WithEmptyPassword_ReturnsBadRequest()
    {
        // Arrange
        var loginDto = new LoginDtoFactory().WithPassword("").Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

    /// <summary>Verifies that a freshly registered account can log in immediately (no lockout/approval step).</summary>
    [Fact]
    public async Task Register_ThenLogin_Succeeds()
    {
        // Arrange
        var email = $"register-login-{Guid.NewGuid():N}@example.com";
        const string password = "Password123!";
        var registerDto = new RegisterDtoFactory().WithEmail(email).WithPassword(password).Build();
        await Client.PostAsJsonAsync("/api/auth/register", registerDto);
        var loginDto = new LoginDtoFactory().WithEmail(email).WithPassword(password).Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------- Roles ----------

    /// <summary>Verifies that all predefined roles are seeded into the Identity store at startup.</summary>
    [Fact]
    public async Task Roles_AreSeededAtStartup()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Act & Assert
        foreach (var roleName in Roles.All)
            Assert.True(await roleManager.RoleExistsAsync(roleName), $"Role '{roleName}' should be seeded.");
    }

    /// <summary>Verifies that Register with valid data assigns the default Commentator role to the new user.</summary>
    [Fact]
    public async Task Register_WithValidData_AssignsCommentatorRole()
    {
        // Arrange
        var email = $"role-{Guid.NewGuid():N}@example.com";
        var registerDto = new RegisterDtoFactory().WithEmail(email).WithPassword("Password123!").Build();

        // Act
        await Client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByEmailAsync(email);
        var roles = await userManager.GetRolesAsync(user!);
        Assert.Contains(Roles.Commentator, roles);
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

    /// <summary>Verifies that Register with a malformed email address returns 400 Bad Request (DTO validation).</summary>
    [Fact]
    public async Task Register_WithInvalidEmailFormat_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDtoFactory().WithEmail("not-an-email").Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///     Verifies Register with an empty password returns 400 via DTO validation, not Identity's password
    ///     policy — distinguished by response shape ("errors" object keyed by field, not an array).
    /// </summary>
    [Fact]
    public async Task Register_WithEmptyPassword_ReturnsBadRequest()
    {
        // Arrange
        var email = $"empty-pw-{Guid.NewGuid():N}@example.com";
        var registerDto = new RegisterDtoFactory().WithEmail(email).WithPassword("").Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errors = body.GetProperty("errors");
        Assert.Equal(JsonValueKind.Object, errors.ValueKind);
        Assert.True(errors.TryGetProperty("Password", out _),
            "Expected the automatic ValidationProblemDetails shape (errors.Password), not AuthController's custom ErrorResponse.");
    }

    /// <summary>
    ///     Verifies Register with a non-empty but policy-violating password (too short/simple) is rejected
    ///     by UserManager.CreateAsync — 400, distinct from the DTO-validation path.
    /// </summary>
    [Fact]
    public async Task Register_WithWeakPassword_ReturnsBadRequest()
    {
        // Arrange
        var email = $"weak-pw-{Guid.NewGuid():N}@example.com";
        var registerDto = new RegisterDtoFactory().WithEmail(email).WithPassword("weak").Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        Assert.Null(await userManager.FindByEmailAsync(email));
    }
}