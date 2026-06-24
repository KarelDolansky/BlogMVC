using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BlogMVC.Models;
namespace BlogMVC.Controllers;


[Route("api/[controller]")]
[ApiController]
public class BlogController : ControllerBase
{
    private List<Post> posts = Post.AllPosts();
    
    [HttpGet("/Blog")]
    public ActionResult<List<Post>> Index()
    {
        return posts;
    }
}