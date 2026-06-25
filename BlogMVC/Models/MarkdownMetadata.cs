namespace BlogMVC.Models;

public class MarkdownMetadata
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public DateTime? PublishDate { get; set; }
    public string? Slug { get; set; }
    public bool Draft { get; set; }
}