using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace BlogMVC.Tests.IntegrationTests;

/// <summary>
///     Integration tests for the authentication pipeline: a plain <c>[Authorize]</c> without a policy must
///     authenticate with the JWT bearer scheme, not with Identity's cookie scheme.
/// </summary>
[Collection("BlogController")]
public class AuthenticationPipelineIntegrationTests(WebApplicationFactory<Program> factory)
    : BlogControllerTestBase(factory)
{
    /// <summary>Route of <see cref="AuthenticationProbeController" />, registered only for this test class.</summary>
    private const string ProbeUrl = "/api/test/auth-probe";

    /// <summary>Builds a client for a host that also serves <see cref="AuthenticationProbeController" />.</summary>
    /// <returns>A client that doesn't follow redirects, so a cookie-login redirect stays visible as 302.</returns>
    private HttpClient CreateProbeClient()
    {
        var probeFactory = Factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddControllers().AddApplicationPart(typeof(AuthenticationProbeController).Assembly)));

        return probeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    /// <summary>Verifies that a plain [Authorize] accepts a valid JWT and exposes the caller's claims.</summary>
    [Fact]
    public async Task PlainAuthorize_WithValidBearerToken_ReturnsOkWithCallerId()
    {
        // Arrange
        var (authenticatedClient, userId) = await CreateAuthenticatedClientAsync("probe");
        var probeClient = CreateProbeClient();
        // The token from the base host is valid here too: both hosts sign with the same Testing Jwt:Key.
        probeClient.DefaultRequestHeaders.Authorization = authenticatedClient.DefaultRequestHeaders.Authorization;

        // Act
        var response = await probeClient.GetAsync(ProbeUrl);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(userId, await response.Content.ReadAsStringAsync());
    }

    /// <summary>Verifies that a plain [Authorize] without a token returns 401 rather than a redirect.</summary>
    [Fact]
    public async Task PlainAuthorize_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var probeClient = CreateProbeClient();

        // Act
        var response = await probeClient.GetAsync(ProbeUrl);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}