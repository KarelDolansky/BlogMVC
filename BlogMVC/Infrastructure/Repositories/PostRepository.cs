using BlogMVC.Infrastructure.Interfaces;
using BlogMVC.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BlogMVC.Infrastructure.Repositories;

/// <summary>
/// Implementation of <see cref="IPostRepository"/> on top of the MongoDB driver.
/// Encapsulates direct access to the posts collection (IMongoCollection&lt;Post&gt;).
/// </summary>
public class PostRepository : IPostRepository
{
    /// <summary>Reference to the MongoDB collection holding post documents.</summary>
    private readonly IMongoCollection<Post> _posts;

    /// <summary>
    /// Creates the repository and connects to the collection defined in <see cref="Models.MongoDbSettings"/>.
    /// </summary>
    /// <param name="settings">Database and collection configuration loaded from appsettings.json.</param>
    /// <param name="client">Shared MongoDB client registered in the DI container.</param>
    public PostRepository(IOptions<MongoDbSettings> settings, MongoClient client)
    {
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _posts = database.GetCollection<Post>(settings.Value.PostsCollectionName);
    }

    /// <inheritdoc />
    public async Task<Post> InsertOneAsync(Post post)
    {
        await _posts.InsertOneAsync(post);
        return post;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Post>> InsertManyAsync(IReadOnlyList<Post> posts)
    {
        await _posts.InsertManyAsync(posts);
        return posts;
    }

    /// <inheritdoc />
    public async Task<bool> ReplaceOneAsync(string id, Post post)
    {
        // Replaces the whole document by Id; ModifiedCount > 0 means the document actually changed.
        var result = await _posts.ReplaceOneAsync(p => p.Id == id, post);
        return result.ModifiedCount > 0;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteOneAsync(string id)
    {
        var result = await _posts.DeleteOneAsync(p => p.Id == id);
        return result.DeletedCount > 0;
    }

    /// <inheritdoc />
    public async Task<Post?> FindAsync(string id)
    {
        return await _posts.Find(p => p.Id == id).FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Post>> FindAllAsync()
    {
        // Ordered from the newest post so the latest content shows up first in the listing.
        return await _posts.Find(_ => true).SortByDescending(x => x.PublishDate).ToListAsync();
    }
}