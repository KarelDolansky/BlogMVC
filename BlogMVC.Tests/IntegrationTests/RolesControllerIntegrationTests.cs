using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using BlogMVC.Data;
using BlogMVC.Dto;
using BlogMVC.Responses;
using BlogMVC.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BlogMVC.Tests.IntegrationTests;

/// <summary>
///     Integration tests for <see cref="RolesController" /> against the real app — real in-memory SQLite
///     Identity, a real JWT-authenticated <see cref="Permissions.Roles.Manage" /> policy, and role permission
///     claims stored/read through the real <see cref="Microsoft.AspNetCore.Identity.RoleManager{TRole}" />.
/// </summary>
[Collection("BlogController")]
public class RolesControllerIntegrationTests(WebApplicationFactory<Program> factory)
    : BlogControllerTestBase(factory)
{
    /// <summary>Registers a confirmed Identity user directly, bypassing the registration endpoint, with no role.</summary>
    /// <param name="email">Email/username for the new account.</param>
    /// <returns>The new user's Identity Id.</returns>
    private async Task<string> RegisterUserAsync(string email)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, DefaultPassword);
        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(e => e.Description)));
        return user.Id;
    }

    /// <summary>
    ///     Verifies that creating a role, assigning it to a user, and logging in as that user reflects the
    ///     granted permissions in the issued JWT.
    /// </summary>
    [Fact]
    public async Task CreateRole_AssignedToUserAndLoggedIn_TokenCarriesGrantedPermissions()
    {
        // Arrange
        var (adminClient, _) = await CreateAuthenticatedClientAsync("admin", role: Roles.Administrator);
        var createDto = new CreateRoleDtoFactory()
            .WithPermissions(Permissions.Posts.Create, Permissions.Posts.EditOwn)
            .Build();
        var createResponse = await adminClient.PostAsJsonAsync("/api/roles", createDto);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var targetEmail = $"target-{Guid.NewGuid():N}@example.com";
        var targetUserId = await RegisterUserAsync(targetEmail);
        var roleDto = new UpdateUserRoleDtoFactory().WithRole(createDto.Name).Build();
        var assignResponse = await adminClient.PutAsJsonAsync($"/api/users/{targetUserId}/role", roleDto);
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);

        // Act
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginDto { Email = targetEmail, Password = DefaultPassword });
        loginResponse.EnsureSuccessStatusCode();
        var body = await loginResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body!["token"]);

        // Assert
        var permissionClaims = jwt.Claims.Where(c => c.Type == Permissions.ClaimType).Select(c => c.Value);
        Assert.Equal(
            new[] { Permissions.Posts.Create, Permissions.Posts.EditOwn }.Order(),
            permissionClaims.Order());
    }

    /// <summary>Verifies that GetRoles as an Administrator returns 200 OK including the seeded predefined roles.</summary>
    [Fact]
    public async Task GetRoles_AsAdministrator_ReturnsOkWithSeededRoles()
    {
        // Arrange
        var (adminClient, _) = await CreateAuthenticatedClientAsync("admin", role: Roles.Administrator);

        // Act
        var response = await adminClient.GetAsync("/api/roles");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<RoleResponse>>();
        Assert.Contains(body!, r => r.Name == Roles.Administrator);
        Assert.Contains(body!, r => r.Name == Roles.Commentator);
    }

    /// <summary>Verifies that GetRoles as an Editor (no Roles.Manage permission) returns 403 Forbidden.</summary>
    [Fact]
    public async Task GetRoles_AsEditor_ReturnsForbidden()
    {
        // Arrange
        var (editorClient, _) = await CreateAuthenticatedClientAsync("editor", role: Roles.Editor);

        // Act
        var response = await editorClient.GetAsync("/api/roles");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Verifies that GetRoles without a bearer token returns 401 Unauthorized.</summary>
    [Fact]
    public async Task GetRoles_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/roles");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Verifies that GetPermissions as an Administrator returns 200 OK with the full permission catalog.</summary>
    [Fact]
    public async Task GetPermissions_AsAdministrator_ReturnsOkWithCatalog()
    {
        // Arrange
        var (adminClient, _) = await CreateAuthenticatedClientAsync("admin", role: Roles.Administrator);

        // Act
        var response = await adminClient.GetAsync("/api/roles/permissions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PermissionsResponse>();
        Assert.Equal(Permissions.All, body!.Permissions);
    }

    /// <summary>Verifies that CreateRole with a name that already exists returns 409 Conflict.</summary>
    [Fact]
    public async Task CreateRole_DuplicateName_ReturnsConflict()
    {
        // Arrange
        var (adminClient, _) = await CreateAuthenticatedClientAsync("admin", role: Roles.Administrator);

        // Act
        var response =
            await adminClient.PostAsJsonAsync("/api/roles", new CreateRoleDtoFactory().WithName(Roles.Editor).Build());

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>Verifies that CreateRole with an unrecognized permission returns 400 BadRequest.</summary>
    [Fact]
    public async Task CreateRole_InvalidPermission_ReturnsBadRequest()
    {
        // Arrange
        var (adminClient, _) = await CreateAuthenticatedClientAsync("admin", role: Roles.Administrator);
        var dto = new CreateRoleDtoFactory().WithPermissions("NotAPermission").Build();

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/roles", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Verifies that CreateRole with an explicit JSON null for permissions returns 400 BadRequest.</summary>
    [Fact]
    public async Task CreateRole_WithNullPermissions_ReturnsBadRequest()
    {
        // Arrange
        // A raw null (rather than an omitted field or an empty array) used to reach RoleService and throw an
        // unhandled ArgumentNullException there instead of failing model validation.
        var (adminClient, _) = await CreateAuthenticatedClientAsync("admin", role: Roles.Administrator);
        var content = JsonContent.Create(new
            { name = $"NullPermsRole-{Guid.NewGuid():N}", permissions = (string[]?)null });

        // Act
        var response = await adminClient.PostAsync("/api/roles", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///     Verifies that a name differing from an existing role only by surrounding whitespace is rejected as a
    ///     duplicate.
    /// </summary>
    [Fact]
    public async Task CreateRole_NameWithSurroundingWhitespace_ReturnsConflictForExistingRole()
    {
        // Arrange
        // Without trimming, this would silently create a second role visually indistinguishable from Editor.
        var (adminClient, _) = await CreateAuthenticatedClientAsync("admin", role: Roles.Administrator);
        var dto = new CreateRoleDtoFactory().WithName($"  {Roles.Editor}  ").Build();

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/roles", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>Verifies that UpdateRolePermissions changes what a subsequent login for that role's user grants.</summary>
    [Fact]
    public async Task UpdateRolePermissions_ThenLogin_ReflectsNewPermissions()
    {
        // Arrange
        var (adminClient, _) = await CreateAuthenticatedClientAsync("admin", role: Roles.Administrator);
        var createDto = new CreateRoleDtoFactory().WithPermissions(Permissions.Posts.Create).Build();
        await adminClient.PostAsJsonAsync("/api/roles", createDto);

        // Act
        var updateDto = new UpdateRolePermissionsDtoFactory().WithPermissions(Permissions.Posts.DeleteOwn).Build();
        var response = await adminClient.PutAsJsonAsync($"/api/roles/{createDto.Name}/permissions", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RoleResponse>();
        Assert.Equal([Permissions.Posts.DeleteOwn], body!.Permissions);
    }

    /// <summary>Verifies that UpdateRolePermissions with an explicit JSON null for permissions returns 400 BadRequest.</summary>
    [Fact]
    public async Task UpdateRolePermissions_WithNullPermissions_ReturnsBadRequest()
    {
        // Arrange
        // A raw null used to reach RoleService and throw an unhandled ArgumentNullException there instead of
        // failing model validation.
        var (adminClient, _) = await CreateAuthenticatedClientAsync("admin", role: Roles.Administrator);
        var content = JsonContent.Create(new { permissions = (string[]?)null });

        // Act
        var response = await adminClient.PutAsync($"/api/roles/{Roles.Editor}/permissions", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Verifies that DeleteRole for a role no user holds returns 204 NoContent.</summary>
    [Fact]
    public async Task DeleteRole_UnusedRole_ReturnsNoContent()
    {
        // Arrange
        var (adminClient, _) = await CreateAuthenticatedClientAsync("admin", role: Roles.Administrator);
        var createDto = new CreateRoleDtoFactory().Build();
        await adminClient.PostAsJsonAsync("/api/roles", createDto);

        // Act
        var response = await adminClient.DeleteAsync($"/api/roles/{createDto.Name}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>Verifies that DeleteRole for a role currently assigned to a user returns 409 Conflict.</summary>
    [Fact]
    public async Task DeleteRole_RoleInUse_ReturnsConflict()
    {
        // Arrange
        var (adminClient, _) = await CreateAuthenticatedClientAsync("admin", role: Roles.Administrator);

        // Act
        var response = await adminClient.DeleteAsync($"/api/roles/{Roles.Administrator}");

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}