using System.ComponentModel.DataAnnotations;

namespace BlogMVC.Models;

/// <summary>
/// Data Transfer Object used to create a new post.
/// Serves as the input model for the form/API request – it does not include fields
/// such as Id, AuthorId or publish date, which are filled in by the service layer.
/// </summary>
public class CreatePostDto
{
    /// <summary>Title of the new post. Required (validated by <see cref="RequiredAttribute"/>).</summary>
    [Required]
    public required string Title { get; set; }

    /// <summary>Content of the new post. Required.</summary>
    [Required]
    public required string Content { get; set; }
}