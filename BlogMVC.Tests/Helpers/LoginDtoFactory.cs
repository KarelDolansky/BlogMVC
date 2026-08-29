using BlogMVC.Dto;

namespace BlogMVC.Tests.Helpers;

/// <summary>
///     Test Data Builder for creating <see cref="LoginDto"/> instances in tests,
///     with sensible defaults and fluent methods to override individual fields.
/// </summary>
public class LoginDtoFactory
{
    private LoginDto _entity = new LoginDto
    {
        Email = "test@example.com",
        Password = "Password123!",
    };

    /// <summary>Sets the email.</summary>
    public LoginDtoFactory WithEmail(string email)
    {
        _entity.Email = email;
        return this;
    }

    /// <summary>Sets the password.</summary>
    public LoginDtoFactory WithPassword(string password)
    {
        _entity.Password = password;
        return this;
    }

    /// <summary>Returns the built <see cref="LoginDto"/> instance.</summary>
    public LoginDto Build()
    {
        return _entity;
    }
}