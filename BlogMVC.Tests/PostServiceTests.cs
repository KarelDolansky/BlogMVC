using BlogMVC.Infrastructure.Interfaces;
using BlogMVC.Models;
using BlogMVC.Services;
using Moq;

namespace BlogMVC.Tests;

public class PostServiceTests
{
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly Mock<IPostRepository> _postRepositoryMock;
    private readonly PostService _postService;

    public PostServiceTests()
    {
        _postRepositoryMock = new Mock<IPostRepository>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _postService = new PostService(_dateTimeProviderMock.Object, _postRepositoryMock.Object);
    }

    // ---------- AddPostAsync ----------

    [Fact]
    public async Task AddPostAsync_SetsPublishDate_FromDateTimeProvider()
    {
        // Arrange
        var expectedDate = new DateTime(2001, 1, 1);
        _dateTimeProviderMock.Setup(t => t.Now).Returns(expectedDate);

        var createPostDto = new CreatePostDto
        {
            Title = "Title",
            Content = "Content",
        };
        // Act
        await _postService.AddPostAsync(createPostDto);

        // Assert
        _postRepositoryMock.Verify(p => p.InsertOneAsync(It.Is<Post>(post =>
            post.PublishDate == expectedDate
        )), Times.Once);
    }

    [Fact]
    public async Task AddPostAsync_SetsModifiedDate_FromDateTimeProvider()
    {
        // Arrange
        var expectedDate = new DateTime(2001, 1, 1);
        _dateTimeProviderMock.Setup(t => t.Now).Returns(expectedDate);

        var createPostDto = new CreatePostDto
        {
            Title = "Title",
            Content = "Content",
        };
        // Act
        await _postService.AddPostAsync(createPostDto);

        // Assert
        _postRepositoryMock.Verify(p => p.InsertOneAsync(It.Is<Post>(post =>
            post.ModifiedDate == expectedDate
        )), Times.Once);
    }

    [Fact]
    public async Task AddPostAsync_MapsTitleAndContent_FromDto()
    {
        // Arrange
        _dateTimeProviderMock.Setup(t => t.Now).Returns(new DateTime(2001, 1, 1));

        var createPostDto = new CreatePostDto
        {
            Title = "My Title",
            Content = "My Content",
        };

        // Act
        await _postService.AddPostAsync(createPostDto);

        // Assert
        _postRepositoryMock.Verify(p => p.InsertOneAsync(It.Is<Post>(post =>
            post.Title == "My Title" &&
            post.Content == "My Content"
        )), Times.Once);
    }

    [Fact]
    public async Task AddPostAsync_ReturnsPost_FromRepository()
    {
        // Arrange
        _dateTimeProviderMock.Setup(t => t.Now).Returns(new DateTime(2001, 1, 1));

        var createPostDto = new CreatePostDto
        {
            Title = "Title",
            Content = "Content",
        };

        var insertedPost = new Post
        {
            Title = "Title",
            Content = "Content",
            Author = "AuthorDefault",
        };

        _postRepositoryMock
            .Setup(p => p.InsertOneAsync(It.IsAny<Post>()))
            .ReturnsAsync(insertedPost);

        // Act
        var result = await _postService.AddPostAsync(createPostDto);

        // Assert
        Assert.Equal(insertedPost, result);
    }

    // ---------- AddBulkPostAsync ----------

    [Fact]
    public async Task AddBulkPostAsync_SetsPublishAndModifiedDate_ForAllPosts_FromDateTimeProvider()
    {
        // Arrange
        var expectedDate = new DateTime(2001, 1, 1);
        _dateTimeProviderMock.Setup(t => t.Now).Returns(expectedDate);

        var createPostDtoes = new List<CreatePostDto>
        {
            new() { Title = "Title1", Content = "Content1" },
            new() { Title = "Title2", Content = "Content2" },
        };

        // Act
        await _postService.AddBulkPostAsync(createPostDtoes);

        // Assert
        _postRepositoryMock.Verify(p => p.InsertManyAsync(It.Is<List<Post>>(posts =>
            posts.Count == 2 &&
            posts.All(post => post.PublishDate == expectedDate && post.ModifiedDate == expectedDate)
        )), Times.Once);
    }

    [Fact]
    public async Task AddBulkPostAsync_MapsTitleAndContent_ForEachDto()
    {
        // Arrange
        _dateTimeProviderMock.Setup(t => t.Now).Returns(new DateTime(2001, 1, 1));

        var createPostDtoes = new List<CreatePostDto>
        {
            new() { Title = "Title1", Content = "Content1" },
            new() { Title = "Title2", Content = "Content2" },
        };

        // Act
        await _postService.AddBulkPostAsync(createPostDtoes);

        // Assert
        _postRepositoryMock.Verify(p => p.InsertManyAsync(It.Is<List<Post>>(posts =>
            posts[0].Title == "Title1" && posts[0].Content == "Content1" &&
            posts[1].Title == "Title2" && posts[1].Content == "Content2"
        )), Times.Once);
    }

    [Fact]
    public async Task AddBulkPostAsync_WithEmptyList_CallsInsertManyAsync_WithEmptyList()
    {
        // Arrange
        var createPostDtoes = new List<CreatePostDto>();

        // Act
        await _postService.AddBulkPostAsync(createPostDtoes);

        // Assert
        _postRepositoryMock.Verify(p => p.InsertManyAsync(It.Is<List<Post>>(posts =>
            posts.Count == 0
        )), Times.Once);
    }

