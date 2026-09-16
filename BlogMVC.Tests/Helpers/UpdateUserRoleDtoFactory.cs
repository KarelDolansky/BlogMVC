using BlogMVC.Data;
using BlogMVC.Dto;

namespace BlogMVC.Tests.Helpers;

/// <summary>
///     Test Data Builder for creating <see cref="UpdateUserRoleDto" /> instances in tests,
///     with a sensible default and a fluent method to override the role.
/// </summary>
public class UpdateUserRoleDtoFactory
{
    /// <summary>The DTO being built, pre-populated with a valid default role.</summary>
    private readonly UpdateUserRoleDto _entity = new()
    {
        Role = Roles.Editor
    };

    /// <summary>Sets the role.</summary>
    /// <param name="role">The role to assign.</param>
    /// <returns>This factory, for chaining.</returns>
    public UpdateUserRoleDtoFactory WithRole(string role)
    {
        _entity.Role = role;
        return this;
    }

    /// <summary>Builds the configured <see cref="UpdateUserRoleDto" /> instance.</summary>
    /// <returns>The built <see cref="UpdateUserRoleDto" />.</returns>
    public UpdateUserRoleDto Build()
    {
        return _entity;
    }
}