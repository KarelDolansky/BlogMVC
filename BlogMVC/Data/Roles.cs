namespace BlogMVC.Data;

/// <summary>
///     Predefined Identity role names. Roles are seeded into the <c>AspNetRoles</c> table at startup
///     (see <see cref="Program" />). Post creation is gated by role via <c>[Authorize(Roles = ...)]</c> on
///     <see cref="Controllers.BlogController" /> (<see cref="Administrator" />/<see cref="Editor" />/
///     <see cref="Author" /> can create posts, <see cref="Editor" />/<see cref="Administrator" /> can also
///     bulk-create; <see cref="Commentator" /> cannot); further per-role authorization checks may be added
///     later. Newly registered accounts are assigned <see cref="Commentator" /> by default (see
///     <see cref="Services.AuthService.RegisterAsync" />).
/// </summary>
public static class Roles
{
    /// <summary>Full administrative access; can create posts individually or in bulk (scope beyond that to be defined later).</summary>
    public const string Administrator = "Administrator";

    /// <summary>
    ///     Can create posts individually or in bulk, unlike <see cref="Author" /> (scope beyond post creation to be
    ///     defined later).
    /// </summary>
    public const string Editor = "Editor";

    /// <summary>
    ///     Can create posts individually, but not in bulk (see <see cref="Controllers.BlogController.BulkCreatePosts" />
    ///     ).
    /// </summary>
    public const string Author = "Author";

    /// <summary>Default role for newly registered accounts; cannot create posts.</summary>
    public const string Commentator = "Commentator";

    /// <summary>All predefined roles, in the order they should be seeded.</summary>
    public static readonly IReadOnlyList<string> All = [Administrator, Editor, Author, Commentator];
}