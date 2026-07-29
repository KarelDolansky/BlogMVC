using System.Net;
using BlogMVC.Infrastructure.Interfaces;
using BlogMVC.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BlogMVC.Tests.IntegrationTests;

/// <summary>
///     Integration tests for the rendered HTML of <see cref="HomeController" />'s Index action
///     (the post index table on the home page), against the real app and a real MongoDB instance.
/// </summary>
[Collection("PostController")]
public class HomeControllerRenderingTests(WebApplicationFactory<Program> factory)
    : PostControllerTestBase(factory)
{
    /// <summary>Verifies that Index with no posts shows the empty state instead of a table.</summary>
    [Fact]
    public async Task Index_WithNoPosts_ShowsEmptyState()
    {
        // Act
        var response = await Client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("No posts yet", content);
        Assert.DoesNotContain("post-index", content);
    }

    /// <summary>Verifies that Index with one post lists its title, description, author and date.</summary>
    [Fact]
    public async Task Index_WithOnePost_ListsPostDetails()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(DefaultId)
            .WithTitle("Test Title")
            .WithDescription("Test Description")
            .WithAuthor("Test Author")
            .WithPublishDate(DefaultDate)
            .WithModifiedDate(DefaultDate)
            .Build();

        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        // Act
        var response = await Client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Title", content);
        Assert.Contains("Test Description", content);
        Assert.Contains("Test Author", content);
        Assert.Contains(DefaultDate.ToString("dd/MM/yyyy"), content);
        Assert.Contains($"/Post/Details/{DefaultId}", content);
    }

    /// <summary>Verifies that Index with exactly one post shows the singular entry count.</summary>
    [Fact]
    public async Task Index_WithOnePost_ShowsSingularEntryCount()
    {
        // Arrange
        var post = new PostFactory().WithId(DefaultId).Build();
        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        // Act
        var response = await Client.GetAsync("/");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("1 entry", content);
        Assert.DoesNotContain("1 entries", content);
    }

    /// <summary>Verifies that an unmodified post shows a "Published" status, not "Updated".</summary>
    [Fact]
    public async Task Index_WithUnmodifiedPost_ShowsPublishedStatus()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(DefaultId)
            .WithPublishDate(DefaultDate)
            .WithModifiedDate(DefaultDate)
            .Build();

        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        // Act
        var response = await Client.GetAsync("/");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Published", content);
        Assert.DoesNotContain("Updated", content);
    }

    /// <summary>Verifies that a modified post shows an "Updated" status with the modified date.</summary>
    [Fact]
    public async Task Index_WithModifiedPost_ShowsUpdatedStatus()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(DefaultId)
            .WithPublishDate(DefaultDate)
            .WithModifiedDate(DefaultDate.AddDays(1))
            .Build();

        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        // Act
        var response = await Client.GetAsync("/");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Updated", content);
        Assert.Contains(DefaultDate.AddDays(1).ToString("dd/MM/yyyy"), content);
    }

    /// <summary>
    ///     Verifies that posts are listed newest-first and numbered so the most recent post
    ///     gets the highest index (matching <see cref="IPostRepository.FindAllAsync" />'s
    ///     newest-first ordering).
    /// </summary>
    [Fact]
    public async Task Index_WithMultiplePosts_OrdersNewestFirstWithDescendingIndex()
    {
        // Arrange
        var olderPost = new PostFactory()
            .WithId("507f1f77bcf86cd799439021")
            .WithTitle("Older Post")
            .WithPublishDate(DefaultDate)
            .WithModifiedDate(DefaultDate)
            .Build();
        var newerPost = new PostFactory()
            .WithId("507f1f77bcf86cd799439022")
            .WithTitle("Newer Post")
            .WithPublishDate(DefaultDate.AddDays(1))
            .WithModifiedDate(DefaultDate.AddDays(1))
            .Build();

        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(olderPost);
        await repository.InsertOneAsync(newerPost);

        // Act
        var response = await Client.GetAsync("/");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("2 entries", content);
        Assert.True(
            content.IndexOf("Newer Post", StringComparison.Ordinal) <
            content.IndexOf("Older Post", StringComparison.Ordinal),
            "Expected the newer post to be listed before the older post.");
        Assert.Contains("<td class=\"post-index__idx\">02</td>", content);
        Assert.Contains("<td class=\"post-index__idx\">01</td>", content);
    }
}