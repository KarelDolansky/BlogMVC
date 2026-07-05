using BlogMVC.Models;
using BlogMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlogMVC.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BlogController(IPostService postService) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Post>>> GetPosts()
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
    public async Task<ActionResult<Post>> CreatePost(CreatePostDto createPostDto)
    {
        var created = await postService.AddPostAsync(createPostDto);
        return CreatedAtRoute("GetPost", new { id = created.Id }, created);
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<IReadOnlyList<Post>>> BulkCreatePosts(List<CreatePostDto> createPostDtoes)
    {
        var created = await postService.AddBulkPostAsync(createPostDtoes);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> EditPost(string id, EditPostDto editPostDto)
    {
        if (!IsValidObjectId(id)) return BadRequest("Invalid post ID.");
        if (!await postService.EditPostAsync(id, editPostDto)) return NotFound("Post not found.");
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeletePost(string id)
    {
        if (!IsValidObjectId(id)) return BadRequest("Invalid post ID.");
        if (!await postService.DeletePostAsync(id)) return NotFound("Post not found.");
        return NoContent();
    }
}