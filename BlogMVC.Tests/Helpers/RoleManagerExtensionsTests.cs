using System.Security.Claims;
using BlogMVC.Data;
using BlogMVC.Helpers;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace BlogMVC.Tests.Helpers;

/// <summary>
///     Unit tests for <see cref="RoleManagerExtensions.GetPermissionsAsync" /> using a mocked
///     <see cref="RoleManager{TRole}" />. Verifies the distinct-union-across-roles behavior that replaced
///     the old static <c>RolePermissions.GetPermissions</c> lookup.
/// </summary>
public class RoleManagerExtensionsTests
{
    /// <summary>Mocked <see cref="RoleManager{TRole}" /> used by every test.</summary>
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;

    /// <summary>Builds a fresh mocked <see cref="RoleManager{TRole}" /> for each test.</summary>
    public RoleManagerExtensionsTests()
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        _roleManagerMock = new Mock<RoleManager<IdentityRole>>(store.Object, null!, null!, null!, null!);
    }

    /// <summary>
    ///     Configures the mock so <paramref name="roleName" /> resolves to a role granting
    ///     <paramref name="permissions" />.
    /// </summary>
    /// <param name="roleName">Role name to configure a lookup for.</param>
    /// <param name="permissions">Permission claim values that role should appear to grant.</param>
    private void SetUpRole(string roleName, params string[] permissions)
    {
        var role = new IdentityRole(roleName);
        _roleManagerMock.Setup(r => r.FindByNameAsync(roleName)).ReturnsAsync(role);
        _roleManagerMock.Setup(r => r.GetClaimsAsync(role))
            .ReturnsAsync(permissions.Select(p => new Claim(Permissions.ClaimType, p)).ToList());
    }

    /// <summary>Verifies that GetPermissionsAsync returns a single role's permission claims.</summary>
    [Fact]
    public async Task GetPermissionsAsync_SingleRole_ReturnsItsPermissions()
    {
        // Arrange
        SetUpRole("Editor", Permissions.Posts.Create, Permissions.Posts.EditOwn);

        // Act
        var permissions = await _roleManagerMock.Object.GetPermissionsAsync(["Editor"]);

        // Assert
        Assert.Equal(
            new[] { Permissions.Posts.Create, Permissions.Posts.EditOwn }.Order(),
            permissions.Order());
    }

    /// <summary>Verifies that GetPermissionsAsync returns the distinct union across multiple roles.</summary>
    [Fact]
    public async Task GetPermissionsAsync_MultipleRoles_ReturnsDistinctUnion()
    {
        // Arrange
        SetUpRole("Author", Permissions.Posts.Create, Permissions.Posts.EditOwn);
        SetUpRole("Administrator", Permissions.Posts.Create, Permissions.Posts.EditAny);

        // Act
        var permissions = await _roleManagerMock.Object.GetPermissionsAsync(["Author", "Administrator"]);

        // Assert
        Assert.Equal(
            new[] { Permissions.Posts.Create, Permissions.Posts.EditAny, Permissions.Posts.EditOwn }.Order(),
            permissions.Order());
    }

    /// <summary>Verifies that GetPermissionsAsync ignores a role name that doesn't resolve to an actual role.</summary>
    [Fact]
    public async Task GetPermissionsAsync_UnknownRoleName_ContributesNothing()
    {
        // Arrange
        _roleManagerMock.Setup(r => r.FindByNameAsync("NotARole")).ReturnsAsync((IdentityRole?)null);

        // Act
        var permissions = await _roleManagerMock.Object.GetPermissionsAsync(["NotARole"]);

        // Assert
        Assert.Empty(permissions);
    }

    /// <summary>Verifies that GetPermissionsAsync with no role names returns an empty set.</summary>
    [Fact]
    public async Task GetPermissionsAsync_NoRoleNames_ReturnsEmpty()
    {
        // Act
        var permissions = await _roleManagerMock.Object.GetPermissionsAsync([]);

        // Assert
        Assert.Empty(permissions);
    }
}