using BlogMVC.Controllers;
using BlogMVC.Services;
using BlogMVC.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace BlogMVC.Tests.Controllers;

/// <summary>
///     Unit tests for <see cref="AuthController" /> using mocked <see cref="UserManager{TUser}" />,
///     <see cref="SignInManager{TUser}" /> and <see cref="ITokenService" />.
///     Verify the returned status code/body depending on whether the user exists, the password
///     is correct, and whether the account is locked out.
/// </summary>
public class AuthControllerTests
{
    private readonly AuthController _authController;
    private readonly string _defaultEmail = "test@example.com";
    private readonly string _defaultPassword = "Password123!";
    private readonly string _defaultToken = "fake-jwt-token";

    private readonly IdentityUser _defaultUser = new()
    {
        Id = "defaultUserId",
        Email = "test@example.com",
        UserName = "test@example.com"
    };

    private readonly Mock<SignInManager<IdentityUser>> _signInManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<UserManager<IdentityUser>> _userManagerMock;

    public AuthControllerTests()
    {
        _userManagerMock = CreateUserManagerMock();
        _signInManagerMock = CreateSignInManagerMock(_userManagerMock.Object);
        _tokenServiceMock = new Mock<ITokenService>();

        _authController = new AuthController(
            _userManagerMock.Object,
            _tokenServiceMock.Object,
            _signInManagerMock.Object);
    }

    /// <summary>Builds a mocked <see cref="UserManager{TUser}" /> (it has no parameterless constructor).</summary>
    private static Mock<UserManager<IdentityUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!,
            null!);
    }

    /// <summary>Builds a mocked <see cref="SignInManager{TUser}" /> (it has no parameterless constructor).</summary>
    private static Mock<SignInManager<IdentityUser>> CreateSignInManagerMock(UserManager<IdentityUser> userManager)
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser>>();
        return new Mock<SignInManager<IdentityUser>>(
            userManager, contextAccessor.Object, claimsFactory.Object, null!, null!, null!, null!);
    }

    // ---------- Login ----------

    /// <summary>Verifies that Login with a non-existing email returns Unauthorized.</summary>
    [Fact]
    public async Task Login_WithNonExistingEmail_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDtoFactory().WithEmail(_defaultEmail).Build();
        _userManagerMock.Setup(u => u.FindByEmailAsync(_defaultEmail)).ReturnsAsync((IdentityUser?)null);

        // Act
        var response = await _authController.Login(loginDto);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(response);
    }

    /// <summary>Verifies that Login with a non-existing email does not attempt to sign in.</summary>
    [Fact]
    public async Task Login_WithNonExistingEmail_DoesNotCallCheckPasswordSignInAsync()
    {
        // Arrange
        var loginDto = new LoginDtoFactory().WithEmail(_defaultEmail).Build();
        _userManagerMock.Setup(u => u.FindByEmailAsync(_defaultEmail)).ReturnsAsync((IdentityUser?)null);

        // Act
        await _authController.Login(loginDto);

        // Assert
        _signInManagerMock.Verify(
            s => s.CheckPasswordSignInAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }

    /// <summary>Verifies that Login with a wrong password returns Unauthorized.</summary>
    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDtoFactory().WithEmail(_defaultEmail).WithPassword(_defaultPassword).Build();
        _userManagerMock.Setup(u => u.FindByEmailAsync(_defaultEmail)).ReturnsAsync(_defaultUser);
        _signInManagerMock
            .Setup(s => s.CheckPasswordSignInAsync(_defaultUser, _defaultPassword, true))
            .ReturnsAsync(SignInResult.Failed);

        // Act
        var response = await _authController.Login(loginDto);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(response);
    }

    /// <summary>Verifies that Login when the account is locked out returns Unauthorized.</summary>
    [Fact]
    public async Task Login_WithLockedOutAccount_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDtoFactory().WithEmail(_defaultEmail).WithPassword(_defaultPassword).Build();
        _userManagerMock.Setup(u => u.FindByEmailAsync(_defaultEmail)).ReturnsAsync(_defaultUser);
        _signInManagerMock
            .Setup(s => s.CheckPasswordSignInAsync(_defaultUser, _defaultPassword, true))
            .ReturnsAsync(SignInResult.LockedOut);

        // Act
        var response = await _authController.Login(loginDto);

        // Assert
        var result = Assert.IsType<UnauthorizedObjectResult>(response);
        Assert.Equal("Account is temporarily locked out", result.Value);
    }

    /// <summary>Verifies that Login with valid credentials returns Ok with a JWT token.</summary>
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var loginDto = new LoginDtoFactory().WithEmail(_defaultEmail).WithPassword(_defaultPassword).Build();
        _userManagerMock.Setup(u => u.FindByEmailAsync(_defaultEmail)).ReturnsAsync(_defaultUser);
        _signInManagerMock
            .Setup(s => s.CheckPasswordSignInAsync(_defaultUser, _defaultPassword, true))
            .ReturnsAsync(SignInResult.Success);
        _tokenServiceMock.Setup(t => t.CreateToken(_defaultUser)).Returns(_defaultToken);

        // Act
        var response = await _authController.Login(loginDto);

        // Assert
        var result = Assert.IsType<OkObjectResult>(response);
        var token = result.Value!.GetType().GetProperty("token")!.GetValue(result.Value);
        Assert.Equal(_defaultToken, token);
    }

    /// <summary>Verifies that Login with valid credentials creates the token for the found user.</summary>
    [Fact]
    public async Task Login_WithValidCredentials_CreatesTokenForFoundUser()
    {
        // Arrange
        var loginDto = new LoginDtoFactory().WithEmail(_defaultEmail).WithPassword(_defaultPassword).Build();
        _userManagerMock.Setup(u => u.FindByEmailAsync(_defaultEmail)).ReturnsAsync(_defaultUser);
        _signInManagerMock
            .Setup(s => s.CheckPasswordSignInAsync(_defaultUser, _defaultPassword, true))
            .ReturnsAsync(SignInResult.Success);
        _tokenServiceMock.Setup(t => t.CreateToken(_defaultUser)).Returns(_defaultToken);

        // Act
        await _authController.Login(loginDto);

        // Assert
        _tokenServiceMock.Verify(t => t.CreateToken(_defaultUser), Times.Once);
    }
}