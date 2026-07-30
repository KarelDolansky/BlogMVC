using System.Net;
using BlogMVC.Infrastructure.Interfaces;
using BlogMVC.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BlogMVC.Tests.IntegrationTests;

/// <summary>
///     Integration tests for the rendered HTML of <see cref="HomeController" />'s Index and Search actions
///     (the post index table on the home page, and the filtered results on the search page),
///     against the real app and a real MongoDB instance.
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

    // ---------- Search ----------

    /// <summary>Verifies that Search with no query shows the empty state instead of a table.</summary>
    [Fact]
    public async Task Search_WithNoQuery_ShowsEmptyState()
    {
        // Act
        var response = await Client.GetAsync("/Home/Search");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("No results for", content);
        Assert.DoesNotContain("post-index-wrap", content);
    }

    /// <summary>Verifies that Search with a query matching a post's title lists that post's details.</summary>
    [Fact]
    public async Task Search_WithQueryMatchingTitle_ListsPostDetails()
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
        var response = await Client.GetAsync("/Home/Search?query=Test");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Title", content);
        Assert.Contains("Test Description", content);
        Assert.Contains("Test Author", content);
        Assert.Contains($"/Post/Details/{DefaultId}", content);
    }

    /// <summary>Verifies that Search with a query matching a post's description lists that post.</summary>
    [Fact]
    public async Task Search_WithQueryMatchingDescription_ListsPost()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(DefaultId)
            .WithTitle("Unrelated Title")
            .WithDescription("Test Filter")
            .Build();

        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        // Act
        var response = await Client.GetAsync("/Home/Search?query=Filter");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Unrelated Title", content);
    }

    /// <summary>Verifies that Search is case-insensitive.</summary>
    [Fact]
    public async Task Search_IsCaseInsensitive()
    {
        // Arrange
        var post = new PostFactory().WithId(DefaultId).WithTitle("CaseSensitiveWord").Build();
        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        // Act
        var response = await Client.GetAsync("/Home/Search?query=casesensitiveword");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("CaseSensitiveWord", content);
    }

    /// <summary>Verifies that Search with a query matching no posts shows the empty state naming the query.</summary>
    [Fact]
    public async Task Search_WithNonMatchingQuery_ShowsEmptyStateWithQuery()
    {
        // Arrange
        var post = new PostFactory().WithId(DefaultId).WithTitle("Test Post").Build();
        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        // Act
        var response = await Client.GetAsync("/Home/Search?query=nothing-matches-this");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("No results for nothing-matches-this", content);
        Assert.DoesNotContain("Test Post", content);
    }

    /// <summary>
    ///     Verifies that Search with a query not matching any post's title or description excludes it, even if other
    ///     fields match.
    /// </summary>
    [Fact]
    public async Task Search_DoesNotMatch_OnContentOnly()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(DefaultId)
            .WithTitle("Unrelated Title")
            .WithDescription("Unrelated Description")
            .WithContent("Test filter")
            .Build();
        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        // Act
        var response = await Client.GetAsync("/Home/Search?query=filter");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("No results for filter", content);
        Assert.DoesNotContain("Unrelated Title", content);
    }

    /// <summary>Verifies that Search shows the entered query in the results heading.</summary>
    [Fact]
    public async Task Search_ShowsQuery_InHeading()
    {
        // Act
        var response = await Client.GetAsync("/Home/Search?query=example");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Results for: example", content);
    }

    /// <summary>Verifies that Search with matching posts shows the correct entry count.</summary>
    [Fact]
    public async Task Search_WithTwoMatches_ShowsPluralEntryCount()
    {
        // Arrange
        var post1 = new PostFactory().WithId("507f1f77bcf86cd799439021").WithTitle("Match One").Build();
        var post2 = new PostFactory().WithId("507f1f77bcf86cd799439022").WithTitle("Match Two").Build();
        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post1);
        await repository.InsertOneAsync(post2);

        // Act
        var response = await Client.GetAsync("/Home/Search?query=Match");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("2 entries", content);
    }

    /// <summary>Verifies that Search with a query containing special regex characters doesn't throw and matches literally.</summary>
    [Fact]
    public async Task Search_WithRegexSpecialCharacters_MatchesLiterally()
    {
        // Arrange
        var post = new PostFactory()
            .WithId(DefaultId)
            .WithTitle("C#")
            .Build();
        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertOneAsync(post);

        // Act
        var response = await Client.GetAsync("/Home/Search?query=C%23");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("C#", content);
    }
}