using BlogMVC.Controllers;
using BlogMVC.Models;
using BlogMVC.Services;
using BlogMVC.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BlogMVC.Tests.Controllers;

/// <summary>
///     Unit tests for <see cref="HomeController" /> using a mocked <see cref="IPostService" />.
///     Verify that Search calls the service (or skips it) depending on the query, and that
///     the "Query" ViewData value and view model are set correctly.
///     Tests are grouped by action (currently only Search).
/// </summary>
public class HomeControllerTests
{
    private readonly HomeController _homeController;
    private readonly Mock<IPostService> _postServiceMock;

    public HomeControllerTests()
    {
        _postServiceMock = new Mock<IPostService>();
        _homeController = new HomeController(_postServiceMock.Object);
    }

    // ---------- Search ----------

    /// <summary>Verifies that Search with a null query does not call the service and returns an empty list.</summary>
    [Fact]
    public async Task Search_WithNullQuery_DoesNotCallService_ReturnsEmptyList()
    {
        // Act
        var response = await _homeController.Search(null!);

        // Assert
        var result = Assert.IsType<ViewResult>(response);
        var model = Assert.IsType<List<Post>>(result.Model);
        Assert.Empty(model);
        _postServiceMock.Verify(p => p.SearchAsync(It.IsAny<string>()), Times.Never);
    }

    /// <summary>Verifies that Search with an empty query does not call the service and returns an empty list.</summary>
    [Fact]
    public async Task Search_WithEmptyQuery_DoesNotCallService_ReturnsEmptyList()
    {
        // Act
        var response = await _homeController.Search(string.Empty);

        // Assert
        var result = Assert.IsType<ViewResult>(response);
        var model = Assert.IsType<List<Post>>(result.Model);
        Assert.Empty(model);
        _postServiceMock.Verify(p => p.SearchAsync(It.IsAny<string>()), Times.Never);
    }

    /// <summary>Verifies that Search with an empty query sets an empty string in the "Query" ViewData.</summary>
    [Fact]
    public async Task Search_WithEmptyQuery_SetsEmptyQueryViewData()
    {
        // Act
        var response = await _homeController.Search(string.Empty);

        // Assert
        var result = Assert.IsType<ViewResult>(response);
        Assert.Equal("", result.ViewData["Query"]);
    }

    /// <summary>Verifies that Search with a non-empty query returns the matching posts from the service.</summary>
    [Fact]
    public async Task Search_WithQuery_ReturnsPosts_FromService()
    {
        // Arrange
        var posts = new List<Post> { new PostFactory().WithTitle("Matching Post").Build() };
        _postServiceMock.Setup(p => p.SearchAsync("term")).ReturnsAsync(posts);

        // Act
        var response = await _homeController.Search("term");

        // Assert
        var result = Assert.IsType<ViewResult>(response);
        Assert.Equal(posts, result.Model);
    }

    /// <summary>Verifies that Search with a non-empty query sets the query text in the "Query" ViewData.</summary>
    [Fact]
    public async Task Search_WithQuery_SetsQueryViewData()
    {
        // Arrange
        _postServiceMock.Setup(p => p.SearchAsync("term")).ReturnsAsync(new List<Post>());

        // Act
        var response = await _homeController.Search("term");

        // Assert
        var result = Assert.IsType<ViewResult>(response);
        Assert.Equal("term", result.ViewData["Query"]);
    }

    /// <summary>Verifies that Search with a non-empty query calls the service exactly once with that query.</summary>
    [Fact]
    public async Task Search_WithQuery_CallsService_Once()
    {
        // Arrange
        _postServiceMock.Setup(p => p.SearchAsync("term")).ReturnsAsync(new List<Post>());

        // Act
        await _homeController.Search("term");

        // Assert
        _postServiceMock.Verify(p => p.SearchAsync("term"), Times.Once);
    }

    /// <summary>Verifies that Search when the service finds no matches returns an empty list.</summary>
    [Fact]
    public async Task Search_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        _postServiceMock.Setup(p => p.SearchAsync("nothing-matches")).ReturnsAsync(new List<Post>());

        // Act
        var response = await _homeController.Search("nothing-matches");

        // Assert
        var result = Assert.IsType<ViewResult>(response);
        var model = Assert.IsType<List<Post>>(result.Model);
        Assert.Empty(model);
    }
}