using BlogMVC.Infrastructure.Interfaces;
using BlogMVC.Services;
using BlogMVC.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace BlogMVC.Tests.Services;

/// <summary>
///     Unit tests for <see cref="AuthService" /> using mocked <see cref="UserManager{TUser}" />,
///     <see cref="SignInManager{TUser}" /> and <see cref="ITokenProvider" />. Verify the returned
///     <see cref="LoginResult" /> depending on whether the user exists, the password is correct,
///     and whether the account is locked out.
/// </summary>
public class AuthServiceTests
{
    private readonly AuthService _authService;
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
    private readonly Mock<ITokenProvider> _tokenProviderMock;
    private readonly Mock<UserManager<IdentityUser>> _userManagerMock;

    public AuthServiceTests()
    {
        _userManagerMock = CreateUserManagerMock();
        _signInManagerMock = CreateSignInManagerMock(_userManagerMock.Object);
        _tokenProviderMock = new Mock<ITokenProvider>();

        _authService = new AuthService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _tokenProviderMock.Object);
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

    // ---------- LoginAsync ----------

    /// <summary>Verifies that LoginAsync with a non-existing email fails with InvalidCredentials.</summary>
    [Fact]
    public async Task LoginAsync_WithNonExistingEmail_FailsWithInvalidCredentials()
    {
        // Arrange
        var loginDto = new LoginDtoFactory().WithEmail(_defaultEmail).Build();
        _userManagerMock.Setup(u => u.FindByEmailAsync(_defaultEmail)).ReturnsAsync((IdentityUser?)null);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(LoginFailureReason.InvalidCredentials, result.FailureReason);
    }

