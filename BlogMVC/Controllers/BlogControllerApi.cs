using BlogMVC.Models;
using BlogMVC.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace BlogMVC.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BlogControllerApi(IPostService postService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<Post>>> GetPosts()
    {
        return Ok(await postService.GetPostsAsync());
    }

    [HttpGet("{id}", Name = "GetPost")]
    public async Task<ActionResult<Post>> GetPost(string id)
    {
        if (!IsValidObjectId(id)) return BadRequest("Invalid post ID.");
        var post = await postService.GetPostAsync(id);
        if (post == null) return NotFound();
        return Ok(post);
    }

    [HttpPost]
    public async Task<ActionResult<Post>> CreatePost(Post post)
    {
        post.Id = null;

        var created = await postService.AddPostAsync(post);
        return CreatedAtRoute("GetPost", new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> EditPost(string id, Post post)
    {
        if (!IsValidObjectId(id)) return BadRequest("Invalid post ID.");
        if (id != post.Id) return BadRequest("ID in URL does not match ID in body.");
        if (!await postService.EditPostAsync(id, post)) return NotFound("Post not found.");
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeletePost(string id)
    {
        if (!IsValidObjectId(id)) return BadRequest("Invalid post ID.");
        if (!await postService.DeletePostAsync(id)) return NotFound("Post not found.");
        return NoContent();
    }

    [HttpGet("markdown")]
    public async Task<ActionResult> AddPostFromMarkDown()
    {
        await postService.AddPostFromMarkDownAsync();
        return Ok();
    }

    private static bool IsValidObjectId(string id)
    {
        return ObjectId.TryParse(id, out _);
    }
}