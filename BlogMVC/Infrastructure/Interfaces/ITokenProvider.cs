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
    ///     Builds a signed JWT for the given Identity user: Id, username, one <c>Role</c> claim per role, and
    ///     one <see cref="Data.Permissions.ClaimType"/> claim per permission those roles grant (see
    ///     <see cref="Data.RolePermissions"/>). Valid for 1 hour from <see cref="IDateTimeProvider"/>.Now.
    /// </summary>
    /// <param name="user">The authenticated Identity user the token is issued for.</param>
    /// <param name="roles">The user's assigned Identity role names (see <see cref="Data.Roles"/>).</param>
    /// <returns>The encoded JWT as a string, ready to be returned to the client.</returns>
    string CreateToken(IdentityUser user, IEnumerable<string> roles);
}