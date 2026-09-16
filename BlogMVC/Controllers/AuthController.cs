using BlogMVC.Dto;
using BlogMVC.Responses;
using BlogMVC.Results;
using BlogMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlogMVC.Controllers;

/// <summary>Authentication API at "api/auth". Exchanges Identity credentials for a JWT used to call protected endpoints.</summary>
/// <param name="authService">Service handling Identity login/registration.</param>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : BaseApiController
{
    /// <summary>POST api/auth/login – returns a JWT on success, 401 on invalid credentials or lockout.</summary>
    /// <param name="loginDto">Email/username and password.</param>
    /// <returns>200 with a <see cref="TokenResponse"/> on success; 401 if credentials are invalid or the account is locked out.</returns>
    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login(LoginDto loginDto)
    {
        var result = await authService.LoginAsync(loginDto);

        if (!result.Succeeded)
            return result.FailureReason == LoginFailureReason.LockedOut
                ? Unauthorized("Account is temporarily locked out")
                : Unauthorized("Invalid email or password");

        return Ok(new TokenResponse { Token = result.Token! });
    }

    /// <summary>POST api/auth/register – creates the account (as a Commentator); 400 with errors on failure.</summary>
    /// <param name="registerDto">New account's email/username and password.</param>
    /// <returns>200 with a <see cref="RegisterResponse"/> on success; 400 with an <see cref="ErrorResponse"/> listing Identity validation errors on failure.</returns>
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register(RegisterDto registerDto)
    {
        var result = await authService.RegisterAsync(registerDto);

        if (!result.Succeeded)
            return BadRequest(new ErrorResponse { Errors = result.Errors! });

        return Ok(new RegisterResponse
        {
            Message = "Account created."
        });
    }
}