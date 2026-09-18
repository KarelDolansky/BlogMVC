using BlogMVC.Helpers;
using Microsoft.Extensions.Configuration;

namespace BlogMVC.Tests.Helpers;

/// <summary>Unit tests for <see cref="JwtConfigurationExtensions.GetRequiredJwtKey" /> using in-memory configuration.</summary>
public class JwtConfigurationExtensionsTests
{
    /// <summary>Builds a configuration that contains only the given <c>Jwt:Key</c> value.</summary>
    /// <param name="key">The value for <c>Jwt:Key</c>, or <c>null</c> to leave it unset.</param>
    /// <returns>The built configuration.</returns>
    private static IConfiguration BuildConfiguration(string? key)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = key })
            .Build();
    }

    /// <summary>Verifies that a missing key throws instead of returning null.</summary>
    [Fact]
    public void GetRequiredJwtKey_MissingKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var configuration = BuildConfiguration(null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => configuration.GetRequiredJwtKey());
    }

    /// <summary>Verifies that a key of only whitespace is treated as missing.</summary>
    [Fact]
    public void GetRequiredJwtKey_WhitespaceKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var configuration = BuildConfiguration("   ");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => configuration.GetRequiredJwtKey());
    }

    /// <summary>Verifies that a key one byte below the minimum is rejected.</summary>
    [Fact]
    public void GetRequiredJwtKey_KeyShorterThanMinimum_ThrowsInvalidOperationException()
    {
        // Arrange
        var configuration = BuildConfiguration(new string('a', JwtConfigurationExtensions.MinimumKeyBytes - 1));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => configuration.GetRequiredJwtKey());
    }

    /// <summary>Verifies that a key of exactly the minimum length is accepted and returned unchanged.</summary>
    [Fact]
    public void GetRequiredJwtKey_KeyOfExactlyMinimumLength_ReturnsKey()
    {
        // Arrange
        var key = new string('a', JwtConfigurationExtensions.MinimumKeyBytes);
        var configuration = BuildConfiguration(key);

        // Act
        var result = configuration.GetRequiredJwtKey();

        // Assert
        Assert.Equal(key, result);
    }

    /// <summary>Verifies that the length is measured in UTF-8 bytes, not in characters.</summary>
    [Fact]
    public void GetRequiredJwtKey_MultiByteKey_CountsBytesNotCharacters()
    {
        // Arrange
        // 16 characters, but 32 bytes: 'é' is 2 bytes in UTF-8.
        var key = new string('é', 16);
        var configuration = BuildConfiguration(key);

        // Act
        var result = configuration.GetRequiredJwtKey();

        // Assert
        Assert.Equal(key, result);
    }
}