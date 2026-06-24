using System.Diagnostics;
using BlogMVC.Models;

namespace BlogMVC.Services;

public interface IPostService
{
    List<Post> GetPosts();
    Post GetPost(int id);
}

public class PostService : IPostService
{
    private readonly List<Post> _posts = Post.AllPosts();

    public List<Post> GetPosts()
    {
        return _posts;
    }

    public Post GetPost(int id)
    {
        Debug.Assert(id >= 0);
        Debug.Assert(id <= _posts.Count);
        return _posts[id];
    }
}