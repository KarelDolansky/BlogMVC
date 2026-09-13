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
///     Unit tests for <see cref="RolesController" /> using a mocked <see cref="IRoleService" />. Verify that
///     the controller maps each result to the correct status code/body, without any Identity logic of its own.
/// </summary>
public class RolesControllerTests
{
    /// <summary>Role name used across tests.</summary>
    private const string DefaultRoleName = "CustomRole";

    /// <summary>Mocked <see cref="IRoleService" /> used to stub outcomes.</summary>
    private readonly Mock<IRoleService> _roleServiceMock;

    /// <summary>The controller under test, wired to the mocked <see cref="IRoleService" />.</summary>
    private readonly RolesController _rolesController;

    /// <summary>Creates the controller under test with a fresh <see cref="IRoleService" /> mock.</summary>
    public RolesControllerTests()
    {
        _roleServiceMock = new Mock<IRoleService>();
        _rolesController = new RolesController(_roleServiceMock.Object);
    }

    // ---------- GetRoles ----------

    /// <summary>Verifies that GetRoles returns Ok with every role mapped to a RoleResponse.</summary>
    [Fact]
    public async Task GetRoles_WithRoles_ReturnsOkWithMappedRoles()
    {
        // Arrange
        var roles = new List<RoleSummary>
        {
            new() { Name = Roles.Editor, Permissions = [Permissions.Posts.Create] },
            new() { Name = DefaultRoleName, Permissions = [] }
        };
        _roleServiceMock.Setup(r => r.GetRolesAsync()).ReturnsAsync(roles);

        // Act
        var response = await _rolesController.GetRoles();

        // Assert
        var result = Assert.IsType<OkObjectResult>(response.Result);
        var body = Assert.IsType<List<RoleResponse>>(result.Value);
        Assert.Equal(2, body.Count);
        Assert.Contains(body, r => r.Name == Roles.Editor && r.Permissions.Contains(Permissions.Posts.Create));
    }

    // ---------- GetPermissions ----------

    /// <summary>Verifies that GetPermissions returns Ok with the full permission catalog.</summary>
    [Fact]
    public void GetPermissions_ReturnsOkWithAllPermissions()
    {
        // Act
        var response = _rolesController.GetPermissions();

        // Assert
        var result = Assert.IsType<OkObjectResult>(response.Result);
        var body = Assert.IsType<PermissionsResponse>(result.Value);
        Assert.Equal(Permissions.All, body.Permissions);
    }

    // ---------- GetRole ----------

    /// <summary>Verifies that GetRole for an existing role returns Ok with its mapped response.</summary>
    [Fact]
    public async Task GetRole_ExistingRole_ReturnsOkWithRole()
    {
        // Arrange
        var role = new RoleSummary { Name = DefaultRoleName, Permissions = [Permissions.Posts.Create] };
        _roleServiceMock.Setup(r => r.GetRoleAsync(DefaultRoleName)).ReturnsAsync(role);

        // Act
        var response = await _rolesController.GetRole(DefaultRoleName);

        // Assert
        var result = Assert.IsType<OkObjectResult>(response.Result);
        var body = Assert.IsType<RoleResponse>(result.Value);
        Assert.Equal(DefaultRoleName, body.Name);
    }

    /// <summary>Verifies that GetRole for a non-existing role returns NotFound.</summary>
    [Fact]
    public async Task GetRole_NonExistingRole_ReturnsNotFound()
    {
        // Arrange
        _roleServiceMock.Setup(r => r.GetRoleAsync(DefaultRoleName)).ReturnsAsync((RoleSummary?)null);

        // Act
        var response = await _rolesController.GetRole(DefaultRoleName);

        // Assert
        Assert.IsType<NotFoundResult>(response.Result);
    }

    // ---------- CreateRole ----------

    /// <summary>Verifies that CreateRole with a duplicate name returns Conflict.</summary>
    [Fact]
    public async Task CreateRole_DuplicateName_ReturnsConflict()
    {
        // Arrange
        var dto = new CreateRoleDtoFactory().WithName(DefaultRoleName).Build();
        _roleServiceMock.Setup(r => r.CreateRoleAsync(dto.Name, dto.Permissions))
            .ReturnsAsync(CreateRoleResult.Failure(CreateRoleFailureReason.DuplicateName));

        // Act
        var response = await _rolesController.CreateRole(dto);

        // Assert
        Assert.IsType<ConflictObjectResult>(response.Result);
    }

    /// <summary>Verifies that CreateRole with an unrecognized permission returns BadRequest.</summary>
    [Fact]
    public async Task CreateRole_InvalidPermission_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateRoleDtoFactory().WithName(DefaultRoleName).WithPermissions("NotAPermission").Build();
        _roleServiceMock.Setup(r => r.CreateRoleAsync(dto.Name, dto.Permissions))
            .ReturnsAsync(CreateRoleResult.Failure(CreateRoleFailureReason.InvalidPermission));

