using BlogMVC.Infrastructure.Interfaces;
using BlogMVC.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BlogMVC.Infrastructure.Repositories;

public class PostRepository : IPostRepository
{
    private readonly IMongoCollection<Post> _posts;

    public PostRepository(IOptions<MongoDbSettings> settings, MongoClient client)
    {
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _posts = database.GetCollection<Post>(settings.Value.PostsCollectionName);
    }

    public async Task<Post> InsertOneAsync(Post post)
    {
        await _posts.InsertOneAsync(post);
        return post;
    }

    public async Task<IReadOnlyList<Post>> InsertManyAsync(IReadOnlyList<Post> posts)
    {
        await _posts.InsertManyAsync(posts);
        return posts;
    }

    public async Task<bool> ReplaceOneAsync(string id, Post post)
    {
        var result = await _posts.ReplaceOneAsync(p => p.Id == id, post);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteOneAsync(string id)
    {
        var result = await _posts.DeleteOneAsync(p => p.Id == id);
        return result.DeletedCount > 0;
    }

    public async Task<Post?> FindAsync(string id)
    {
        return await _posts.Find(p => p.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<Post>> FindAllAsync()
    {
        return await _posts.Find(_ => true).SortByDescending(x => x.PublishDate).ToListAsync();
    }
}