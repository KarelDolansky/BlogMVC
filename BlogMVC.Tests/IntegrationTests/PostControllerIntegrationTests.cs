using System.Net;
using BlogMVC.Infrastructure.Interfaces;
using BlogMVC.Models;
using BlogMVC.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BlogMVC.Tests.IntegrationTests;

public class PostControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public PostControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
            builder.UseEnvironment("Testing"));
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        var client = _factory.Services.GetRequiredService<MongoClient>();
        var settings = _factory.Services.GetRequiredService<IOptions<MongoDbSettings>>();
        var database = client.GetDatabase(settings.Value.DatabaseName);
        var posts = database.GetCollection<Post>(settings.Value.PostsCollectionName);
        await posts.DeleteManyAsync(_ => true);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ---------- Details ----------

    [Fact]
    public async Task GetDetails_WithWrongPostId_ReturnsNotFound()
    {
        //Arrange
        var id = "wrongPostId";

        //Act
        var response = await _client.GetAsync($"/Post/Details/{id}/");

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDetails_WithValidPost_ReturnsOk()
    {
        // Arrange
        var post = new PostFactory()
            .WithId("507f1f77bcf86cd799439011")
            .WithTitle("Test Title")
            .Build();

        var repository = _factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        // Act
        var response = await _client.GetAsync($"/Post/Details/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Title", content);
    }
}