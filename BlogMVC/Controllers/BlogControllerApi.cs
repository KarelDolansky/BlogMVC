using BlogMVC.Models;
using BlogMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlogMVC.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BlogControllerApi(IPostService postService) : ControllerBase
{
    [HttpGet]
    public ActionResult<List<Post>> GetPosts()
    {
        return Ok(postService.GetPosts());
    }

    [HttpGet("{id}", Name = "GetPost")]
    public ActionResult<Post> GetPost(int id)
    {
        var post = postService.GetPost(id);

        if (post == null) return NotFound();

        return Ok(post);
    }

    [HttpPost]
    public ActionResult<Post> CreatePost(Post post)
    {
        if (!postService.AddPost(post)) return BadRequest("Post with same id already exists.");

        return CreatedAtRoute("GetPost", new { id = post.Id }, post);
    }

    [HttpPut("{id}")]
    public ActionResult EditPost(int id, Post post)
    {
        if (id != post.Id) return BadRequest("ID in URL does not match ID in body.");

        if (!postService.EditPost(id, post)) return NotFound("Post not found.");
        return NoContent();
    }

    [HttpDelete("{id}")]
    public ActionResult DeletePost(int id)
    {
        if (!postService.DeletePost(id)) return NotFound("Post not found.");
        return NoContent();
    }
}