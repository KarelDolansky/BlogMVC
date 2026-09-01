using BlogMVC.Models;

namespace BlogMVC.Tests.Helpers;

/// <summary>
///     Test Data Builder (fluent factory) for creating <see cref="Post" /> instances in tests.
///     Provides sensible defaults and "With..." methods to override only the fields
///     a given test cares about.
/// </summary>
public class PostFactory
{
    /// <summary>The post being built, pre-populated with default title/description/content/author.</summary>
    private readonly Post _entity = new()
    {
        Title = "Title",
        Description = "Description",
        Content = "Content",
        AuthorId = "AuthorId",
        Author = "Author"
    };

    /// <summary>Sets the post's Id.</summary>
    /// <param name="id">The Id to assign.</param>
    /// <returns>This factory, for chaining.</returns>
    public PostFactory WithId(string id)
    {
        _entity.Id = id;
        return this;
    }

    /// <summary>Sets the post's title.</summary>
    /// <param name="title">The title to assign.</param>
    /// <returns>This factory, for chaining.</returns>
    public PostFactory WithTitle(string title)
    {
        _entity.Title = title;
        return this;
    }

    /// <summary>Sets the description.</summary>
    /// <param name="description">The description to assign.</param>
    /// <returns>This factory, for chaining.</returns>
    public PostFactory WithDescription(string description)
    {
        _entity.Description = description;
        return this;
    }

    /// <summary>Sets the post's content.</summary>
    /// <param name="content">The content to assign.</param>
    /// <returns>This factory, for chaining.</returns>
    public PostFactory WithContent(string content)
    {
        _entity.Content = content;
        return this;
    }

    /// <summary>Sets the post's author Id.</summary>
    /// <param name="authorId">The author Id to assign.</param>
    /// <returns>This factory, for chaining.</returns>
    public PostFactory WithAuthorId(string authorId)
    {
        _entity.AuthorId = authorId;
        return this;
    }

    /// <summary>Sets the post's author name.</summary>
    /// <param name="author">The author name to assign.</param>
    /// <returns>This factory, for chaining.</returns>
    public PostFactory WithAuthor(string author)
    {
        _entity.Author = author;
        return this;
    }

    /// <summary>Sets the post's publish date.</summary>
    /// <param name="publishDate">The publish date to assign.</param>
    /// <returns>This factory, for chaining.</returns>
    public PostFactory WithPublishDate(DateTime publishDate)
    {
        _entity.PublishDate = publishDate;
        return this;
    }

    /// <summary>Sets the post's last-modified date.</summary>
    /// <param name="modifiedDate">The last-modified date to assign.</param>
    /// <returns>This factory, for chaining.</returns>
    public PostFactory WithModifiedDate(DateTime modifiedDate)
    {
        _entity.ModifiedDate = modifiedDate;
        return this;
    }

    /// <summary>Sets the post's optimistic-concurrency version.</summary>
    /// <param name="version">The version to assign.</param>
    /// <returns>This factory, for chaining.</returns>
    public PostFactory WithVersion(long version)
    {
        _entity.Version = version;
        return this;
    }

    /// <summary>Builds the configured <see cref="Post" /> instance.</summary>
    /// <returns>The built <see cref="Post" />.</returns>
    public Post Build()
    {
        return _entity;
    }
}