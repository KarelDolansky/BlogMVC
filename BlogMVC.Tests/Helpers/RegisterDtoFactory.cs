using BlogMVC.Dto;

namespace BlogMVC.Tests.Helpers;

/// <summary>
///     Test Data Builder for creating <see cref="RegisterDto" /> instances in tests,
///     with sensible defaults and fluent methods to override individual fields.
/// </summary>
public class RegisterDtoFactory
{
    private readonly RegisterDto _entity = new()
    {
        Email = "test@example.com",
        Password = "Password123!"
    };

    /// <summary>Sets the email.</summary>
    public RegisterDtoFactory WithEmail(string email)
    {
        _entity.Email = email;
        return this;
    }

    /// <summary>Sets the password.</summary>
    public RegisterDtoFactory WithPassword(string password)
    {
        _entity.Password = password;
        return this;
    }

    /// <summary>Returns the built <see cref="RegisterDto" /> instance.</summary>
    public RegisterDto Build()
    {
        return _entity;
    }
}