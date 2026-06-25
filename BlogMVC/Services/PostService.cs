using BlogMVC.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BlogMVC.Services;

public interface IPostService
{
    Task<List<Post>> GetPostsAsync();
    Task<Post?> GetPostAsync(string id);
    Task<Post> AddPostAsync(Post post);
    Task<bool> DeletePostAsync(string id);
    Task<bool> EditPostAsync(string id, Post post);
    Task AddPostFromMarkDownAsync();
}

public class PostService : IPostService
{
    private readonly IPostMarkdownReaderService _postMarkdownReaderService;
    private readonly IMongoCollection<Post> _posts;

    public PostService(IOptions<MongoDbSettings> settings, IPostMarkdownReaderService markdownReaderService)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _posts = database.GetCollection<Post>(settings.Value.PostsCollectionName);
        _postMarkdownReaderService = markdownReaderService;
    }

    public async Task<List<Post>> GetPostsAsync()
    {
        return await _posts.Find(_ => true).ToListAsync();
    }

    public async Task<Post?> GetPostAsync(string id)
    {
        return await _posts.Find(p => p.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Post> AddPostAsync(Post post)
    {
        post.PublishDate ??= DateTime.UtcNow;
        await _posts.InsertOneAsync(post);
        return post;
    }

    public async Task<bool> DeletePostAsync(string id)
    {
        var result = await _posts.DeleteOneAsync(p => p.Id == id);
        return result.DeletedCount > 0;
    }

    public async Task<bool> EditPostAsync(string id, Post post)
    {
        post.ModifiedDate = DateTime.UtcNow;
        var result = await _posts.ReplaceOneAsync(p => p.Id == id, post);
        return result.ModifiedCount > 0;
    }

    public async Task AddPostFromMarkDownAsync()
    {
        var posts = _postMarkdownReaderService.GetAllPostsFromMarkdown();
        foreach (var post in posts) await AddPostAsync(post);
    }
}