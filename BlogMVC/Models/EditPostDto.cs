using System.ComponentModel.DataAnnotations;

namespace BlogMVC.Models;

public class EditPostDto
{
    [Required] public required string Title { get; set; }

    [Required] public required string Content { get; set; }
}