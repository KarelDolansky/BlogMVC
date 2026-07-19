using BlogMVC.Models;

namespace BlogMVC.Tests.Helpers;

/// <summary>
///     Test Data Builder for creating <see cref="EditPostDto" /> instances in tests,
///     with sensible defaults and fluent methods to override individual fields.
/// </summary>
public class EditPostDtoFactory
{
    private readonly EditPostDto _entity = new()
    {
        Title = "Title",
        Description = "Description",
        Content = "Content"
    };

    /// <summary>Sets the title.</summary>
    public EditPostDtoFactory WithTitle(string title)
    {
        _entity.Title = title;
        return this;
    }

    /// <summary>Sets the description.</summary>
    public EditPostDtoFactory WithDescription(string description)
    {
        _entity.Description = description;
        return this;
    }

    /// <summary>Sets the content.</summary>
    public EditPostDtoFactory WithContent(string content)
    {
        _entity.Content = content;
        return this;
    }

    /// <summary>Returns the built <see cref="EditPostDto" /> instance.</summary>
    public EditPostDto Build()
    {
        return _entity;
    }
}