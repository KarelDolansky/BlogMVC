using BlogMVC.Dto;
using BlogMVC.Infrastructure.Interfaces;
using BlogMVC.Models;
using BlogMVC.Results;

namespace BlogMVC.Services;

/// <summary>
///     Default implementation of <see cref="IPostService" />. Uses <see cref="IDateTimeProvider" />
///     for a testable timestamp and <see cref="IPostRepository" /> for persistence in MongoDB.
/// </summary>
/// <param name="dateTimeProvider">Supplies the current time for publish/modified timestamps.</param>
/// <param name="postRepository">Persistence layer for posts in MongoDB.</param>
public class PostService(IDateTimeProvider dateTimeProvider, IPostRepository postRepository) : IPostService
{
    /// <summary>Delegates to <see cref="IPostRepository.FindAllAsync" />.</summary>
    /// <returns>All posts currently stored.</returns>
    public async Task<IReadOnlyList<Post>> GetPostsAsync()
    {
        return await postRepository.FindAllAsync();
    }

    /// <summary>Delegates to <see cref="IPostRepository.FindAsync" />.</summary>
    /// <param name="id">MongoDB ObjectId of the post, as a string.</param>
    /// <returns>The matching post, or null if none exists.</returns>
    public async Task<Post?> GetPostAsync(string id)
    {
        return await postRepository.FindAsync(id);
    }

    /// <summary>
    ///     Maps <paramref name="createPostDto" /> onto a new <see cref="Post" />, stamping PublishDate and
    ///     ModifiedDate with the same <see cref="IDateTimeProvider" />.Now value, then persists it via
    ///     <see cref="IPostRepository.InsertOneAsync" />.
    /// </summary>
    /// <param name="createPostDto">Input data (title, content) from the user.</param>
    /// <param name="authorId">Id of the logged-in user creating the post.</param>
    /// <param name="author">Display name of the author.</param>
    /// <returns>The newly persisted post, including its assigned Id.</returns>
    public async Task<Post> AddPostAsync(CreatePostDto createPostDto, string authorId, string author)
    {
        // Publish date and modified date are set to the same instant on creation.
        var date = dateTimeProvider.Now;
        var post = new Post
        {
            Title = createPostDto.Title,
            Description = createPostDto.Description,
            Content = createPostDto.Content,
            AuthorId = authorId,
            Author = author,
            PublishDate = date,
            ModifiedDate = date
        };
        return await postRepository.InsertOneAsync(post);
    }

    /// <summary>
    ///     Maps each entry of <paramref name="createPostDtoes" /> onto its own <see cref="Post" /> — each gets its
    ///     own timestamp from <see cref="IDateTimeProvider" />.Now, all share the same author — then inserts them
    ///     in one <see cref="IPostRepository.InsertManyAsync" /> call.
    /// </summary>
    /// <param name="createPostDtoes">Input data for each post to create.</param>
    /// <param name="authorId">Id of the logged-in user creating the posts.</param>
    /// <param name="author">Display name of the author.</param>
    /// <returns>The newly persisted posts, including their assigned Ids.</returns>
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
                Description = createPostDto.Description,
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

    /// <summary>Delegates to <see cref="IPostRepository.DeleteOneAsync" />.</summary>
    /// <param name="id">MongoDB ObjectId of the post, as a string.</param>
    /// <returns>True if a post with the given Id existed and was deleted; false otherwise.</returns>
    public async Task<bool> DeletePostAsync(string id)
    {
        return await postRepository.DeleteOneAsync(id);
    }

    /// <summary>
    ///     Loads the existing post via <see cref="IPostRepository.FindAsync" /> (so untouched fields like Author/
    ///     PublishDate are preserved), applies the new title/content/description, stamps ModifiedDate, increments
    ///     <see cref="Post.Version" />, and replaces it via <see cref="IPostRepository.ReplaceOneAsync" /> with
    ///     optimistic concurrency on <paramref name="expectedVersion" />.
    /// </summary>
    /// <param name="id">MongoDB ObjectId of the post, as a string.</param>
    /// <param name="editPostDto">The new title/description/content to apply.</param>
    /// <param name="expectedVersion">The <see cref="Post.Version" /> the caller last read, for optimistic concurrency.</param>
    /// <returns>
    ///     <see cref="PostUpdateResult.Success" /> if updated, <see cref="PostUpdateResult.NotFound" /> if no such
    ///     post exists, or <see cref="PostUpdateResult.Conflict" /> if the version no longer matches.
    /// </returns>
    public async Task<PostUpdateResult> EditPostAsync(string id, EditPostDto editPostDto, long expectedVersion)
    {
        // Load the existing document first so unchanged fields (Author, PublishDate...) are preserved.
        var post = await postRepository.FindAsync(id);
        if (post == null) return PostUpdateResult.NotFound;
        post.Title = editPostDto.Title;
        post.Content = editPostDto.Content;
        post.Description = editPostDto.Description;
        post.ModifiedDate = dateTimeProvider.Now;
        post.Version += 1;
        return await postRepository.ReplaceOneAsync(id, expectedVersion, post);
    }

    /// <summary>Delegates to <see cref="IPostRepository.SearchAsync" />.</summary>
    /// <param name="query">The text to search for.</param>
    /// <returns>Matching posts, ordered from the most recently published.</returns>
    public async Task<IReadOnlyList<Post>> SearchAsync(string query)
    {
        return await postRepository.SearchAsync(query);
    }
}