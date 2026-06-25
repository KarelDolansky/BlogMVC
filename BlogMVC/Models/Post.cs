namespace BlogMVC.Models;

public class Post
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Author { get; set; }
    public DateTime? PublishDate { get; set; }
    public DateTime? ModifiedDate { get; set; }

    public static List<Post> AllPosts()
    {
        return
        [
            new Post { Id = 0, Title = "First", Content = "First", Author = "First", PublishDate = DateTime.Now },
            new Post { Id = 1, Title = "Second", Content = "Second", Author = "Second", PublishDate = DateTime.Now },
            new Post { Id = 2, Title = "Third", Content = "Third", Author = "Third", PublishDate = DateTime.Now }
        ];
    }
}