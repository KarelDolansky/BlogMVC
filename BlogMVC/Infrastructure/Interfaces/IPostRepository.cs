using BlogMVC.Models;
using BlogMVC.Results;

namespace BlogMVC.Infrastructure.Interfaces;

/// <summary>
/// Data-access abstraction for working with posts stored in MongoDB.
/// Decouples the service layer (<see cref="BlogMVC.Services.IPostService"/>) from the concrete storage implementation.
/// </summary>
public interface IPostRepository
{
    /// <summary>Inserts a new post into the collection and returns it (including the generated Id).</summary>
    Task<Post> InsertOneAsync(Post post);

    /// <summary>Inserts multiple posts at once and returns them.</summary>
    Task<IReadOnlyList<Post>> InsertManyAsync(IReadOnlyList<Post> posts);

    /// <summary>Replaces a document by Id, only if its version still matches <paramref name="expectedVersion" />.</summary>
    Task<PostUpdateResult> ReplaceOneAsync(string id, long expectedVersion, Post post);

    /// <summary>Deletes a document by Id. Returns true if a document was actually deleted.</summary>
    Task<bool> DeleteOneAsync(string id);

    /// <summary>Finds a single post by Id, or null if it doesn't exist.</summary>
    Task<Post?> FindAsync(string id);

    /// <summary>Returns all posts, ordered from the most recently published.</summary>
    Task<IReadOnlyList<Post>> FindAllAsync();

    /// <summary>
    ///     Searches posts by a case-insensitive match against their title or description.
    /// </summary>
    /// <param name="query">The text to search for.</param>
    /// <returns>Matching posts, ordered from the most recently published.</returns>
    Task<IReadOnlyList<Post>> SearchAsync(string query);
}