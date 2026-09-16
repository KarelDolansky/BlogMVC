using BlogMVC.Dto;
using BlogMVC.Results;

namespace BlogMVC.Services;

/// <summary>
///     Application (business) layer for authentication, sitting between <see cref="Controllers.AuthController" />
///     and the underlying credential store. Validates credentials and issues a JWT on success.
/// </summary>
public interface IAuthService
{
    /// <summary>
    ///     Validates the given email/password and, on success, issues a JWT for that user. Fails with
    ///     <see cref="LoginFailureReason.InvalidCredentials" /> if the email doesn't exist or the password is
    ///     wrong, or <see cref="LoginFailureReason.LockedOut" /> if the account is currently locked out.
    /// </summary>
    /// <param name="loginDto">The email/password credentials to validate.</param>
    /// <returns>A <see cref="LoginResult" /> carrying the issued JWT on success, or the failure reason.</returns>
    Task<LoginResult> LoginAsync(LoginDto loginDto);

    /// <summary>
    ///     Creates a new account for the given email/password, immediately usable via <see cref="LoginAsync" /> –
    ///     no email confirmation step. The account is assigned the default <see cref="Data.Roles.Commentator" /> role.
    /// </summary>
    /// <param name="registerDto">The email/password to register the new account with.</param>
    /// <returns>A <see cref="RegisterResult" /> indicating success, or the validation errors that caused failure.</returns>
    Task<RegisterResult> RegisterAsync(RegisterDto registerDto);
}