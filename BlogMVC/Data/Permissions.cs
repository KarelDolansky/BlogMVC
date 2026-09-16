namespace BlogMVC.Data;

/// <summary>
///     Permission claim values, checked via policies in <c>Program.cs</c>. Which roles grant which of these is
///     runtime-editable via <see cref="Services.IRoleService" />, stored as Identity role claims.
/// </summary>
public static class Permissions
{
    /// <summary>Claim type used for permission claims in issued JWTs.</summary>
    public const string ClaimType = "permission";

    /// <summary>
    ///     Every known permission claim value across all categories. A role can only be granted values from this
    ///     set (see <see cref="Services.IRoleService" />) — an arbitrary string would have no policy behind it.
    /// </summary>
    public static readonly IReadOnlyList<string> All = [.. Posts.All, .. Users.All, .. Roles.All];

    /// <summary>Permissions governing blog post operations.</summary>
    public static class Posts
    {
        /// <summary>Permission claim required to create a single post.</summary>
        public const string Create = "Posts.Create";

        /// <summary>Permission claim required to create multiple posts in one request. Narrower than <see cref="Create" />.</summary>
        public const string CreateBulk = "Posts.CreateBulk";

        /// <summary>Permission claim required to edit a post the caller authored.</summary>
        public const string EditOwn = "Posts.EditOwn";

        /// <summary>Superset of <see cref="EditOwn" /> — edit any post, regardless of author.</summary>
        public const string EditAny = "Posts.EditAny";

        /// <summary>Permission claim required to delete a post the caller authored.</summary>
        public const string DeleteOwn = "Posts.DeleteOwn";

        /// <summary>Superset of <see cref="DeleteOwn" /> — delete any post, regardless of author.</summary>
        public const string DeleteAny = "Posts.DeleteAny";

        /// <summary>Policy name for the edit endpoint — satisfied by either <see cref="EditOwn" /> or <see cref="EditAny" />.</summary>
        public const string EditPolicy = "Posts.Edit";

        /// <summary>
        ///     Policy name for the delete endpoint — satisfied by either <see cref="DeleteOwn" /> or <see cref="DeleteAny" />.
        /// </summary>
        public const string DeletePolicy = "Posts.Delete";

        /// <summary>All post-related permission claim values (policy names excluded).</summary>
        public static readonly IReadOnlyList<string> All = [Create, CreateBulk, EditOwn, EditAny, DeleteOwn, DeleteAny];
    }

    /// <summary>Permissions governing user account administration.</summary>
    public static class Users
    {
        /// <summary>Permission claim required to change another user's role.</summary>
        public const string ManageRoles = "Users.ManageRoles";

        /// <summary>All user-related permission claim values (policy names excluded).</summary>
        public static readonly IReadOnlyList<string> All = [ManageRoles];
    }

    /// <summary>Permissions governing role administration (creating roles, editing their granted permissions).</summary>
    public static class Roles
    {
        /// <summary>Permission claim required to create, delete, or edit the permissions of a role.</summary>
        public const string Manage = "Roles.Manage";

        /// <summary>All role-related permission claim values.</summary>
        public static readonly IReadOnlyList<string> All = [Manage];
    }
}