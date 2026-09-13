using BlogMVC.Services;

namespace BlogMVC.Results;

/// <summary>Why a call to <see cref="IRoleService.DeleteRoleAsync" /> did not succeed.</summary>
public enum DeleteRoleFailureReason
{
    /// <summary>No role with the given name exists.</summary>
    RoleNotFound,

    /// <summary>At least one user currently holds this role; deleting it would silently orphan their access.</summary>
    RoleInUse
}

/// <summary>Outcome of <see cref="IRoleService.DeleteRoleAsync" />: success, or the reason it failed.</summary>
public class DeleteRoleResult
{
    /// <summary>Whether the role was deleted.</summary>
    public required bool Succeeded { get; init; }

    /// <summary>Why deletion failed, set when <see cref="Succeeded" /> is false.</summary>
    public DeleteRoleFailureReason? FailureReason { get; init; }

    /// <summary>Builds a successful result.</summary>
    public static DeleteRoleResult Success()
    {
        return new DeleteRoleResult { Succeeded = true };
    }

    /// <summary>Builds a failed result carrying the reason.</summary>
    public static DeleteRoleResult Failure(DeleteRoleFailureReason reason)
    {
        return new DeleteRoleResult { Succeeded = false, FailureReason = reason };
    }
}