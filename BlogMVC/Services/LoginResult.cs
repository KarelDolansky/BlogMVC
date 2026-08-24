namespace BlogMVC.Services;

/// <summary>Why a call to <see cref="IAuthService.LoginAsync" /> did not succeed.</summary>
public enum LoginFailureReason
{
    /// <summary>The email doesn't exist, or the password is wrong.</summary>
    InvalidCredentials,

    /// <summary>The account is temporarily locked out due to too many failed attempts.</summary>
    LockedOut
}

/// <summary>Outcome of <see cref="IAuthService.LoginAsync" />: either a signed JWT, or the reason it failed.</summary>
public class LoginResult
{
    /// <summary>Whether the login succeeded.</summary>
    public required bool Succeeded { get; init; }

    /// <summary>The signed JWT, set when <see cref="Succeeded" /> is true.</summary>
    public string? Token { get; init; }

    /// <summary>Why the login failed, set when <see cref="Succeeded" /> is false.</summary>
    public LoginFailureReason? FailureReason { get; init; }

    /// <summary>Builds a successful result carrying the issued JWT.</summary>
    public static LoginResult Success(string token)
    {
        return new LoginResult { Succeeded = true, Token = token };
    }

    /// <summary>Builds a failed result carrying the reason.</summary>
    public static LoginResult Failure(LoginFailureReason reason)
    {
        return new LoginResult { Succeeded = false, FailureReason = reason };
    }
}