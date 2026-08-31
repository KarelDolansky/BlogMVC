using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BlogMVC.Infrastructure.Interfaces;
using BlogMVC.Infrastructure.Providers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace BlogMVC.Tests.Providers;

/// <summary>
///     Unit tests for <see cref="TokenProvider" /> using a mocked <see cref="IConfiguration" /> and
///     <see cref="IDateTimeProvider" />. Verify the generated JWT's claims, issuer/audience,
///     expiration and signature.
/// </summary>
public class TokenProviderTests
{
    private const string DefaultAudience = "defaultAudience";
    private const string DefaultIssuer = "defaultIssuer";
    private const string DefaultKey = "this-is-a-sufficiently-long-test-only-signing-key-1234567890";
    private static readonly DateTime DefaultDate = new(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly string[] _defaultRoles = [];

    private readonly IdentityUser _defaultUser = new()
    {
        Id = "defaultUserId",
        UserName = "defaultUserName"
    };

    private readonly TokenProvider _tokenProvider;

    public TokenProviderTests()
    {
        var configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(c => c["Jwt:Key"]).Returns(DefaultKey);
        configurationMock.Setup(c => c["Jwt:Issuer"]).Returns(DefaultIssuer);
        configurationMock.Setup(c => c["Jwt:Audience"]).Returns(DefaultAudience);

        var dateTimeProviderMock = new Mock<IDateTimeProvider>();
        dateTimeProviderMock.Setup(d => d.Now).Returns(DefaultDate);

        _tokenProvider = new TokenProvider(configurationMock.Object, dateTimeProviderMock.Object);
    }

    // ---------- CreateToken ----------

    /// <summary>Verifies that CreateToken returns a non-empty JWT string.</summary>
    [Fact]
    public void CreateToken_ReturnsNonEmptyToken()
    {
        // Act
        var token = _tokenProvider.CreateToken(_defaultUser, _defaultRoles);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    /// <summary>Verifies that CreateToken includes the user's Id as the NameIdentifier claim.</summary>
    [Fact]
    public void CreateToken_IncludesNameIdentifierClaim_WithUserId()
    {
        // Act
        var token = _tokenProvider.CreateToken(_defaultUser, _defaultRoles);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // Assert
        var claim = jwt.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier);
        Assert.Equal(_defaultUser.Id, claim.Value);
    }

    /// <summary>Verifies that CreateToken includes the user's UserName as the Name claim.</summary>
    [Fact]
    public void CreateToken_IncludesNameClaim_WithUserName()
    {
        // Act
        var token = _tokenProvider.CreateToken(_defaultUser, _defaultRoles);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // Assert
        var claim = jwt.Claims.Single(c => c.Type == ClaimTypes.Name);
        Assert.Equal(_defaultUser.UserName, claim.Value);
    }

    /// <summary>Verifies that CreateToken includes one Role claim per role passed in.</summary>
    [Fact]
    public void CreateToken_IncludesRoleClaim_ForEachRole()
    {
        // Act
        var token = _tokenProvider.CreateToken(_defaultUser, ["Author", "Editor"]);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // Assert
        var roleClaims = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
        Assert.Equal(["Author", "Editor"], roleClaims);
    }

    /// <summary>Verifies that CreateToken with no roles adds no Role claims.</summary>
    [Fact]
    public void CreateToken_WithNoRoles_AddsNoRoleClaims()
    {
        // Act
        var token = _tokenProvider.CreateToken(_defaultUser, _defaultRoles);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // Assert
        Assert.DoesNotContain(jwt.Claims, c => c.Type == ClaimTypes.Role);
    }

    /// <summary>Verifies that CreateToken sets the issuer and audience from configuration.</summary>
    [Fact]
    public void CreateToken_SetsIssuerAndAudience_FromConfiguration()
    {
        // Act
        var token = _tokenProvider.CreateToken(_defaultUser, _defaultRoles);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // Assert
        Assert.Equal(DefaultIssuer, jwt.Issuer);
        Assert.Equal(DefaultAudience, jwt.Audiences.Single());
    }

    /// <summary>Verifies that CreateToken sets the expiration to 1 hour after IDateTimeProvider.Now.</summary>
    [Fact]
    public void CreateToken_SetsExpiration_OneHourFromDateTimeProviderNow()
    {
        // Act
        var token = _tokenProvider.CreateToken(_defaultUser, _defaultRoles);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // Assert
        Assert.Equal(DefaultDate.AddHours(1), jwt.ValidTo);
    }

    /// <summary>
    ///     Verifies that the JWT is signed with the key from configuration, so it validates
    ///     successfully when checked against that same key.
    /// </summary>
    [Fact]
    public void CreateToken_SignsTokenWithConfiguredKey_ValidatesWithSameKey()
    {
        // Arrange
        var token = _tokenProvider.CreateToken(_defaultUser, _defaultRoles);
        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = DefaultIssuer,
            ValidAudience = DefaultAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DefaultKey)),
            ValidateLifetime = false
        };

        // Act & Assert (throws if the signature doesn't validate)
        new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);
    }

    /// <summary>Verifies that the JWT's signature does not validate against a different signing key.</summary>
    [Fact]
    public void CreateToken_SignsTokenWithConfiguredKey_FailsValidationWithDifferentKey()
    {
        // Arrange
        var token = _tokenProvider.CreateToken(_defaultUser, _defaultRoles);
        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = DefaultIssuer,
            ValidAudience = DefaultAudience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("a-completely-different-test-only-signing-key-0987654321")),
            ValidateLifetime = false
        };

        // Act & Assert
        Assert.ThrowsAny<SecurityTokenException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _));
    }
}