using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BlogMVC.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace BlogMVC.Services;

/// <summary>
/// Default <see cref="ITokenService"/> implementation. Issues HMAC-SHA256 signed JWTs
/// using the "Jwt:Key" / "Jwt:Issuer" / "Jwt:Audience" configuration values, which must
/// be present in configuration (e.g. appsettings.json) or token creation will fail.
/// </summary>
public class TokenService(IConfiguration configuration, IDateTimeProvider dateTimeProvider) : ITokenService
{
    /// <inheritdoc />
    public string CreateToken(IdentityUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName!)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
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