using BlogMVC.Models;

namespace BlogMVC.Tests.Helpers;

/// <summary>
///     Test Data Builder (fluent factory) for creating <see cref="Post" /> instances in tests.
///     Provides sensible defaults and "With..." methods to override only the fields
///     a given test cares about.
/// </summary>
public class PostFactory
{
    private readonly Post _entity = new()
    {
        Title = "Title",
        Description = "Description",
        Content = "Content",
        AuthorId = "AuthorId",
        Author = "Author"
    };

    /// <summary>Sets the post's Id.</summary>
    public PostFactory WithId(string id)
    {
        _entity.Id = id;
        return this;
    }

    /// <summary>Sets the post's title.</summary>
    public PostFactory WithTitle(string title)
    {
        _entity.Title = title;
        return this;
    }

    /// <summary>Sets the description.</summary>
    public PostFactory WithDescription(string description)
    {
        _entity.Description = description;
        return this;
    }

    /// <summary>Sets the post's content.</summary>
    public PostFactory WithContent(string content)
    {
        _entity.Content = content;
        return this;
    }

    /// <summary>Sets the post's author Id.</summary>
    public PostFactory WithAuthorId(string authorId)
    {
        _entity.AuthorId = authorId;
        return this;
    }

    /// <summary>Sets the post's author name.</summary>
    public PostFactory WithAuthor(string author)
    {
        _entity.Author = author;
        return this;
    }

    /// <summary>Sets the post's publish date.</summary>
    public PostFactory WithPublishDate(DateTime publishDate)
    {
        _entity.PublishDate = publishDate;
        return this;
    }

    /// <summary>Sets the post's last-modified date.</summary>
    public PostFactory WithModifiedDate(DateTime modifiedDate)
    {
        _entity.ModifiedDate = modifiedDate;
        return this;
    }

    /// <summary>Sets the post's optimistic-concurrency version.</summary>
    public PostFactory WithVersion(long version)
    {
        _entity.Version = version;
        return this;
    }

    /// <summary>Returns the built <see cref="Post" /> instance.</summary>
    public Post Build()
    {
        return _entity;
    }
}