using BlogMVC.Models;
using BlogMVC.Services;

namespace BlogMVC.Results;

/// <summary>Why a call to <see cref="IRoleService.UpdateRolePermissionsAsync" /> did not succeed.</summary>
public enum UpdateRolePermissionsFailureReason
{
    /// <summary>No role with the given name exists.</summary>
    RoleNotFound,

    /// <summary>One or more requested permissions aren't in <see cref="Data.Permissions.All" />.</summary>
    InvalidPermission
}

/// <summary>
///     Outcome of <see cref="IRoleService.UpdateRolePermissionsAsync" />: either the role's new permission set,
///     or the reason it failed.
/// </summary>
public class UpdateRolePermissionsResult
{
    /// <summary>Whether the permission set was replaced.</summary>
    public required bool Succeeded { get; init; }

    /// <summary>The role with its updated permissions, set when <see cref="Succeeded" /> is true.</summary>
    public RoleSummary? Role { get; init; }

    /// <summary>Why the update failed, set when <see cref="Succeeded" /> is false.</summary>
    public UpdateRolePermissionsFailureReason? FailureReason { get; init; }

    /// <summary>Builds a successful result carrying the role's updated permissions.</summary>
    public static UpdateRolePermissionsResult Success(RoleSummary role)
    {
        return new UpdateRolePermissionsResult { Succeeded = true, Role = role };
    }

    /// <summary>Builds a failed result carrying the reason.</summary>
    public static UpdateRolePermissionsResult Failure(UpdateRolePermissionsFailureReason reason)
    {
        return new UpdateRolePermissionsResult { Succeeded = false, FailureReason = reason };
    }
}