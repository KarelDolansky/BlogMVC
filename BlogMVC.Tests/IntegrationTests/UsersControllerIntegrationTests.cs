using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BlogMVC.Data;
using BlogMVC.Responses;
using BlogMVC.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BlogMVC.Tests.IntegrationTests;

/// <summary>
///     Integration tests for <see cref="UsersController" /> against the real app — real in-memory SQLite
///     Identity and a real JWT-authenticated permission policy for <see cref="Permissions.Users.ManageRoles" />.
/// </summary>
[Collection("BlogController")]
public class UsersControllerIntegrationTests(WebApplicationFactory<Program> factory)
    : BlogControllerTestBase(factory)
{
    /// <summary>Reads the target user's current Identity roles directly through <see cref="UserManager{TUser}" />.</summary>
    /// <param name="userId">Identity id of the user to inspect.</param>
    /// <returns>The role names currently assigned to the user.</returns>
    private async Task<IList<string>> GetRolesAsync(string userId)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByIdAsync(userId);
        return await userManager.GetRolesAsync(user!);
    }

    /// <summary>Verifies that UpdateUserRole as an Administrator returns 200 OK and actually changes the role.</summary>
    [Fact]
    public async Task UpdateUserRole_AsAdministrator_ReturnsOkAndChangesRole()
    {
        // Arrange
        var (adminClient, _) = await CreateAuthenticatedClientAsync("admin", role: Roles.Administrator);
        var (_, targetUserId) = await CreateAuthenticatedClientAsync("target", role: Roles.Author);
        var dto = new UpdateUserRoleDtoFactory().WithRole(Roles.Editor).Build();

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/users/{targetUserId}/role", dto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserRoleResponse>();
        Assert.Equal(Roles.Editor, body!.Role);
        Assert.Contains(Roles.Editor, await GetRolesAsync(targetUserId));
    }

    /// <summary>Verifies that UpdateUserRole replaces the target's existing role rather than adding to it.</summary>
    [Fact]
    public async Task UpdateUserRole_AsAdministrator_RemovesPreviousRole()
    {
        // Arrange
        var (adminClient, _) = await CreateAuthenticatedClientAsync("admin", role: Roles.Administrator);
        var (_, targetUserId) = await CreateAuthenticatedClientAsync("target", role: Roles.Author);
        var dto = new UpdateUserRoleDtoFactory().WithRole(Roles.Editor).Build();

        // Act
        await adminClient.PutAsJsonAsync($"/api/users/{targetUserId}/role", dto);

        // Assert
        Assert.DoesNotContain(Roles.Author, await GetRolesAsync(targetUserId));
    }

    /// <summary>Verifies that UpdateUserRole without a bearer token returns 401 Unauthorized.</summary>
    [Fact]
    public async Task UpdateUserRole_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var (_, targetUserId) = await CreateAuthenticatedClientAsync("target", role: Roles.Author);
        var dto = new UpdateUserRoleDtoFactory().Build();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/users/{targetUserId}/role", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    ///     Verifies that UpdateUserRole with a malformed/tampered Bearer token returns 401 Unauthorized
    ///     (JWT bearer authentication rejects it before the policy or the action ever run).
    /// </summary>
    [Fact]
    public async Task UpdateUserRole_WithMalformedBearerToken_ReturnsUnauthorized()
    {
        // Arrange
        var (_, targetUserId) = await CreateAuthenticatedClientAsync("target", role: Roles.Author);
        var dto = new UpdateUserRoleDtoFactory().Build();
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/users/{targetUserId}/role")
            { Content = JsonContent.Create(dto) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-jwt-token");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Verifies that UpdateUserRole as an Editor (no Users.ManageRoles permission) returns 403 Forbidden.</summary>
    [Fact]
    public async Task UpdateUserRole_AsEditor_ReturnsForbidden()
    {
        // Arrange
        var (editorClient, _) = await CreateAuthenticatedClientAsync("editor", role: Roles.Editor);
        var (_, targetUserId) = await CreateAuthenticatedClientAsync("target", role: Roles.Author);
        var dto = new UpdateUserRoleDtoFactory().Build();

        // Act
        var response = await editorClient.PutAsJsonAsync($"/api/users/{targetUserId}/role", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Verifies that UpdateUserRole as a Commentator (default role) returns 403 Forbidden.</summary>
    [Fact]
    public async Task UpdateUserRole_AsCommentator_ReturnsForbidden()
    {
        // Arrange
        var (commentatorClient, _) = await CreateAuthenticatedClientAsync("commentator", role: Roles.Commentator);
        var (_, targetUserId) = await CreateAuthenticatedClientAsync("target", role: Roles.Author);
        var dto = new UpdateUserRoleDtoFactory().Build();

        // Act
        var response = await commentatorClient.PutAsJsonAsync($"/api/users/{targetUserId}/role", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Verifies that UpdateUserRole for a non-existing user id returns 404 Not Found.</summary>
    [Fact]
    public async Task UpdateUserRole_WithNonExistingUserId_ReturnsNotFound()
    {
        // Arrange
        var (adminClient, _) = await CreateAuthenticatedClientAsync("admin", role: Roles.Administrator);
        var dto = new UpdateUserRoleDtoFactory().Build();

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/users/{Guid.NewGuid()}/role", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifies that UpdateUserRole with an unrecognized role name returns 400 Bad Request.</summary>
    [Fact]
    public async Task UpdateUserRole_WithUnrecognizedRoleName_ReturnsBadRequest()
    {
        // Arrange
        var (adminClient, _) = await CreateAuthenticatedClientAsync("admin", role: Roles.Administrator);
        var (_, targetUserId) = await CreateAuthenticatedClientAsync("target", role: Roles.Author);
        var dto = new UpdateUserRoleDtoFactory().WithRole("NotARole").Build();

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/users/{targetUserId}/role", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    ///     Verifies UpdateUserRole with an empty role returns 400 via DTO validation, not UserService's
    ///     InvalidRole path — distinguished by response shape (automatic ValidationProblemDetails
    ///     "errors.Role" as a JSON object), not UsersController's plain string BadRequest body.
    /// </summary>
    [Fact]
    public async Task UpdateUserRole_WithEmptyRole_ReturnsBadRequest()
    {
        // Arrange
        var (adminClient, _) = await CreateAuthenticatedClientAsync("admin", role: Roles.Administrator);
        var (_, targetUserId) = await CreateAuthenticatedClientAsync("target", role: Roles.Author);
        var dto = new UpdateUserRoleDtoFactory().WithRole("").Build();

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/users/{targetUserId}/role", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errors = body.GetProperty("errors");
        Assert.Equal(JsonValueKind.Object, errors.ValueKind);
        Assert.True(errors.TryGetProperty("Role", out _),
            "Expected the automatic ValidationProblemDetails shape (errors.Role), not UsersController's plain BadRequest string.");
    }

    // ---------- GetUsers ----------

    /// <summary>Verifies that GetUsers as an Administrator returns 200 OK with every created user and their role.</summary>
    [Fact]
    public async Task GetUsers_AsAdministrator_ReturnsOkWithAllUsersAndRoles()
    {
        // Arrange
        var (adminClient, adminId) = await CreateAuthenticatedClientAsync("admin", role: Roles.Administrator);
        var (_, targetUserId) = await CreateAuthenticatedClientAsync("target", role: Roles.Author);

        // Act
        var response = await adminClient.GetAsync("/api/users");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<UserSummaryResponse>>() ?? [];
        Assert.Contains(body, u => u.Id == adminId && u.Role == Roles.Administrator);
        Assert.Contains(body, u => u.Id == targetUserId && u.Role == Roles.Author);
    }

    /// <summary>Verifies that GetUsers without a bearer token returns 401 Unauthorized.</summary>
    [Fact]
    public async Task GetUsers_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/users");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Verifies that GetUsers as an Editor (no Users.ManageRoles permission) returns 403 Forbidden.</summary>
    [Fact]
    public async Task GetUsers_AsEditor_ReturnsForbidden()
    {
        // Arrange
        var (editorClient, _) = await CreateAuthenticatedClientAsync("editor", role: Roles.Editor);

        // Act
        var response = await editorClient.GetAsync("/api/users");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Verifies that GetUsers as a Commentator (default role) returns 403 Forbidden.</summary>
    [Fact]
    public async Task GetUsers_AsCommentator_ReturnsForbidden()
    {
        // Arrange
        var (commentatorClient, _) = await CreateAuthenticatedClientAsync("commentator", role: Roles.Commentator);

        // Act
        var response = await commentatorClient.GetAsync("/api/users");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- GetRoles ----------

    /// <summary>Verifies that GetRoles as an Administrator returns 200 OK with every predefined role.</summary>
    [Fact]
    public async Task GetRoles_AsAdministrator_ReturnsOkWithAllRoles()
    {
        // Arrange
        var (adminClient, _) = await CreateAuthenticatedClientAsync("admin", role: Roles.Administrator);

        // Act
        var response = await adminClient.GetAsync("/api/users/roles");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RolesResponse>();
        Assert.Equal(Roles.All, body!.Roles);
    }

    /// <summary>Verifies that GetRoles without a bearer token returns 401 Unauthorized.</summary>
    [Fact]
    public async Task GetRoles_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/users/roles");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Verifies that GetRoles as an Editor (no Users.ManageRoles permission) returns 403 Forbidden.</summary>
    [Fact]
    public async Task GetRoles_AsEditor_ReturnsForbidden()
    {
        // Arrange
        var (editorClient, _) = await CreateAuthenticatedClientAsync("editor", role: Roles.Editor);

        // Act
        var response = await editorClient.GetAsync("/api/users/roles");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Verifies that GetRoles as a Commentator (default role) returns 403 Forbidden.</summary>
    [Fact]
    public async Task GetRoles_AsCommentator_ReturnsForbidden()
    {
        // Arrange
        var (commentatorClient, _) = await CreateAuthenticatedClientAsync("commentator", role: Roles.Commentator);

        // Act
        var response = await commentatorClient.GetAsync("/api/users/roles");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}