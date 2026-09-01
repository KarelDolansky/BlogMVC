using BlogMVC.Models;
using BlogMVC.Results;

namespace BlogMVC.Infrastructure.Interfaces;

/// <summary>
///     Data-access abstraction for posts. Decouples the service layer
///     (<see cref="BlogMVC.Services.IPostService"/>) from the concrete storage implementation.
/// </summary>
public interface IPostRepository
{
    /// <summary>Inserts a new post and returns it, including the generated Id.</summary>
    /// <param name="post">The post to insert.</param>
    /// <returns>The inserted post, including its generated Id.</returns>
    Task<Post> InsertOneAsync(Post post);

    /// <summary>Inserts multiple posts at once and returns them.</summary>
    /// <param name="posts">The posts to insert.</param>
    /// <returns>The inserted posts.</returns>
    Task<IReadOnlyList<Post>> InsertManyAsync(IReadOnlyList<Post> posts);

    /// <summary>Replaces a post by Id, only if its version still matches <paramref name="expectedVersion" />.</summary>
    /// <param name="id">Id of the post to replace.</param>
    /// <param name="expectedVersion">Version the caller last read; the replace only applies if it still matches.</param>
    /// <param name="post">The replacement post.</param>
    /// <returns>
    ///     <see cref="PostUpdateResult.Success" />, <see cref="PostUpdateResult.Conflict" /> if the version no longer
    ///     matches, or <see cref="PostUpdateResult.NotFound" />.
    /// </returns>
    Task<PostUpdateResult> ReplaceOneAsync(string id, long expectedVersion, Post post);

    /// <summary>Deletes a post by Id.</summary>
    /// <param name="id">Id of the post to delete.</param>
    /// <returns>True if a post was deleted; false if none matched.</returns>
    Task<bool> DeleteOneAsync(string id);

    /// <summary>Finds a single post by Id, or null if it doesn't exist.</summary>
    /// <param name="id">Id of the post to find.</param>
    /// <returns>The matching post, or null if none exists.</returns>
    Task<Post?> FindAsync(string id);

    /// <summary>Returns all posts, ordered from the most recently published.</summary>
    /// <returns>All posts, most recently published first.</returns>
    Task<IReadOnlyList<Post>> FindAllAsync();

    /// <summary>
    ///     Searches posts by a case-insensitive match against their title or description.
    /// </summary>
    /// <param name="query">The text to search for.</param>
    /// <returns>Matching posts, ordered from the most recently published.</returns>
    Task<IReadOnlyList<Post>> SearchAsync(string query);
}