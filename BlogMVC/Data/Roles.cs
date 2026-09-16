namespace BlogMVC.Data;

/// <summary>
///     Predefined Identity role names, seeded into <c>AspNetRoles</c> at startup with default permissions
///     (see <see cref="Helpers.IdentityRoleSeederExtensions" />). An administrator can create additional roles
///     and edit any role's permissions at runtime via <see cref="Services.IRoleService" />; endpoints check the
///     permission claim, not the role name, so a user holding multiple roles gets their union.
/// </summary>
public static class Roles
{
    /// <summary>Grants post creation (single/bulk) and edit/delete of any post, not just its own.</summary>
    public const string Administrator = "Administrator";

    /// <summary>Grants post creation (single/bulk) and edit/delete of its own posts.</summary>
    public const string Editor = "Editor";

    /// <summary>Grants single (not bulk) post creation and edit/delete of its own posts.</summary>
    public const string Author = "Author";

    /// <summary>Default role for newly registered accounts; cannot create posts.</summary>
    public const string Commentator = "Commentator";

    /// <summary>All predefined roles, in the order they should be seeded.</summary>
    public static readonly IReadOnlyList<string> All = [Administrator, Editor, Author, Commentator];
}