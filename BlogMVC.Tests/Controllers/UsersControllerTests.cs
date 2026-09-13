using BlogMVC.Controllers;
using BlogMVC.Data;
using BlogMVC.Models;
using BlogMVC.Responses;
using BlogMVC.Results;
using BlogMVC.Services;
using BlogMVC.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BlogMVC.Tests.Controllers;

/// <summary>
///     Unit tests for <see cref="UsersController" /> using a mocked <see cref="IUserService" />.
///     Verify that the controller maps each <see cref="UpdateUserRoleResult" /> to the correct status
///     code/body, without any Identity logic of its own.
/// </summary>
public class UsersControllerTests
{
    /// <summary>Identity id used across tests for the target user.</summary>
    private readonly string _defaultUserId = "defaultUserId";

    /// <summary>Mocked <see cref="IUserService" /> used to stub UpdateUserRoleAsync outcomes.</summary>
    private readonly Mock<IUserService> _userServiceMock;

    /// <summary>The controller under test, wired to the mocked <see cref="IUserService" />.</summary>
    private readonly UsersController _usersController;

    /// <summary>Creates the controller under test with a fresh <see cref="IUserService" /> mock.</summary>
    public UsersControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _usersController = new UsersController(_userServiceMock.Object);
    }

    // ---------- UpdateUserRole ----------

    /// <summary>Verifies that UpdateUserRole for a non-existing user returns NotFound.</summary>
    [Fact]
    public async Task UpdateUserRole_WithNonExistingUser_ReturnsNotFound()
    {
        // Arrange
        var dto = new UpdateUserRoleDtoFactory().Build();
        _userServiceMock.Setup(u => u.UpdateUserRoleAsync(_defaultUserId, dto.Role))
            .ReturnsAsync(UpdateUserRoleResult.Failure(UpdateUserRoleFailureReason.UserNotFound));

        // Act
        var response = await _usersController.UpdateUserRole(_defaultUserId, dto);

        // Assert
        Assert.IsType<NotFoundResult>(response.Result);
    }

    /// <summary>Verifies that UpdateUserRole with an unrecognized role returns BadRequest.</summary>
    [Fact]
    public async Task UpdateUserRole_WithInvalidRole_ReturnsBadRequest()
    {
        // Arrange
        var dto = new UpdateUserRoleDtoFactory().WithRole("NotARole").Build();
        _userServiceMock.Setup(u => u.UpdateUserRoleAsync(_defaultUserId, dto.Role))
            .ReturnsAsync(UpdateUserRoleResult.Failure(UpdateUserRoleFailureReason.InvalidRole));

        // Act
        var response = await _usersController.UpdateUserRole(_defaultUserId, dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    /// <summary>Verifies that UpdateUserRole with valid data returns Ok with the new role.</summary>
    [Fact]
    public async Task UpdateUserRole_WithValidData_ReturnsOkWithNewRole()
    {
        // Arrange
        var dto = new UpdateUserRoleDtoFactory().WithRole(Roles.Editor).Build();
        _userServiceMock.Setup(u => u.UpdateUserRoleAsync(_defaultUserId, dto.Role))
            .ReturnsAsync(UpdateUserRoleResult.Success());

        // Act
        var response = await _usersController.UpdateUserRole(_defaultUserId, dto);

        // Assert
        var result = Assert.IsType<OkObjectResult>(response.Result);
        var body = Assert.IsType<UserRoleResponse>(result.Value);
        Assert.Equal(_defaultUserId, body.UserId);
        Assert.Equal(Roles.Editor, body.Role);
    }

    /// <summary>Verifies that UpdateUserRole passes the route id and DTO's role through to the service unchanged.</summary>
    [Fact]
    public async Task UpdateUserRole_PassesIdAndRole_ToUserService()
    {
        // Arrange
        var dto = new UpdateUserRoleDtoFactory().WithRole(Roles.Administrator).Build();
        _userServiceMock.Setup(u => u.UpdateUserRoleAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(UpdateUserRoleResult.Success());

        // Act
        await _usersController.UpdateUserRole(_defaultUserId, dto);

        // Assert
        _userServiceMock.Verify(u => u.UpdateUserRoleAsync(_defaultUserId, Roles.Administrator), Times.Once);
    }

    // ---------- GetUsers ----------

    /// <summary>Verifies that GetUsers returns Ok with every user mapped to a UserSummaryResponse.</summary>
    [Fact]
    public async Task GetUsers_WithUsers_ReturnsOkWithMappedUsers()
    {
        // Arrange
        var users = new List<UserSummary>
        {
            new() { Id = "user1", UserName = "user1@example.com", Role = Roles.Editor },
            new() { Id = "user2", UserName = "user2@example.com", Role = null }
        };
        _userServiceMock.Setup(u => u.GetUsersAsync()).ReturnsAsync(users);

        // Act
        var response = await _usersController.GetUsers();

        // Assert
        var result = Assert.IsType<OkObjectResult>(response.Result);
        var body = Assert.IsType<List<UserSummaryResponse>>(result.Value);
        Assert.Equal(2, body.Count);
        Assert.Contains(body, u => u.Id == "user1" && u.UserName == "user1@example.com" && u.Role == Roles.Editor);
        Assert.Contains(body, u => u.Id == "user2" && u.UserName == "user2@example.com" && u.Role == null);
    }

    /// <summary>Verifies that GetUsers with no users returns Ok with an empty list.</summary>
    [Fact]
    public async Task GetUsers_WithNoUsers_ReturnsOkWithEmptyList()
    {
        // Arrange
        _userServiceMock.Setup(u => u.GetUsersAsync()).ReturnsAsync(new List<UserSummary>());

        // Act
        var response = await _usersController.GetUsers();

        // Assert
        var result = Assert.IsType<OkObjectResult>(response.Result);
        var body = Assert.IsType<List<UserSummaryResponse>>(result.Value);
        Assert.Empty(body);
    }
}