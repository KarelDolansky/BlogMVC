using BlogMVC.Data;

namespace BlogMVC.Tests.Data;

/// <summary>Unit tests for <see cref="RolePermissions" />'s role-to-permission mapping.</summary>
public class RolePermissionsTests
{
    [Fact]
    public void GetPermissions_Administrator_GrantsCreateAndAnyEditDelete()
    {
        var permissions = RolePermissions.GetPermissions([Roles.Administrator]);

        Assert.Equal(
            [
                Permissions.Posts.Create, Permissions.Posts.CreateBulk, Permissions.Posts.DeleteAny,
                Permissions.Posts.EditAny
            ],
            permissions.Order());
    }

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

    [Fact]
    public void GetPermissions_Author_GrantsCreateAndOwnEditDeleteButNotCreateBulk()
    {
        var permissions = RolePermissions.GetPermissions([Roles.Author]);

        Assert.Equal(
            [Permissions.Posts.Create, Permissions.Posts.DeleteOwn, Permissions.Posts.EditOwn],
            permissions.Order());
    }

    [Fact]
    public void GetPermissions_Administrator_DoesNotGrantOwnVariants()
    {
        var permissions = RolePermissions.GetPermissions([Roles.Administrator]);

        Assert.DoesNotContain(Permissions.Posts.EditOwn, permissions);
        Assert.DoesNotContain(Permissions.Posts.DeleteOwn, permissions);
    }

    [Fact]
    public void GetPermissions_Editor_DoesNotGrantAnyVariants()
    {
        var permissions = RolePermissions.GetPermissions([Roles.Editor]);

        Assert.DoesNotContain(Permissions.Posts.EditAny, permissions);
        Assert.DoesNotContain(Permissions.Posts.DeleteAny, permissions);
    }

    [Fact]
    public void GetPermissions_Commentator_GrantsNothing()
    {
        var permissions = RolePermissions.GetPermissions([Roles.Commentator]);

        Assert.Empty(permissions);
    }

    [Fact]
    public void GetPermissions_UnknownRole_GrantsNothing()
    {
        var permissions = RolePermissions.GetPermissions(["NotARole"]);

        Assert.Empty(permissions);
    }

    [Fact]
    public void GetPermissions_MultipleRoles_ReturnsDistinctUnion()
    {
        var permissions = RolePermissions.GetPermissions([Roles.Author, Roles.Administrator]);

        Assert.Equal(
            [
                Permissions.Posts.Create, Permissions.Posts.CreateBulk, Permissions.Posts.DeleteAny,
                Permissions.Posts.DeleteOwn, Permissions.Posts.EditAny, Permissions.Posts.EditOwn
            ],
            permissions.Order());
    }

    [Fact]
    public void GetPermissions_NoRoles_ReturnsEmpty()
    {
        var permissions = RolePermissions.GetPermissions([]);

        Assert.Empty(permissions);
    }
}