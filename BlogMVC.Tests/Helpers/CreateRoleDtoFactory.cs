using BlogMVC.Dto;

namespace BlogMVC.Tests.Helpers;

/// <summary>
///     Test Data Builder for creating <see cref="CreateRoleDto" /> instances in tests, with a sensible
///     default and fluent methods to override the name/permissions.
/// </summary>
public class CreateRoleDtoFactory
{
    /// <summary>The DTO being built, pre-populated with a unique default name and no permissions.</summary>
    private readonly CreateRoleDto _entity = new()
    {
        Name = $"TestRole-{Guid.NewGuid():N}",
        Permissions = []
    };

    /// <summary>Sets the role name.</summary>
    /// <param name="name">The role name to assign.</param>
    /// <returns>This factory, for chaining.</returns>
    public CreateRoleDtoFactory WithName(string name)
    {
        _entity.Name = name;
        return this;
    }

    /// <summary>Sets the initial permissions.</summary>
    /// <param name="permissions">The permission claim values to grant.</param>
    /// <returns>This factory, for chaining.</returns>
    public CreateRoleDtoFactory WithPermissions(params string[] permissions)
    {
        _entity.Permissions = permissions;
        return this;
    }

    /// <summary>Builds the configured <see cref="CreateRoleDto" /> instance.</summary>
    /// <returns>The built <see cref="CreateRoleDto" />.</returns>
    public CreateRoleDto Build()
    {
        return _entity;
    }
}