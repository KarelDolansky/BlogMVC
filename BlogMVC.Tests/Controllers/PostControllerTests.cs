using System.Security.Claims;
using BlogMVC.Controllers;
using BlogMVC.Models;
using BlogMVC.Services;
using BlogMVC.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BlogMVC.Tests.Controllers;

/// <summary>
///     Unit tests for <see cref="PostController" /> using a mocked <see cref="IPostService" />.
///     Verify the return type of each action (View/NotFound/Unauthorized/Forbid/Redirect)
///     depending on Id validity, model state, and whether the logged-in user is the author.
///     Tests are grouped by action (Details, Create, Edit, Delete).
/// </summary>
public class PostControllerTests
{
    private readonly string _defaultAuthor = "defaultAuthor";
    private readonly string _defaultAuthorId = "defaultAuthorId";
    private readonly string _defaultId = "507f1f77bcf86cd799439011";
    private readonly PostController _postController;
    private readonly Mock<IPostService> _postServiceMock;

    public PostControllerTests()
    {
        _postServiceMock = new Mock<IPostService>();
        _postController = new PostController(_postServiceMock.Object);

        _postController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(ClaimTypes.NameIdentifier, _defaultId),
                    new Claim(ClaimTypes.Name, _defaultAuthor)
                ], "test"))
            }
        };
    }

    private void SetEmptyUser()
    {
        _postController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };
    }

    private void SetUserWithoutName()
    {
        _postController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(ClaimTypes.NameIdentifier, _defaultId)
                ], "test"))
            }
        };
    }

    // ---------- Details ----------

    /// <summary>Verifies that Details with an invalid post Id returns NotFound.</summary>
    [Fact]
    public async Task Details_WithWrongPostId_ReturnsNotFound()
    {
        // Arrange
        var id = "wrongPostId";

        // Act
        var response = await _postController.Details(id);

        // Assert
        Assert.IsType<NotFoundResult>(response);
    }

    /// <summary>Verifies that Details when the post does not exist returns NotFound.</summary>
    [Fact]
    public async Task Details_NotFoundPost_ReturnsNotFound()
    {
        // Arrange
        var id = _defaultId;
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync((Post?)null);

        // Act
        var response = await _postController.Details(id);

        // Assert
        Assert.IsType<NotFoundResult>(response);
    }

    /// <summary>Verifies that Details returns a View.</summary>
    [Fact]
    public async Task Details_ReturnsView()
    {
        // Arrange
        var id = _defaultId;
        var post = new PostFactory().Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);

        // Act
        var response = await _postController.Details(id);

        //Assert
        Assert.IsType<ViewResult>(response);
        Assert.Equal(post, ((ViewResult)response).Model);
    }

    // ---------- Create GET ----------

    /// <summary>Verifies that Create returns a View.</summary>
    [Fact]
    public void Create_ReturnsView()
    {
        // Act
        var response = _postController.Create();

        //Assert
        Assert.IsType<ViewResult>(response);
    }

    // ---------- Create POST ----------

    /// <summary>Verifies that Create (POST) with an invalid title model returns the View again.</summary>
    [Fact]
    public async Task Create_POST_WithModelInvalidTitle_ReturnView()
    {
        // Arrange
        var createPostDto = new CreatePostDtoFactory()
            .WithTitle("")
            .Build();
        _postController.ModelState.AddModelError("Title", "Required");

        //Act
        var response = await _postController.Create(createPostDto);

        //Assert
        Assert.IsType<ViewResult>(response);
        Assert.Equal(createPostDto, ((ViewResult)response).Model);
    }

    /// <summary>Verifies that Create (POST) with an invalid content model returns the View again.</summary>
    [Fact]
    public async Task Create_POST_WithModelInvalidContent_ReturnView()
    {
        // Arrange
        var createPostDto = new CreatePostDtoFactory()
            .WithContent("")
            .Build();
        _postController.ModelState.AddModelError("Content", "Required");

        //Act
        var response = await _postController.Create(createPostDto);

        //Assert
        Assert.IsType<ViewResult>(response);
        Assert.Equal(createPostDto, ((ViewResult)response).Model);
    }

    /// <summary>Verifies that Create (POST) with an invalid description model returns the View again.</summary>
    [Fact]
    public async Task Create_POST_WithModelInvalidDescription_ReturnView()
    {
        // Arrange
        var createPostDto = new CreatePostDtoFactory()
            .WithDescription("")
            .Build();
        _postController.ModelState.AddModelError("Description", "Required");

        //Act
        var response = await _postController.Create(createPostDto);

        //Assert
        Assert.IsType<ViewResult>(response);
        Assert.Equal(createPostDto, ((ViewResult)response).Model);
    }

    /// <summary>Verifies that Create (POST) without a logged-in user's Id returns Unauthorized.</summary>
    [Fact]
    public async Task Create_POST_WithEmptyUserId_ReturnsUnauthorized()
    {
        //Arrange
        SetEmptyUser();
        var createPostDto = new CreatePostDtoFactory().Build();

        //Act
        var response = await _postController.Create(createPostDto);

        //Assert
        Assert.IsType<UnauthorizedResult>(response);
    }

    /// <summary>Verifies that Create (POST) without a logged-in user's name returns Unauthorized.</summary>
    [Fact]
    public async Task Create_POST_WithEmptyUserName_ReturnsUnauthorized()
    {
        //Arrange
        SetUserWithoutName();
        var createPostDto = new CreatePostDtoFactory().Build();

        //Act
        var response = await _postController.Create(createPostDto);

        //Assert
        Assert.IsType<UnauthorizedResult>(response);
    }

    /// <summary>Verifies that Create (POST) with a valid model returns a redirect (RedirectToAction).</summary>
    [Fact]
    public async Task Create_POST_WithModelValid_ReturnRedirectToAction()
    {
        //Arrange
        var createPostDto = new CreatePostDtoFactory().Build();
        var post = new PostFactory()
            .WithAuthor(_defaultAuthor)
            .WithAuthorId(_defaultAuthorId)
            .WithId(_defaultId)
            .Build();
        _postServiceMock.Setup(p => p.AddPostAsync(createPostDto, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(post);

        //Act
        var response = await _postController.Create(createPostDto);

        //Assert
        Assert.IsType<RedirectToActionResult>(response);
        Assert.Equal("Details", ((RedirectToActionResult)response).ActionName);
        Assert.Equal(_defaultId, ((RedirectToActionResult)response).RouteValues!["id"]);
    }

    // ---------- Edit GET ----------

    /// <summary>Verifies that Edit with an invalid post Id returns NotFound.</summary>
    [Fact]
    public async Task Edit_WithWrongPostId_ReturnsNotFound()
    {
        //Arrange
        var id = "wrongPostId";

        //Act
        var response = await _postController.Edit(id);

        //Assert
        Assert.IsType<NotFoundResult>(response);
    }

    /// <summary>Verifies that Edit when the post does not exist returns NotFound.</summary>
    [Fact]
    public async Task Edit_NotFoundPost_ReturnsNotFound()
    {
        //Arrange
        var id = _defaultId;
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync((Post?)null);

        //Act
        var response = await _postController.Edit(id);

        //Assert
        Assert.IsType<NotFoundResult>(response);
    }

    /// <summary>Verifies that Edit without a logged-in user's Id returns Unauthorized.</summary>
    [Fact]
    public async Task Edit_WithEmptyUserId_ReturnsUnauthorized()
    {
        //Arrange
        SetEmptyUser();
        var id = _defaultId;

        //Act
        var response = await _postController.Edit(id);

        //Assert
        Assert.IsType<UnauthorizedResult>(response);
    }

    /// <summary>Verifies that Edit when the logged-in user is not the post's author returns Forbid.</summary>
    [Fact]
    public async Task Edit_NotAuthor_ReturnsForbid()
    {
        //Arrange
        var id = _defaultId;
        var post = new PostFactory()
            .WithAuthorId("different-author-id")
            .Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);

        //Act
        var response = await _postController.Edit(id);

        //Assert
        Assert.IsType<ForbidResult>(response);
    }

    /// <summary>Verifies that Edit returns a View.</summary>
    [Fact]
    public async Task Edit_ReturnsView()
    {
        //Arrange
        var id = _defaultId;
        var post = new PostFactory()
            .WithTitle("Title1")
            .WithDescription("Description1")
            .WithContent("Content1")
            .WithAuthorId(_defaultId)
            .Build();
        var editPostDto = new EditPostDtoFactory()
            .WithTitle("Title1")
            .WithDescription("Description1")
            .WithContent("Content1")
            .Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);

        //Act
        var response = await _postController.Edit(id);

        //Assert
        Assert.IsType<ViewResult>(response);
        Assert.Equal(editPostDto.Title, ((EditPostDto)((ViewResult)response).Model!).Title);
        Assert.Equal(editPostDto.Content, ((EditPostDto)((ViewResult)response).Model!).Content);
    }

    // ---------- Edit POST ----------

    /// <summary>Verifies that Edit (POST) with an invalid post Id returns NotFound.</summary>
    [Fact]
    public async Task Edit_POST_WithWrongPostId_ReturnsNotFound()
    {
        //Arrange
        var id = "wrongPostId";
        var editPostDto = new EditPostDtoFactory().Build();

        //Act
        var response = await _postController.Edit(id, editPostDto);

        //Assert
        Assert.IsType<NotFoundResult>(response);
    }

    /// <summary>Verifies that Edit (POST) when the post does not exist returns NotFound.</summary>
    [Fact]
    public async Task Edit_POST_NotFoundPost_ReturnsNotFound()
    {
        //Arrange
        var id = _defaultId;
        var editPostDto = new EditPostDtoFactory().Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync((Post?)null);

        //Act
        var response = await _postController.Edit(id, editPostDto);

        //Assert
        Assert.IsType<NotFoundResult>(response);
    }

    /// <summary>Verifies that Edit (POST) with an invalid model title returns the View again.</summary>
    [Fact]
    public async Task Edit_POST_WithModelInvalidTitle_ReturnView()
    {
        //Arrange
        var id = _defaultId;
        var editPostDto = new EditPostDtoFactory()
            .WithTitle("")
            .Build();
        _postController.ModelState.AddModelError("Title", "Required");

        //Act
        var response = await _postController.Edit(id, editPostDto);

        //Assert
        Assert.IsType<ViewResult>(response);
        Assert.Equal(editPostDto, ((ViewResult)response).Model);
    }

    /// <summary>Verifies that Edit (POST) with an invalid model description returns the View again.</summary>
    [Fact]
    public async Task Edit_POST_WithModelInvalidDescription_ReturnView()
    {
        //Arrange
        var id = _defaultId;
        var editPostDto = new EditPostDtoFactory()
            .WithDescription("")
            .Build();
        _postController.ModelState.AddModelError("Description", "Required");

        //Act
        var response = await _postController.Edit(id, editPostDto);

        //Assert
        Assert.IsType<ViewResult>(response);
        Assert.Equal(editPostDto, ((ViewResult)response).Model);
    }

    /// <summary>Verifies that Edit (POST) with an invalid model content returns the View again.</summary>
    [Fact]
    public async Task Edit_POST_WithModelInvalidContent_ReturnView()
    {
        //Arrange
        var id = _defaultId;
        var editPostDto = new EditPostDtoFactory()
            .WithContent("")
            .Build();
        _postController.ModelState.AddModelError("Content", "Required");

        //Act
        var response = await _postController.Edit(id, editPostDto);

        //Assert
        Assert.IsType<ViewResult>(response);
        Assert.Equal(editPostDto, ((ViewResult)response).Model);
    }

    /// <summary>Verifies that Edit (POST) without a logged-in user's Id returns Unauthorized.</summary>
    [Fact]
    public async Task Edit_POST_WithEmptyUserId_ReturnsUnauthorized()
    {
        //Arrange
        SetEmptyUser();
        var id = _defaultId;
        var editPostDto = new EditPostDtoFactory().Build();

        //Act
        var response = await _postController.Edit(id, editPostDto);

        //Assert
        Assert.IsType<UnauthorizedResult>(response);
    }

    /// <summary>Verifies that Edit (POST) when the logged-in user is not the post's author returns Forbid.</summary>
    [Fact]
    public async Task Edit_POST_NotAuthor_ReturnsForbid()
    {
        //Arrange
        var id = _defaultId;
        var post = new PostFactory()
            .WithAuthorId("different-author-id")
            .Build();
        var editPostDto = new EditPostDtoFactory().Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);

        //Act
        var response = await _postController.Edit(id, editPostDto);

        //Assert
        Assert.IsType<ForbidResult>(response);
    }

    /// <summary>Verifies that Edit (POST) when the edit fails in the repository returns a View.</summary>
    [Fact]
    public async Task Edit_POST_EditReturnFalse_ReturnsView()
    {
        //Arrange
        var id = _defaultId;
        var post = new PostFactory()
            .WithAuthorId(_defaultId)
            .Build();
        var editPostDto = new EditPostDtoFactory().Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);
        _postServiceMock.Setup(p => p.EditPostAsync(id, editPostDto)).ReturnsAsync(false);

        //Act
        var response = await _postController.Edit(id, editPostDto);

        //Assert
        Assert.IsType<ViewResult>(response);
        Assert.Equal(editPostDto, ((ViewResult)response).Model);
    }

    /// <summary>Verifies that Edit (POST) with a valid model returns a redirect (RedirectToAction).</summary>
    [Fact]
    public async Task Edit_POST_WithModelValid_ReturnRedirectToAction()
    {
        //Arrange
        var id = _defaultId;
        var post = new PostFactory()
            .WithAuthorId(_defaultId)
            .Build();
        var editPostDto = new EditPostDtoFactory().Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);
        _postServiceMock.Setup(p => p.EditPostAsync(id, editPostDto)).ReturnsAsync(true);

        //Act
        var response = await _postController.Edit(id, editPostDto);

        //Assert
        Assert.IsType<RedirectToActionResult>(response);
        Assert.Equal("Details", ((RedirectToActionResult)response).ActionName);
        Assert.Equal(_defaultId, ((RedirectToActionResult)response).RouteValues!["id"]);
    }

    // ---------- Delete GET ----------

    /// <summary>Verifies that Delete with an invalid post Id returns NotFound.</summary>
    [Fact]
    public async Task Delete_WithWrongPostId_ReturnsNotFound()
    {
        //Arrange
        var id = "wrongPostId";

        //Act
        var response = await _postController.Delete(id);

        //Assert
        Assert.IsType<NotFoundResult>(response);
    }

    /// <summary>Verifies that Delete when the post does not exist returns NotFound.</summary>
    [Fact]
    public async Task Delete_NotFoundPost_ReturnsNotFound()
    {
        //Arrange
        var id = _defaultId;
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync((Post?)null);

        //Act
        var response = await _postController.Delete(id);

        //Assert
        Assert.IsType<NotFoundResult>(response);
    }

    /// <summary>Verifies that Delete without a logged-in user's Id returns Unauthorized.</summary>
    [Fact]
    public async Task Delete_WithEmptyUserId_ReturnsUnauthorized()
    {
        //Arrange
        SetEmptyUser();
        var id = _defaultId;

        //Act
        var response = await _postController.Delete(id);

        //Assert
        Assert.IsType<UnauthorizedResult>(response);
    }

    /// <summary>Verifies that Delete when the logged-in user is not the post's author returns Forbid.</summary>
    [Fact]
    public async Task Delete_NotAuthor_ReturnsForbid()
    {
        //Arrange
        var id = _defaultId;
        var post = new PostFactory()
            .WithAuthorId("different-author-id")
            .Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);

        //Act
        var response = await _postController.Delete(id);

        //Assert
        Assert.IsType<ForbidResult>(response);
    }

    /// <summary>Verifies that Delete returns a View.</summary>
    [Fact]
    public async Task Delete_ReturnsView()
    {
        //Arrange
        var id = _defaultId;
        var post = new PostFactory()
            .WithAuthorId(_defaultId)
            .Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);

        //Act
        var response = await _postController.Delete(id);

        //Assert
        Assert.IsType<ViewResult>(response);
        Assert.Equal(post, ((ViewResult)response).Model);
    }

    // ---------- Delete POST ----------

    /// <summary>Verifies that Delete (POST) with an invalid post Id returns NotFound.</summary>
    [Fact]
    public async Task Delete_POST_WithWrongPostId_ReturnsNotFound()
    {
        //Arrange
        var id = "wrongPostId";

        //Act
        var response = await _postController.DeletePost(id);

        //Assert
        Assert.IsType<NotFoundResult>(response);
    }

    /// <summary>Verifies that Delete (POST) when the post does not exist returns NotFound.</summary>
    [Fact]
    public async Task Delete_POST_NotFoundPost_ReturnsNotFound()
    {
        //Arrange
        var id = _defaultId;
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync((Post?)null);

        //Act
        var response = await _postController.DeletePost(id);

        //Assert
        Assert.IsType<NotFoundResult>(response);
    }

    /// <summary>Verifies that Delete (POST) without a logged-in user's Id returns Unauthorized.</summary>
    [Fact]
    public async Task Delete_POST_WithEmptyUserId_ReturnsUnauthorized()
    {
        //Arrange
        SetEmptyUser();
        var id = _defaultId;

        //Act
        var response = await _postController.DeletePost(id);

        //Assert
        Assert.IsType<UnauthorizedResult>(response);
    }

    /// <summary>Verifies that Delete (POST) when the logged-in user is not the post's author returns Forbid.</summary>
    [Fact]
    public async Task Delete_POST_NotAuthor_ReturnsForbid()
    {
        //Arrange
        var id = _defaultId;
        var post = new PostFactory()
            .WithAuthorId("different-author-id")
            .Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);

        //Act
        var response = await _postController.DeletePost(id);

        //Assert
        Assert.IsType<ForbidResult>(response);
    }

    /// <summary>Verifies that Delete (POST) when deletion fails in the repository returns NotFound.</summary>
    [Fact]
    public async Task Delete_POST_DeleteFails_ReturnsNotFound()
    {
        //Arrange
        var id = _defaultId;
        var post = new PostFactory()
            .WithAuthorId(_defaultId)
            .Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);
        _postServiceMock.Setup(p => p.DeletePostAsync(_defaultId)).ReturnsAsync(false);

        //Act
        var response = await _postController.DeletePost(id);

        //Assert
        Assert.IsType<NotFoundResult>(response);
    }

    /// <summary>Verifies that Delete (POST) returns a redirect (RedirectToActionResult).</summary>
    [Fact]
    public async Task Delete_POST_ReturnsRedirectToActionResult()
    {
        //Arrange
        var id = _defaultId;
        var post = new PostFactory()
            .WithAuthorId(_defaultId)
            .Build();
        _postServiceMock.Setup(p => p.GetPostAsync(_defaultId)).ReturnsAsync(post);
        _postServiceMock.Setup(p => p.DeletePostAsync(_defaultId)).ReturnsAsync(true);

        //Act
        var response = await _postController.DeletePost(id);

        //Assert
        Assert.IsType<RedirectToActionResult>(response);
        Assert.Equal("Index", ((RedirectToActionResult)response).ActionName);
        Assert.Equal("Home", ((RedirectToActionResult)response).ControllerName);
    }
}