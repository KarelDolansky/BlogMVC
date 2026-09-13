using BlogMVC.Models;
using BlogMVC.Services;

namespace BlogMVC.Results;

/// <summary>Why a call to <see cref="IRoleService.CreateRoleAsync" /> did not succeed.</summary>
public enum CreateRoleFailureReason
{
    /// <summary>A role with the requested name already exists.</summary>
    DuplicateName,

    /// <summary>One or more requested permissions aren't in <see cref="Data.Permissions.All" />.</summary>
    InvalidPermission
}

/// <summary>Outcome of <see cref="IRoleService.CreateRoleAsync" />: either the created role, or the reason it failed.</summary>
public class CreateRoleResult
{
    /// <summary>Whether the role was created.</summary>
    public required bool Succeeded { get; init; }

    /// <summary>The created role, set when <see cref="Succeeded" /> is true.</summary>
    public RoleSummary? Role { get; init; }

    /// <summary>Why creation failed, set when <see cref="Succeeded" /> is false.</summary>
    public CreateRoleFailureReason? FailureReason { get; init; }

    /// <summary>Builds a successful result carrying the created role.</summary>
    public static CreateRoleResult Success(RoleSummary role)
    {
        return new CreateRoleResult { Succeeded = true, Role = role };
    }

    /// <summary>Builds a failed result carrying the reason.</summary>
    public static CreateRoleResult Failure(CreateRoleFailureReason reason)
    {
        return new CreateRoleResult { Succeeded = false, FailureReason = reason };
    }
}