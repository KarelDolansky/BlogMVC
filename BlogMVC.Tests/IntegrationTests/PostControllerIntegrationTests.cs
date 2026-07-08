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
    private static readonly DateTime DefaultDate = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly HttpClient _client;
    private readonly string _defaultId = "507f1f77bcf86cd799439011";
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
    public async Task Details_WithInvalidPostId_ReturnsNotFound()
    {
        // Arrange
        var id = "wrongPostId";

        // Act
        var response = await _client.GetAsync($"/Post/Details/{id}/");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Details_NotFoundPost_ReturnsNotFound()
    {
        // Arrange
        var id = _defaultId;

        // Act
        var response = await _client.GetAsync($"/Post/Details/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Details_WithValidPost_ReturnsView()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(_defaultId)
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
        Assert.Contains("AuthorDefault", content);
        Assert.Contains($"/Post/Edit/{_defaultId}", content);
        Assert.Contains($"/Post/Delete/{_defaultId}", content);
    }

    [Fact]
    public async Task Details_WithModifiedPost_ShowsModifiedDate()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(_defaultId)
            .WithTitle("Test Title")
            .WithPublishDate(DefaultDate)
            .WithModifiedDate(DefaultDate.AddDays(1))
            .Build();

        var repository = _factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        // Act
        var response = await _client.GetAsync($"/Post/Details/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains(DefaultDate.AddDays(1).ToString("dd/MM/yyyy"), content);
    }

    // ---------- Create GET ----------

    [Fact]
    public async Task Create_ReturnsView()
    {
        // Act
        var response = await _client.GetAsync("/Post/Create");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------- Create POST ----------

    [Fact]
    public async Task Create_POST_WithInvalidTitle_ReturnsView()
    {
        // Arrange
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Title", "" },
            { "Content", "Test Content" }
        });

        // Act
        var response = await _client.PostAsync("/Post/Create", formData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Content", content);
        Assert.Contains("The Title field is required.", content);
    }

    [Fact]
    public async Task Create_POST_WithInvalidContent_ReturnsView()
    {
        // Arrange
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Title", "Test Title" },
            { "Content", "" }
        });

        // Act
        var response = await _client.PostAsync("/Post/Create", formData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Title", content);
        Assert.Contains("The Content field is required.", content);
    }

    [Fact]
    public async Task Create_POST_WithValidContent_ReturnsDetailsView()
    {
        // Arrange
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Title", "Test Title" },
            { "Content", "Test Content" }
        });

        // Act
        var response = await _client.PostAsync("/Post/Create", formData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Title", content);
        Assert.Contains("Test Content", content);
    }

    // ---------- Edit GET ----------

    [Fact]
    public async Task Edit_WithInvalidPostId_ReturnsNotFound()
    {
        // Arrange
        var id = "wrongPostId";

        // Act
        var response = await _client.GetAsync($"/Post/Edit/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Edit_NotFoundPost_ReturnsNotFound()
    {
        // Arrange
        var id = _defaultId;

        // Act
        var response = await _client.GetAsync($"/Post/Edit/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Edit_ReturnsView()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(_defaultId)
            .WithTitle("Test Title")
            .WithContent("Test Content")
            .Build();

        var repository = _factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        // Act
        var response = await _client.GetAsync($"/Post/Edit/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Title", content);
        Assert.Contains("Test Content", content);
        Assert.Contains($"/Post/Details/{post.Id}", content);
    }

    // ---------- Edit POST ----------

    [Fact]
    public async Task Edit_POST_WithInvalidPostId_ReturnsNotFound()
    {
        // Arrange
        var id = "wrongPostId";

        // Act
        var response = await _client.PostAsync($"/Post/Edit/{id}",
            new FormUrlEncodedContent(new Dictionary<string, string>()));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Edit_POST_WithInvalidTitle_ReturnsView()
    {
        // Arrange
        var id = _defaultId;
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Title", "" },
            { "Content", "Test Content" }
        });

        // Act
        var response = await _client.PostAsync($"/Post/Edit/{id}", formData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Content", content);
    }

    [Fact]
    public async Task Edit_POST_WithInvalidContent_ReturnsView()
    {
        // Arrange
        var id = _defaultId;
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Title", "Test Title" },
            { "Content", "" }
        });

        // Act
        var response = await _client.PostAsync($"/Post/Edit/{id}", formData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Title", content);
    }

    [Fact]
    public async Task Edit_POST_NotFoundPost_ReturnsView()
    {
        // Arrange
        var id = _defaultId;
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Title", "Test Title" },
            { "Content", "Test Content" }
        });

        // Act
        var response = await _client.PostAsync($"/Post/Edit/{id}", formData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Title", content);
        Assert.Contains("Test Content", content);
    }

    [Fact]
    public async Task Edit_POST_ReturnsView()
    {
        // Arrange
        var id = _defaultId;
        var post = new PostFactory()
            .WithId(_defaultId)
            .WithTitle("Test Title2")
            .WithContent("Test Content2")
            .Build();

        var repository = _factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Title", "Test Title" },
            { "Content", "Test Content" }
        });

        // Act
        var response = await _client.PostAsync($"/Post/Edit/{id}", formData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Title", content);
        Assert.Contains("Test Content", content);
        Assert.Contains("AuthorDefault", content);
        Assert.Contains($"/Post/Edit/{_defaultId}", content);
        Assert.Contains($"/Post/Delete/{_defaultId}", content);
    }

    // ---------- Delete GET ----------

    [Fact]
    public async Task Delete_WithInvalidPostId_ReturnsNotFound()
    {
        // Arrange
        var id = "wrongPostId";

        // Act
        var response = await _client.GetAsync($"/Post/Delete/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_NotFoundPost_ReturnsNotFound()
    {
        // Arrange
        var id = _defaultId;

        // Act
        var response = await _client.GetAsync($"/Post/Delete/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ReturnsView()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(_defaultId)
            .WithTitle("Test Title")
            .WithContent("Test Content")
            .Build();

        var repository = _factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        // Act
        var response = await _client.GetAsync($"/Post/Delete/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Title", content);
        Assert.Contains("AuthorDefault", content);
        Assert.Contains($"/Post/Delete/{_defaultId}", content);
        Assert.Contains($"/Post/Details/{post.Id}", content);
    }

    // ---------- Delete POST ----------

    [Fact]
    public async Task Delete_POST_WithInvalidPostId_ReturnsNotFound()
    {
        // Arrange
        var id = "wrongPostId";

        // Act
        var response = await _client.PostAsync($"/Post/Delete/{id}",
            new FormUrlEncodedContent(new Dictionary<string, string>()));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_POST_NotFoundPost_ReturnsNotFound()
    {
        // Arrange
        var id = _defaultId;

        // Act
        var response = await _client.PostAsync($"/Post/Delete/{id}",
            new FormUrlEncodedContent(new Dictionary<string, string>()));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_POST_ReturnsView()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(_defaultId)
            .Build();

        var repository = _factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        // Act
        var response = await _client.PostAsync($"/Post/Delete/{post.Id}",
            new FormUrlEncodedContent(new Dictionary<string, string>()));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}