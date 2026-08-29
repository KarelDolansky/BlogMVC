using System.ComponentModel.DataAnnotations;

namespace BlogMVC.Dto;

/// <summary>
///     Data Transfer Object used to edit an existing post.
///     Contains only the fields a user is allowed to change (title, description and content).
/// </summary>
public class EditPostDto
{
    /// <summary>New title of the post. Required.</summary>
    [Required]
    public required string Title { get; set; }

    /// <summary>New short description of the post. Required.</summary>
    [Required]
    public required string Description { get; set; }

    /// <summary>New content of the post. Required.</summary>
    [Required]
    public required string Content { get; set; }
}