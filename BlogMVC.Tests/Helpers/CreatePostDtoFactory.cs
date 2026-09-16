using BlogMVC.Dto;

namespace BlogMVC.Tests.Helpers;

/// <summary>
///     Test Data Builder for creating <see cref="CreatePostDto" /> instances in tests,
///     with sensible defaults and fluent methods to override individual fields.
/// </summary>
public class CreatePostDtoFactory
{
    /// <summary>The DTO being built, pre-populated with default title/description/content.</summary>
    private readonly CreatePostDto _entity = new()
    {
        Title = "Title",
        Description = "Description",
        Content = "Content"
    };

    /// <summary>Sets the title.</summary>
    /// <param name="title">The title to assign.</param>
    /// <returns>This factory, for chaining.</returns>
    public CreatePostDtoFactory WithTitle(string title)
    {
        _entity.Title = title;
        return this;
    }

    /// <summary>Sets the description.</summary>
    /// <param name="description">The description to assign.</param>
    /// <returns>This factory, for chaining.</returns>
    public CreatePostDtoFactory WithDescription(string description)
    {
        _entity.Description = description;
        return this;
    }

    /// <summary>Sets the content.</summary>
    /// <param name="content">The content to assign.</param>
    /// <returns>This factory, for chaining.</returns>
    public CreatePostDtoFactory WithContent(string content)
    {
        _entity.Content = content;
        return this;
    }

    /// <summary>Builds the configured <see cref="CreatePostDto" /> instance.</summary>
    /// <returns>The built <see cref="CreatePostDto" />.</returns>
    public CreatePostDto Build()
    {
        return _entity;
    }
}