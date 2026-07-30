using BlogMVC.Infrastructure.Interfaces;
using BlogMVC.Models;
using BlogMVC.Services;
using BlogMVC.Tests.Helpers;
using Moq;

namespace BlogMVC.Tests.Services;

/// <summary>
///     Unit tests for <see cref="PostService" /> using a mocked <see cref="IPostRepository" /> and
///     <see cref="IDateTimeProvider" />. Verify correct mapping from DTOs to the <see cref="Post" /> entity,
///     timestamp assignment, and delegation of calls to the repository.
/// </summary>
public class PostServiceTests
{
    private static readonly DateTime DefaultDate = new(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly string _defaultAuthor = "defaultAuthor";
    private readonly string _defaultAuthorId = "defaultAuthorId";
    private readonly Mock<IPostRepository> _postRepositoryMock;
    private readonly PostService _postService;

    public PostServiceTests()
    {
        _postRepositoryMock = new Mock<IPostRepository>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _postService = new PostService(_dateTimeProviderMock.Object, _postRepositoryMock.Object);
    }

    // ---------- AddPostAsync ----------

    /// <summary>Verifies that AddPostAsync sets the publish date from IDateTimeProvider.</summary>
    [Fact]
    public async Task AddPostAsync_SetsPublishDate_FromDateTimeProvider()
    {
        // Arrange
        _dateTimeProviderMock.Setup(t => t.Now).Returns(DefaultDate);

        var createPostDto = new CreatePostDtoFactory().Build();
        // Act
        await _postService.AddPostAsync(createPostDto, _defaultAuthorId, _defaultAuthor);

        // Assert
        _postRepositoryMock.Verify(p => p.InsertOneAsync(It.Is<Post>(post =>
            post.PublishDate == DefaultDate
        )), Times.Once);
    }

    /// <summary>Verifies that AddPostAsync sets the modified date from IDateTimeProvider.</summary>
    [Fact]
    public async Task AddPostAsync_SetsModifiedDate_FromDateTimeProvider()
    {
        // Arrange
        _dateTimeProviderMock.Setup(t => t.Now).Returns(DefaultDate);

        var createPostDto = new CreatePostDtoFactory().Build();
        // Act
        await _postService.AddPostAsync(createPostDto, _defaultAuthorId, _defaultAuthor);

        // Assert
        _postRepositoryMock.Verify(p => p.InsertOneAsync(It.Is<Post>(post =>
            post.ModifiedDate == DefaultDate
        )), Times.Once);
    }

    /// <summary>Verifies that AddPostAsync maps the title, description and content from the DTO.</summary>
    [Fact]
    public async Task AddPostAsync_MapsTitleAndContent_FromDto()
    {
        // Arrange
        _dateTimeProviderMock.Setup(t => t.Now).Returns(DefaultDate);

        var createPostDto = new CreatePostDtoFactory()
            .WithTitle("Title1")
            .WithDescription("Description1")
            .WithContent("Content1")
            .Build();

        // Act
        await _postService.AddPostAsync(createPostDto, _defaultAuthorId, _defaultAuthor);

        // Assert
        _postRepositoryMock.Verify(p => p.InsertOneAsync(It.Is<Post>(post =>
            post.Title == "Title1" &&
            post.Description == "Description1" &&
            post.Content == "Content1"
        )), Times.Once);
    }

    /// <summary>Verifies that AddPostAsync maps the author and author Id from the call parameters.</summary>
    [Fact]
    public async Task AddPostAsync_MapsAuthorAndAuthorId_FromParameters()
    {
        // Arrange
        _dateTimeProviderMock.Setup(t => t.Now).Returns(DefaultDate);

        var createPostDto = new CreatePostDtoFactory().Build();

        // Act
        await _postService.AddPostAsync(createPostDto, _defaultAuthorId, _defaultAuthor);

        // Assert
        _postRepositoryMock.Verify(p => p.InsertOneAsync(It.Is<Post>(post =>
            post.AuthorId == _defaultAuthorId &&
            post.Author == _defaultAuthor
        )), Times.Once);
    }

    /// <summary>Verifies that AddPostAsync returns the post from the repository.</summary>
    [Fact]
    public async Task AddPostAsync_ReturnsPost_FromRepository()
    {
        // Arrange
        _dateTimeProviderMock.Setup(t => t.Now).Returns(DefaultDate);

        var createPostDto = new CreatePostDtoFactory()
            .WithTitle("Title1")
            .WithDescription("Description1")
            .WithContent("Content1")
            .Build();
        var insertedPost = new PostFactory()
            .WithTitle("Title1")
            .WithDescription("Description1")
            .WithContent("Content1")
            .Build();

        _postRepositoryMock
            .Setup(p => p.InsertOneAsync(It.IsAny<Post>()))
            .ReturnsAsync(insertedPost);

        // Act
        var result = await _postService.AddPostAsync(createPostDto, _defaultAuthorId, _defaultAuthor);

        // Assert
        Assert.Equal(insertedPost, result);
    }

    // ---------- AddBulkPostAsync ----------

    /// <summary>Verifies that AddBulkPostAsync sets both the publish and modified dates for all posts from IDateTimeProvider.</summary>
    [Fact]
    public async Task AddBulkPostAsync_SetsPublishAndModifiedDate_ForAllPosts_FromDateTimeProvider()
    {
        // Arrange
        _dateTimeProviderMock.Setup(t => t.Now).Returns(DefaultDate);

        var createPostDtoes = new List<CreatePostDto>
        {
            new CreatePostDtoFactory().WithTitle("Title1").WithContent("Content1").Build(),
            new CreatePostDtoFactory().WithTitle("Title2").WithContent("Content2").Build()
        };

        // Act
        await _postService.AddBulkPostAsync(createPostDtoes, _defaultAuthorId, _defaultAuthor);

        // Assert
        _postRepositoryMock.Verify(p => p.InsertManyAsync(It.Is<List<Post>>(posts =>
            posts.Count == 2 &&
            posts.All(post => post.PublishDate == DefaultDate && post.ModifiedDate == DefaultDate)
        )), Times.Once);
    }

    /// <summary>Verifies that AddBulkPostAsync maps the title, description and content for each DTO.</summary>
    [Fact]
    public async Task AddBulkPostAsync_MapsTitleAndContent_ForEachDto()
    {
        // Arrange
        _dateTimeProviderMock.Setup(t => t.Now).Returns(DefaultDate);

        var createPostDtoes = new List<CreatePostDto>
        {
            new CreatePostDtoFactory().WithTitle("Title1").WithDescription("Description1").WithContent("Content1")
                .Build(),
            new CreatePostDtoFactory().WithTitle("Title2").WithDescription("Description2").WithContent("Content2")
                .Build()
        };

        // Act
        await _postService.AddBulkPostAsync(createPostDtoes, _defaultAuthorId, _defaultAuthor);

        // Assert
        _postRepositoryMock.Verify(p => p.InsertManyAsync(It.Is<List<Post>>(posts =>
            posts[0].Title == "Title1" && posts[0].Description == "Description1" && posts[0].Content == "Content1" &&
            posts[1].Title == "Title2" && posts[1].Description == "Description2" && posts[1].Content == "Content2"
        )), Times.Once);
    }

    /// <summary>Verifies that AddBulkPostAsync maps the author and author Id for each post.</summary>
    [Fact]
    public async Task AddBulkPostAsync_MapsAuthorAndAuthorId_ForEachPost()
    {
        // Arrange
        _dateTimeProviderMock.Setup(t => t.Now).Returns(DefaultDate);

        var createPostDtoes = new List<CreatePostDto>
        {
            new CreatePostDtoFactory().WithTitle("Title1").WithContent("Content1").Build(),
            new CreatePostDtoFactory().WithTitle("Title2").WithContent("Content2").Build()
        };

        // Act
        await _postService.AddBulkPostAsync(createPostDtoes, _defaultAuthorId, _defaultAuthor);

        // Assert
        _postRepositoryMock.Verify(p => p.InsertManyAsync(It.Is<List<Post>>(posts =>
            posts.All(post => post.AuthorId == _defaultAuthorId && post.Author == _defaultAuthor)
        )), Times.Once);
    }

    /// <summary>Verifies that AddBulkPostAsync with an empty list calls InsertManyAsync with an empty list.</summary>
    [Fact]
    public async Task AddBulkPostAsync_WithEmptyList_CallsInsertManyAsync_WithEmptyList()
    {
        // Arrange
        var createPostDtoes = new List<CreatePostDto>();

        // Act
        await _postService.AddBulkPostAsync(createPostDtoes, _defaultAuthorId, _defaultAuthor);

        // Assert
        _postRepositoryMock.Verify(p => p.InsertManyAsync(It.Is<List<Post>>(posts =>
            posts.Count == 0
        )), Times.Once);
    }

    /// <summary>Verifies that AddBulkPostAsync returns the posts from the repository.</summary>
    [Fact]
    public async Task AddBulkPostAsync_ReturnsPosts_FromRepository()
    {
        // Arrange
        _dateTimeProviderMock.Setup(t => t.Now).Returns(DefaultDate);

        var createPostDtoes = new List<CreatePostDto>
        {
            new CreatePostDtoFactory().WithTitle("Title1").WithContent("Content1").Build()
        };

        var insertedPosts = new List<Post>
        {
            new PostFactory().WithTitle("Title1").WithContent("Content1").Build()
        };

        _postRepositoryMock
            .Setup(p => p.InsertManyAsync(It.IsAny<List<Post>>()))
            .ReturnsAsync(insertedPosts);

        // Act
        var result = await _postService.AddBulkPostAsync(createPostDtoes, _defaultAuthorId, _defaultAuthor);

        // Assert
        Assert.Equal(insertedPosts, result);
    }

    // ---------- GetPostsAsync ----------

    /// <summary>Verifies that GetPostsAsync returns all posts from the repository.</summary>
    [Fact]
    public async Task GetPostsAsync_ReturnsAllPosts_FromRepository()
    {
        // Arrange
        var posts = new List<Post>
        {
            new PostFactory().WithTitle("Title1").WithContent("Content1").Build(),
            new PostFactory().WithTitle("Title2").WithContent("Content2").Build(),
            new PostFactory().WithTitle("Title3").WithContent("Content3").Build()
        };
        _postRepositoryMock.Setup(p => p.FindAllAsync()).ReturnsAsync(posts);

        // Act
        var result = await _postService.GetPostsAsync();

        // Assert
        Assert.Equal(posts, result);
        _postRepositoryMock.Verify(p => p.FindAllAsync(), Times.Once);
    }

    /// <summary>Verifies that GetPostsAsync when there are no posts returns an empty list.</summary>
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

    /// <summary>Verifies that GetPostAsync with an existing Id returns the post from the repository.</summary>
    [Fact]
    public async Task GetPostAsync_WithExistingId_ReturnsPost_FromRepository()
    {
        // Arrange
        var post = new PostFactory()
            .Build();
        _postRepositoryMock.Setup(p => p.FindAsync("1")).ReturnsAsync(post);

        // Act
        var result = await _postService.GetPostAsync("1");

        // Assert
        Assert.Equal(post, result);
        _postRepositoryMock.Verify(p => p.FindAsync("1"), Times.Once);
    }

    /// <summary>Verifies that GetPostAsync with a non-existing Id returns null.</summary>
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

    /// <summary>Verifies that DeletePostAsync with an existing Id returns true.</summary>
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

    /// <summary>Verifies that DeletePostAsync with a non-existing Id returns false.</summary>
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

    /// <summary>Verifies that EditPostAsync sets the modified date from IDateTimeProvider.</summary>
    [Fact]
    public async Task EditPostAsync_SetsModifiedDate_FromDateTimeProvider()
    {
        // Arrange
        _dateTimeProviderMock.Setup(t => t.Now).Returns(DefaultDate);

        var post = new PostFactory()
            .Build();
        _postRepositoryMock.Setup(p => p.FindAsync("1")).ReturnsAsync(post);
        _postRepositoryMock.Setup(p => p.ReplaceOneAsync("1", It.IsAny<Post>())).ReturnsAsync(true);

        var editPostDto = new EditPostDtoFactory()
            .Build();

        // Act
        await _postService.EditPostAsync("1", editPostDto);

        // Assert
        _postRepositoryMock.Verify(p => p.ReplaceOneAsync("1", It.Is<Post>(post1 =>
            post1.ModifiedDate == DefaultDate
        )), Times.Once);
    }

    /// <summary>Verifies that EditPostAsync maps the title, description and content from the DTO.</summary>
    [Fact]
    public async Task EditPostAsync_MapsTitleAndContent_FromDto()
    {
        // Arrange
        _dateTimeProviderMock.Setup(t => t.Now).Returns(DefaultDate);

        var editPostDto = new EditPostDtoFactory()
            .WithTitle("Title1")
            .WithDescription("Description1")
            .WithContent("Content1")
            .Build();

        var post = new PostFactory()
            .Build();
        _postRepositoryMock.Setup(p => p.FindAsync("1")).ReturnsAsync(post);
        _postRepositoryMock.Setup(p => p.ReplaceOneAsync("1", It.IsAny<Post>())).ReturnsAsync(true);

        // Act
        await _postService.EditPostAsync("1", editPostDto);

        // Assert
        _postRepositoryMock.Verify(p => p.ReplaceOneAsync("1", It.Is<Post>(post1 =>
            post1.Title == "Title1" &&
            post1.Description == "Description1" &&
            post1.Content == "Content1"
        )), Times.Once);
    }

    /// <summary>Verifies that EditPostAsync does not change the publish date, author, or author Id.</summary>
    [Fact]
    public async Task EditPostAsync_DoesNotChange_PublishDateAuthorOrAuthorId()
    {
        // Arrange
        _dateTimeProviderMock.Setup(t => t.Now).Returns(DefaultDate);

        var originalPost = new PostFactory()
            .WithPublishDate(new DateTime(1999, 5, 5, 0, 0, 0, DateTimeKind.Utc))
            .WithAuthor("OriginalAuthor")
            .WithAuthorId("OriginalAuthorId")
            .Build();
        _postRepositoryMock.Setup(p => p.FindAsync("1")).ReturnsAsync(originalPost);
        _postRepositoryMock.Setup(p => p.ReplaceOneAsync("1", It.IsAny<Post>())).ReturnsAsync(true);

        var editPostDto = new EditPostDtoFactory()
            .Build();

        // Act
        await _postService.EditPostAsync("1", editPostDto);

        // Assert
        _postRepositoryMock.Verify(p => p.ReplaceOneAsync("1", It.Is<Post>(post1 =>
            post1.PublishDate == originalPost.PublishDate &&
            post1.Author == "OriginalAuthor" &&
            post1.AuthorId == "OriginalAuthorId"
        )), Times.Once);
    }

    /// <summary>Verifies that EditPostAsync with an existing Id returns true.</summary>
    [Fact]
    public async Task EditPostAsync_WithExistingId_ReturnsTrue()
    {
        // Arrange
        _dateTimeProviderMock.Setup(t => t.Now).Returns(DefaultDate);
        var post = new PostFactory()
            .Build();
        _postRepositoryMock.Setup(p => p.FindAsync("1")).ReturnsAsync(post);
        _postRepositoryMock.Setup(p => p.ReplaceOneAsync("1", It.IsAny<Post>())).ReturnsAsync(true);
        var editPostDto = new EditPostDtoFactory()
            .Build();

        // Act
        var result = await _postService.EditPostAsync("1", editPostDto);

        // Assert
        Assert.True(result);
    }

    /// <summary>Verifies that EditPostAsync with a non-existing Id returns false.</summary>
    [Fact]
    public async Task EditPostAsync_WithNonExistingId_ReturnsFalse()
    {
        // Arrange
        _postRepositoryMock.Setup(p => p.FindAsync("missing")).ReturnsAsync((Post?)null);
        var editPostDto = new EditPostDtoFactory()
            .Build();

        // Act
        var result = await _postService.EditPostAsync("missing", editPostDto);

        // Assert
        Assert.False(result);
    }

    /// <summary>Verifies that EditPostAsync with a non-existing Id does not call ReplaceOneAsync.</summary>
    [Fact]
    public async Task EditPostAsync_WithNonExistingId_DoesNotCallReplaceOneAsync()
    {
        // Arrange
        _postRepositoryMock.Setup(p => p.FindAsync("missing")).ReturnsAsync((Post?)null);
        var editPostDto = new EditPostDtoFactory()
            .Build();

        // Act
        await _postService.EditPostAsync("missing", editPostDto);

        // Assert
        _postRepositoryMock.Verify(p => p.ReplaceOneAsync(It.IsAny<string>(), It.IsAny<Post>()), Times.Never);
    }

    /// <summary>Verifies that EditPostAsync when the repository fails to replace the document returns false.</summary>
    [Fact]
    public async Task EditPostAsync_WhenRepositoryFailsToReplace_ReturnsFalse()
    {
        // Arrange
        _dateTimeProviderMock.Setup(t => t.Now).Returns(DefaultDate);
        var post = new PostFactory()
            .Build();
        _postRepositoryMock.Setup(p => p.FindAsync("1")).ReturnsAsync(post);
        _postRepositoryMock.Setup(p => p.ReplaceOneAsync("1", It.IsAny<Post>())).ReturnsAsync(false);
        var editPostDto = new EditPostDtoFactory()
            .Build();

        // Act
        var result = await _postService.EditPostAsync("1", editPostDto);

        // Assert
        Assert.False(result);
    }

    // ---------- SearchAsync ----------

    /// <summary>Verifies that SearchAsync returns the matching posts from the repository.</summary>
    [Fact]
    public async Task SearchAsync_ReturnsMatchingPosts_FromRepository()
    {
        // Arrange
        var posts = new List<Post>
        {
            new PostFactory().WithTitle("Title1").Build(),
            new PostFactory().WithTitle("Title2").Build()
        };
        _postRepositoryMock.Setup(p => p.SearchAsync("term")).ReturnsAsync(posts);

        // Act
        var result = await _postService.SearchAsync("term");

        // Assert
        Assert.Equal(posts, result);
    }

    /// <summary>Verifies that SearchAsync passes the query through to the repository unchanged.</summary>
    [Fact]
    public async Task SearchAsync_PassesQuery_ToRepository()
    {
        // Arrange
        _postRepositoryMock.Setup(p => p.SearchAsync(It.IsAny<string>())).ReturnsAsync(new List<Post>());

        // Act
        await _postService.SearchAsync("some query");

        // Assert
        _postRepositoryMock.Verify(p => p.SearchAsync("some query"), Times.Once);
    }

    /// <summary>Verifies that SearchAsync when there are no matches returns an empty list.</summary>
    [Fact]
    public async Task SearchAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        _postRepositoryMock.Setup(p => p.SearchAsync("nothing-matches")).ReturnsAsync(new List<Post>());

        // Act
        var result = await _postService.SearchAsync("nothing-matches");

        // Assert
        Assert.Empty(result);
    }
}