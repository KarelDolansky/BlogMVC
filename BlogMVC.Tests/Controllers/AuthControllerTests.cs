using BlogMVC.Controllers;
using BlogMVC.Dto;
using BlogMVC.Services;
using BlogMVC.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BlogMVC.Tests.Controllers;

/// <summary>
///     Unit tests for <see cref="AuthController" /> using a mocked <see cref="IAuthService" />.
///     Verify that the controller maps each <see cref="LoginResult" /> to the correct status
///     code/body, without any Identity/JWT logic of its own.
/// </summary>
public class AuthControllerTests
{
    private readonly AuthController _authController;
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly string _defaultToken = "fake-jwt-token";

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _authController = new AuthController(_authServiceMock.Object);
    }

    // ---------- Login ----------

    /// <summary>Verifies that Login with invalid credentials returns Unauthorized.</summary>
    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDtoFactory().Build();
        _authServiceMock.Setup(a => a.LoginAsync(loginDto))
            .ReturnsAsync(LoginResult.Failure(LoginFailureReason.InvalidCredentials));

        // Act
        var response = await _authController.Login(loginDto);

        // Assert
        var result = Assert.IsType<UnauthorizedObjectResult>(response);
        Assert.Equal("Invalid email or password", result.Value);
    }

    /// <summary>Verifies that Login when the account is locked out returns Unauthorized with the lockout message.</summary>
    [Fact]
    public async Task Login_WithLockedOutAccount_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDtoFactory().Build();
        _authServiceMock.Setup(a => a.LoginAsync(loginDto))
            .ReturnsAsync(LoginResult.Failure(LoginFailureReason.LockedOut));

        // Act
        var response = await _authController.Login(loginDto);

        // Assert
        var result = Assert.IsType<UnauthorizedObjectResult>(response);
        Assert.Equal("Account is temporarily locked out", result.Value);
    }

    /// <summary>Verifies that Login with valid credentials returns Ok with the JWT token from the service.</summary>
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var loginDto = new LoginDtoFactory().Build();
        _authServiceMock.Setup(a => a.LoginAsync(loginDto))
            .ReturnsAsync(LoginResult.Success(_defaultToken));

        // Act
        var response = await _authController.Login(loginDto);

        // Assert
        var result = Assert.IsType<OkObjectResult>(response);
        var token = result.Value!.GetType().GetProperty("token")!.GetValue(result.Value);
        Assert.Equal(_defaultToken, token);
    }

    /// <summary>Verifies that Login passes the DTO through to the service unchanged.</summary>
    [Fact]
    public async Task Login_PassesLoginDto_ToAuthService()
    {
        // Arrange
        var loginDto = new LoginDtoFactory().WithEmail("someone@example.com").Build();
        _authServiceMock.Setup(a => a.LoginAsync(It.IsAny<LoginDto>()))
            .ReturnsAsync(LoginResult.Success(_defaultToken));

        // Act
        await _authController.Login(loginDto);

        // Assert
        _authServiceMock.Verify(a => a.LoginAsync(loginDto), Times.Once);
    }
}