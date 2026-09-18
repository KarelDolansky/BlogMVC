using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogMVC.Tests.IntegrationTests;

/// <summary>Test-only endpoint protected by a plain <see cref="AuthorizeAttribute" /> with no policy or scheme.</summary>
[ApiController]
[Route("api/test/auth-probe")]
[Authorize]
public class AuthenticationProbeController : ControllerBase
{
    /// <summary>GET api/test/auth-probe – returns the caller's NameIdentifier claim.</summary>
    /// <returns>200 with the authenticated caller's user id.</returns>
    [HttpGet]
    public ActionResult<string> Get()
    {
        return Ok(User.FindFirstValue(ClaimTypes.NameIdentifier));
    }
}