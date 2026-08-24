using BlogMVC.Dto;
using BlogMVC.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace BlogMVC.Services;

/// <summary>
///     Default implementation of <see cref="IAuthService" />. Uses <see cref="UserManager{TUser}" /> and
///     <see cref="SignInManager{TUser}" /> to validate credentials, and <see cref="ITokenProvider" /> to
///     issue a JWT on success.
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
}