namespace BlogMVC.Data;

/// <summary>
///     Static role-to-permission mapping. A user's effective permissions are the union across every role
///     they hold (see <see cref="GetPermissions" />), not just the first match.
/// </summary>
public static class RolePermissions
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Map =
        new Dictionary<string, IReadOnlySet<string>>
        {
            [Roles.Administrator] = new HashSet<string>
            {
                Permissions.Posts.Create, Permissions.Posts.CreateBulk,
                Permissions.Posts.EditAny, Permissions.Posts.DeleteAny
            },
            [Roles.Editor] = new HashSet<string>
            {
                Permissions.Posts.Create, Permissions.Posts.CreateBulk,
                Permissions.Posts.EditOwn, Permissions.Posts.DeleteOwn
            },
            [Roles.Author] = new HashSet<string>
            {
                Permissions.Posts.Create, Permissions.Posts.EditOwn, Permissions.Posts.DeleteOwn
            },
            [Roles.Commentator] = new HashSet<string>()
        };

    /// <summary>Returns the distinct union of permissions granted by the given roles.</summary>
    public static IReadOnlyCollection<string> GetPermissions(IEnumerable<string> roles)
    {
        return roles.SelectMany(role => Map.GetValueOrDefault(role, new HashSet<string>()))
            .Distinct()
            .ToList();
    }
}