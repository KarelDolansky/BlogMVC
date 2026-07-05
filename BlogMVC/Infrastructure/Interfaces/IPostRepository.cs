using BlogMVC.Models;

namespace BlogMVC.Infrastructure.Interfaces;

public interface IPostRepository
{
    Task<Post> InsertOneAsync(Post post);
    Task<IReadOnlyList<Post>> InsertManyAsync(IReadOnlyList<Post> posts);
    Task<bool> ReplaceOneAsync(string id, Post post);
    Task<bool> DeleteOneAsync(string id);
    Task<Post?> FindAsync(string id);
    Task<IReadOnlyList<Post>> FindAllAsync();
}