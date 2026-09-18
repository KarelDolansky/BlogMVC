using System.Text;

namespace BlogMVC.Helpers;

/// <summary>Extension helpers for reading and validating the JWT signing configuration.</summary>
public static class JwtConfigurationExtensions
{
    /// <summary>Minimum length of the signing key in UTF-8 bytes; HMAC-SHA256 requires at least 256 bits.</summary>
    public const int MinimumKeyBytes = 32;

    /// <summary>Reads <c>Jwt:Key</c> and validates that it is present and long enough for HMAC-SHA256.</summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The configured signing key.</returns>
    /// <exception cref="InvalidOperationException">
    ///     <c>Jwt:Key</c> is missing, blank, or shorter than <see cref="MinimumKeyBytes" /> UTF-8 bytes.
    /// </exception>
    public static string GetRequiredJwtKey(this IConfiguration configuration)
    {
        var key = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Configuration value 'Jwt:Key' is missing.");

        // The key itself is never included in the message: exception messages end up in logs.
        if (Encoding.UTF8.GetByteCount(key) < MinimumKeyBytes)
            throw new InvalidOperationException(
                $"Configuration value 'Jwt:Key' must be at least {MinimumKeyBytes} bytes (UTF-8) for HMAC-SHA256.");

        return key;
    }
}