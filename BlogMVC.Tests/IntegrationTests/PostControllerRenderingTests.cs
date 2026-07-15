using System.Net;
using BlogMVC.Infrastructure.Interfaces;
using BlogMVC.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BlogMVC.Tests.IntegrationTests;

/// <summary>
/// Integration tests focused on the rendered HTML output of <see cref="PostController"/>
/// (checking that the response contains the expected content, forms, and field values),
/// against the real app and a real MongoDB instance.
/// </summary>
[Collection("PostController")]
public class PostControllerRenderingTests(WebApplicationFactory<Program> factory)
    : PostControllerTestBase(factory)
{
    // ---------- Details ----------

    /// <summary>Verifies that Details with a valid post returns a View.</summary>
    [Fact]
    public async Task Details_WithValidPost_ReturnsView()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(DefaultId)
            .WithTitle("Test Title")
            .WithContent("Test Content")
            .Build();

        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        // Act
        var response = await Client.GetAsync($"/Post/Details/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Title", content);
        Assert.Contains("Test Content", content);
        Assert.Contains("Go back", content);
    }

    /// <summary>Verifies that Details AsAnonymousUser HidesEditAndDeleteLinks.</summary>
    [Fact]
    public async Task Details_AsAnonymousUser_HidesEditAndDeleteLinks()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(DefaultId)
            .WithAuthorId(OwnerId)
            .Build();

        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        // Act
        var response = await Client.GetAsync($"/Post/Details/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain($"/Post/Edit/{DefaultId}", content);
        Assert.DoesNotContain($"/Post/Delete/{DefaultId}", content);
    }

    /// <summary>Verifies that Details AsOwner ShowsEditAndDeleteLinks.</summary>
    [Fact]
    public async Task Details_AsOwner_ShowsEditAndDeleteLinks()
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
        var response = await client.GetAsync($"/Post/Details/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains($"/Post/Edit/{DefaultId}", content);
        Assert.Contains($"/Post/Delete/{DefaultId}", content);
    }

    /// <summary>Verifies that Details AsDifferentUser HidesEditAndDeleteLinks.</summary>
    [Fact]
    public async Task Details_AsDifferentUser_HidesEditAndDeleteLinks()
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
        var response = await client.GetAsync($"/Post/Details/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain($"/Post/Edit/{DefaultId}", content);
        Assert.DoesNotContain($"/Post/Delete/{DefaultId}", content);
    }

    /// <summary>Verifies that Details WithModifiedPost ShowsModifiedDate.</summary>
    [Fact]
    public async Task Details_WithModifiedPost_ShowsModifiedDate()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(DefaultId)
            .WithTitle("Test Title")
            .WithPublishDate(DefaultDate)
            .WithModifiedDate(DefaultDate.AddDays(1))
            .Build();

        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        // Act
        var response = await Client.GetAsync($"/Post/Details/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains(DefaultDate.AddDays(1).ToString("dd/MM/yyyy"), content);
    }

    /// <summary>Verifies that Details WithUnmodifiedPost DoesNotDuplicateDate.</summary>
    [Fact]
    public async Task Details_WithUnmodifiedPost_DoesNotDuplicateDate()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(DefaultId)
            .WithTitle("Test Title")
            .WithPublishDate(DefaultDate)
            .WithModifiedDate(DefaultDate)
            .Build();

        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        // Act
        var response = await Client.GetAsync($"/Post/Details/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var dateText = DefaultDate.ToString("dd/MM/yyyy");
        var occurrences = content.Split(dateText).Length - 1;
        Assert.Equal(1, occurrences);
    }

    // ---------- Create GET ----------

    /// <summary>Verifies that Create returns a View.</summary>
    [Fact]
    public async Task Create_ReturnsView()
    {
        // Arrange
        var client = CreateAuthenticatedClient(OwnerId);

        // Act
        var response = await client.GetAsync("/Post/Create");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("name=\"Title\"", content);
        Assert.Contains("name=\"Content\"", content);
        Assert.Contains("Submit", content);
    }

    // ---------- Create POST ----------

    /// <summary>Verifies that Create (POST) WithInvalidTitle returns a View.</summary>
    [Fact]
    public async Task Create_POST_WithInvalidTitle_ReturnsView()
    {
        // Arrange
        var client = CreateAuthenticatedClient(OwnerId);
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Title", "" },
            { "Content", "Test Content" }
        });

        // Act
        var response = await client.PostAsync("/Post/Create", formData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Content", content);
        Assert.Contains("The Title field is required.", content);
    }

    /// <summary>Verifies that Create (POST) WithInvalidContent returns a View.</summary>
    [Fact]
    public async Task Create_POST_WithInvalidContent_ReturnsView()
    {
        // Arrange
        var client = CreateAuthenticatedClient(OwnerId);
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Title", "Test Title" },
            { "Content", "" }
        });

        // Act
        var response = await client.PostAsync("/Post/Create", formData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Title", content);
        Assert.Contains("The Content field is required.", content);
    }

    /// <summary>Verifies that Create (POST) WithValidContent ReturnsDetailsView.</summary>
    [Fact]
    public async Task Create_POST_WithValidContent_ReturnsDetailsView()
    {
        // Arrange
        var client = CreateAuthenticatedClient(OwnerId, "AuthorDefault");
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Title", "Test Title" },
            { "Content", "Test Content" }
        });

        // Act
        var response = await client.PostAsync("/Post/Create", formData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Title", content);
        Assert.Contains("Test Content", content);
    }

    // ---------- Edit ----------

    /// <summary>Verifies that Edit returns a View.</summary>
    [Fact]
    public async Task Edit_ReturnsView()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(DefaultId)
            .WithTitle("Test Title")
            .WithContent("Test Content")
            .WithAuthorId(OwnerId)
            .Build();

        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        var client = CreateAuthenticatedClient(OwnerId);

        // Act
        var response = await client.GetAsync($"/Post/Edit/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Title", content);
        Assert.Contains("Test Content", content);
        Assert.Contains($"/Post/Details/{post.Id}", content);
        Assert.Contains("Go back", content);
        Assert.Contains("name=\"Title\"", content);
        Assert.Contains("name=\"Content\"", content);
    }

    /// <summary>Verifies that Edit (POST) WithInvalidTitle returns a View.</summary>
    [Fact]
    public async Task Edit_POST_WithInvalidTitle_ReturnsView()
    {
        // Arrange
        var id = DefaultId;
        var client = CreateAuthenticatedClient(OwnerId);
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Title", "" },
            { "Content", "Test Content" }
        });

        // Act
        var response = await client.PostAsync($"/Post/Edit/{id}", formData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Content", content);
        Assert.Contains("The Title field is required.", content);
    }

    /// <summary>Verifies that Edit (POST) WithInvalidContent returns a View.</summary>
    [Fact]
    public async Task Edit_POST_WithInvalidContent_ReturnsView()
    {
        // Arrange
        var id = DefaultId;
        var client = CreateAuthenticatedClient(OwnerId);
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Title", "Test Title" },
            { "Content", "" }
        });

        // Act
        var response = await client.PostAsync($"/Post/Edit/{id}", formData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Title", content);
        Assert.Contains("The Content field is required.", content);
    }

    /// <summary>Verifies that Edit (POST) returns a View.</summary>
    [Fact]
    public async Task Edit_POST_ReturnsView()
    {
        // Arrange
        var id = DefaultId;
        var post = new PostFactory()
            .WithId(DefaultId)
            .WithTitle("Test Title2")
            .WithContent("Test Content2")
            .WithAuthorId(OwnerId)
            .Build();

        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        var client = CreateAuthenticatedClient(OwnerId);
        var formData = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Title", "Test Title" },
            { "Content", "Test Content" }
        });

        // Act
        var response = await client.PostAsync($"/Post/Edit/{id}", formData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Title", content);
        Assert.Contains("Test Content", content);
        Assert.Contains($"/Post/Edit/{DefaultId}", content);
        Assert.Contains($"/Post/Delete/{DefaultId}", content);
    }

    // ---------- Delete ----------

    /// <summary>Verifies that Delete returns a View.</summary>
    [Fact]
    public async Task Delete_ReturnsView()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(DefaultId)
            .WithTitle("Test Title")
            .WithContent("Test Content")
            .WithAuthorId(OwnerId)
            .Build();

        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        var client = CreateAuthenticatedClient(OwnerId);

        // Act
        var response = await client.GetAsync($"/Post/Delete/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Title", content);
        Assert.Contains("Go back", content);
        Assert.Contains($"/Post/Delete/{DefaultId}", content);
        Assert.Contains($"/Post/Details/{post.Id}", content);
    }

    /// <summary>Verifies that Delete WithModifiedPost ShowsModifiedDate.</summary>
    [Fact]
    public async Task Delete_WithModifiedPost_ShowsModifiedDate()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(DefaultId)
            .WithAuthorId(OwnerId)
            .WithPublishDate(DefaultDate)
            .WithModifiedDate(DefaultDate.AddDays(1))
            .Build();

        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        var client = CreateAuthenticatedClient(OwnerId);

        // Act
        var response = await client.GetAsync($"/Post/Delete/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains(DefaultDate.AddDays(1).ToString("dd/MM/yyyy"), content);
    }

    /// <summary>Verifies that Delete WithUnmodifiedPost DoesNotDuplicateDate.</summary>
    [Fact]
    public async Task Delete_WithUnmodifiedPost_DoesNotDuplicateDate()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(DefaultId)
            .WithAuthorId(OwnerId)
            .WithPublishDate(DefaultDate)
            .WithModifiedDate(DefaultDate)
            .Build();

        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        var client = CreateAuthenticatedClient(OwnerId);

        // Act
        var response = await client.GetAsync($"/Post/Delete/{post.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var dateText = DefaultDate.ToString("dd/MM/yyyy");
        var occurrences = content.Split(dateText).Length - 1;
        Assert.Equal(1, occurrences);
    }
}