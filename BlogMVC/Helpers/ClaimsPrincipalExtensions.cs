using System.Security.Claims;

namespace BlogMVC.Helpers;

/// <summary>
/// Extension methods for reading identity claims off <see cref="ClaimsPrincipal"/> (HttpContext.User).
/// Used by <see cref="Controllers.BaseController"/> and <see cref="Controllers.BaseApiController"/>
/// so controllers don't have to look up claim types directly, regardless of whether the user
/// was authenticated via the Identity cookie or a JWT bearer token.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>Returns the user's Id (NameIdentifier claim), or null if not present/authenticated.</summary>
    public static string? GetUserId(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>Returns the user's name (Name claim), or null if not present/authenticated.</summary>
    public static string? GetUserName(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Name);
}