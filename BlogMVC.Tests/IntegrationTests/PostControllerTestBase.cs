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

public abstract class PostControllerTestBase : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    protected static readonly DateTime DefaultDate = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    protected readonly HttpClient Client;
    protected readonly string DefaultId = "507f1f77bcf86cd799439011";
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly string OtherUserId = "507f1f77bcf86cd799439013";
    protected readonly string OwnerId = "507f1f77bcf86cd799439012";

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

    public async Task InitializeAsync()
    {
        var client = Factory.Services.GetRequiredService<MongoClient>();
        var settings = Factory.Services.GetRequiredService<IOptions<MongoDbSettings>>();
        var database = client.GetDatabase(settings.Value.DatabaseName);
        var posts = database.GetCollection<Post>(settings.Value.PostsCollectionName);
        await posts.DeleteManyAsync(_ => true);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected HttpClient CreateAuthenticatedClient(string userId, string userName = "TestUser")
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserNameHeader, userName);
        return client;
    }
}