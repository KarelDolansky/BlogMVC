using BlogMVC.Dto;
using BlogMVC.Responses;
using BlogMVC.Results;
using BlogMVC.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogMVC.Controllers;

/// <summary>Blog posts API at "api/blog". Reading is public; writing requires a JWT, editing/deleting requires ownership.</summary>
[Route("api/[controller]")]
[ApiController]
public class BlogController(IPostService postService) : BaseApiController
{
    /// <summary>GET api/blog – returns all posts as a JSON array. Public, no authentication required.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PostResponse>>> GetPosts()
    {
        var posts = await postService.GetPostsAsync();
        return Ok(posts.Select(PostResponse.FromPost).ToList());
    }

    /// <summary>GET api/blog/{id} – returns a single post by Id. Public. Named route used by <see cref="CreatePost" />.</summary>
    [HttpGet("{id}", Name = "GetPost")]
    public async Task<ActionResult<PostResponse>> GetPost(string id)
    {
        if (!IsValidObjectId(id)) return BadRequest("Invalid post ID.");
        var post = await postService.GetPostAsync(id);
        if (post == null) return NotFound();
        SetETag(post.Version);
        return Ok(PostResponse.FromPost(post));
    }

    /// <summary>POST api/blog – creates a post; JWT required, author taken from the token's claims.</summary>
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<PostResponse>> CreatePost(CreatePostDto createPostDto)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var userName = GetUserName();
        if (string.IsNullOrEmpty(userName)) return Unauthorized();

        var created = await postService.AddPostAsync(createPostDto, userId, userName);
        SetETag(created.Version);
        return CreatedAtRoute("GetPost", new { id = created.Id }, PostResponse.FromPost(created));
    }

    /// <summary>
    ///     POST api/blog/bulk – creates multiple posts authored by the caller (same JWT requirement as
    ///     <see cref="CreatePost" />).
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<IReadOnlyList<PostResponse>>> BulkCreatePosts(List<CreatePostDto> createPostDtoes)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var userName = GetUserName();
        if (string.IsNullOrEmpty(userName)) return Unauthorized();

        var created = await postService.AddBulkPostAsync(createPostDtoes, userId, userName);
        return StatusCode(StatusCodes.Status201Created, created.Select(PostResponse.FromPost).ToList());
    }

    /// <summary>PUT api/blog/{id} – updates a post. Requires JWT + ownership (403 otherwise), 400/404 for invalid/missing Id.</summary>
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

        if (!TryGetIfMatchVersion(out var expectedVersion))
            return BadRequest("If-Match header with the post's current ETag is required.");

        var result = await postService.EditPostAsync(id, editPostDto, expectedVersion);
        if (result == PostUpdateResult.Success) SetETag(expectedVersion + 1);
        return result switch
        {
            PostUpdateResult.Success => NoContent(),
            PostUpdateResult.Conflict => StatusCode(StatusCodes.Status412PreconditionFailed,
                "Post was modified since you last fetched it. Reload and try again."),
            _ => NotFound("Post not found.")
        };
    }

    /// <summary>
    ///     DELETE api/blog/{id} – deletes a post. Requires JWT + ownership (403 otherwise), 400/404 for invalid/missing
    ///     Id.
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