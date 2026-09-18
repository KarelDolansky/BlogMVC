using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BlogMVC.Data;
using BlogMVC.Helpers;
using BlogMVC.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace BlogMVC.Infrastructure.Providers;

/// <summary>
///     Default <see cref="ITokenProvider"/> implementation. Issues HMAC-SHA256 signed JWTs
///     using the "Jwt:Key" / "Jwt:Issuer" / "Jwt:Audience" configuration values; "Jwt:Key" must be
///     at least 32 UTF-8 bytes (see <see cref="JwtConfigurationExtensions.GetRequiredJwtKey" />).
/// </summary>
public class TokenProvider(IConfiguration configuration, IDateTimeProvider dateTimeProvider) : ITokenProvider
{
    /// <summary>
    ///     Builds the claims list (NameIdentifier, Name, one Role claim per role, one
    ///     <see cref="Permissions.ClaimType" /> claim per entry in <paramref name="permissions" />), signs it
    ///     with HMAC-SHA256 using the configured "Jwt:Key", and serializes it via
    ///     <see cref="JwtSecurityTokenHandler" />. Expiry is <see cref="IDateTimeProvider" />.Now + 1 hour.
    /// </summary>
    /// <param name="user">The authenticated Identity user the token is issued for.</param>
    /// <param name="roles">The user's assigned Identity role names.</param>
    /// <param name="permissions">The permission claim values to embed, resolved by the caller.</param>
    /// <returns>The encoded JWT as a string.</returns>
    public string CreateToken(IdentityUser user, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName!)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim(Permissions.ClaimType, p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetRequiredJwtKey()));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: dateTimeProvider.Now.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}