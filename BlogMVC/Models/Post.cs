namespace BlogMVC.Models;

public class Post
{
    public Post(int postId, string title, string content, string author, string publishDate)
    {
        PostId = postId;
        Title = title;
        Content = content;
        Author = author;
        PublishDate = publishDate;
    }

    public int PostId { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Author { get; set; }
    public string? PublishDate { get; set; }
    public string? ModifiedDate { get; set; }

    public static List<Post> AllPosts()
    {
        //Create dummy Posts
        return new List<Post>
        {
            new(0, "First", "First", "First", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
            new(1, "Second", "Second", "Second", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
            new(2, "Third", "Third", "Third", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
        };
    }
}