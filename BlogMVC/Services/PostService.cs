using BlogMVC.Infrastructure.Interfaces;
using BlogMVC.Models;

namespace BlogMVC.Services;

/// <summary>
/// Default implementation of <see cref="IPostService"/>. Uses <see cref="IDateTimeProvider"/>
/// for a testable timestamp and <see cref="IPostRepository"/> for persistence in MongoDB.
/// </summary>
public class PostService(IDateTimeProvider dateTimeProvider, IPostRepository postRepository) : IPostService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Post>> GetPostsAsync()
    {
        return await postRepository.FindAllAsync();
    }

    /// <inheritdoc />
    public async Task<Post?> GetPostAsync(string id)
    {
        return await postRepository.FindAsync(id);
    }

    /// <inheritdoc />
    public async Task<Post> AddPostAsync(CreatePostDto createPostDto, string authorId, string author)
    {
        // Publish date and modified date are set to the same instant on creation.
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

    /// <inheritdoc />
    public async Task<IReadOnlyList<Post>> AddBulkPostAsync(List<CreatePostDto> createPostDtoes, string authorId,
        string author)
    {
        // Each post gets its own timestamp (Now is called inside the loop); all share the same author.
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

    /// <inheritdoc />
    public async Task<bool> DeletePostAsync(string id)
    {
        return await postRepository.DeleteOneAsync(id);
    }

    /// <inheritdoc />
    public async Task<bool> EditPostAsync(string id, EditPostDto editPostDto)
    {
        // Load the existing document first so unchanged fields (Author, PublishDate...) are preserved.
        var post = await postRepository.FindAsync(id);
        if (post == null) return false;
        post.Title = editPostDto.Title;
        post.Content = editPostDto.Content;
        post.ModifiedDate = dateTimeProvider.Now;
        return await postRepository.ReplaceOneAsync(id, post);
    }
}