using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using BlogMVC.Data;
using BlogMVC.Dto;
using BlogMVC.Infrastructure.Interfaces;
using BlogMVC.Models;
using BlogMVC.Responses;
using BlogMVC.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace BlogMVC.Tests.IntegrationTests;

/// <summary>
///     Integration tests for <see cref="BlogController" /> against the real app — real MongoDB and real
///     JWT bearer auth (tokens from the actual "api/auth/login" endpoint).
/// </summary>
[Collection("BlogController")]
public class BlogControllerIntegrationTests(WebApplicationFactory<Program> factory)
    : BlogControllerTestBase(factory)
{
    /// <summary>Inserts a post directly via <see cref="IPostRepository" />, bypassing the API, for test setup.</summary>
    /// <param name="authorId">Id to set as the post's AuthorId.</param>
    /// <param name="author">Display name to set as the post's Author. Defaults to "Author".</param>
    /// <param name="publishDate">PublishDate/ModifiedDate to set on the post. Defaults to <see cref="DefaultDate" />.</param>
    /// <returns>The inserted <see cref="Post" />, including its generated Id.</returns>
    private async Task<Post> SeedPostAsync(string authorId, string author = "Author", DateTime? publishDate = null)
    {
        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        var post = new PostFactory()
            .WithAuthorId(authorId)
            .WithAuthor(author)
            .WithPublishDate(publishDate ?? DefaultDate)
            .WithModifiedDate(publishDate ?? DefaultDate)
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

    /// <summary>Verifies that GetPosts returns posts ordered newest-first by PublishDate.</summary>
    [Fact]
    public async Task GetPosts_WithMultiplePosts_ReturnsNewestFirst()
    {
        // Arrange
        var older = await SeedPostAsync("author-1", publishDate: DefaultDate);
        var newer = await SeedPostAsync("author-2", publishDate: DefaultDate.AddDays(1));

        // Act
        var response = await Client.GetAsync("/api/blog");

        // Assert
        var posts = await response.Content.ReadFromJsonAsync<List<PostResponse>>();
        Assert.Equal([newer.Id, older.Id], posts!.Select(p => p.Id));
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

    // ---------- CORS ----------

    /// <summary>
    ///     Verifies that a request from an allowed CORS origin (the hardcoded Vite dev server default,
    ///     see Program.cs) gets that origin echoed back in Access-Control-Allow-Origin.
    /// </summary>
    [Fact]
    public async Task GetPosts_WithAllowedOrigin_ReturnsAccessControlAllowOriginHeader()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/blog");
        request.Headers.Add("Origin", "http://localhost:5173");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
        Assert.Equal("http://localhost:5173", values!.Single());
    }

    /// <summary>Verifies that a request from a disallowed origin does not get an Access-Control-Allow-Origin header.</summary>
    [Fact]
    public async Task GetPosts_WithDisallowedOrigin_DoesNotReturnAccessControlAllowOriginHeader()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/blog");
        request.Headers.Add("Origin", "http://evil.example.com");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
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

    /// <summary>
    ///     Verifies that CreatePost with a malformed/tampered Bearer token returns 401 Unauthorized
    ///     (JWT bearer authentication rejects it before the policy or the action ever run).
    /// </summary>
    [Fact]
    public async Task CreatePost_WithMalformedBearerToken_ReturnsUnauthorized()
    {
        // Arrange
        var createPostDto = new CreatePostDtoFactory().Build();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/blog")
            { Content = JsonContent.Create(createPostDto) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-jwt-token");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    ///     Verifies CreatePost with an expired but validly-signed Bearer token returns 401 — JWT bearer
    ///     middleware enforces expiration, distinct from a garbage/tampered token.
    /// </summary>
    [Fact]
    public async Task CreatePost_WithExpiredBearerToken_ReturnsUnauthorized()
    {
        // Arrange
        var configuration = Factory.Services.GetRequiredService<IConfiguration>();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiredToken = new JwtSecurityToken(
            configuration["Jwt:Issuer"],
            configuration["Jwt:Audience"],
            [new Claim(ClaimTypes.NameIdentifier, "some-user-id")],
            DateTime.UtcNow.AddHours(-2),
            DateTime.UtcNow.AddHours(-1),
            credentials);
        var tokenString = new JwtSecurityTokenHandler().WriteToken(expiredToken);

        var createPostDto = new CreatePostDtoFactory().Build();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/blog")
            { Content = JsonContent.Create(createPostDto) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenString);

        // Act
        var response = await Client.SendAsync(request);

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

    /// <summary>Verifies that CreatePost as an Administrator succeeds (Posts.Create).</summary>
    [Fact]
    public async Task CreatePost_AsAdministrator_ReturnsCreated()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync("admin", role: Roles.Administrator);
        var createPostDto = new CreatePostDtoFactory().Build();

        // Act
        var response = await client.PostAsJsonAsync("/api/blog", createPostDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<PostResponse>();
        Assert.Equal(userId, created!.AuthorId);
    }

    /// <summary>Verifies that CreatePost as an Editor succeeds (Posts.Create).</summary>
    [Fact]
    public async Task CreatePost_AsEditor_ReturnsCreated()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync("editor", role: Roles.Editor);
        var createPostDto = new CreatePostDtoFactory().Build();

        // Act
        var response = await client.PostAsJsonAsync("/api/blog", createPostDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<PostResponse>();
        Assert.Equal(userId, created!.AuthorId);
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

    /// <summary>Verifies that CreatePost with a missing (empty) Title returns 400 Bad Request (DTO validation).</summary>
    [Fact]
    public async Task CreatePost_WithMissingTitle_ReturnsBadRequest()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync("author", role: Roles.Author);
        var createPostDto = new CreatePostDtoFactory().WithTitle("").Build();

        // Act
        var response = await client.PostAsJsonAsync("/api/blog", createPostDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

    /// <summary>Verifies that BulkCreatePosts as an Administrator succeeds (Posts.CreateBulk).</summary>
    [Fact]
    public async Task BulkCreatePosts_AsAdministrator_ReturnsCreatedAuthoredByCaller()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync("bulk-admin", role: Roles.Administrator);
        var dtos = new List<CreatePostDto> { new CreatePostDtoFactory().Build() };

        // Act
        var response = await client.PostAsJsonAsync("/api/blog/bulk", dtos);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<List<PostResponse>>();
        Assert.All(created!, p => Assert.Equal(userId, p.AuthorId));
    }

    /// <summary>
    ///     Verifies that BulkCreatePosts with an invalid item in the list (missing Title) returns 400 Bad
    ///     Request — collection items are validated individually, same as a single CreatePostDto.
    /// </summary>
    [Fact]
    public async Task BulkCreatePosts_WithInvalidItemInList_ReturnsBadRequest()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClientAsync("bulk-editor", role: Roles.Editor);
        var dtos = new List<CreatePostDto>
        {
            new CreatePostDtoFactory().Build(),
            new CreatePostDtoFactory().WithTitle("").Build()
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/blog/bulk", dtos);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
        var (client, _) = await CreateAuthenticatedClientAsync("author", role: Roles.Author);

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
        var (client, _) = await CreateAuthenticatedClientAsync("author", role: Roles.Author);

        // Act
        var response = await client.PutAsJsonAsync($"/api/blog/{DefaultId}", new EditPostDtoFactory().Build());

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifies that EditPost as a Commentator (no edit permission) returns 403 Forbidden.</summary>
    [Fact]
    public async Task EditPost_AsCommentator_ReturnsForbidden()
    {
        // Arrange
        var post = await SeedPostAsync("some-author-id");
        var (client, _) = await CreateAuthenticatedClientAsync("commentator", role: Roles.Commentator);

        // Act
        var response = await client.PutAsJsonAsync($"/api/blog/{post.Id}", new EditPostDtoFactory().Build());

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Verifies that EditPost by a user who isn't the post's author returns 403 Forbidden.</summary>
    [Fact]
    public async Task EditPost_AsNonAuthor_ReturnsForbidden()
    {
        // Arrange
        var post = await SeedPostAsync("the-real-author-id");
        var (client, _) = await CreateAuthenticatedClientAsync("impostor", role: Roles.Author);

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
        var (client, userId) = await CreateAuthenticatedClientAsync("author", role: Roles.Author);
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

    /// <summary>
    ///     Verifies that EditPost by an Administrator succeeds even for a post the Administrator doesn't own
    ///     (Posts.EditAny bypasses the ownership check that gates Author/Editor).
    /// </summary>
    [Fact]
    public async Task EditPost_AsAdministrator_UpdatesAnyPost()
    {
        // Arrange
        var post = await SeedPostAsync("the-real-author-id");
        var (client, _) = await CreateAuthenticatedClientAsync("admin", role: Roles.Administrator);
        var editPostDto = new EditPostDtoFactory().WithTitle("Updated By Admin").Build();
        var eTag = (await Client.GetAsync($"/api/blog/{post.Id}")).Headers.ETag;

        // Act
        var response = await PutWithIfMatchAsync(client, $"/api/blog/{post.Id}", editPostDto, eTag!);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var getResponse = await Client.GetAsync($"/api/blog/{post.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<PostResponse>();
        Assert.Equal("Updated By Admin", updated!.Title);
    }

    /// <summary>Verifies that EditPost by an Editor on their own post succeeds (Posts.EditOwn).</summary>
    [Fact]
    public async Task EditPost_AsEditor_OwnPost_UpdatesPost()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync("editor", role: Roles.Editor);
        var post = await SeedPostAsync(userId);
        var editPostDto = new EditPostDtoFactory().WithTitle("Updated By Editor").Build();
        var eTag = (await Client.GetAsync($"/api/blog/{post.Id}")).Headers.ETag;

        // Act
        var response = await PutWithIfMatchAsync(client, $"/api/blog/{post.Id}", editPostDto, eTag!);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var getResponse = await Client.GetAsync($"/api/blog/{post.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<PostResponse>();
        Assert.Equal("Updated By Editor", updated!.Title);
    }

    /// <summary>
    ///     Verifies that EditPost by an Editor on a post they don't own returns 403 Forbidden
    ///     (Posts.EditOwn doesn't grant Posts.EditAny — only Administrator gets that).
    /// </summary>
    [Fact]
    public async Task EditPost_AsEditor_NonOwnPost_ReturnsForbidden()
    {
        // Arrange
        var post = await SeedPostAsync("the-real-author-id");
        var (client, _) = await CreateAuthenticatedClientAsync("editor", role: Roles.Editor);

        // Act
        var response = await client.PutAsJsonAsync($"/api/blog/{post.Id}", new EditPostDtoFactory().Build());

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    ///     Verifies EditPost with an empty Title returns 400 via DTO validation — a valid If-Match is
    ///     supplied so the 400 can't instead come from the separate If-Match check.
    /// </summary>
    [Fact]
    public async Task EditPost_WithMissingTitle_ReturnsBadRequest()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync("author", role: Roles.Author);
        var post = await SeedPostAsync(userId);
        var editPostDto = new EditPostDtoFactory().WithTitle("").Build();
        var eTag = (await Client.GetAsync($"/api/blog/{post.Id}")).Headers.ETag;

        // Act
        var response = await PutWithIfMatchAsync(client, $"/api/blog/{post.Id}", editPostDto, eTag!);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Verifies that EditPost without an If-Match header returns 400 Bad Request.</summary>
    [Fact]
    public async Task EditPost_WithoutIfMatchHeader_ReturnsBadRequest()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync("author", role: Roles.Author);
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
        var (client, userId) = await CreateAuthenticatedClientAsync("author", role: Roles.Author);
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
        var (client, _) = await CreateAuthenticatedClientAsync("deleter", role: Roles.Author);

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
        var (client, _) = await CreateAuthenticatedClientAsync("deleter", role: Roles.Author);

        // Act
        var response = await client.DeleteAsync($"/api/blog/{DefaultId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Verifies that DeletePost as a Commentator (no delete permission) returns 403 Forbidden.</summary>
    [Fact]
    public async Task DeletePost_AsCommentator_ReturnsForbidden()
    {
        // Arrange
        var post = await SeedPostAsync("some-author-id");
        var (client, _) = await CreateAuthenticatedClientAsync("commentator", role: Roles.Commentator);

        // Act
        var response = await client.DeleteAsync($"/api/blog/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Verifies that DeletePost by a user who isn't the post's author returns 403 Forbidden.</summary>
    [Fact]
    public async Task DeletePost_AsNonAuthor_ReturnsForbidden()
    {
        // Arrange
        var post = await SeedPostAsync("the-real-author-id");
        var (client, _) = await CreateAuthenticatedClientAsync("impostor", role: Roles.Author);

        // Act
        var response = await client.DeleteAsync($"/api/blog/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    ///     Verifies that DeletePost by an Administrator succeeds even for a post the Administrator doesn't own
    ///     (Posts.DeleteAny bypasses the ownership check that gates Author/Editor).
    /// </summary>
    [Fact]
    public async Task DeletePost_AsAdministrator_RemovesAnyPost()
    {
        // Arrange
        var post = await SeedPostAsync("the-real-author-id");
        var (client, _) = await CreateAuthenticatedClientAsync("admin", role: Roles.Administrator);

        // Act
        var response = await client.DeleteAsync($"/api/blog/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var getResponse = await Client.GetAsync($"/api/blog/{post.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    /// <summary>Verifies that DeletePost by an Editor on their own post succeeds (Posts.DeleteOwn).</summary>
    [Fact]
    public async Task DeletePost_AsEditor_OwnPost_RemovesPost()
    {
        // Arrange
        var (client, userId) = await CreateAuthenticatedClientAsync("editor", role: Roles.Editor);
        var post = await SeedPostAsync(userId);

        // Act
        var response = await client.DeleteAsync($"/api/blog/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var getResponse = await Client.GetAsync($"/api/blog/{post.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    /// <summary>
    ///     Verifies that DeletePost by an Editor on a post they don't own returns 403 Forbidden
    ///     (Posts.DeleteOwn doesn't grant Posts.DeleteAny — only Administrator gets that).
    /// </summary>
    [Fact]
    public async Task DeletePost_AsEditor_NonOwnPost_ReturnsForbidden()
    {
        // Arrange
        var post = await SeedPostAsync("the-real-author-id");
        var (client, _) = await CreateAuthenticatedClientAsync("editor", role: Roles.Editor);

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
        var (client, userId) = await CreateAuthenticatedClientAsync("author", role: Roles.Author);
        var post = await SeedPostAsync(userId);

        // Act
        var response = await client.DeleteAsync($"/api/blog/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var getResponse = await Client.GetAsync($"/api/blog/{post.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}