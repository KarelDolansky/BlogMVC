using BlogMVC.Controllers;
using BlogMVC.Dto;
using BlogMVC.Responses;
using BlogMVC.Results;
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
        var result = Assert.IsType<UnauthorizedObjectResult>(response.Result);
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
        var result = Assert.IsType<UnauthorizedObjectResult>(response.Result);
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
        var result = Assert.IsType<OkObjectResult>(response.Result);
        var tokenResponse = Assert.IsType<TokenResponse>(result.Value);
        Assert.Equal(_defaultToken, tokenResponse.Token);
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

    // ---------- Register ----------

    /// <summary>Verifies that Register with valid data returns Ok.</summary>
    [Fact]
    public async Task Register_WithValidData_ReturnsOk()
    {
        // Arrange
        var registerDto = new RegisterDtoFactory().Build();
        _authServiceMock.Setup(a => a.RegisterAsync(registerDto))
            .ReturnsAsync(RegisterResult.Success());

        // Act
        var response = await _authController.Register(registerDto);

        // Assert
        Assert.IsType<OkObjectResult>(response.Result);
    }

    /// <summary>Verifies that Register when the service fails (e.g. duplicate email) returns BadRequest with the errors.</summary>
    [Fact]
    public async Task Register_WhenServiceFails_ReturnsBadRequestWithErrors()
    {
        // Arrange
        var registerDto = new RegisterDtoFactory().Build();
        var errors = new[] { "Email 'test@example.com' is already taken." };
        _authServiceMock.Setup(a => a.RegisterAsync(registerDto))
            .ReturnsAsync(RegisterResult.Failure(errors));

        // Act
        var response = await _authController.Register(registerDto);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(response.Result);
        var errorResponse = Assert.IsType<ErrorResponse>(result.Value);
        Assert.Equal(errors, errorResponse.Errors);
    }

    /// <summary>Verifies that Register passes the DTO through to the service unchanged.</summary>
    [Fact]
    public async Task Register_PassesRegisterDto_ToAuthService()
    {
        // Arrange
        var registerDto = new RegisterDtoFactory().WithEmail("someone@example.com").Build();
        _authServiceMock.Setup(a => a.RegisterAsync(It.IsAny<RegisterDto>()))
            .ReturnsAsync(RegisterResult.Success());

        // Act
        await _authController.Register(registerDto);

        // Assert
        _authServiceMock.Verify(a => a.RegisterAsync(registerDto), Times.Once);
    }
}