using BlogMVC.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace BlogMVC.Controllers;

/// <summary>
/// Base class for Web API controllers (e.g. <see cref="BlogController"/>).
/// Provides shared helper functionality so it doesn't have to be repeated in every API controller.
/// </summary>
public class BaseApiController : ControllerBase
{
    /// <summary>Checks whether the given string is a valid MongoDB ObjectId.</summary>
    protected bool IsValidObjectId(string id) => MongoDbHelper.IsValidObjectId(id);

    /// <summary>Returns the Id of the currently logged-in user (NameIdentifier claim), or null if not authenticated.</summary>
    protected string? GetUserId() => User.GetUserId();

    /// <summary>Returns the username of the currently logged-in user (Name claim), or null if not authenticated.</summary>
    protected string? GetUserName() => User.GetUserName();
}