    [Fact]
    public async Task AddBulkPostAsync_ReturnsPosts_FromRepository()
    {
        // Arrange
        _dateTimeProviderMock.Setup(t => t.Now).Returns(new DateTime(2001, 1, 1));

        var createPostDtoes = new List<CreatePostDto>
        {
            new() { Title = "Title1", Content = "Content1" },
        };

        var insertedPosts = new List<Post>
        {
            new() { Title = "Title1", Content = "Content1", Author = "AuthorDefault" },
        };

        _postRepositoryMock
            .Setup(p => p.InsertManyAsync(It.IsAny<List<Post>>()))
            .ReturnsAsync(insertedPosts);

        // Act
        var result = await _postService.AddBulkPostAsync(createPostDtoes);

        // Assert
        Assert.Equal(insertedPosts, result);
    }

    // ---------- GetPostsAsync ----------

    [Fact]
    public async Task GetPostsAsync_ReturnsAllPosts_FromRepository()
    {
        // Arrange
        var posts = new List<Post>
        {
            new() { Title = "Title1", Content = "Content1", Author = "AuthorDefault" },
            new() { Title = "Title2", Content = "Content2", Author = "AuthorDefault" },
        };
        _postRepositoryMock.Setup(p => p.FindAllAsync()).ReturnsAsync(posts);

        // Act
        var result = await _postService.GetPostsAsync();

        // Assert
        Assert.Equal(posts, result);
        _postRepositoryMock.Verify(p => p.FindAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetPostsAsync_WithNoPosts_ReturnsEmptyList()
    {
        // Arrange
        _postRepositoryMock.Setup(p => p.FindAllAsync()).ReturnsAsync(new List<Post>());

        // Act
        var result = await _postService.GetPostsAsync();

        // Assert
        Assert.Empty(result);
    }

    // ---------- GetPostAsync ----------

    [Fact]
    public async Task GetPostAsync_WithExistingId_ReturnsPost_FromRepository()
    {
        // Arrange
        var post = new Post { Title = "Title", Content = "Content", Author = "AuthorDefault" };
        _postRepositoryMock.Setup(p => p.FindAsync("1")).ReturnsAsync(post);

        // Act
        var result = await _postService.GetPostAsync("1");

        // Assert
        Assert.Equal(post, result);
        _postRepositoryMock.Verify(p => p.FindAsync("1"), Times.Once);
    }

    [Fact]
    public async Task GetPostAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        _postRepositoryMock.Setup(p => p.FindAsync("missing")).ReturnsAsync((Post?)null);

        // Act
        var result = await _postService.GetPostAsync("missing");

        // Assert
        Assert.Null(result);
    }

    // ---------- DeletePostAsync ----------

    [Fact]
    public async Task DeletePostAsync_WithExistingId_ReturnsTrue()
    {
        // Arrange
        _postRepositoryMock.Setup(p => p.DeleteOneAsync("1")).ReturnsAsync(true);

        // Act
        var result = await _postService.DeletePostAsync("1");

        // Assert
        Assert.True(result);
        _postRepositoryMock.Verify(p => p.DeleteOneAsync("1"), Times.Once);
    }

    [Fact]
    public async Task DeletePostAsync_WithNonExistingId_ReturnsFalse()
    {
        // Arrange
        _postRepositoryMock.Setup(p => p.DeleteOneAsync("missing")).ReturnsAsync(false);

        // Act
        var result = await _postService.DeletePostAsync("missing");

        // Assert
        Assert.False(result);
    }

    // ---------- EditPostAsync ----------

    [Fact]
    public async Task EditPostAsync_SetsModifiedDate_FromDateTimeProvider()
    {
        // Arrange
        var expectedDate = new DateTime(2001, 1, 1);
        _dateTimeProviderMock.Setup(t => t.Now).Returns(expectedDate);

        var post = new Post { Title = "Title", Content = "Content", Author = "AuthorDefault" };

        // Act
        await _postService.EditPostAsync("1", post);

        // Assert
        Assert.Equal(expectedDate, post.ModifiedDate);
    }

    [Fact]
    public async Task EditPostAsync_CallsReplaceOneAsync_WithIdAndPost()
    {
        // Arrange
        _dateTimeProviderMock.Setup(t => t.Now).Returns(new DateTime(2001, 1, 1));
        var post = new Post { Title = "Title", Content = "Content", Author = "AuthorDefault" };

        // Act
        await _postService.EditPostAsync("1", post);

        // Assert
        _postRepositoryMock.Verify(p => p.ReplaceOneAsync("1", post), Times.Once);
    }

    [Fact]
    public async Task EditPostAsync_WithExistingId_ReturnsTrue()
    {
        // Arrange
        _dateTimeProviderMock.Setup(t => t.Now).Returns(new DateTime(2001, 1, 1));
        _postRepositoryMock.Setup(p => p.ReplaceOneAsync("1", It.IsAny<Post>())).ReturnsAsync(true);
        var post = new Post { Title = "Title", Content = "Content", Author = "AuthorDefault" };

        // Act
        var result = await _postService.EditPostAsync("1", post);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task EditPostAsync_WithNonExistingId_ReturnsFalse()
    {
        // Arrange
        _dateTimeProviderMock.Setup(t => t.Now).Returns(new DateTime(2001, 1, 1));
        _postRepositoryMock.Setup(p => p.ReplaceOneAsync("missing", It.IsAny<Post>())).ReturnsAsync(false);
        var post = new Post { Title = "Title", Content = "Content", Author = "AuthorDefault" };

        // Act
        var result = await _postService.EditPostAsync("missing", post);

        // Assert
        Assert.False(result);
    }
}