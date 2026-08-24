using BlogMVC.Dto;
using BlogMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlogMVC.Controllers;

/// <summary>
/// REST Web API for authentication, available at "api/auth". Exchanges ASP.NET Core Identity
/// credentials (email and password) for a JWT access token that can then be sent as a
/// "Authorization: Bearer {token}" header to JWT-protected endpoints such as <see cref="BlogController"/>.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : BaseApiController
{
    /// <summary>
    /// POST api/auth/login – validates the given email/password (via <see cref="IAuthService"/>) and,
    /// on success, returns 200 OK with <c>{ "token": "..." }</c> containing a signed JWT.
    /// Returns 401 Unauthorized if the email doesn't exist, the password is wrong, or the account
    /// is currently locked out.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult> Login(LoginDto loginDto)
    {
        var result = await authService.LoginAsync(loginDto);

        if (!result.Succeeded)
        {
            return result.FailureReason == LoginFailureReason.LockedOut
                ? Unauthorized("Account is temporarily locked out")
                : Unauthorized("Invalid email or password");
        }

        return Ok(new { token = result.Token });
    }
}