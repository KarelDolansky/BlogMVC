using BlogMVC.Models;

namespace BlogMVC.Services;

public interface IPostService
{
    List<Post> GetPosts();
    Post? GetPost(int id);
    bool AddPost(Post post);
    bool DeletePost(int id);
    bool EditPost(int id, Post post);
}

public class PostService(IPostMarkdownReaderService postMarkdownReaderService) : IPostService
{
    private readonly List<Post> _posts = postMarkdownReaderService.GetAllPostsFromMarkdown();

    public List<Post> GetPosts()
    {
        return _posts;
    }

    public Post? GetPost(int id)
    {
        return _posts.FirstOrDefault(p => p.Id == id);
    }

    public bool AddPost(Post post)
    {
        if (_posts.Any(p => p.Id == post.Id)) return false;
        _posts.Add(post);
        return true;
    }

    public bool DeletePost(int id)
    {
        var post = GetPost(id);
        if (post != null)
        {
            _posts.Remove(post);
            return true;
        }

        return false;
    }

    public bool EditPost(int id, Post post)
    {
        var index = _posts.FindIndex(p => p.Id == id);
        if (index != -1)
        {
            _posts[index] = post;
            return true;
        }

        return false;
    }
}