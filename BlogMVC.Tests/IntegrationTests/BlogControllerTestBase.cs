using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BlogMVC.Data;
using BlogMVC.Dto;
using BlogMVC.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BlogMVC.Tests.IntegrationTests;

/// <summary>
///     Shared base for API integration tests, run through <see cref="WebApplicationFactory{Program}" />
///     (the whole app in memory, including a real MongoDB instance for posts).
///     The Identity (SQLite) store is swapped for a fresh in-memory SQLite database per test, so
///     registering users doesn't depend on or pollute a real app.db file. Clears the posts
///     collection before every test so tests don't affect each other.
/// </summary>
public abstract class BlogControllerTestBase : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    /// <summary>Default password used for test-registered Identity accounts.</summary>
    protected const string DefaultPassword = "Password123!";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Fixed reference date used in tests instead of the real current time.</summary>
    protected static readonly DateTime DefaultDate = new(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Unauthenticated HTTP client against the test app instance.</summary>
    protected readonly HttpClient Client;

    /// <summary>A valid but non-existent ObjectId used for "not found" scenarios.</summary>
    protected readonly string DefaultId = "507f1f77bcf86cd799439011";

    /// <summary>Test host factory instance, customized for this test class instance.</summary>
    protected readonly WebApplicationFactory<Program> Factory;

    /// <summary>Keeps the in-memory SQLite connection for the Identity store open for the test's lifetime.</summary>
    private readonly SqliteConnection _identityConnection;

    /// <summary>
    ///     Configures the test host: "Testing" environment (loads appsettings.Testing.json, which
    ///     provides a Jwt signing key so real login tokens can be issued and validated), and replaces
    ///     the SQLite Identity store with a fresh in-memory database.
    /// </summary>
    protected BlogControllerTestBase(WebApplicationFactory<Program> factory)
    {
        _identityConnection = new SqliteConnection("DataSource=:memory:");
        _identityConnection.Open();

        Factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_identityConnection));
            });
        });

        Client = Factory.CreateClient();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
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

    /// <summary>Closes the in-memory Identity database connection after the test.</summary>
    public Task DisposeAsync()
    {
        _identityConnection.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Registers a new, already-confirmed Identity user directly through <see cref="UserManager{TUser}" />,
    ///     then logs in via the real "api/auth/login" endpoint to obtain a genuine JWT.
    ///     Returns an <see cref="HttpClient" /> with the token pre-set as a Bearer Authorization header,
    ///     along with the new user's Id (for asserting authorship/ownership).
    /// </summary>
    /// <param name="userName">Display name for the new user; also used to derive a unique email.</param>
    /// <param name="password">Password for the new account; defaults to <see cref="DefaultPassword" />.</param>
    protected async Task<(HttpClient Client, string UserId)> CreateAuthenticatedClientAsync(
        string userName = "TestUser", string password = DefaultPassword)
    {
        var email = $"{userName.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";
        string userId;

        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

            userId = user.Id;
        }

        var client = Factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = email,
            Password = password
        });
        loginResponse.EnsureSuccessStatusCode();

        var body = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);

        return (client, userId);
    }

    /// <summary>Shape of the JSON body returned by "api/auth/login" on success.</summary>
    private class TokenResponse
    {
        public string Token { get; set; } = null!;
    }
}