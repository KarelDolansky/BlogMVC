using BlogMVC.Data;
using BlogMVC.Dto;
using BlogMVC.Responses;
using BlogMVC.Results;
using BlogMVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogMVC.Controllers;

/// <summary>
///     Blog posts API at "api/blog". Reading is public; writes require the matching claim from
///     <see cref="Permissions.Posts" /> (see <see cref="RolePermissions" />). Edit/delete also check
///     resource ownership unless the caller holds the "Any" variant.
/// </summary>
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

    /// <summary>POST api/blog – creates a post; requires <see cref="Permissions.Posts.Create" />.</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.Posts.Create)]
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
    ///     POST api/blog/bulk – creates multiple posts; requires <see cref="Permissions.Posts.CreateBulk" />
    ///     (narrower than <see cref="Permissions.Posts.Create" /> — Author lacks it).
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(Policy = Permissions.Posts.CreateBulk)]
    public async Task<ActionResult<IReadOnlyList<PostResponse>>> BulkCreatePosts(List<CreatePostDto> createPostDtoes)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var userName = GetUserName();
        if (string.IsNullOrEmpty(userName)) return Unauthorized();

        var created = await postService.AddBulkPostAsync(createPostDtoes, userId, userName);
        return StatusCode(StatusCodes.Status201Created, created.Select(PostResponse.FromPost).ToList());
    }

    /// <summary>
    ///     PUT api/blog/{id} – updates a post. Requires <see cref="Permissions.Posts.EditOwn" /> plus ownership,
    ///     or <see cref="Permissions.Posts.EditAny" /> for any post.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = Permissions.Posts.EditPolicy)]
    public async Task<ActionResult> EditPost(string id, EditPostDto editPostDto)
    {
        if (!IsValidObjectId(id)) return BadRequest("Invalid post ID.");

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var post = await postService.GetPostAsync(id);
        if (post == null) return NotFound();
        if (post.AuthorId != userId && !User.HasClaim(Permissions.ClaimType, Permissions.Posts.EditAny))
            return Forbid();

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
    ///     DELETE api/blog/{id} – deletes a post. Requires <see cref="Permissions.Posts.DeleteOwn" /> plus
    ///     ownership, or <see cref="Permissions.Posts.DeleteAny" /> for any post.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = Permissions.Posts.DeletePolicy)]
    public async Task<ActionResult> DeletePost(string id)
    {
        if (!IsValidObjectId(id)) return BadRequest("Invalid post ID.");

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var post = await postService.GetPostAsync(id);
        if (post == null) return NotFound();
        if (post.AuthorId != userId && !User.HasClaim(Permissions.ClaimType, Permissions.Posts.DeleteAny))
            return Forbid();

        if (!await postService.DeletePostAsync(id)) return NotFound("Post not found.");
        return NoContent();
    }
}