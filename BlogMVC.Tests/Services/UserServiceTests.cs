using BlogMVC.Data;
using BlogMVC.Results;
using BlogMVC.Services;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace BlogMVC.Tests.Services;

/// <summary>
///     Unit tests for <see cref="UserService" /> using a mocked <see cref="UserManager{TUser}" />.
///     Verify the returned <see cref="UpdateUserRoleResult" /> depending on whether the role name is
///     recognized and the user exists, and that role assignment replaces (not adds to) existing roles.
/// </summary>
public class UserServiceTests
{
    /// <summary>Identity user returned by the mocked <see cref="UserManager{TUser}" /> in most tests.</summary>
    private readonly IdentityUser _defaultUser = new()
    {
        Id = "defaultUserId",
        Email = "test@example.com",
        UserName = "test@example.com"
    };

    /// <summary>Identity id used across tests for the default user.</summary>
    private readonly string _defaultUserId = "defaultUserId";

    /// <summary>Mocked <see cref="UserManager{TUser}" /> passed to <see cref="_userService" />.</summary>
    private readonly Mock<UserManager<IdentityUser>> _userManagerMock;

    /// <summary>System under test, constructed with a mocked Identity manager.</summary>
    private readonly UserService _userService;

    /// <summary>Builds <see cref="_userService" /> with a fresh mock for each test.</summary>
    public UserServiceTests()
    {
        _userManagerMock = CreateUserManagerMock();
        _userService = new UserService(_userManagerMock.Object);
    }

