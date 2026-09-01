using BlogMVC.Dto;
using BlogMVC.Models;
using BlogMVC.Results;

namespace BlogMVC.Services;

/// <summary>
///     Application (business) layer for working with blog posts.
///     Sits between controllers and the data layer (<see cref="BlogMVC.Infrastructure.Interfaces.IPostRepository" />) –
///     handles mapping DTOs to entities and filling in derived values (author, publish/modified timestamps).
/// </summary>
public interface IPostService
{
    /// <summary>Returns all posts, ordered from the newest.</summary>
    /// <returns>All posts currently stored.</returns>
    Task<IReadOnlyList<Post>> GetPostsAsync();

    /// <summary>Returns a single post by Id, or null if it doesn't exist.</summary>
    /// <param name="id">Id of the post.</param>
    /// <returns>The matching post, or null if none exists.</returns>
    Task<Post?> GetPostAsync(string id);

    /// <summary>
    ///     Creates a new post from a <see cref="CreatePostDto" /> and fills in the author
    ///     and the publish/modified timestamps.
    /// </summary>
    /// <param name="createPostDto">Input data (title, content) from the user.</param>
    /// <param name="authorId">Id of the logged-in user creating the post.</param>
    /// <param name="author">Display name of the author.</param>
    /// <returns>The newly persisted post, including its assigned Id.</returns>
    Task<Post> AddPostAsync(CreatePostDto createPostDto, string authorId, string author);

    /// <summary>Deletes a post by Id. Returns true if it was deleted.</summary>
    /// <param name="id">Id of the post.</param>
    /// <returns>True if a post with the given Id existed and was deleted; false otherwise.</returns>
    Task<bool> DeletePostAsync(string id);

    /// <summary>Updates a post's title/content if <paramref name="expectedVersion" /> still matches its current version.</summary>
    /// <param name="id">Id of the post.</param>
    /// <param name="editPostDto">The new title/description/content to apply.</param>
    /// <param name="expectedVersion">The <see cref="Post.Version" /> the caller last read, for optimistic concurrency.</param>
    /// <returns>
    ///     <see cref="PostUpdateResult.Success" /> if updated, <see cref="PostUpdateResult.NotFound" /> if no such
    ///     post exists, or <see cref="PostUpdateResult.Conflict" /> if the version no longer matches.
    /// </returns>
    Task<PostUpdateResult> EditPostAsync(string id, EditPostDto editPostDto, long expectedVersion);

    /// <summary>Bulk-creates multiple posts at once with the same author.</summary>
    /// <param name="createPostDtoes">Input data for each post to create.</param>
    /// <param name="authorId">Id of the logged-in user creating the posts.</param>
    /// <param name="author">Display name of the author.</param>
    /// <returns>The newly persisted posts, including their assigned Ids.</returns>
    Task<IReadOnlyList<Post>> AddBulkPostAsync(List<CreatePostDto> createPostDtoes, string authorId, string author);

    /// <summary>
    ///     Searches posts by a case-insensitive match against their title or description.
    /// </summary>
    /// <param name="query">The text to search for.</param>
    /// <returns>Matching posts, ordered from the most recently published.</returns>
    Task<IReadOnlyList<Post>> SearchAsync(string query);
}