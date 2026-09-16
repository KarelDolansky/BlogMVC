using BlogMVC.Dto;

namespace BlogMVC.Tests.Helpers;

/// <summary>
///     Test Data Builder for creating <see cref="EditPostDto" /> instances in tests,
///     with sensible defaults and fluent methods to override individual fields.
/// </summary>
public class EditPostDtoFactory
{
    /// <summary>The DTO being built, pre-populated with default title/description/content.</summary>
    private readonly EditPostDto _entity = new()
    {
        Title = "Title",
        Description = "Description",
        Content = "Content"
    };

    /// <summary>Sets the title.</summary>
    /// <param name="title">The title to assign.</param>
    /// <returns>This factory, for chaining.</returns>
    public EditPostDtoFactory WithTitle(string title)
    {
        _entity.Title = title;
        return this;
    }

    /// <summary>Sets the description.</summary>
    /// <param name="description">The description to assign.</param>
    /// <returns>This factory, for chaining.</returns>
    public EditPostDtoFactory WithDescription(string description)
    {
        _entity.Description = description;
        return this;
    }

    /// <summary>Sets the content.</summary>
    /// <param name="content">The content to assign.</param>
    /// <returns>This factory, for chaining.</returns>
    public EditPostDtoFactory WithContent(string content)
    {
        _entity.Content = content;
        return this;
    }

    /// <summary>Builds the configured <see cref="EditPostDto" /> instance.</summary>
    /// <returns>The built <see cref="EditPostDto" />.</returns>
    public EditPostDto Build()
    {
        return _entity;
    }
}