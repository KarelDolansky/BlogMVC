using BlogMVC.Dto;
using BlogMVC.Models;
using BlogMVC.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogMVC.Controllers;

/// <summary>
/// REST Web API for blog posts (JSON), available at "api/blog".
/// Reading posts (<see cref="GetPosts"/>, <see cref="GetPost"/>) is public by design.
/// Creating, editing, deleting, and bulk-creating posts require a valid JWT bearer token
/// (see <see cref="AuthController.Login"/> to obtain one); editing/deleting additionally
/// require the caller to be the post's author.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class BlogController(IPostService postService) : BaseApiController
{
    /// <summary>GET api/blog – returns all posts as a JSON array. Public, no authentication required.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Post>>> GetPosts()
    {
        return Ok(await postService.GetPostsAsync());
    }

    /// <summary>
    /// GET api/blog/{id} – returns a single post by Id. Public, no authentication required.
    /// The named route "GetPost" is used in <see cref="CreatePost"/> to build the Location header.
    /// </summary>
    [HttpGet("{id}", Name = "GetPost")]
    public async Task<ActionResult<Post>> GetPost(string id)
    {
        if (!IsValidObjectId(id)) return BadRequest("Invalid post ID.");
        var post = await postService.GetPostAsync(id);
        if (post == null) return NotFound();
        return Ok(post);
    }

    /// <summary>
    /// POST api/blog – creates a new post. Requires a valid JWT bearer token; the author is
    /// taken from the token's claims (see <see cref="BaseApiController.GetUserId"/>/<see cref="BaseApiController.GetUserName"/>).
    /// </summary>
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<Post>> CreatePost(CreatePostDto createPostDto)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var userName = GetUserName();
        if (string.IsNullOrEmpty(userName)) return Unauthorized();

        var created = await postService.AddPostAsync(createPostDto, userId, userName);
        return CreatedAtRoute("GetPost", new { id = created.Id }, created);
    }

    /// <summary>
    /// POST api/blog/bulk – bulk-creates multiple posts at once, all authored by the caller
    /// (same JWT requirement as <see cref="CreatePost"/>).
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<IReadOnlyList<Post>>> BulkCreatePosts(List<CreatePostDto> createPostDtoes)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var userName = GetUserName();
        if (string.IsNullOrEmpty(userName)) return Unauthorized();

        var created = await postService.AddBulkPostAsync(createPostDtoes, userId, userName);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>
    /// PUT api/blog/{id} – updates an existing post. Requires a valid JWT bearer token and that
    /// the caller is the post's author (403 Forbid otherwise). Returns 400 for an invalid Id,
    /// 404 if the post doesn't exist.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult> EditPost(string id, EditPostDto editPostDto)
    {
        if (!IsValidObjectId(id)) return BadRequest("Invalid post ID.");

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var post = await postService.GetPostAsync(id);
        if (post == null) return NotFound();
        if (post.AuthorId != userId) return Forbid();

        if (!await postService.EditPostAsync(id, editPostDto)) return NotFound("Post not found.");
        return NoContent();
    }

    /// <summary>
    /// DELETE api/blog/{id} – deletes a post. Requires a valid JWT bearer token and that the
    /// caller is the post's author (403 Forbid otherwise). Returns 400 for an invalid Id,
    /// 404 if the post doesn't exist.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult> DeletePost(string id)
    {
        if (!IsValidObjectId(id)) return BadRequest("Invalid post ID.");

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var post = await postService.GetPostAsync(id);
        if (post == null) return NotFound();
        if (post.AuthorId != userId) return Forbid();

        if (!await postService.DeletePostAsync(id)) return NotFound("Post not found.");
        return NoContent();
    }
}