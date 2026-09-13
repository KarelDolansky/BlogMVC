using BlogMVC.Dto;

namespace BlogMVC.Tests.Helpers;

/// <summary>
///     Test Data Builder for creating <see cref="UpdateRolePermissionsDto" /> instances in tests, with a
///     sensible (empty) default and a fluent method to override the permissions.
/// </summary>
public class UpdateRolePermissionsDtoFactory
{
    /// <summary>The DTO being built, pre-populated with no permissions.</summary>
    private readonly UpdateRolePermissionsDto _entity = new() { Permissions = [] };

    /// <summary>Sets the permissions.</summary>
    /// <param name="permissions">The permission claim values the role should grant.</param>
    /// <returns>This factory, for chaining.</returns>
    public UpdateRolePermissionsDtoFactory WithPermissions(params string[] permissions)
    {
        _entity.Permissions = permissions;
        return this;
    }

    /// <summary>Builds the configured <see cref="UpdateRolePermissionsDto" /> instance.</summary>
    /// <returns>The built <see cref="UpdateRolePermissionsDto" />.</returns>
    public UpdateRolePermissionsDto Build()
    {
        return _entity;
    }
}