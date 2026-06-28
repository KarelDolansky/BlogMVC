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

    Task<List<Post>> AddBulkPostAsync(List<Post> posts);
    //Task AddPostFromMarkDownAsync();
}

public class PostService : IPostService
{
    //private readonly IPostMarkdownReaderService _postMarkdownReaderService;
    private readonly IMongoCollection<Post> _posts;

    public PostService(IOptions<MongoDbSettings> settings, MongoClient client)
    {
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _posts = database.GetCollection<Post>(settings.Value.PostsCollectionName);
    }

    public async Task<List<Post>> GetPostsAsync()
    {
        return await _posts.Find(_ => true).SortByDescending(x => x.PublishDate).ToListAsync();
    }

    public async Task<Post?> GetPostAsync(string id)
    {
        return await _posts.Find(p => p.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Post> AddPostAsync(Post post)
    {
        post.Id = null;
        post.PublishDate ??= DateTime.UtcNow;
        post.ModifiedDate = post.PublishDate;
        await _posts.InsertOneAsync(post);
        return post;
    }

    public async Task<List<Post>> AddBulkPostAsync(List<Post> posts)
    {
        foreach (var post in posts)
        {
            post.Id = null;
            post.PublishDate ??= DateTime.UtcNow;
            post.ModifiedDate = post.PublishDate;
        }

        await _posts.InsertManyAsync(posts);
        return posts;
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


    //public async Task AddPostFromMarkDownAsync()
    //{
    //    var posts = _postMarkdownReaderService.GetAllPostsFromMarkdown();
    //    foreach (var post in posts) await AddPostAsync(post);
    //}
}