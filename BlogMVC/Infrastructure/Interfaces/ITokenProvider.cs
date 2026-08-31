using Microsoft.AspNetCore.Identity;

namespace BlogMVC.Infrastructure.Interfaces;

/// <summary>
///     Creates signed JWT access tokens for authenticated users, used by
///     <see cref="Controllers.AuthController"/> and validated by the JWT bearer
///     authentication scheme configured in <c>Program.cs</c>.
/// </summary>
public interface ITokenProvider
{
    /// <summary>
    ///     Builds a signed JWT for the given Identity user, containing the user's Id
    ///     (<c>NameIdentifier</c> claim), username (<c>Name</c> claim) and one <c>Role</c> claim per
    ///     entry in <paramref name="roles"/>. Valid for 1 hour from the current time (via
    ///     <see cref="Infrastructure.Interfaces.IDateTimeProvider"/>, so it's mockable in tests).
    /// </summary>
    /// <param name="user">The authenticated Identity user the token is issued for.</param>
    /// <param name="roles">The user's assigned Identity role names (see <see cref="Data.Roles"/>).</param>
    /// <returns>The encoded JWT as a string, ready to be returned to the client.</returns>
    string CreateToken(IdentityUser user, IEnumerable<string> roles);
}