    /// <summary>Builds a mocked <see cref="UserManager{TUser}" /> (it has no parameterless constructor).</summary>
    /// <returns>A mock with a mocked <see cref="IUserStore{TUser}" /> and null dependencies otherwise.</returns>
    private static Mock<UserManager<IdentityUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!,
            null!);
    }

    // ---------- UpdateUserRoleAsync ----------

    /// <summary>Verifies that UpdateUserRoleAsync with an unrecognized role fails with InvalidRole.</summary>
    [Fact]
    public async Task UpdateUserRoleAsync_WithUnrecognizedRole_FailsWithInvalidRole()
    {
        // Act
        var result = await _userService.UpdateUserRoleAsync(_defaultUserId, "NotARole");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(UpdateUserRoleFailureReason.InvalidRole, result.FailureReason);
    }

    /// <summary>Verifies that UpdateUserRoleAsync with an unrecognized role does not look up the user.</summary>
    [Fact]
    public async Task UpdateUserRoleAsync_WithUnrecognizedRole_DoesNotCallFindByIdAsync()
    {
        // Act
        await _userService.UpdateUserRoleAsync(_defaultUserId, "NotARole");

        // Assert
        _userManagerMock.Verify(u => u.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }

    /// <summary>Verifies that UpdateUserRoleAsync with a non-existing user id fails with UserNotFound.</summary>
    [Fact]
    public async Task UpdateUserRoleAsync_WithNonExistingUserId_FailsWithUserNotFound()
    {
        // Arrange
        _userManagerMock.Setup(u => u.FindByIdAsync(_defaultUserId)).ReturnsAsync((IdentityUser?)null);

        // Act
        var result = await _userService.UpdateUserRoleAsync(_defaultUserId, Roles.Editor);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(UpdateUserRoleFailureReason.UserNotFound, result.FailureReason);
    }

    /// <summary>Verifies that UpdateUserRoleAsync with a valid role and existing user succeeds.</summary>
    [Fact]
    public async Task UpdateUserRoleAsync_WithValidRoleAndExistingUser_Succeeds()
    {
        // Arrange
        _userManagerMock.Setup(u => u.FindByIdAsync(_defaultUserId)).ReturnsAsync(_defaultUser);
        _userManagerMock.Setup(u => u.GetRolesAsync(_defaultUser)).ReturnsAsync(new List<string> { Roles.Author });
        _userManagerMock
            .Setup(u => u.RemoveFromRolesAsync(_defaultUser, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(u => u.AddToRoleAsync(_defaultUser, Roles.Editor)).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _userService.UpdateUserRoleAsync(_defaultUserId, Roles.Editor);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Null(result.FailureReason);
    }

    /// <summary>Verifies that UpdateUserRoleAsync removes every role the user currently holds.</summary>
    [Fact]
    public async Task UpdateUserRoleAsync_UserWithExistingRoles_RemovesThemBeforeAssigningNewRole()
    {
        // Arrange
        var currentRoles = new List<string> { Roles.Author, Roles.Commentator };
        _userManagerMock.Setup(u => u.FindByIdAsync(_defaultUserId)).ReturnsAsync(_defaultUser);
        _userManagerMock.Setup(u => u.GetRolesAsync(_defaultUser)).ReturnsAsync(currentRoles);
        _userManagerMock
            .Setup(u => u.RemoveFromRolesAsync(_defaultUser, currentRoles))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(u => u.AddToRoleAsync(_defaultUser, Roles.Editor)).ReturnsAsync(IdentityResult.Success);

        // Act
        await _userService.UpdateUserRoleAsync(_defaultUserId, Roles.Editor);

        // Assert
        _userManagerMock.Verify(u => u.RemoveFromRolesAsync(_defaultUser, currentRoles), Times.Once);
    }

    /// <summary>Verifies that UpdateUserRoleAsync for a user with no existing roles skips the removal call.</summary>
    [Fact]
    public async Task UpdateUserRoleAsync_UserWithNoExistingRoles_DoesNotCallRemoveFromRolesAsync()
    {
        // Arrange
        _userManagerMock.Setup(u => u.FindByIdAsync(_defaultUserId)).ReturnsAsync(_defaultUser);
        _userManagerMock.Setup(u => u.GetRolesAsync(_defaultUser)).ReturnsAsync(new List<string>());
        _userManagerMock.Setup(u => u.AddToRoleAsync(_defaultUser, Roles.Editor)).ReturnsAsync(IdentityResult.Success);

        // Act
        await _userService.UpdateUserRoleAsync(_defaultUserId, Roles.Editor);

        // Assert
        _userManagerMock.Verify(
            u => u.RemoveFromRolesAsync(It.IsAny<IdentityUser>(), It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    /// <summary>Verifies that UpdateUserRoleAsync assigns the requested role to the found user.</summary>
    [Fact]
    public async Task UpdateUserRoleAsync_WithValidRole_AssignsRequestedRoleToFoundUser()
    {
        // Arrange
        _userManagerMock.Setup(u => u.FindByIdAsync(_defaultUserId)).ReturnsAsync(_defaultUser);
        _userManagerMock.Setup(u => u.GetRolesAsync(_defaultUser)).ReturnsAsync(new List<string>());
        _userManagerMock.Setup(u => u.AddToRoleAsync(_defaultUser, Roles.Administrator))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _userService.UpdateUserRoleAsync(_defaultUserId, Roles.Administrator);

        // Assert
        _userManagerMock.Verify(u => u.AddToRoleAsync(_defaultUser, Roles.Administrator), Times.Once);
    }

    // ---------- GetUsersAsync ----------

    /// <summary>Verifies that GetUsersAsync maps every user to a summary carrying their first assigned role.</summary>
    [Fact]
    public async Task GetUsersAsync_WithUsers_ReturnsSummariesWithRoles()
    {
        // Arrange
        var otherUser = new IdentityUser
            { Id = "otherUserId", Email = "other@example.com", UserName = "other@example.com" };
        _userManagerMock.Setup(u => u.Users).Returns(new[] { _defaultUser, otherUser }.AsQueryable());
        _userManagerMock.Setup(u => u.GetRolesAsync(_defaultUser)).ReturnsAsync(new List<string> { Roles.Editor });
        _userManagerMock.Setup(u => u.GetRolesAsync(otherUser)).ReturnsAsync(new List<string> { Roles.Author });

        // Act
        var result = await _userService.GetUsersAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result,
            u => u.Id == _defaultUser.Id && u.UserName == _defaultUser.UserName && u.Role == Roles.Editor);
        Assert.Contains(result,
            u => u.Id == otherUser.Id && u.UserName == otherUser.UserName && u.Role == Roles.Author);
    }

    /// <summary>Verifies that GetUsersAsync maps a user with no assigned role to a null Role.</summary>
    [Fact]
    public async Task GetUsersAsync_UserWithNoRole_ReturnsNullRole()
    {
        // Arrange
        _userManagerMock.Setup(u => u.Users).Returns(new[] { _defaultUser }.AsQueryable());
        _userManagerMock.Setup(u => u.GetRolesAsync(_defaultUser)).ReturnsAsync(new List<string>());

        // Act
        var result = await _userService.GetUsersAsync();

        // Assert
        Assert.Single(result);
        Assert.Null(result[0].Role);
    }

    /// <summary>Verifies that GetUsersAsync with no users returns an empty list.</summary>
    [Fact]
    public async Task GetUsersAsync_NoUsers_ReturnsEmptyList()
    {
        // Arrange
        _userManagerMock.Setup(u => u.Users).Returns(Array.Empty<IdentityUser>().AsQueryable());

        // Act
        var result = await _userService.GetUsersAsync();

        // Assert
        Assert.Empty(result);
    }
}