        // Act
        var response = await _rolesController.CreateRole(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    /// <summary>Verifies that CreateRole with valid data returns 201 Created with the new role.</summary>
    [Fact]
    public async Task CreateRole_WithValidData_ReturnsCreatedWithRole()
    {
        // Arrange
        var dto = new CreateRoleDtoFactory().WithName(DefaultRoleName).WithPermissions(Permissions.Posts.Create)
            .Build();
        var created = new RoleSummary { Name = DefaultRoleName, Permissions = dto.Permissions };
        _roleServiceMock.Setup(r => r.CreateRoleAsync(dto.Name, dto.Permissions))
            .ReturnsAsync(CreateRoleResult.Success(created));

        // Act
        var response = await _rolesController.CreateRole(dto);

        // Assert
        var result = Assert.IsType<CreatedAtRouteResult>(response.Result);
        var body = Assert.IsType<RoleResponse>(result.Value);
        Assert.Equal(DefaultRoleName, body.Name);
    }

    // ---------- UpdateRolePermissions ----------

    /// <summary>Verifies that UpdateRolePermissions for a non-existing role returns NotFound.</summary>
    [Fact]
    public async Task UpdateRolePermissions_NonExistingRole_ReturnsNotFound()
    {
        // Arrange
        var dto = new UpdateRolePermissionsDtoFactory().Build();
        _roleServiceMock.Setup(r => r.UpdateRolePermissionsAsync(DefaultRoleName, dto.Permissions))
            .ReturnsAsync(UpdateRolePermissionsResult.Failure(UpdateRolePermissionsFailureReason.RoleNotFound));

        // Act
        var response = await _rolesController.UpdateRolePermissions(DefaultRoleName, dto);

        // Assert
        Assert.IsType<NotFoundResult>(response.Result);
    }

    /// <summary>Verifies that UpdateRolePermissions with an unrecognized permission returns BadRequest.</summary>
    [Fact]
    public async Task UpdateRolePermissions_InvalidPermission_ReturnsBadRequest()
    {
        // Arrange
        var dto = new UpdateRolePermissionsDtoFactory().WithPermissions("NotAPermission").Build();
        _roleServiceMock.Setup(r => r.UpdateRolePermissionsAsync(DefaultRoleName, dto.Permissions))
            .ReturnsAsync(UpdateRolePermissionsResult.Failure(UpdateRolePermissionsFailureReason.InvalidPermission));

        // Act
        var response = await _rolesController.UpdateRolePermissions(DefaultRoleName, dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    /// <summary>Verifies that UpdateRolePermissions with valid data returns Ok with the role's new permissions.</summary>
    [Fact]
    public async Task UpdateRolePermissions_WithValidData_ReturnsOkWithUpdatedRole()
    {
        // Arrange
        var dto = new UpdateRolePermissionsDtoFactory().WithPermissions(Permissions.Posts.DeleteOwn).Build();
        var updated = new RoleSummary { Name = DefaultRoleName, Permissions = dto.Permissions };
        _roleServiceMock.Setup(r => r.UpdateRolePermissionsAsync(DefaultRoleName, dto.Permissions))
            .ReturnsAsync(UpdateRolePermissionsResult.Success(updated));

        // Act
        var response = await _rolesController.UpdateRolePermissions(DefaultRoleName, dto);

        // Assert
        var result = Assert.IsType<OkObjectResult>(response.Result);
        var body = Assert.IsType<RoleResponse>(result.Value);
        Assert.Equal([Permissions.Posts.DeleteOwn], body.Permissions);
    }

    // ---------- DeleteRole ----------

    /// <summary>Verifies that DeleteRole for a non-existing role returns NotFound.</summary>
    [Fact]
    public async Task DeleteRole_NonExistingRole_ReturnsNotFound()
    {
        // Arrange
        _roleServiceMock.Setup(r => r.DeleteRoleAsync(DefaultRoleName))
            .ReturnsAsync(DeleteRoleResult.Failure(DeleteRoleFailureReason.RoleNotFound));

        // Act
        var response = await _rolesController.DeleteRole(DefaultRoleName);

        // Assert
        Assert.IsType<NotFoundResult>(response);
    }

    /// <summary>Verifies that DeleteRole for a role still in use returns Conflict.</summary>
    [Fact]
    public async Task DeleteRole_RoleInUse_ReturnsConflict()
    {
        // Arrange
        _roleServiceMock.Setup(r => r.DeleteRoleAsync(DefaultRoleName))
            .ReturnsAsync(DeleteRoleResult.Failure(DeleteRoleFailureReason.RoleInUse));

        // Act
        var response = await _rolesController.DeleteRole(DefaultRoleName);

        // Assert
        Assert.IsType<ConflictObjectResult>(response);
    }

    /// <summary>Verifies that DeleteRole for an unused, existing role returns NoContent.</summary>
    [Fact]
    public async Task DeleteRole_UnusedExistingRole_ReturnsNoContent()
    {
        // Arrange
        _roleServiceMock.Setup(r => r.DeleteRoleAsync(DefaultRoleName)).ReturnsAsync(DeleteRoleResult.Success());

        // Act
        var response = await _rolesController.DeleteRole(DefaultRoleName);

        // Assert
        Assert.IsType<NoContentResult>(response);
    }
}