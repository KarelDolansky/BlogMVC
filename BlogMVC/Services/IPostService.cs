using BlogMVC.Models;

namespace BlogMVC.Services;

public interface IPostService
{
    Task<IReadOnlyList<Post>> GetPostsAsync();
    Task<Post?> GetPostAsync(string id);
    Task<Post> AddPostAsync(CreatePostDto createPostDto);
    Task<bool> DeletePostAsync(string id);
    Task<bool> EditPostAsync(string id, EditPostDto editPostDto);

    Task<IReadOnlyList<Post>> AddBulkPostAsync(List<CreatePostDto> createPostDtoes);
}