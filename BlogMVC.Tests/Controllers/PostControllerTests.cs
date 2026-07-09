using System.Security.Claims;
using BlogMVC.Controllers;
using BlogMVC.Models;
using BlogMVC.Services;
using BlogMVC.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BlogMVC.Tests.Controllers;

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
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, _defaultId),
                    new Claim(ClaimTypes.Name, _defaultAuthor),
                }, "test"))
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
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, _defaultId),
                }, "test"))
            }
        };
    }

    // ---------- Details ----------

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

    [Fact]
    public void Create_ReturnsView()
    {
        // Act
        var response = _postController.Create();

        //Assert
        Assert.IsType<ViewResult>(response);
    }

    // ---------- Create POST ----------

    [Fact]
    public async Task Create_POST_WithModelInvalid_ReturnView()
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

    [Fact]
    public async Task Edit_ReturnsView()
    {
        //Arrange
        var id = _defaultId;
        var post = new PostFactory()
            .WithTitle("Title1")
            .WithContent("Content1")
            .WithAuthorId(_defaultId)
            .Build();
        var editPostDto = new EditPostDtoFactory()
            .WithTitle("Title1")
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

    [Fact]
    public async Task Edit_POST_WithModelInvalid_ReturnView()
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