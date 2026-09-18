using BlogMVC.Data;
using BlogMVC.Dto;
using BlogMVC.Helpers;
using BlogMVC.Infrastructure.Interfaces;
using BlogMVC.Results;
using Microsoft.AspNetCore.Identity;

namespace BlogMVC.Services;

/// <summary>
///     Default <see cref="IAuthService" />: validates credentials via Identity, issues a JWT via
///     <see cref="ITokenProvider" />.
/// </summary>
/// <param name="userManager">Identity's user store, used to look up and create accounts.</param>
/// <param name="signInManager">Identity's sign-in manager, used to check credentials and lockout state.</param>
/// <param name="roleManager">Identity's role store, used to resolve the permissions the user's roles grant.</param>
/// <param name="tokenProvider">Issues the JWT for a successfully authenticated user.</param>
public class AuthService(
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    RoleManager<IdentityRole> roleManager,
    ITokenProvider tokenProvider) : IAuthService
{
    /// <summary>Placeholder user passed to the password hasher when the requested email doesn't exist.</summary>
    private static readonly IdentityUser DummyUser = new();

    /// <summary>
    ///     Hash verified against when the requested email doesn't exist; created on first use with the
    ///     configured hasher so its cost matches a real verification.
    /// </summary>
    private static string? _dummyPasswordHash;

    /// <summary>
    ///     Looks up the user by email via <c>UserManager.FindByEmailAsync</c>, checks the password via
    ///     <c>SignInManager.CheckPasswordSignInAsync</c> (with lockout tracking on failure), resolves the
    ///     permissions the user's roles currently grant (<see cref="RoleManagerExtensions.GetPermissionsAsync" />),
    ///     and on success issues a JWT via <see cref="ITokenProvider" />. An unknown email still costs one
    ///     password-hash verification, so response time doesn't reveal whether the email exists.
    /// </summary>
    /// <param name="loginDto">The email/password credentials to validate.</param>
    /// <returns>A <see cref="LoginResult" /> carrying the issued JWT on success, or the failure reason.</returns>
    public async Task<LoginResult> LoginAsync(LoginDto loginDto)
    {
        var user = await userManager.FindByEmailAsync(loginDto.Email);
        if (user == null)
        {
            VerifyAgainstDummyHash(loginDto.Password);
            return LoginResult.Failure(LoginFailureReason.InvalidCredentials);
        }

        var result = await signInManager.CheckPasswordSignInAsync(
            user, loginDto.Password, true);

        if (!result.Succeeded)
            return result.IsLockedOut
                ? LoginResult.Failure(LoginFailureReason.LockedOut)
                : LoginResult.Failure(LoginFailureReason.InvalidCredentials);

        var roles = await userManager.GetRolesAsync(user);
        var permissions = await roleManager.GetPermissionsAsync(roles);
        return LoginResult.Success(tokenProvider.CreateToken(user, roles, permissions));
    }

    /// <summary>
    ///     Creates the Identity user via <c>UserManager.CreateAsync</c> and assigns the
    ///     <see cref="Roles.Commentator" /> role.
    /// </summary>
    /// <param name="registerDto">The email/password to register the new account with.</param>
    /// <returns>A <see cref="RegisterResult" /> indicating success, or the Identity errors that caused failure.</returns>
    public async Task<RegisterResult> RegisterAsync(RegisterDto registerDto)
    {
        var user = new IdentityUser { UserName = registerDto.Email, Email = registerDto.Email };
        var createResult = await userManager.CreateAsync(user, registerDto.Password);
        if (!createResult.Succeeded)
            return RegisterResult.Failure(createResult.Errors.Select(e => e.Description));

        // Every new account starts as a Commentator; an administrator can grant a higher role later.
        await userManager.AddToRoleAsync(user, Roles.Commentator);

        return RegisterResult.Success();
    }

    /// <summary>
    ///     Runs one password-hash verification against <see cref="_dummyPasswordHash" /> and discards the
    ///     result, matching the cost of checking a real user's password.
    /// </summary>
    /// <param name="password">The password supplied in the login request.</param>
    private void VerifyAgainstDummyHash(string password)
    {
        var hasher = userManager.PasswordHasher;
        // A concurrent first call may compute the hash twice; either value is equally valid.
        _dummyPasswordHash ??= hasher.HashPassword(DummyUser, Guid.NewGuid().ToString());
        hasher.VerifyHashedPassword(DummyUser, _dummyPasswordHash, password);
    }
}