using BlogMVC.Dto;
using BlogMVC.Infrastructure.Interfaces;
using BlogMVC.Results;
using Microsoft.AspNetCore.Identity;

namespace BlogMVC.Services;

/// <summary>
///     Default <see cref="IAuthService" />: validates credentials via Identity, issues a JWT via
///     <see cref="ITokenProvider" />.
/// </summary>
public class AuthService(
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    ITokenProvider tokenProvider) : IAuthService
{
    /// <inheritdoc />
    public async Task<LoginResult> LoginAsync(LoginDto loginDto)
    {
        var user = await userManager.FindByEmailAsync(loginDto.Email);
        if (user == null)
            return LoginResult.Failure(LoginFailureReason.InvalidCredentials);

        var result = await signInManager.CheckPasswordSignInAsync(
            user, loginDto.Password, true);

        if (!result.Succeeded)
            return result.IsLockedOut
                ? LoginResult.Failure(LoginFailureReason.LockedOut)
                : LoginResult.Failure(LoginFailureReason.InvalidCredentials);

        return LoginResult.Success(tokenProvider.CreateToken(user));
    }

    /// <inheritdoc />
    public async Task<RegisterResult> RegisterAsync(RegisterDto registerDto)
    {
        var user = new IdentityUser { UserName = registerDto.Email, Email = registerDto.Email };
        var createResult = await userManager.CreateAsync(user, registerDto.Password);
        if (!createResult.Succeeded)
            return RegisterResult.Failure(createResult.Errors.Select(e => e.Description));

        // Lock the account until an administrator approves it, in place of email confirmation.
        await userManager.SetLockoutEnabledAsync(user, true);
        await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        return RegisterResult.Success();
    }
}