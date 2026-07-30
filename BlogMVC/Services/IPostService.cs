using BlogMVC.Models;

namespace BlogMVC.Services;

/// <summary>
///     Application (business) layer for working with blog posts.
///     Sits between controllers and the data layer (<see cref="BlogMVC.Infrastructure.Interfaces.IPostRepository" />) –
///     handles mapping DTOs to entities and filling in derived values (author, publish/modified timestamps).
/// </summary>
public interface IPostService
{
    /// <summary>Returns all posts, ordered from the newest.</summary>
    Task<IReadOnlyList<Post>> GetPostsAsync();

    /// <summary>Returns a single post by Id, or null if it doesn't exist.</summary>
    Task<Post?> GetPostAsync(string id);

    /// <summary>
    ///     Creates a new post from a <see cref="CreatePostDto" /> and fills in the author
    ///     and the publish/modified timestamps.
    /// </summary>
    /// <param name="createPostDto">Input data (title, content) from the user.</param>
    /// <param name="authorId">Id of the logged-in user creating the post.</param>
    /// <param name="author">Display name of the author.</param>
    Task<Post> AddPostAsync(CreatePostDto createPostDto, string authorId, string author);

    /// <summary>Deletes a post by Id. Returns true if it was deleted.</summary>
    Task<bool> DeletePostAsync(string id);

    /// <summary>Updates the title and content of an existing post and refreshes the modified date. Returns true on success.</summary>
    Task<bool> EditPostAsync(string id, EditPostDto editPostDto);

    /// <summary>Bulk-creates multiple posts at once with the same author.</summary>
    Task<IReadOnlyList<Post>> AddBulkPostAsync(List<CreatePostDto> createPostDtoes, string authorId, string author);

    /// <summary>
    ///     Searches posts by a case-insensitive match against their title or description.
    /// </summary>
    /// <param name="query">The text to search for.</param>
    /// <returns>Matching posts, ordered from the most recently published.</returns>
    Task<IReadOnlyList<Post>> SearchAsync(string query);
}