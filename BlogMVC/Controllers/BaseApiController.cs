using BlogMVC.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace BlogMVC.Controllers;

/// <summary>
///     Base class for Web API controllers (e.g. <see cref="BlogController"/>).
///     Provides shared helper functionality so it doesn't have to be repeated in every API controller.
/// </summary>
public class BaseApiController : ControllerBase
{
    /// <summary>Checks whether the given string is a valid MongoDB ObjectId.</summary>
    /// <param name="id">Candidate identifier string.</param>
    /// <returns><c>true</c> if <paramref name="id"/> is a well-formed ObjectId; otherwise <c>false</c>.</returns>
    protected bool IsValidObjectId(string id) => MongoDbHelper.IsValidObjectId(id);

    /// <summary>Returns the Id of the currently logged-in user (NameIdentifier claim), or null if not authenticated.</summary>
    /// <returns>The caller's user id, or <c>null</c> if the request is unauthenticated.</returns>
    protected string? GetUserId() => User.GetUserId();

    /// <summary>Returns the username of the currently logged-in user (Name claim), or null if not authenticated.</summary>
    /// <returns>The caller's username, or <c>null</c> if the request is unauthenticated.</returns>
    protected string? GetUserName() => User.GetUserName();

    /// <summary>Sets the ETag response header from a version number.</summary>
    /// <param name="version">Document version to encode as a quoted ETag.</param>
    protected void SetETag(long version)
    {
        Response.GetTypedHeaders().ETag = new EntityTagHeaderValue($"\"{version}\"");
    }

    /// <summary>Reads the expected version from the If-Match header. False if missing or malformed.</summary>
    /// <param name="version">The parsed version, if the header was present and well-formed; otherwise default.</param>
    /// <returns><c>true</c> if a strong If-Match tag was present and parsed as a version number; otherwise <c>false</c>.</returns>
    protected bool TryGetIfMatchVersion(out long version)
    {
        version = default;
        var tag = Request.GetTypedHeaders().IfMatch?.FirstOrDefault(t => !t.IsWeak);
        return tag != null && long.TryParse(tag.Tag.ToString().Trim('"'), out version);
    }
}