using BlogMVC.Dto;

namespace BlogMVC.Tests.Helpers;

/// <summary>
///     Test Data Builder for creating <see cref="RegisterDto" /> instances in tests,
///     with sensible defaults and fluent methods to override individual fields.
/// </summary>
public class RegisterDtoFactory
{
    /// <summary>The DTO being built, pre-populated with a default email/password.</summary>
    private readonly RegisterDto _entity = new()
    {
        Email = "test@example.com",
        Password = "Password123!"
    };

    /// <summary>Sets the email.</summary>
    /// <param name="email">The email to assign.</param>
    /// <returns>This factory, for chaining.</returns>
    public RegisterDtoFactory WithEmail(string email)
    {
        _entity.Email = email;
        return this;
    }

    /// <summary>Sets the password.</summary>
    /// <param name="password">The password to assign.</param>
    /// <returns>This factory, for chaining.</returns>
    public RegisterDtoFactory WithPassword(string password)
    {
        _entity.Password = password;
        return this;
    }

    /// <summary>Builds the configured <see cref="RegisterDto" /> instance.</summary>
    /// <returns>The built <see cref="RegisterDto" />.</returns>
    public RegisterDto Build()
    {
        return _entity;
    }
}