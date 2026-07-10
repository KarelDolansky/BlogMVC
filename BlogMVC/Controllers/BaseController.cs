using System.Security.Claims;
using BlogMVC.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace BlogMVC.Controllers;

/// <summary>
/// Base class for MVC controllers that return views (e.g. <see cref="PostController"/>, <see cref="HomeController"/>).
/// Provides shared helper methods for Id validation and reading the logged-in user's identity.
/// </summary>
public class BaseController : Controller
{
    /// <summary>Checks whether the given string is a valid MongoDB ObjectId.</summary>
    protected bool IsValidObjectId(string id) => MongoDbHelper.IsValidObjectId(id);

    /// <summary>Returns the Id of the currently logged-in user (NameIdentifier claim), or null if not authenticated.</summary>
    protected string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>Returns the username of the currently logged-in user (Name claim), or null if not authenticated.</summary>
    protected string? GetUserName() => User.FindFirstValue(ClaimTypes.Name);
}