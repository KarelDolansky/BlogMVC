using BlogMVC.Dto;

namespace BlogMVC.Tests.Helpers;

/// <summary>
///     Test Data Builder for creating <see cref="CreatePostDto" /> instances in tests,
///     with sensible defaults and fluent methods to override individual fields.
/// </summary>
public class CreatePostDtoFactory
{
    private readonly CreatePostDto _entity = new()
    {
        Title = "Title",
        Description = "Description",
        Content = "Content"
    };

    /// <summary>Sets the title.</summary>
    public CreatePostDtoFactory WithTitle(string title)
    {
        _entity.Title = title;
        return this;
    }

    /// <summary>Sets the description.</summary>
    public CreatePostDtoFactory WithDescription(string description)
    {
        _entity.Description = description;
        return this;
    }

    /// <summary>Sets the content.</summary>
    public CreatePostDtoFactory WithContent(string content)
    {
        _entity.Content = content;
        return this;
    }

    /// <summary>Returns the built <see cref="CreatePostDto" /> instance.</summary>
    public CreatePostDto Build()
    {
        return _entity;
    }
}