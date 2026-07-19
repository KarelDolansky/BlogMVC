using System.Net;
using BlogMVC.Infrastructure.Interfaces;
using BlogMVC.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BlogMVC.Tests.IntegrationTests;

/// <summary>
///     Integration tests for <see cref="PostController" /> against the real app (via an HTTP client from
///     <see cref="WebApplicationFactory{Program}" />) and a real MongoDB instance. Verify the behavior of the
///     whole request including authorization (authenticated/unauthenticated client) and HTTP status codes.
/// </summary>
[Collection("PostController")]
public class PostControllerIntegrationTests(WebApplicationFactory<Program> factory)
    : PostControllerTestBase(factory)
{
    // ---------- Details ----------

    /// <summary>Verifies that Details with an invalid post Id returns NotFound.</summary>
    [Fact]
    public async Task Details_WithInvalidPostId_ReturnsNotFound()
    {
        // Arrange
        var id = "wrongPostId";

        // Act
        var response = await Client.GetAsync($"/Post/Details/{id}/");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifies that Details when the post does not exist returns NotFound.</summary>
    [Fact]
    public async Task Details_NotFoundPost_ReturnsNotFound()
    {
        // Arrange
        var id = DefaultId;

        // Act
        var response = await Client.GetAsync($"/Post/Details/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- Create GET ----------

    /// <summary>Verifies that Create without authentication returns Unauthorized.</summary>
    [Fact]
    public async Task Create_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/Post/Create");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Create POST ----------

    /// <summary>Verifies that Create (POST) without authentication returns Unauthorized.</summary>
    [Fact]
    public async Task Create_POST_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Title", "Test Title" },
            { "Content", "Test Content" }
        });

        // Act
        var response = await Client.PostAsync("/Post/Create", formData);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Edit GET ----------

    /// <summary>Verifies that Edit with an invalid post Id returns NotFound.</summary>
    [Fact]
    public async Task Edit_WithInvalidPostId_ReturnsNotFound()
    {
        // Arrange
        var id = "wrongPostId";
        var client = CreateAuthenticatedClient(OwnerId);

        // Act
        var response = await client.GetAsync($"/Post/Edit/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifies that Edit when the post does not exist returns NotFound.</summary>
    [Fact]
    public async Task Edit_NotFoundPost_ReturnsNotFound()
    {
        // Arrange
        var id = DefaultId;
        var client = CreateAuthenticatedClient(OwnerId);

        // Act
        var response = await client.GetAsync($"/Post/Edit/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifies that Edit without authentication returns Unauthorized.</summary>
    [Fact]
    public async Task Edit_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(DefaultId)
            .WithAuthorId(OwnerId)
            .Build();

        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        // Act
        var response = await Client.GetAsync($"/Post/Edit/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Verifies that Edit when a different user than the author is logged in returns Forbidden.</summary>
    [Fact]
    public async Task Edit_WithDifferentAuthor_ReturnsForbidden()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(DefaultId)
            .WithAuthorId(OwnerId)
            .Build();

        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        var client = CreateAuthenticatedClient(OtherUserId);

        // Act
        var response = await client.GetAsync($"/Post/Edit/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Edit POST ----------

    /// <summary>Verifies that Edit (POST) with an invalid post Id returns NotFound.</summary>
    [Fact]
    public async Task Edit_POST_WithInvalidPostId_ReturnsNotFound()
    {
        // Arrange
        var id = "wrongPostId";
        var client = CreateAuthenticatedClient(OwnerId);

        // Act
        var response = await client.PostAsync($"/Post/Edit/{id}",
            new FormUrlEncodedContent(new Dictionary<string, string>()));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifies that Edit (POST) when the post does not exist returns NotFound.</summary>
    [Fact]
    public async Task Edit_POST_NotFoundPost_ReturnsNotFound()
    {
        // Arrange: no post is inserted, so the controller's post lookup must return 404.
        var id = DefaultId;
        var client = CreateAuthenticatedClient(OwnerId);
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Title", "Test Title" },
            { "Description", "Test Description" },
            { "Content", "Test Content" }
        });

        // Act
        var response = await client.PostAsync($"/Post/Edit/{id}", formData);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifies that Edit (POST) without authentication returns Unauthorized.</summary>
    [Fact]
    public async Task Edit_POST_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Title", "Test Title" },
            { "Description", "Test Description" },
            { "Content", "Test Content" }
        });

        // Act
        var response = await Client.PostAsync($"/Post/Edit/{DefaultId}", formData);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Verifies that Edit (POST) when a different user than the author is logged in returns Forbidden.</summary>
    [Fact]
    public async Task Edit_POST_WithDifferentAuthor_ReturnsForbidden()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(DefaultId)
            .WithAuthorId(OwnerId)
            .Build();

        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        var client = CreateAuthenticatedClient(OtherUserId);
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Title", "Test Title" },
            { "Description", "Test Description" },
            { "Content", "Test Content" }
        });

        // Act
        var response = await client.PostAsync($"/Post/Edit/{post.Id}", formData);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Delete GET ----------

    /// <summary>Verifies that Delete with an invalid post Id returns NotFound.</summary>
    [Fact]
    public async Task Delete_WithInvalidPostId_ReturnsNotFound()
    {
        // Arrange
        var id = "wrongPostId";
        var client = CreateAuthenticatedClient(OwnerId);

        // Act
        var response = await client.GetAsync($"/Post/Delete/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifies that Delete when the post does not exist returns NotFound.</summary>
    [Fact]
    public async Task Delete_NotFoundPost_ReturnsNotFound()
    {
        // Arrange
        var id = DefaultId;
        var client = CreateAuthenticatedClient(OwnerId);

        // Act
        var response = await client.GetAsync($"/Post/Delete/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifies that Delete without authentication returns Unauthorized.</summary>
    [Fact]
    public async Task Delete_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(DefaultId)
            .WithAuthorId(OwnerId)
            .Build();

        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        // Act
        var response = await Client.GetAsync($"/Post/Delete/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Verifies that Delete when a different user than the author is logged in returns Forbidden.</summary>
    [Fact]
    public async Task Delete_WithDifferentAuthor_ReturnsForbidden()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(DefaultId)
            .WithAuthorId(OwnerId)
            .Build();

        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        var client = CreateAuthenticatedClient(OtherUserId);

        // Act
        var response = await client.GetAsync($"/Post/Delete/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Delete POST ----------

    /// <summary>Verifies that Delete (POST) with an invalid post Id returns NotFound.</summary>
    [Fact]
    public async Task Delete_POST_WithInvalidPostId_ReturnsNotFound()
    {
        // Arrange
        var id = "wrongPostId";
        var client = CreateAuthenticatedClient(OwnerId);

        // Act
        var response = await client.PostAsync($"/Post/Delete/{id}",
            new FormUrlEncodedContent(new Dictionary<string, string>()));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifies that Delete (POST) when the post does not exist returns NotFound.</summary>
    [Fact]
    public async Task Delete_POST_NotFoundPost_ReturnsNotFound()
    {
        // Arrange
        var id = DefaultId;
        var client = CreateAuthenticatedClient(OwnerId);

        // Act
        var response = await client.PostAsync($"/Post/Delete/{id}",
            new FormUrlEncodedContent(new Dictionary<string, string>()));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifies that Delete (POST) returns a View.</summary>
    [Fact]
    public async Task Delete_POST_ReturnsView()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(DefaultId)
            .WithAuthorId(OwnerId)
            .Build();

        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        var client = CreateAuthenticatedClient(OwnerId);

        // Act
        var response = await client.PostAsync($"/Post/Delete/{post.Id}",
            new FormUrlEncodedContent(new Dictionary<string, string>()));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Verifies that Delete (POST) without authentication returns Unauthorized.</summary>
    [Fact]
    public async Task Delete_POST_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsync($"/Post/Delete/{DefaultId}",
            new FormUrlEncodedContent(new Dictionary<string, string>()));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Verifies that Delete (POST) when a different user than the author is logged in returns Forbidden.</summary>
    [Fact]
    public async Task Delete_POST_WithDifferentAuthor_ReturnsForbidden()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(DefaultId)
            .WithAuthorId(OwnerId)
            .Build();

        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        var client = CreateAuthenticatedClient(OtherUserId);

        // Act
        var response = await client.PostAsync($"/Post/Delete/{post.Id}",
            new FormUrlEncodedContent(new Dictionary<string, string>()));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}