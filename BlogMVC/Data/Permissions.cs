namespace BlogMVC.Data;

/// <summary>
///     Permission claim values granted per role by <see cref="RolePermissions" />, checked via policies in
///     <c>Program.cs</c>.
/// </summary>
public static class Permissions
{
    /// <summary>Claim type used for permission claims in issued JWTs.</summary>
    public const string ClaimType = "permission";

    /// <summary>Permissions governing blog post operations.</summary>
    public static class Posts
    {
        public const string Create = "Posts.Create";
        public const string CreateBulk = "Posts.CreateBulk";
        public const string EditOwn = "Posts.EditOwn";

        /// <summary>Superset of <see cref="EditOwn" /> — edit any post, regardless of author.</summary>
        public const string EditAny = "Posts.EditAny";

        public const string DeleteOwn = "Posts.DeleteOwn";

        /// <summary>Superset of <see cref="DeleteOwn" /> — delete any post, regardless of author.</summary>
        public const string DeleteAny = "Posts.DeleteAny";

        /// <summary>Policy name for the edit endpoint — satisfied by either <see cref="EditOwn" /> or <see cref="EditAny" />.</summary>
        public const string EditPolicy = "Posts.Edit";

        /// <summary>
        ///     Policy name for the delete endpoint — satisfied by either <see cref="DeleteOwn" /> or <see cref="DeleteAny" />
        ///     .
        /// </summary>
        public const string DeletePolicy = "Posts.Delete";

        /// <summary>All post-related permission claim values (policy names excluded).</summary>
        public static readonly IReadOnlyList<string> All = [Create, CreateBulk, EditOwn, EditAny, DeleteOwn, DeleteAny];
    }
}