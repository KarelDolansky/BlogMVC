using System.Security.Claims;
using BlogMVC.Controllers;
using BlogMVC.Data;
using BlogMVC.Dto;
using BlogMVC.Models;
using BlogMVC.Responses;
using BlogMVC.Results;
using BlogMVC.Services;
using BlogMVC.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BlogMVC.Tests.Controllers;

/// <summary>
///     Unit tests for <see cref="BlogController" /> with a mocked <see cref="IPostService" /> — verifies
///     each action's returned status by Id validity, ownership/claims, and the service's result.
/// </summary>
public class BlogControllerTests
{
    /// <summary>The controller under test, wired to the mocked <see cref="IPostService" />.</summary>
    private readonly BlogController _blogController;

    /// <summary>Default author name used for the authenticated caller and matching authored posts.</summary>
    private readonly string _defaultAuthor = "defaultAuthor";

    /// <summary>Default author Id used for the authenticated caller and matching authored posts.</summary>
    private readonly string _defaultAuthorId = "defaultAuthorId";

    /// <summary>Default valid MongoDB ObjectId string used as both the caller's Id and a post Id.</summary>
    private readonly string _defaultId = "507f1f77bcf86cd799439011";

    /// <summary>Mocked <see cref="IPostService" /> used to stub post CRUD outcomes.</summary>
    private readonly Mock<IPostService> _postServiceMock;

