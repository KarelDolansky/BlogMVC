using System.Security.Claims;
using BlogMVC.Data;
using BlogMVC.Results;
using BlogMVC.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BlogMVC.Tests.Services;

/// <summary>
///     Unit tests for <see cref="RoleService" /> using mocked <see cref="RoleManager{TRole}" /> and
///     <see cref="UserManager{TUser}" />, plus a real in-memory SQLite <see cref="ApplicationDbContext" /> —
///     needed only so <see cref="RoleService" />'s explicit transactions (<c>Database.BeginTransactionAsync</c>)
///     have a real connection to open one on; Identity operations themselves stay mocked. Verify role
///     creation/lookup, permission validation, permission replacement, and the "can't delete a role in use"
///     guard.
/// </summary>
public class RoleServiceTests : IDisposable
{
    /// <summary>Name used across tests for the role under test.</summary>
    private const string DefaultRoleName = "CustomRole";

    /// <summary>In-memory SQLite connection backing <see cref="_dbContext" />, kept open for the test's lifetime.</summary>
    private readonly SqliteConnection _connection;

    /// <summary>
    ///     Real (not mocked) <see cref="ApplicationDbContext" />, so <see cref="RoleService" /> can open a real
    ///     transaction.
    /// </summary>
    private readonly ApplicationDbContext _dbContext;

    /// <summary>Identity role returned by the mocked <see cref="RoleManager{TRole}" /> in most tests.</summary>
    private readonly IdentityRole _defaultRole = new(DefaultRoleName) { Id = "roleId" };

    /// <summary>Mocked <see cref="RoleManager{TRole}" /> passed to <see cref="_roleService" />.</summary>
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;

    /// <summary>System under test, constructed with mocked Identity managers and a real in-memory DbContext.</summary>
    private readonly RoleService _roleService;

    /// <summary>Mocked <see cref="UserManager{TUser}" /> passed to <see cref="_roleService" />.</summary>
    private readonly Mock<UserManager<IdentityUser>> _userManagerMock;

    /// <summary>Builds <see cref="_roleService" /> with fresh mocks and a fresh in-memory database for each test.</summary>
    public RoleServiceTests()
    {
        _roleManagerMock = CreateRoleManagerMock();
        _userManagerMock = CreateUserManagerMock();

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _dbContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
        _dbContext.Database.EnsureCreated();

        _roleService = new RoleService(_roleManagerMock.Object, _userManagerMock.Object, _dbContext);
    }

