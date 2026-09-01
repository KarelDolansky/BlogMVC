using System.Text.RegularExpressions;
using BlogMVC.Infrastructure.Interfaces;
using BlogMVC.Models;
using BlogMVC.Results;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BlogMVC.Infrastructure.Repositories;

/// <summary>
///     Implementation of <see cref="IPostRepository"/> on top of the MongoDB driver.
///     Encapsulates direct access to the posts collection (IMongoCollection&lt;Post&gt;).
/// </summary>
public class PostRepository : IPostRepository
{
    /// <summary>Reference to the MongoDB collection holding post documents.</summary>
    private readonly IMongoCollection<Post> _posts;

    /// <summary>
    ///     Creates the repository and connects to the collection defined in <see cref="Models.MongoDbSettings"/>.
    /// </summary>
    /// <param name="settings">Database and collection configuration loaded from appsettings.json.</param>
    /// <param name="client">Shared MongoDB client registered in the DI container.</param>
    public PostRepository(IOptions<MongoDbSettings> settings, MongoClient client)
    {
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _posts = database.GetCollection<Post>(settings.Value.PostsCollectionName);
    }

    /// <summary>
    ///     Inserts <paramref name="post" /> via the MongoDB driver's <c>InsertOneAsync</c>; the driver populates
    ///     <see cref="Post.Id" /> with the generated ObjectId on the same instance.
    /// </summary>
    /// <param name="post">The post to insert.</param>
    /// <returns>The same post instance, with its generated Id populated.</returns>
    public async Task<Post> InsertOneAsync(Post post)
    {
        await _posts.InsertOneAsync(post);
        return post;
    }

    /// <summary>Inserts all <paramref name="posts" /> in a single <c>InsertManyAsync</c> call.</summary>
    /// <param name="posts">The posts to insert.</param>
    /// <returns>The same post instances, each with its generated Id populated.</returns>
    public async Task<IReadOnlyList<Post>> InsertManyAsync(IReadOnlyList<Post> posts)
    {
        await _posts.InsertManyAsync(posts);
        return posts;
    }

    /// <summary>
    ///     Replaces the document matching both <paramref name="id" /> and <paramref name="expectedVersion" /> in one
    ///     atomic MongoDB filter. If nothing was modified, re-checks by Id alone to distinguish a version conflict
    ///     from the document not existing at all.
    /// </summary>
    /// <param name="id">Id of the post to replace.</param>
    /// <param name="expectedVersion">Version the caller last read; the replace only applies if it still matches.</param>
    /// <param name="post">The replacement document.</param>
    /// <returns>
    ///     <see cref="PostUpdateResult.Success" />, <see cref="PostUpdateResult.Conflict" /> if the version no longer
    ///     matched, or <see cref="PostUpdateResult.NotFound" /> if no document with that Id exists.
    /// </returns>
    public async Task<PostUpdateResult> ReplaceOneAsync(string id, long expectedVersion, Post post)
    {
        var result = await _posts.ReplaceOneAsync(p => p.Id == id && p.Version == expectedVersion, post);
        if (result.ModifiedCount > 0) return PostUpdateResult.Success;

        var exists = await _posts.Find(p => p.Id == id).AnyAsync();
        return exists ? PostUpdateResult.Conflict : PostUpdateResult.NotFound;
    }

    /// <summary>Deletes the document matching <paramref name="id" /> via <c>DeleteOneAsync</c>.</summary>
    /// <param name="id">Id of the post to delete.</param>
    /// <returns>True if a document was deleted (<c>DeletedCount &gt; 0</c>); false if none matched.</returns>
    public async Task<bool> DeleteOneAsync(string id)
    {
        var result = await _posts.DeleteOneAsync(p => p.Id == id);
        return result.DeletedCount > 0;
    }

    /// <summary>Looks up the document matching <paramref name="id" /> via <c>Find(...).FirstOrDefaultAsync()</c>.</summary>
    /// <param name="id">Id of the post to find.</param>
    /// <returns>The matching post, or null if none exists.</returns>
    public async Task<Post?> FindAsync(string id)
    {
        return await _posts.Find(p => p.Id == id).FirstOrDefaultAsync();
    }

    /// <summary>Returns every document in the collection, sorted descending by <see cref="Post.PublishDate" />.</summary>
    /// <returns>All posts, most recently published first.</returns>
    public async Task<IReadOnlyList<Post>> FindAllAsync()
    {
        // Ordered from the newest post so the latest content shows up first in the listing.
        return await _posts.Find(_ => true).SortByDescending(x => x.PublishDate).ToListAsync();
    }

    /// <summary>
    ///     Escapes <paramref name="query" /> and matches it as a case-insensitive regex against Title or Description
    ///     via a MongoDB <c>$or</c> filter, sorted descending by PublishDate.
    /// </summary>
    /// <param name="query">The text to search for.</param>
    /// <returns>Matching posts, most recently published first.</returns>
    public async Task<IReadOnlyList<Post>> SearchAsync(string query)
    {
        var escapedQuery = Regex.Escape(query);
        var filter = Builders<Post>.Filter.Or(
            Builders<Post>.Filter.Regex(p => p.Title, new BsonRegularExpression(escapedQuery, "i")),
            Builders<Post>.Filter.Regex(p => p.Description, new BsonRegularExpression(escapedQuery, "i"))
        );
        return await _posts.Find(filter).SortByDescending(x => x.PublishDate).ToListAsync();
    }
}