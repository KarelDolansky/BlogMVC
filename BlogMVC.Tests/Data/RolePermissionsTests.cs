using BlogMVC.Data;

namespace BlogMVC.Tests.Data;

/// <summary>Unit tests for <see cref="RolePermissions" />'s role-to-permission mapping.</summary>
public class RolePermissionsTests
{
    /// <summary>
    ///     Verifies that Administrator gets Create, CreateBulk, both Any edit/delete permissions, and
    ///     Users.ManageRoles.
    /// </summary>
    [Fact]
    public void GetPermissions_Administrator_GrantsCreateAndAnyEditDeleteAndManageRoles()
    {
        var permissions = RolePermissions.GetPermissions([Roles.Administrator]);

        Assert.Equal(
            [
                Permissions.Posts.Create, Permissions.Posts.CreateBulk, Permissions.Posts.DeleteAny,
                Permissions.Posts.EditAny, Permissions.Users.ManageRoles
            ],
            permissions.Order());
    }

    /// <summary>Verifies that Editor gets Create, CreateBulk, and both Own edit/delete permissions.</summary>
    [Fact]
    public void GetPermissions_Editor_GrantsCreateAndOwnEditDelete()
    {
        var permissions = RolePermissions.GetPermissions([Roles.Editor]);

        Assert.Equal(
            [
                Permissions.Posts.Create, Permissions.Posts.CreateBulk, Permissions.Posts.DeleteOwn,
                Permissions.Posts.EditOwn
            ],
            permissions.Order());
    }

    /// <summary>Verifies that Author gets Create and both Own edit/delete permissions, but not CreateBulk.</summary>
    [Fact]
    public void GetPermissions_Author_GrantsCreateAndOwnEditDeleteButNotCreateBulk()
    {
        var permissions = RolePermissions.GetPermissions([Roles.Author]);

        Assert.Equal(
            [Permissions.Posts.Create, Permissions.Posts.DeleteOwn, Permissions.Posts.EditOwn],
            permissions.Order());
    }

    /// <summary>Verifies that Administrator's permission set excludes the Own edit/delete variants.</summary>
    [Fact]
    public void GetPermissions_Administrator_DoesNotGrantOwnVariants()
    {
        var permissions = RolePermissions.GetPermissions([Roles.Administrator]);

        Assert.DoesNotContain(Permissions.Posts.EditOwn, permissions);
        Assert.DoesNotContain(Permissions.Posts.DeleteOwn, permissions);
    }

    /// <summary>Verifies that Editor's permission set excludes the Any edit/delete variants.</summary>
    [Fact]
    public void GetPermissions_Editor_DoesNotGrantAnyVariants()
    {
        var permissions = RolePermissions.GetPermissions([Roles.Editor]);

        Assert.DoesNotContain(Permissions.Posts.EditAny, permissions);
        Assert.DoesNotContain(Permissions.Posts.DeleteAny, permissions);
    }

    /// <summary>Verifies that Commentator is granted no post permissions.</summary>
    [Fact]
    public void GetPermissions_Commentator_GrantsNothing()
    {
        var permissions = RolePermissions.GetPermissions([Roles.Commentator]);

        Assert.Empty(permissions);
    }

    /// <summary>Verifies that a role name with no mapping entry grants no permissions.</summary>
    [Fact]
    public void GetPermissions_UnknownRole_GrantsNothing()
    {
        var permissions = RolePermissions.GetPermissions(["NotARole"]);

        Assert.Empty(permissions);
    }

    /// <summary>Verifies that holding multiple roles returns the distinct union of their permissions.</summary>
    [Fact]
    public void GetPermissions_MultipleRoles_ReturnsDistinctUnion()
    {
        var permissions = RolePermissions.GetPermissions([Roles.Author, Roles.Administrator]);

        Assert.Equal(
            [
                Permissions.Posts.Create, Permissions.Posts.CreateBulk, Permissions.Posts.DeleteAny,
                Permissions.Posts.DeleteOwn, Permissions.Posts.EditAny, Permissions.Posts.EditOwn,
                Permissions.Users.ManageRoles
            ],
            permissions.Order());
    }

    /// <summary>Verifies that an empty role list grants no permissions.</summary>
    [Fact]
    public void GetPermissions_NoRoles_ReturnsEmpty()
    {
        var permissions = RolePermissions.GetPermissions([]);

        Assert.Empty(permissions);
    }
}