    /// <summary>Closes the in-memory database connection after the test.</summary>
    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    /// <summary>Builds a mocked <see cref="RoleManager{TRole}" /> (it has no parameterless constructor).</summary>
    /// <returns>A mock with a mocked <see cref="IRoleStore{TRole}" /> and null dependencies otherwise.</returns>
    private static Mock<RoleManager<IdentityRole>> CreateRoleManagerMock()
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        return new Mock<RoleManager<IdentityRole>>(store.Object, null!, null!, null!, null!);
    }

    /// <summary>Builds a mocked <see cref="UserManager{TUser}" /> (it has no parameterless constructor).</summary>
    /// <returns>A mock with a mocked <see cref="IUserStore{TUser}" /> and null dependencies otherwise.</returns>
    private static Mock<UserManager<IdentityUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!,
            null!);
    }

    // ---------- GetRolesAsync ----------

    /// <summary>Verifies that GetRolesAsync maps every role to a summary carrying its permission claims.</summary>
    [Fact]
    public async Task GetRolesAsync_WithRoles_ReturnsSummariesWithPermissions()
    {
        // Arrange
        _roleManagerMock.Setup(r => r.Roles).Returns(new[] { _defaultRole }.AsQueryable());
        _roleManagerMock.Setup(r => r.GetClaimsAsync(_defaultRole)).ReturnsAsync(new List<Claim>
        {
            new(Permissions.ClaimType, Permissions.Posts.Create)
        });

        // Act
        var result = await _roleService.GetRolesAsync();

        // Assert
        var role = Assert.Single(result);
        Assert.Equal(DefaultRoleName, role.Name);
        Assert.Equal([Permissions.Posts.Create], role.Permissions);
    }

    /// <summary>Verifies that GetRolesAsync with no roles returns an empty list.</summary>
    [Fact]
    public async Task GetRolesAsync_NoRoles_ReturnsEmptyList()
    {
        // Arrange
        _roleManagerMock.Setup(r => r.Roles).Returns(Array.Empty<IdentityRole>().AsQueryable());

        // Act
        var result = await _roleService.GetRolesAsync();

        // Assert
        Assert.Empty(result);
    }

    // ---------- GetRoleAsync ----------

    /// <summary>Verifies that GetRoleAsync for an existing role returns its summary.</summary>
    [Fact]
    public async Task GetRoleAsync_ExistingRole_ReturnsSummary()
    {
        // Arrange
        _roleManagerMock.Setup(r => r.FindByNameAsync(DefaultRoleName)).ReturnsAsync(_defaultRole);
        _roleManagerMock.Setup(r => r.GetClaimsAsync(_defaultRole)).ReturnsAsync(new List<Claim>());

        // Act
        var result = await _roleService.GetRoleAsync(DefaultRoleName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DefaultRoleName, result.Name);
    }

    /// <summary>Verifies that GetRoleAsync for a non-existing role returns null.</summary>
    [Fact]
    public async Task GetRoleAsync_NonExistingRole_ReturnsNull()
    {
        // Arrange
        _roleManagerMock.Setup(r => r.FindByNameAsync("NotARole")).ReturnsAsync((IdentityRole?)null);

        // Act
        var result = await _roleService.GetRoleAsync("NotARole");

        // Assert
        Assert.Null(result);
    }

    // ---------- CreateRoleAsync ----------

    /// <summary>Verifies that CreateRoleAsync with a name already in use fails with DuplicateName.</summary>
    [Fact]
    public async Task CreateRoleAsync_DuplicateName_FailsWithDuplicateName()
    {
        // Arrange
        _roleManagerMock.Setup(r => r.RoleExistsAsync(DefaultRoleName)).ReturnsAsync(true);

        // Act
        var result = await _roleService.CreateRoleAsync(DefaultRoleName, []);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(CreateRoleFailureReason.DuplicateName, result.FailureReason);
    }

    /// <summary>Verifies that CreateRoleAsync with an unrecognized permission fails with InvalidPermission.</summary>
    [Fact]
    public async Task CreateRoleAsync_UnrecognizedPermission_FailsWithInvalidPermission()
    {
        // Arrange
        _roleManagerMock.Setup(r => r.RoleExistsAsync(DefaultRoleName)).ReturnsAsync(false);

        // Act
        var result = await _roleService.CreateRoleAsync(DefaultRoleName, ["NotAPermission"]);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(CreateRoleFailureReason.InvalidPermission, result.FailureReason);
    }

    /// <summary>Verifies that CreateRoleAsync with valid data creates the role and grants each requested permission.</summary>
    [Fact]
    public async Task CreateRoleAsync_WithValidData_CreatesRoleAndGrantsPermissions()
    {
        // Arrange
        _roleManagerMock.Setup(r => r.RoleExistsAsync(DefaultRoleName)).ReturnsAsync(false);
        _roleManagerMock.Setup(r => r.CreateAsync(It.IsAny<IdentityRole>())).ReturnsAsync(IdentityResult.Success);
        _roleManagerMock
            .Setup(r => r.AddClaimAsync(It.IsAny<IdentityRole>(), It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _roleService.CreateRoleAsync(DefaultRoleName, [Permissions.Posts.Create]);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(DefaultRoleName, result.Role!.Name);
        Assert.Equal([Permissions.Posts.Create], result.Role.Permissions);
        _roleManagerMock.Verify(
            r => r.AddClaimAsync(It.Is<IdentityRole>(role => role.Name == DefaultRoleName),
                It.Is<Claim>(c => c.Type == Permissions.ClaimType && c.Value == Permissions.Posts.Create)),
            Times.Once);
    }

    /// <summary>Verifies that CreateRoleAsync trims leading/trailing whitespace from the name before using it.</summary>
    [Fact]
    public async Task CreateRoleAsync_NameWithSurroundingWhitespace_TrimsBeforeCreating()
    {
        // Arrange
        _roleManagerMock.Setup(r => r.RoleExistsAsync(DefaultRoleName)).ReturnsAsync(false);
        _roleManagerMock.Setup(r => r.CreateAsync(It.IsAny<IdentityRole>())).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _roleService.CreateRoleAsync($"  {DefaultRoleName}  ", []);

        // Assert
        Assert.Equal(DefaultRoleName, result.Role!.Name);
        _roleManagerMock.Verify(r => r.RoleExistsAsync(DefaultRoleName), Times.Once);
        _roleManagerMock.Verify(r => r.CreateAsync(It.Is<IdentityRole>(role => role.Name == DefaultRoleName)),
            Times.Once);
    }

    /// <summary>Verifies that CreateRoleAsync maps a CreateAsync failure to DuplicateName instead of a misleading success.</summary>
    [Fact]
    public async Task CreateRoleAsync_WhenCreateAsyncFails_ReturnsDuplicateNameFailure()
    {
        // Arrange
        // Simulates the TOCTOU race the initial RoleExistsAsync check can't fully close: a role with the same
        // normalized name slipped in between our check and the create.
        _roleManagerMock.Setup(r => r.RoleExistsAsync(DefaultRoleName)).ReturnsAsync(false);
        _roleManagerMock.Setup(r => r.CreateAsync(It.IsAny<IdentityRole>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role name already taken." }));

        // Act
        var result = await _roleService.CreateRoleAsync(DefaultRoleName, [Permissions.Posts.Create]);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(CreateRoleFailureReason.DuplicateName, result.FailureReason);
        _roleManagerMock.Verify(r => r.AddClaimAsync(It.IsAny<IdentityRole>(), It.IsAny<Claim>()), Times.Never);
    }

    /// <summary>
    ///     Verifies that a DbUpdateException from CreateAsync is caught and mapped to DuplicateName rather than
    ///     propagating as an unhandled 500.
    /// </summary>
    [Fact]
    public async Task CreateRoleAsync_WhenCreateAsyncThrowsDbUpdateException_ReturnsDuplicateNameFailure()
    {
        // Arrange
        // Simulates the same race surfacing as a DB-level exception instead of a failed IdentityResult: the
        // unique index on the role's normalized name rejecting a concurrent duplicate insert.
        _roleManagerMock.Setup(r => r.RoleExistsAsync(DefaultRoleName)).ReturnsAsync(false);
        _roleManagerMock.Setup(r => r.CreateAsync(It.IsAny<IdentityRole>()))
            .ThrowsAsync(new DbUpdateException("UNIQUE constraint failed: AspNetRoles.NormalizedName"));

        // Act
        var result = await _roleService.CreateRoleAsync(DefaultRoleName, []);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(CreateRoleFailureReason.DuplicateName, result.FailureReason);
    }

    // ---------- UpdateRolePermissionsAsync ----------

    /// <summary>Verifies that UpdateRolePermissionsAsync for a non-existing role fails with RoleNotFound.</summary>
    [Fact]
    public async Task UpdateRolePermissionsAsync_NonExistingRole_FailsWithRoleNotFound()
    {
        // Arrange
        _roleManagerMock.Setup(r => r.FindByNameAsync(DefaultRoleName)).ReturnsAsync((IdentityRole?)null);

        // Act
        var result = await _roleService.UpdateRolePermissionsAsync(DefaultRoleName, []);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(UpdateRolePermissionsFailureReason.RoleNotFound, result.FailureReason);
    }

    /// <summary>Verifies that UpdateRolePermissionsAsync with an unrecognized permission fails with InvalidPermission.</summary>
    [Fact]
    public async Task UpdateRolePermissionsAsync_UnrecognizedPermission_FailsWithInvalidPermission()
    {
        // Arrange
        _roleManagerMock.Setup(r => r.FindByNameAsync(DefaultRoleName)).ReturnsAsync(_defaultRole);

        // Act
        var result = await _roleService.UpdateRolePermissionsAsync(DefaultRoleName, ["NotAPermission"]);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(UpdateRolePermissionsFailureReason.InvalidPermission, result.FailureReason);
    }

    /// <summary>
    ///     Verifies that UpdateRolePermissionsAsync removes every existing permission claim and adds the newly
    ///     requested set.
    /// </summary>
    [Fact]
    public async Task UpdateRolePermissionsAsync_WithValidData_ReplacesPermissionClaims()
    {
        // Arrange
        var existingClaim = new Claim(Permissions.ClaimType, Permissions.Posts.EditOwn);
        _roleManagerMock.Setup(r => r.FindByNameAsync(DefaultRoleName)).ReturnsAsync(_defaultRole);
        _roleManagerMock.Setup(r => r.GetClaimsAsync(_defaultRole)).ReturnsAsync(new List<Claim> { existingClaim });
        _roleManagerMock.Setup(r => r.RemoveClaimAsync(_defaultRole, existingClaim))
            .ReturnsAsync(IdentityResult.Success);
        _roleManagerMock
            .Setup(r => r.AddClaimAsync(_defaultRole, It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _roleService.UpdateRolePermissionsAsync(DefaultRoleName, [Permissions.Posts.DeleteOwn]);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal([Permissions.Posts.DeleteOwn], result.Role!.Permissions);
        _roleManagerMock.Verify(r => r.RemoveClaimAsync(_defaultRole, existingClaim), Times.Once);
        _roleManagerMock.Verify(
            r => r.AddClaimAsync(_defaultRole,
                It.Is<Claim>(c => c.Type == Permissions.ClaimType && c.Value == Permissions.Posts.DeleteOwn)),
            Times.Once);
    }

    /// <summary>Verifies that a failed RemoveClaimAsync stops the new permissions from ever being added.</summary>
    [Fact]
    public async Task UpdateRolePermissionsAsync_WhenRemoveClaimFails_NeverAddsNewPermissions()
    {
        // Arrange
        // The exception should propagate instead of leaving the role with a partial mix of old and new claims —
        // the transaction wrapping the whole remove+add sequence then rolls back on disposal.
        var existingClaim = new Claim(Permissions.ClaimType, Permissions.Posts.EditOwn);
        _roleManagerMock.Setup(r => r.FindByNameAsync(DefaultRoleName)).ReturnsAsync(_defaultRole);
        _roleManagerMock.Setup(r => r.GetClaimsAsync(_defaultRole)).ReturnsAsync(new List<Claim> { existingClaim });
        _roleManagerMock.Setup(r => r.RemoveClaimAsync(_defaultRole, existingClaim))
            .ThrowsAsync(new DbUpdateException("simulated failure"));

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            _roleService.UpdateRolePermissionsAsync(DefaultRoleName, [Permissions.Posts.DeleteOwn]));
        _roleManagerMock.Verify(r => r.AddClaimAsync(It.IsAny<IdentityRole>(), It.IsAny<Claim>()), Times.Never);
    }

    // ---------- DeleteRoleAsync ----------

    /// <summary>Verifies that DeleteRoleAsync for a non-existing role fails with RoleNotFound.</summary>
    [Fact]
    public async Task DeleteRoleAsync_NonExistingRole_FailsWithRoleNotFound()
    {
        // Arrange
        _roleManagerMock.Setup(r => r.FindByNameAsync(DefaultRoleName)).ReturnsAsync((IdentityRole?)null);

        // Act
        var result = await _roleService.DeleteRoleAsync(DefaultRoleName);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(DeleteRoleFailureReason.RoleNotFound, result.FailureReason);
    }

    /// <summary>Verifies that DeleteRoleAsync for a role still held by a user fails with RoleInUse.</summary>
    [Fact]
    public async Task DeleteRoleAsync_RoleStillInUse_FailsWithRoleInUse()
    {
        // Arrange
        _roleManagerMock.Setup(r => r.FindByNameAsync(DefaultRoleName)).ReturnsAsync(_defaultRole);
        _userManagerMock.Setup(u => u.GetUsersInRoleAsync(DefaultRoleName))
            .ReturnsAsync(new List<IdentityUser> { new() { Id = "someUserId" } });

        // Act
        var result = await _roleService.DeleteRoleAsync(DefaultRoleName);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(DeleteRoleFailureReason.RoleInUse, result.FailureReason);
        _roleManagerMock.Verify(r => r.DeleteAsync(It.IsAny<IdentityRole>()), Times.Never);
    }

    /// <summary>Verifies that DeleteRoleAsync for an unused, existing role succeeds and deletes it.</summary>
    [Fact]
    public async Task DeleteRoleAsync_UnusedExistingRole_Succeeds()
    {
        // Arrange
        _roleManagerMock.Setup(r => r.FindByNameAsync(DefaultRoleName)).ReturnsAsync(_defaultRole);
        _userManagerMock.Setup(u => u.GetUsersInRoleAsync(DefaultRoleName)).ReturnsAsync(new List<IdentityUser>());
        _roleManagerMock.Setup(r => r.DeleteAsync(_defaultRole)).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _roleService.DeleteRoleAsync(DefaultRoleName);

        // Assert
        Assert.True(result.Succeeded);
        _roleManagerMock.Verify(r => r.DeleteAsync(_defaultRole), Times.Once);
    }
}