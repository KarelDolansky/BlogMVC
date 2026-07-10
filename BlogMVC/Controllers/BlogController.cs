using BlogMVC.Models;
using BlogMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlogMVC.Controllers;

/// <summary>
/// REST Web API for blog posts (JSON), available at "api/blog".
/// Complements the MVC controller <see cref="PostController"/>, which serves the web UI.
/// WARNING: none of the endpoints in this controller are protected by <c>[Authorize]</c> —
/// anyone can read, create, edit, or delete posts through this API. The create endpoints
/// also hard-code the author as "default" instead of using an authenticated identity.
/// This must be fixed with proper authorization before this API is used in production.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class BlogController(IPostService postService) : BaseApiController
{
    /// <summary>
    /// GET api/blog – returns all posts as a JSON array.
    /// TODO: currently public/unauthenticated by design (read access) – confirm this is intended before production.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Post>>> GetPosts()
    {
        return Ok(await postService.GetPostsAsync());
    }

    /// <summary>
    /// GET api/blog/{id} – returns a single post by Id.
    /// The named route "GetPost" is used in <see cref="CreatePost"/> to build the Location header.
    /// TODO: currently public/unauthenticated by design (read access) – confirm this is intended before production.
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
    /// POST api/blog – creates a new post.
    /// TODO: no authorization check – add [Authorize] and use the authenticated user's Id/name
    /// instead of the hard-coded "default" author.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Post>> CreatePost(CreatePostDto createPostDto)
    {
        var created = await postService.AddPostAsync(createPostDto, "default", "default"); //TODO: Authorized;
        return CreatedAtRoute("GetPost", new { id = created.Id }, created);
    }

    /// <summary>
    /// POST api/blog/bulk – bulk-creates multiple posts at once with the same (still hard-coded) author.
    /// TODO: no authorization check – same as <see cref="CreatePost"/>, needs [Authorize] and a real author.
    /// </summary>
    [HttpPost("bulk")]
    public async Task<ActionResult<IReadOnlyList<Post>>> BulkCreatePosts(List<CreatePostDto> createPostDtoes)
    {
        var created = await postService.AddBulkPostAsync(createPostDtoes, "default", "default"); //TODO: Authorized;
        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>
    /// PUT api/blog/{id} – updates an existing post. Returns 400 for an invalid Id, 404 if the post doesn't exist.
    /// TODO: no authorization check at all – any caller can edit any post. Add [Authorize] and verify
    /// the caller is the post's author (see <see cref="PostController.Edit(string, EditPostDto)"/> for the pattern).
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> EditPost(string id, EditPostDto editPostDto)
    {
        if (!IsValidObjectId(id)) return BadRequest("Invalid post ID.");
        if (!await postService.EditPostAsync(id, editPostDto)) return NotFound("Post not found.");
        return NoContent();
    }

    /// <summary>
    /// DELETE api/blog/{id} – deletes a post. Returns 400 for an invalid Id, 404 if the post doesn't exist.
    /// TODO: no authorization check at all – any caller can delete any post. Add [Authorize] and verify
    /// the caller is the post's author (see <see cref="PostController.DeletePost"/> for the pattern).
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeletePost(string id)
    {
        if (!IsValidObjectId(id)) return BadRequest("Invalid post ID.");
        if (!await postService.DeletePostAsync(id)) return NotFound("Post not found.");
        return NoContent();
    }
}