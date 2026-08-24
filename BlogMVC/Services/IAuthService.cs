using BlogMVC.Dto;

namespace BlogMVC.Services;

/// <summary>
///     Application (business) layer for authentication. Sits between <see cref="Controllers.AuthController" />
///     and ASP.NET Core Identity (<see cref="Microsoft.AspNetCore.Identity.UserManager{TUser}" />,
///     <see cref="Microsoft.AspNetCore.Identity.SignInManager{TUser}" />) plus
///     <see cref="Infrastructure.Interfaces.ITokenProvider" /> – validates credentials and issues a JWT on success.
/// </summary>
public interface IAuthService
{
    /// <summary>
    ///     Validates the given email/password against ASP.NET Core Identity and, on success, issues a JWT
    ///     for that user. Fails with <see cref="LoginFailureReason.InvalidCredentials" /> if the email doesn't
    ///     exist or the password is wrong, or <see cref="LoginFailureReason.LockedOut" /> if the account is
    ///     currently locked out (failed attempts count towards Identity's account lockout).
    /// </summary>
    Task<LoginResult> LoginAsync(LoginDto loginDto);
}