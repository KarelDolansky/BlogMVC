using System.Diagnostics;
using BlogMVC.Models;

namespace BlogMVC.Services;

public interface IPostService
{
    List<Post> GetPosts();
    Post GetPost(int id);
}

public class PostService(IPostMarkdownReaderService postMarkdownReaderService) : IPostService
{
    private readonly List<Post> _posts = postMarkdownReaderService.GetAllPostsFromMarkdown();

    public List<Post> GetPosts()
    {
        return _posts;
    }

    public Post GetPost(int id)
    {
        Debug.Assert(id >= 1);
        Debug.Assert(id <= _posts.Count);
        return _posts[id-1];
    }
}