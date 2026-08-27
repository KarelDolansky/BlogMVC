namespace BlogMVC.Services;

/// <summary>Outcome of <see cref="IAuthService.RegisterAsync" />: success, or the Identity errors that caused failure.</summary>
public class RegisterResult
{
    /// <summary>Whether the account was created.</summary>
    public required bool Succeeded { get; init; }

    /// <summary>Failure reasons (e.g. email taken, weak password), set when <see cref="Succeeded" /> is false.</summary>
    public IEnumerable<string>? Errors { get; init; }

    /// <summary>Builds a successful result.</summary>
    public static RegisterResult Success()
    {
        return new RegisterResult { Succeeded = true };
    }

    /// <summary>Builds a failed result carrying the reasons.</summary>
    public static RegisterResult Failure(IEnumerable<string> errors)
    {
        return new RegisterResult { Succeeded = false, Errors = errors };
    }
}