    /// <summary>
    ///     Creates the controller under test with a fresh <see cref="IPostService" /> mock and an
    ///     authenticated <see cref="ClaimsPrincipal" /> using the default Id/name claims.
    /// </summary>
    public BlogControllerTests()
    {
        _postServiceMock = new Mock<IPostService>();
        _blogController = new BlogController(_postServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([
                        new Claim(ClaimTypes.NameIdentifier, _defaultId),
                        new Claim(ClaimTypes.Name, _defaultAuthor)
                    ], "test"))
                }
            }
        };
    }

    /// <summary>Sets the request's If-Match header to the given version, quoted as an ETag.</summary>
    /// <param name="version">The version value to send back as the If-Match ETag.</param>
    private void SetIfMatchHeader(long version)
    {
        _blogController.HttpContext.Request.Headers.IfMatch = $"\"{version}\"";
    }

    /// <summary>Replaces the current user with an empty <see cref="ClaimsIdentity" /> carrying no claims.</summary>
    private void SetEmptyUser()
    {
        _blogController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };
    }

    /// <summary>
    ///     Asserts that a <see cref="PostResponse" /> carries the same data as the <see cref="Post" /> it was mapped
    ///     from.
    /// </summary>
    private static void AssertMatches(Post expected, PostResponse actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Title, actual.Title);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.Content, actual.Content);
        Assert.Equal(expected.AuthorId, actual.AuthorId);
        Assert.Equal(expected.Author, actual.Author);
        Assert.Equal(expected.PublishDate, actual.PublishDate);
        Assert.Equal(expected.ModifiedDate, actual.ModifiedDate);
    }

    /// <summary>Replaces the current user with only the Id claim set, omitting the name claim.</summary>
    private void SetUserWithoutName()
    {
        _blogController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(ClaimTypes.NameIdentifier, _defaultId)
                ], "test"))
            }
        };
    }

    /// <summary>Sets the current user's Id/Name to the defaults plus an extra claim (e.g. a permission claim).</summary>
    /// <param name="claimType">The claim type to add (e.g. <see cref="Permissions.ClaimType" />).</param>
    /// <param name="claimValue">The claim value to add (e.g. <see cref="Permissions.Posts.EditAny" />).</param>
    private void SetUserWithClaim(string claimType, string claimValue)
    {
        _blogController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(ClaimTypes.NameIdentifier, _defaultId),
                    new Claim(ClaimTypes.Name, _defaultAuthor),
                    new Claim(claimType, claimValue)
                ], "test"))
            }
        };
    }

    // ---------- GetPosts ----------

    /// <summary>Verifies that GetPosts returns Ok with all posts from the service.</summary>
    [Fact]
    public async Task GetPosts_ReturnsOkWithAllPosts()
    {
        // Arrange
        var posts = new List<Post> { new PostFactory().WithId(_defaultId).Build() };
        _postServiceMock.Setup(p => p.GetPostsAsync()).ReturnsAsync(posts);

        // Act
        var response = await _blogController.GetPosts();

        // Assert
        var result = Assert.IsType<OkObjectResult>(response.Result);
        var returned = Assert.IsType<List<PostResponse>>(result.Value);
        Assert.Equal(posts.Count, returned.Count);
        AssertMatches(posts[0], returned[0]);
    }

    // ---------- GetPost ----------

    /// <summary>Verifies that GetPost with an invalid post Id returns BadRequest.</summary>
    [Fact]
    public async Task GetPost_WithWrongPostId_ReturnsBadRequest()
    {
        // Arrange
        var id = "wrongPostId";

        // Act
        var response = await _blogController.GetPost(id);

        // Assert
        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    /// <summary>Verifies that GetPost when the post does not exist returns NotFound.</summary>
    [Fact]
    public async Task GetPost_NotFoundPost_ReturnsNotFound()
    {
        // Arrange
        var id = _defaultId;
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync((Post?)null);

        // Act
        var response = await _blogController.GetPost(id);

        // Assert
        Assert.IsType<NotFoundResult>(response.Result);
    }

    /// <summary>Verifies that GetPost returns Ok with the found post.</summary>
    [Fact]
    public async Task GetPost_ReturnsOkWithPost()
    {
        // Arrange
        var id = _defaultId;
        var post = new PostFactory().WithId(_defaultId).Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);

        // Act
        var response = await _blogController.GetPost(id);

        // Assert
        var result = Assert.IsType<OkObjectResult>(response.Result);
        var returned = Assert.IsType<PostResponse>(result.Value);
        AssertMatches(post, returned);
    }

    // ---------- CreatePost ----------

    /// <summary>Verifies that CreatePost without a logged-in user's Id returns Unauthorized.</summary>
    [Fact]
    public async Task CreatePost_WithEmptyUserId_ReturnsUnauthorized()
    {
        // Arrange
        SetEmptyUser();
        var createPostDto = new CreatePostDtoFactory().Build();

        // Act
        var response = await _blogController.CreatePost(createPostDto);

        // Assert
        Assert.IsType<UnauthorizedResult>(response.Result);
    }

    /// <summary>Verifies that CreatePost without a logged-in user's name returns Unauthorized.</summary>
    [Fact]
    public async Task CreatePost_WithEmptyUserName_ReturnsUnauthorized()
    {
        // Arrange
        SetUserWithoutName();
        var createPostDto = new CreatePostDtoFactory().Build();

        // Act
        var response = await _blogController.CreatePost(createPostDto);

        // Assert
        Assert.IsType<UnauthorizedResult>(response.Result);
    }

    /// <summary>Verifies that CreatePost returns CreatedAtRoute ("GetPost") with the created post.</summary>
    [Fact]
    public async Task CreatePost_ReturnsCreatedAtRouteWithPost()
    {
        // Arrange
        var createPostDto = new CreatePostDtoFactory().Build();
        var post = new PostFactory()
            .WithAuthor(_defaultAuthor)
            .WithAuthorId(_defaultAuthorId)
            .WithId(_defaultId)
            .Build();
        _postServiceMock.Setup(p => p.AddPostAsync(createPostDto, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(post);

        // Act
        var response = await _blogController.CreatePost(createPostDto);

        // Assert
        var result = Assert.IsType<CreatedAtRouteResult>(response.Result);
        Assert.Equal("GetPost", result.RouteName);
        Assert.Equal(_defaultId, result.RouteValues!["id"]);
        var returned = Assert.IsType<PostResponse>(result.Value);
        AssertMatches(post, returned);
    }

    // ---------- BulkCreatePosts ----------

    /// <summary>Verifies that BulkCreatePosts without a logged-in user's Id returns Unauthorized.</summary>
    [Fact]
    public async Task BulkCreatePosts_WithEmptyUserId_ReturnsUnauthorized()
    {
        // Arrange
        SetEmptyUser();
        var createPostDtoes = new List<CreatePostDto> { new CreatePostDtoFactory().Build() };

        // Act
        var response = await _blogController.BulkCreatePosts(createPostDtoes);

        // Assert
        Assert.IsType<UnauthorizedResult>(response.Result);
    }

    /// <summary>Verifies that BulkCreatePosts without a logged-in user's name returns Unauthorized.</summary>
    [Fact]
    public async Task BulkCreatePosts_WithEmptyUserName_ReturnsUnauthorized()
    {
        // Arrange
        SetUserWithoutName();
        var createPostDtoes = new List<CreatePostDto> { new CreatePostDtoFactory().Build() };

        // Act
        var response = await _blogController.BulkCreatePosts(createPostDtoes);

        // Assert
        Assert.IsType<UnauthorizedResult>(response.Result);
    }

    /// <summary>Verifies that BulkCreatePosts returns 201 Created with the created posts.</summary>
    [Fact]
    public async Task BulkCreatePosts_ReturnsCreatedWithPosts()
    {
        // Arrange
        var createPostDtoes = new List<CreatePostDto> { new CreatePostDtoFactory().Build() };
        var posts = new List<Post>
        {
            new PostFactory()
                .WithAuthor(_defaultAuthor)
                .WithAuthorId(_defaultAuthorId)
                .WithId(_defaultId)
                .Build()
        };
        _postServiceMock
            .Setup(p => p.AddBulkPostAsync(createPostDtoes, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(posts);

        // Act
        var response = await _blogController.BulkCreatePosts(createPostDtoes);

        // Assert
        var result = Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        var returned = Assert.IsType<List<PostResponse>>(result.Value);
        Assert.Equal(posts.Count, returned.Count);
        AssertMatches(posts[0], returned[0]);
    }

    // ---------- EditPost ----------

    /// <summary>Verifies that EditPost with an invalid post Id returns BadRequest.</summary>
    [Fact]
    public async Task EditPost_WithWrongPostId_ReturnsBadRequest()
    {
        // Arrange
        var id = "wrongPostId";
        var editPostDto = new EditPostDtoFactory().Build();

        // Act
        var response = await _blogController.EditPost(id, editPostDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(response);
    }

    /// <summary>Verifies that EditPost without a logged-in user's Id returns Unauthorized.</summary>
    [Fact]
    public async Task EditPost_WithEmptyUserId_ReturnsUnauthorized()
    {
        // Arrange
        SetEmptyUser();
        var id = _defaultId;
        var editPostDto = new EditPostDtoFactory().Build();

        // Act
        var response = await _blogController.EditPost(id, editPostDto);

        // Assert
        Assert.IsType<UnauthorizedResult>(response);
    }

    /// <summary>Verifies that EditPost when the post does not exist returns NotFound.</summary>
    [Fact]
    public async Task EditPost_NotFoundPost_ReturnsNotFound()
    {
        // Arrange
        var id = _defaultId;
        var editPostDto = new EditPostDtoFactory().Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync((Post?)null);

        // Act
        var response = await _blogController.EditPost(id, editPostDto);

        // Assert
        Assert.IsType<NotFoundResult>(response);
    }

    /// <summary>Verifies that EditPost when the logged-in user is not the post's author returns Forbid.</summary>
    [Fact]
    public async Task EditPost_NotAuthor_ReturnsForbid()
    {
        // Arrange
        var id = _defaultId;
        var editPostDto = new EditPostDtoFactory().Build();
        var post = new PostFactory().WithAuthorId("different-author-id").Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);

        // Act
        var response = await _blogController.EditPost(id, editPostDto);

        // Assert
        Assert.IsType<ForbidResult>(response);
    }

    /// <summary>
    ///     Verifies that EditPost when the caller isn't the author but holds the Posts.EditAny claim
    ///     bypasses the ownership check and proceeds to the service call.
    /// </summary>
    [Fact]
    public async Task EditPost_NotAuthorWithEditAnyClaim_BypassesOwnershipCheck()
    {
        // Arrange
        var id = _defaultId;
        var editPostDto = new EditPostDtoFactory().Build();
        var post = new PostFactory().WithAuthorId("different-author-id").Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);
        _postServiceMock.Setup(p => p.EditPostAsync(id, editPostDto, 0)).ReturnsAsync(PostUpdateResult.Success);
        SetUserWithClaim(Permissions.ClaimType, Permissions.Posts.EditAny);
        SetIfMatchHeader(0);

        // Act
        var response = await _blogController.EditPost(id, editPostDto);

        // Assert
        Assert.IsType<NoContentResult>(response);
    }

    /// <summary>
    ///     Verifies that EditPost when the caller isn't the author and only holds Posts.EditOwn (not
    ///     Posts.EditAny) still returns Forbid — the claim value, not just its type, is what's checked.
    /// </summary>
    [Fact]
    public async Task EditPost_NotAuthorWithEditOwnClaim_ReturnsForbid()
    {
        // Arrange
        var id = _defaultId;
        var editPostDto = new EditPostDtoFactory().Build();
        var post = new PostFactory().WithAuthorId("different-author-id").Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);
        SetUserWithClaim(Permissions.ClaimType, Permissions.Posts.EditOwn);

        // Act
        var response = await _blogController.EditPost(id, editPostDto);

        // Assert
        Assert.IsType<ForbidResult>(response);
    }

    /// <summary>Verifies that EditPost without an If-Match header returns BadRequest.</summary>
    [Fact]
    public async Task EditPost_WithoutIfMatchHeader_ReturnsBadRequest()
    {
        // Arrange
        var id = _defaultId;
        var editPostDto = new EditPostDtoFactory().Build();
        var post = new PostFactory().WithAuthorId(_defaultId).Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);

        // Act
        var response = await _blogController.EditPost(id, editPostDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(response);
    }

    /// <summary>Verifies that EditPost when the service reports the post missing returns NotFound.</summary>
    [Fact]
    public async Task EditPost_EditReturnsNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = _defaultId;
        var editPostDto = new EditPostDtoFactory().Build();
        var post = new PostFactory().WithAuthorId(_defaultId).Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);
        _postServiceMock.Setup(p => p.EditPostAsync(id, editPostDto, 0)).ReturnsAsync(PostUpdateResult.NotFound);
        SetIfMatchHeader(0);

        // Act
        var response = await _blogController.EditPost(id, editPostDto);

        // Assert
        var result = Assert.IsType<NotFoundObjectResult>(response);
        Assert.Equal("Post not found.", result.Value);
    }

    /// <summary>Verifies that EditPost when the service reports a stale version returns 412 Precondition Failed.</summary>
    [Fact]
    public async Task EditPost_EditReturnsConflict_ReturnsPreconditionFailed()
    {
        // Arrange
        var id = _defaultId;
        var editPostDto = new EditPostDtoFactory().Build();
        var post = new PostFactory().WithAuthorId(_defaultId).Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);
        _postServiceMock.Setup(p => p.EditPostAsync(id, editPostDto, 0)).ReturnsAsync(PostUpdateResult.Conflict);
        SetIfMatchHeader(0);

        // Act
        var response = await _blogController.EditPost(id, editPostDto);

        // Assert
        var result = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status412PreconditionFailed, result.StatusCode);
    }

    /// <summary>Verifies that EditPost returns NoContent on success.</summary>
    [Fact]
    public async Task EditPost_EditSucceeds_ReturnsNoContent()
    {
        // Arrange
        var id = _defaultId;
        var editPostDto = new EditPostDtoFactory().Build();
        var post = new PostFactory().WithAuthorId(_defaultId).Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);
        _postServiceMock.Setup(p => p.EditPostAsync(id, editPostDto, 0)).ReturnsAsync(PostUpdateResult.Success);
        SetIfMatchHeader(0);

        // Act
        var response = await _blogController.EditPost(id, editPostDto);

        // Assert
        Assert.IsType<NoContentResult>(response);
    }

    // ---------- DeletePost ----------

    /// <summary>Verifies that DeletePost with an invalid post Id returns BadRequest.</summary>
    [Fact]
    public async Task DeletePost_WithWrongPostId_ReturnsBadRequest()
    {
        // Arrange
        var id = "wrongPostId";

        // Act
        var response = await _blogController.DeletePost(id);

        // Assert
        Assert.IsType<BadRequestObjectResult>(response);
    }

    /// <summary>Verifies that DeletePost without a logged-in user's Id returns Unauthorized.</summary>
    [Fact]
    public async Task DeletePost_WithEmptyUserId_ReturnsUnauthorized()
    {
        // Arrange
        SetEmptyUser();
        var id = _defaultId;

        // Act
        var response = await _blogController.DeletePost(id);

        // Assert
        Assert.IsType<UnauthorizedResult>(response);
    }

    /// <summary>Verifies that DeletePost when the post does not exist returns NotFound.</summary>
    [Fact]
    public async Task DeletePost_NotFoundPost_ReturnsNotFound()
    {
        // Arrange
        var id = _defaultId;
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync((Post?)null);

        // Act
        var response = await _blogController.DeletePost(id);

        // Assert
        Assert.IsType<NotFoundResult>(response);
    }

    /// <summary>Verifies that DeletePost when the logged-in user is not the post's author returns Forbid.</summary>
    [Fact]
    public async Task DeletePost_NotAuthor_ReturnsForbid()
    {
        // Arrange
        var id = _defaultId;
        var post = new PostFactory().WithAuthorId("different-author-id").Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);

        // Act
        var response = await _blogController.DeletePost(id);

        // Assert
        Assert.IsType<ForbidResult>(response);
    }

    /// <summary>
    ///     Verifies that DeletePost when the caller isn't the author but holds the Posts.DeleteAny claim
    ///     bypasses the ownership check and proceeds to the service call.
    /// </summary>
    [Fact]
    public async Task DeletePost_NotAuthorWithDeleteAnyClaim_BypassesOwnershipCheck()
    {
        // Arrange
        var id = _defaultId;
        var post = new PostFactory().WithAuthorId("different-author-id").Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);
        _postServiceMock.Setup(p => p.DeletePostAsync(_defaultId)).ReturnsAsync(true);
        SetUserWithClaim(Permissions.ClaimType, Permissions.Posts.DeleteAny);

        // Act
        var response = await _blogController.DeletePost(id);

        // Assert
        Assert.IsType<NoContentResult>(response);
    }

    /// <summary>
    ///     Verifies that DeletePost when the caller isn't the author and only holds Posts.DeleteOwn (not
    ///     Posts.DeleteAny) still returns Forbid — the claim value, not just its type, is what's checked.
    /// </summary>
    [Fact]
    public async Task DeletePost_NotAuthorWithDeleteOwnClaim_ReturnsForbid()
    {
        // Arrange
        var id = _defaultId;
        var post = new PostFactory().WithAuthorId("different-author-id").Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);
        SetUserWithClaim(Permissions.ClaimType, Permissions.Posts.DeleteOwn);

        // Act
        var response = await _blogController.DeletePost(id);

        // Assert
        Assert.IsType<ForbidResult>(response);
    }

    /// <summary>Verifies that DeletePost when the service fails to delete the post returns NotFound.</summary>
    [Fact]
    public async Task DeletePost_DeleteReturnsFalse_ReturnsNotFound()
    {
        // Arrange
        var id = _defaultId;
        var post = new PostFactory().WithAuthorId(_defaultId).Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);
        _postServiceMock.Setup(p => p.DeletePostAsync(_defaultId)).ReturnsAsync(false);

        // Act
        var response = await _blogController.DeletePost(id);

        // Assert
        var result = Assert.IsType<NotFoundObjectResult>(response);
        Assert.Equal("Post not found.", result.Value);
    }

    /// <summary>Verifies that DeletePost returns NoContent on success.</summary>
    [Fact]
    public async Task DeletePost_DeleteSucceeds_ReturnsNoContent()
    {
        // Arrange
        var id = _defaultId;
        var post = new PostFactory().WithAuthorId(_defaultId).Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);
        _postServiceMock.Setup(p => p.DeletePostAsync(_defaultId)).ReturnsAsync(true);

        // Act
        var response = await _blogController.DeletePost(id);

        // Assert
        Assert.IsType<NoContentResult>(response);
    }
}