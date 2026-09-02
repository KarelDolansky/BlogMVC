using BlogMVC.Services;

namespace BlogMVC.Results;

/// <summary>Why a call to <see cref="IUserService.UpdateUserRoleAsync" /> did not succeed.</summary>
public enum UpdateUserRoleFailureReason
{
    /// <summary>No user exists with the given id.</summary>
    UserNotFound,

    /// <summary>The requested role isn't one of <see cref="Data.Roles.All" />.</summary>
    InvalidRole
}

/// <summary>Outcome of <see cref="IUserService.UpdateUserRoleAsync" />: either the new role, or the reason it failed.</summary>
public class UpdateUserRoleResult
{
    /// <summary>Whether the role change succeeded.</summary>
    public required bool Succeeded { get; init; }

    /// <summary>Why the change failed, set when <see cref="Succeeded" /> is false.</summary>
    public UpdateUserRoleFailureReason? FailureReason { get; init; }

    /// <summary>Builds a successful result.</summary>
    public static UpdateUserRoleResult Success()
    {
        return new UpdateUserRoleResult { Succeeded = true };
    }

    /// <summary>Builds a failed result carrying the reason.</summary>
    public static UpdateUserRoleResult Failure(UpdateUserRoleFailureReason reason)
    {
        return new UpdateUserRoleResult { Succeeded = false, FailureReason = reason };
    }
}