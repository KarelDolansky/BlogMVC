using BlogMVC.Models;
using BlogMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlogMVC.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BlogControllerApi(IPostService postService) : ControllerBase
{
    [HttpGet]
    public ActionResult<List<Post>> Index()
    {
        return postService.GetPosts();
    }

    [HttpGet("{id}")]
    public ActionResult<Post> GetPost(int id)
    {
        return postService.GetPost(id);
    }
}