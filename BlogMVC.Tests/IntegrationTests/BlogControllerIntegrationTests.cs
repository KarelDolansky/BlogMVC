using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlogMVC.Data;
using BlogMVC.Dto;
using BlogMVC.Infrastructure.Interfaces;
using BlogMVC.Models;
using BlogMVC.Responses;
using BlogMVC.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BlogMVC.Tests.IntegrationTests;

/// <summary>
///     Integration tests for <see cref="BlogController" /> against the real app (via an HTTP client
///     from <see cref="WebApplicationFactory{Program}" />), a real MongoDB instance, and real JWT
///     bearer authentication (tokens obtained through the actual "api/auth/login" endpoint).
///     Verifies the behavior of the whole request pipeline, including authorization and HTTP status codes.
/// </summary>
[Collection("BlogController")]
public class BlogControllerIntegrationTests(WebApplicationFactory<Program> factory)
    : BlogControllerTestBase(factory)
{
    private async Task<Post> SeedPostAsync(string authorId, string author = "Author")
    {
        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        var post = new PostFactory()
            .WithAuthorId(authorId)
            .WithAuthor(author)
            .WithPublishDate(DefaultDate)
            .WithModifiedDate(DefaultDate)
            .Build();
        return await repository.InsertOneAsync(post);
    }

    private static async Task<HttpResponseMessage> PutWithIfMatchAsync(
        HttpClient client, string url, EditPostDto editPostDto, EntityTagHeaderValue eTag)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = JsonContent.Create(editPostDto) };
        request.Headers.IfMatch.Add(eTag);
        return await client.SendAsync(request);
    }

    // ---------- GetPosts ----------

    /// <summary>Verifies that GetPosts with no posts returns an empty array.</summary>
    [Fact]
    public async Task GetPosts_WithNoPosts_ReturnsEmptyArray()
    {
        // Act
        var response = await Client.GetAsync("/api/blog");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var posts = await response.Content.ReadFromJsonAsync<List<PostResponse>>();
        Assert.Empty(posts!);
    }

    /// <summary>Verifies that GetPosts returns previously seeded posts, without requiring authentication.</summary>
    [Fact]
    public async Task GetPosts_WithSeededPosts_ReturnsThem()
    {
        // Arrange
        await SeedPostAsync("some-author-id");

        // Act
        var response = await Client.GetAsync("/api/blog");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var posts = await response.Content.ReadFromJsonAsync<List<PostResponse>>();
        Assert.Single(posts!);
    }

    // ---------- GetPost ----------

    /// <summary>Verifies that GetPost with an invalid Id returns 400 Bad Request.</summary>
    [Fact]
    public async Task GetPost_WithInvalidId_ReturnsBadRequest()
    {
        // Act
        var response = await Client.GetAsync("/api/blog/not-an-object-id");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Verifies that GetPost for a non-existent post returns 404 Not Found.</summary>
    [Fact]
    public async Task GetPost_NotFound_ReturnsNotFound()
    {
        // Act
        var response = await Client.GetAsync($"/api/blog/{DefaultId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifies that GetPost for an existing post returns it, without requiring authentication.</summary>
    [Fact]
    public async Task GetPost_ExistingPost_ReturnsPost()
    {
        // Arrange
        var post = await SeedPostAsync("some-author-id");

        // Act
        var response = await Client.GetAsync($"/api/blog/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var returned = await response.Content.ReadFromJsonAsync<PostResponse>();
        Assert.Equal(post.Title, returned!.Title);
    }

    /// <summary>Verifies that GetPost sets an ETag header, used by EditPost's If-Match check.</summary>
    [Fact]
    public async Task GetPost_ExistingPost_SetsETag()
    {
        // Arrange
        var post = await SeedPostAsync("some-author-id");

        // Act
        var response = await Client.GetAsync($"/api/blog/{post.Id}");

        // Assert
        Assert.NotNull(response.Headers.ETag);
    }

    // ---------- CreatePost ----------

    /// <summary>Verifies that CreatePost without a bearer token returns 401 Unauthorized.</summary>
    [Fact]
    public async Task CreatePost_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var createPostDto = new CreatePostDtoFactory().Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/blog", createPostDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Verifies that CreatePost with a valid token creates the post, authored by the caller.</summary>
    [Fact]
    public async Task CreatePost_Authenticated_ReturnsCreatedAuthoredByCaller()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync("author", role: Roles.Author);
        var createPostDto = new CreatePostDtoFactory().WithTitle("New Post").Build();

        // Act
        var response = await client.PostAsJsonAsync("/api/blog", createPostDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<PostResponse>();
        Assert.Equal("New Post", created!.Title);
        Assert.Equal(userId, created.AuthorId);
        Assert.NotNull(response.Headers.Location);
    }

    /// <summary>Verifies that CreatePost as a Commentator (no post-creation role) returns 403 Forbidden.</summary>
    [Fact]
    public async Task CreatePost_AsCommentator_ReturnsForbidden()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync("commentator", role: Roles.Commentator);
        var createPostDto = new CreatePostDtoFactory().Build();

        // Act
        var response = await client.PostAsJsonAsync("/api/blog", createPostDto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- BulkCreatePosts ----------

    /// <summary>Verifies that BulkCreatePosts without a bearer token returns 401 Unauthorized.</summary>
    [Fact]
    public async Task BulkCreatePosts_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var dtos = new List<CreatePostDto> { new CreatePostDtoFactory().Build() };

        // Act
        var response = await Client.PostAsJsonAsync("/api/blog/bulk", dtos);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Verifies that BulkCreatePosts with a valid token creates all posts, authored by the caller.</summary>
    [Fact]
    public async Task BulkCreatePosts_Authenticated_ReturnsCreatedAuthoredByCaller()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync("bulk-editor", role: Roles.Editor);
        var dtos = new List<CreatePostDto>
        {
            new CreatePostDtoFactory().WithTitle("First").Build(),
            new CreatePostDtoFactory().WithTitle("Second").Build()
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/blog/bulk", dtos);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<List<PostResponse>>();
        Assert.Equal(2, created!.Count);
        Assert.All(created, p => Assert.Equal(userId, p.AuthorId));
    }

    /// <summary>Verifies that BulkCreatePosts as an Author (single-post creation only) returns 403 Forbidden.</summary>
    [Fact]
    public async Task BulkCreatePosts_AsAuthor_ReturnsForbidden()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync("bulk-author", role: Roles.Author);
        var dtos = new List<CreatePostDto> { new CreatePostDtoFactory().Build() };

        // Act
        var response = await client.PostAsJsonAsync("/api/blog/bulk", dtos);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Verifies that BulkCreatePosts as a Commentator returns 403 Forbidden.</summary>
    [Fact]
    public async Task BulkCreatePosts_AsCommentator_ReturnsForbidden()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync("bulk-commentator", role: Roles.Commentator);
        var dtos = new List<CreatePostDto> { new CreatePostDtoFactory().Build() };

        // Act
        var response = await client.PostAsJsonAsync("/api/blog/bulk", dtos);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- EditPost ----------

    /// <summary>Verifies that EditPost with an invalid Id returns 400 Bad Request.</summary>
    [Fact]
    public async Task EditPost_WithInvalidId_ReturnsBadRequest()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync("editor");

        // Act
        var response = await client.PutAsJsonAsync("/api/blog/not-an-object-id", new EditPostDtoFactory().Build());

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Verifies that EditPost without a bearer token returns 401 Unauthorized.</summary>
    [Fact]
    public async Task EditPost_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PutAsJsonAsync($"/api/blog/{DefaultId}", new EditPostDtoFactory().Build());

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Verifies that EditPost for a non-existent post returns 404 Not Found.</summary>
    [Fact]
    public async Task EditPost_NotFound_ReturnsNotFound()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync("editor");

        // Act
        var response = await client.PutAsJsonAsync($"/api/blog/{DefaultId}", new EditPostDtoFactory().Build());

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifies that EditPost by a user who isn't the post's author returns 403 Forbidden.</summary>
    [Fact]
    public async Task EditPost_AsNonAuthor_ReturnsForbidden()
    {
        // Arrange
        var post = await SeedPostAsync("the-real-author-id");
        var (client, _) = await CreateAuthenticatedClientAsync("impostor");

        // Act
        var response = await client.PutAsJsonAsync($"/api/blog/{post.Id}", new EditPostDtoFactory().Build());

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Verifies that EditPost by the post's author succeeds and persists the new content.</summary>
    [Fact]
    public async Task EditPost_AsAuthor_UpdatesPost()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync("author");
        var post = await SeedPostAsync(userId);
        var editPostDto = new EditPostDtoFactory().WithTitle("Updated Title").Build();
        var eTag = (await Client.GetAsync($"/api/blog/{post.Id}")).Headers.ETag;

        // Act
        var response = await PutWithIfMatchAsync(client, $"/api/blog/{post.Id}", editPostDto, eTag!);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var getResponse = await Client.GetAsync($"/api/blog/{post.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<PostResponse>();
        Assert.Equal("Updated Title", updated!.Title);
    }

    /// <summary>Verifies that EditPost without an If-Match header returns 400 Bad Request.</summary>
    [Fact]
    public async Task EditPost_WithoutIfMatchHeader_ReturnsBadRequest()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync("author");
        var post = await SeedPostAsync(userId);

        // Act
        var response = await client.PutAsJsonAsync($"/api/blog/{post.Id}", new EditPostDtoFactory().Build());

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Verifies that EditPost with a stale If-Match (post already edited since) returns 412.</summary>
    [Fact]
    public async Task EditPost_WithStaleIfMatch_ReturnsPreconditionFailed()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync("author");
        var post = await SeedPostAsync(userId);
        var staleETag = (await Client.GetAsync($"/api/blog/{post.Id}")).Headers.ETag;

        await PutWithIfMatchAsync(client, $"/api/blog/{post.Id}",
            new EditPostDtoFactory().WithTitle("First Update").Build(), staleETag!);

        // Act: second edit still carries the pre-first-edit ETag.
        var response = await PutWithIfMatchAsync(client, $"/api/blog/{post.Id}",
            new EditPostDtoFactory().WithTitle("Second Update").Build(), staleETag!);

        // Assert
        Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);
    }

    // ---------- DeletePost ----------

    /// <summary>Verifies that DeletePost with an invalid Id returns 400 Bad Request.</summary>
    [Fact]
    public async Task DeletePost_WithInvalidId_ReturnsBadRequest()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync("deleter");

        // Act
        var response = await client.DeleteAsync("/api/blog/not-an-object-id");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Verifies that DeletePost without a bearer token returns 401 Unauthorized.</summary>
    [Fact]
    public async Task DeletePost_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.DeleteAsync($"/api/blog/{DefaultId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Verifies that DeletePost for a non-existent post returns 404 Not Found.</summary>
    [Fact]
    public async Task DeletePost_NotFound_ReturnsNotFound()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync("deleter");

        // Act
        var response = await client.DeleteAsync($"/api/blog/{DefaultId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifies that DeletePost by a user who isn't the post's author returns 403 Forbidden.</summary>
    [Fact]
    public async Task DeletePost_AsNonAuthor_ReturnsForbidden()
    {
        // Arrange
        var post = await SeedPostAsync("the-real-author-id");
        var (client, _) = await CreateAuthenticatedClientAsync("impostor");

        // Act
        var response = await client.DeleteAsync($"/api/blog/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Verifies that DeletePost by the post's author succeeds and actually removes the post.</summary>
    [Fact]
    public async Task DeletePost_AsAuthor_RemovesPost()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync("author");
        var post = await SeedPostAsync(userId);

        // Act
        var response = await client.DeleteAsync($"/api/blog/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var getResponse = await Client.GetAsync($"/api/blog/{post.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}