    /// <summary>Verifies that LoginAsync with a non-existing email does not attempt to sign in.</summary>
    [Fact]
    public async Task LoginAsync_WithNonExistingEmail_DoesNotCallCheckPasswordSignInAsync()
    {
        // Arrange
        var loginDto = new LoginDtoFactory().WithEmail(_defaultEmail).Build();
        _userManagerMock.Setup(u => u.FindByEmailAsync(_defaultEmail)).ReturnsAsync((IdentityUser?)null);

        // Act
        await _authService.LoginAsync(loginDto);

        // Assert
        _signInManagerMock.Verify(
            s => s.CheckPasswordSignInAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }

    /// <summary>Verifies that LoginAsync with a wrong password fails with InvalidCredentials.</summary>
    [Fact]
    public async Task LoginAsync_WithWrongPassword_FailsWithInvalidCredentials()
    {
        // Arrange
        var loginDto = new LoginDtoFactory().WithEmail(_defaultEmail).WithPassword(_defaultPassword).Build();
        _userManagerMock.Setup(u => u.FindByEmailAsync(_defaultEmail)).ReturnsAsync(_defaultUser);
        _signInManagerMock
            .Setup(s => s.CheckPasswordSignInAsync(_defaultUser, _defaultPassword, true))
            .ReturnsAsync(SignInResult.Failed);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(LoginFailureReason.InvalidCredentials, result.FailureReason);
    }

    /// <summary>Verifies that LoginAsync when the account is locked out fails with LockedOut.</summary>
    [Fact]
    public async Task LoginAsync_WithLockedOutAccount_FailsWithLockedOut()
    {
        // Arrange
        var loginDto = new LoginDtoFactory().WithEmail(_defaultEmail).WithPassword(_defaultPassword).Build();
        _userManagerMock.Setup(u => u.FindByEmailAsync(_defaultEmail)).ReturnsAsync(_defaultUser);
        _signInManagerMock
            .Setup(s => s.CheckPasswordSignInAsync(_defaultUser, _defaultPassword, true))
            .ReturnsAsync(SignInResult.LockedOut);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(LoginFailureReason.LockedOut, result.FailureReason);
    }

    /// <summary>Verifies that LoginAsync checks the password with lockout tracking enabled.</summary>
    [Fact]
    public async Task LoginAsync_ChecksPassword_WithLockoutOnFailureEnabled()
    {
        // Arrange
        var loginDto = new LoginDtoFactory().WithEmail(_defaultEmail).WithPassword(_defaultPassword).Build();
        _userManagerMock.Setup(u => u.FindByEmailAsync(_defaultEmail)).ReturnsAsync(_defaultUser);
        _signInManagerMock
            .Setup(s => s.CheckPasswordSignInAsync(_defaultUser, _defaultPassword, true))
            .ReturnsAsync(SignInResult.Success);
        _tokenProviderMock.Setup(t => t.CreateToken(_defaultUser)).Returns(_defaultToken);

        // Act
        await _authService.LoginAsync(loginDto);

        // Assert
        _signInManagerMock.Verify(s => s.CheckPasswordSignInAsync(_defaultUser, _defaultPassword, true), Times.Once);
    }

    /// <summary>Verifies that LoginAsync with valid credentials succeeds with a JWT token.</summary>
    [Fact]
    public async Task LoginAsync_WithValidCredentials_SucceedsWithToken()
    {
        // Arrange
        var loginDto = new LoginDtoFactory().WithEmail(_defaultEmail).WithPassword(_defaultPassword).Build();
        _userManagerMock.Setup(u => u.FindByEmailAsync(_defaultEmail)).ReturnsAsync(_defaultUser);
        _signInManagerMock
            .Setup(s => s.CheckPasswordSignInAsync(_defaultUser, _defaultPassword, true))
            .ReturnsAsync(SignInResult.Success);
        _tokenProviderMock.Setup(t => t.CreateToken(_defaultUser)).Returns(_defaultToken);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(_defaultToken, result.Token);
        Assert.Null(result.FailureReason);
    }

    /// <summary>Verifies that LoginAsync with valid credentials creates the token for the found user.</summary>
    [Fact]
    public async Task LoginAsync_WithValidCredentials_CreatesTokenForFoundUser()
    {
        // Arrange
        var loginDto = new LoginDtoFactory().WithEmail(_defaultEmail).WithPassword(_defaultPassword).Build();
        _userManagerMock.Setup(u => u.FindByEmailAsync(_defaultEmail)).ReturnsAsync(_defaultUser);
        _signInManagerMock
            .Setup(s => s.CheckPasswordSignInAsync(_defaultUser, _defaultPassword, true))
            .ReturnsAsync(SignInResult.Success);
        _tokenProviderMock.Setup(t => t.CreateToken(_defaultUser)).Returns(_defaultToken);

        // Act
        await _authService.LoginAsync(loginDto);

        // Assert
        _tokenProviderMock.Verify(t => t.CreateToken(_defaultUser), Times.Once);
    }

    // ---------- RegisterAsync ----------

    /// <summary>Verifies that RegisterAsync with valid data creates the Identity user with email as username.</summary>
    [Fact]
    public async Task RegisterAsync_WithValidData_CreatesUserWithEmailAsUserName()
    {
        // Arrange
        var registerDto = new RegisterDtoFactory().WithEmail(_defaultEmail).WithPassword(_defaultPassword).Build();
        _userManagerMock
            .Setup(u => u.CreateAsync(It.IsAny<IdentityUser>(), _defaultPassword))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _authService.RegisterAsync(registerDto);

        // Assert
        _userManagerMock.Verify(
            u => u.CreateAsync(
                It.Is<IdentityUser>(user => user.Email == _defaultEmail && user.UserName == _defaultEmail),
                _defaultPassword),
            Times.Once);
    }

    /// <summary>Verifies that RegisterAsync with valid data succeeds.</summary>
    [Fact]
    public async Task RegisterAsync_WithValidData_Succeeds()
    {
        // Arrange
        var registerDto = new RegisterDtoFactory().Build();
        _userManagerMock
            .Setup(u => u.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Null(result.Errors);
    }

    /// <summary>Verifies that RegisterAsync locks the newly created account out indefinitely.</summary>
    [Fact]
    public async Task RegisterAsync_WithValidData_LocksTheAccount()
    {
        // Arrange
        var registerDto = new RegisterDtoFactory().Build();
        IdentityUser? createdUser = null;
        _userManagerMock
            .Setup(u => u.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
            .Callback<IdentityUser, string>((user, _) => createdUser = user)
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock
            .Setup(u => u.SetLockoutEnabledAsync(It.IsAny<IdentityUser>(), true))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock
            .Setup(u => u.SetLockoutEndDateAsync(It.IsAny<IdentityUser>(), DateTimeOffset.MaxValue))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _authService.RegisterAsync(registerDto);

        // Assert
        _userManagerMock.Verify(u => u.SetLockoutEnabledAsync(createdUser!, true), Times.Once);
        _userManagerMock.Verify(u => u.SetLockoutEndDateAsync(createdUser!, DateTimeOffset.MaxValue), Times.Once);
    }

    /// <summary>Verifies that RegisterAsync when account creation fails (e.g. duplicate email) returns the Identity errors.</summary>
    [Fact]
    public async Task RegisterAsync_WhenCreateFails_ReturnsFailureWithErrors()
    {
        // Arrange
        var registerDto = new RegisterDtoFactory().Build();
        var error = new IdentityError
            { Code = "DuplicateEmail", Description = "Email 'test@example.com' is already taken." };
        _userManagerMock
            .Setup(u => u.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(error));

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(error.Description, result.Errors!);
    }

    /// <summary>Verifies that RegisterAsync when account creation fails does not lock out a (non-existent) account.</summary>
    [Fact]
    public async Task RegisterAsync_WhenCreateFails_DoesNotAttemptToLockAccount()
    {
        // Arrange
        var registerDto = new RegisterDtoFactory().Build();
        var error = new IdentityError { Code = "DuplicateEmail", Description = "Email already taken." };
        _userManagerMock
            .Setup(u => u.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(error));

        // Act
        await _authService.RegisterAsync(registerDto);

        // Assert
        _userManagerMock.Verify(u => u.SetLockoutEnabledAsync(It.IsAny<IdentityUser>(), It.IsAny<bool>()), Times.Never);
        _userManagerMock.Verify(
            u => u.SetLockoutEndDateAsync(It.IsAny<IdentityUser>(), It.IsAny<DateTimeOffset?>()), Times.Never);
    }
}