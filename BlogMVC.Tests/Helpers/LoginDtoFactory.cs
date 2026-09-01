using BlogMVC.Dto;

namespace BlogMVC.Tests.Helpers;

/// <summary>
///     Test Data Builder for creating <see cref="LoginDto"/> instances in tests,
///     with sensible defaults and fluent methods to override individual fields.
/// </summary>
public class LoginDtoFactory
{
    /// <summary>The DTO being built, pre-populated with a default email/password.</summary>
    private LoginDto _entity = new LoginDto
    {
        Email = "test@example.com",
        Password = "Password123!",
    };

    /// <summary>Sets the email.</summary>
    /// <param name="email">The email to assign.</param>
    /// <returns>This factory, for chaining.</returns>
    public LoginDtoFactory WithEmail(string email)
    {
        _entity.Email = email;
        return this;
    }

    /// <summary>Sets the password.</summary>
    /// <param name="password">The password to assign.</param>
    /// <returns>This factory, for chaining.</returns>
    public LoginDtoFactory WithPassword(string password)
    {
        _entity.Password = password;
        return this;
    }

    /// <summary>Builds the configured <see cref="LoginDto" /> instance.</summary>
    /// <returns>The built <see cref="LoginDto" />.</returns>
    public LoginDto Build()
    {
        return _entity;
    }
}