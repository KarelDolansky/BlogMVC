using BlogMVC.Models;
using BlogMVC.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BlogMVC.Controllers;

/// <summary>
/// REST Web API for authentication, available at "api/auth". Exchanges ASP.NET Core Identity
/// credentials (email + password) for a JWT access token that can then be sent as a
/// "Authorization: Bearer {token}" header to JWT-protected endpoints such as <see cref="BlogController"/>.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<IdentityUser> userManager,
    ITokenService tokenService,
    SignInManager<IdentityUser> signInManager) : BaseApiController
{
    /// <summary>
    /// POST api/auth/login – validates the given email/password against ASP.NET Core Identity
    /// and, on success, returns 200 OK with <c>{ "token": "..." }</c> containing a signed JWT.
    /// Returns 401 Unauthorized if the email doesn't exist, the password is wrong, or the account
    /// is currently locked out (failed attempts count towards Identity's account lockout).
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult> Login(LoginDto loginDto)
    {
        var user = await userManager.FindByEmailAsync(loginDto.Email);
        if (user == null)
            return Unauthorized("Invalid email or password");

        var result = await signInManager.CheckPasswordSignInAsync(
            user, loginDto.Password, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
                return Unauthorized("Account is temporarily locked out");

            return Unauthorized("Invalid email or password");
        }

        return Ok(new { token = tokenService.CreateToken(user) });
    }
}