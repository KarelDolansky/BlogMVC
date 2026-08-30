namespace BlogMVC.Data;

/// <summary>
///     Predefined Identity role names. Roles are seeded into the <c>AspNetRoles</c> table at startup
///     (see <see cref="Program" />) with no permissions attached yet — authorization checks per role
///     will be added later. Newly registered accounts are assigned <see cref="Commentator" /> by
///     default (see <see cref="Services.AuthService.RegisterAsync" />).
/// </summary>
public static class Roles
{
    /// <summary>Full administrative access (scope to be defined later).</summary>
    public const string Administrator = "Administrator";

    /// <summary>Can create/manage posts, with broader rights than <see cref="Author" /> (scope to be defined later).</summary>
    public const string Editor = "Editor";

    /// <summary>Can create posts (scope to be defined later).</summary>
    public const string Author = "Author";

    /// <summary>Default role for newly registered accounts; no post-creation rights yet.</summary>
    public const string Commentator = "Commentator";

    /// <summary>All predefined roles, in the order they should be seeded.</summary>
    public static readonly IReadOnlyList<string> All = [Administrator, Editor, Author, Commentator];
}