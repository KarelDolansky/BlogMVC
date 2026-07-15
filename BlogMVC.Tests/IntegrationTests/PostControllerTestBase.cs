using BlogMVC.Models;
using BlogMVC.Tests.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BlogMVC.Tests.IntegrationTests;

/// <summary>
/// Shared base for <see cref="PostController"/> integration tests, run through
/// <see cref="WebApplicationFactory{Program}"/> (the whole app in memory, including a real MongoDB).
/// Replaces the standard authentication with a test scheme (<see cref="TestAuthHandler"/>)
/// and clears the posts collection before every test so tests don't affect each other.
/// </summary>
public abstract class PostControllerTestBase : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    /// <summary>Fixed reference date used in tests instead of the real current time.</summary>
    protected static readonly DateTime DefaultDate = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Unauthenticated HTTP client against the test app instance.</summary>
    protected readonly HttpClient Client;

    /// <summary>A valid but non-existent ObjectId used for "not found" scenarios.</summary>
    protected readonly string DefaultId = "507f1f77bcf86cd799439011";

    /// <summary>Test host factory instance, shared across the tests in a class.</summary>
    protected readonly WebApplicationFactory<Program> Factory;

    /// <summary>Id of a user who is NOT the owner of the posts under test (used for Forbid scenarios).</summary>
    protected readonly string OtherUserId = "507f1f77bcf86cd799439013";

    /// <summary>Id of the user who owns (authored) the posts under test.</summary>
    protected readonly string OwnerId = "507f1f77bcf86cd799439012";

    /// <summary>
    /// Configures the test host: "Testing" environment and replaces the authentication scheme
    /// with a test handler that reads the user's identity from HTTP headers.
    /// </summary>
    protected PostControllerTestBase(WebApplicationFactory<Program> factory)
    {
        Factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName, _ => { });
            });
        });
        Client = Factory.CreateClient();
    }

    /// <summary>Clears all documents in the posts collection before every test so previous state doesn't leak in.</summary>
    public async Task InitializeAsync()
    {
        var client = Factory.Services.GetRequiredService<MongoClient>();
        var settings = Factory.Services.GetRequiredService<IOptions<MongoDbSettings>>();
        var database = client.GetDatabase(settings.Value.DatabaseName);
        var posts = database.GetCollection<Post>(settings.Value.PostsCollectionName);
        await posts.DeleteManyAsync(_ => true);
    }

    /// <summary>No cleanup needed after a test.</summary>
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Creates an HTTP client whose requests carry headers identifying a user,
    /// so <see cref="TestAuthHandler"/> treats them as logged in.
    /// </summary>
    protected HttpClient CreateAuthenticatedClient(string userId, string userName = "TestUser")
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserNameHeader, userName);
        return client;
    }
}