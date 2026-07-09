using BlogMVC.Infrastructure.Interfaces;
using BlogMVC.Models;

namespace BlogMVC.Services;

public class PostService(IDateTimeProvider dateTimeProvider, IPostRepository postRepository) : IPostService
{
    public async Task<IReadOnlyList<Post>> GetPostsAsync()
    {
        return await postRepository.FindAllAsync();
    }

    public async Task<Post?> GetPostAsync(string id)
    {
        return await postRepository.FindAsync(id);
    }

    public async Task<Post> AddPostAsync(CreatePostDto createPostDto, string authorId, string author)
    {
        var date = dateTimeProvider.Now;
        var post = new Post
        {
            Title = createPostDto.Title,
            Content = createPostDto.Content,
            AuthorId = authorId,
            Author = author,
            PublishDate = date,
            ModifiedDate = date
        };
        return await postRepository.InsertOneAsync(post);
    }

    public async Task<IReadOnlyList<Post>> AddBulkPostAsync(List<CreatePostDto> createPostDtoes, string authorId,
        string author)
    {
        var posts = new List<Post>();
        foreach (var createPostDto in createPostDtoes)
        {
            var date = dateTimeProvider.Now;
            var post = new Post
            {
                Title = createPostDto.Title,
                Content = createPostDto.Content,
                AuthorId = authorId,
                Author = author,
                PublishDate = date,
                ModifiedDate = date
            };
            posts.Add(post);
        }

        return await postRepository.InsertManyAsync(posts);
    }

    public async Task<bool> DeletePostAsync(string id)
    {
        return await postRepository.DeleteOneAsync(id);
    }

    public async Task<bool> EditPostAsync(string id, EditPostDto editPostDto)
    {
        var post = await postRepository.FindAsync(id);
        if (post == null) return false;
        post.Title = editPostDto.Title;
        post.Content = editPostDto.Content;
        post.ModifiedDate = dateTimeProvider.Now;
        return await postRepository.ReplaceOneAsync(id, post);
    }
}