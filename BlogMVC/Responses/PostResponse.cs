using BlogMVC.Models;

namespace BlogMVC.Responses;

/// <summary>Response body representing a single blog post, returned from the api/blog endpoints.</summary>
public class PostResponse
{
    /// <summary>Unique identifier of the post (MongoDB ObjectId as a string).</summary>
    public required string Id { get; init; }

    /// <summary>Title of the post.</summary>
    public required string Title { get; init; }

    /// <summary>Short description of the post.</summary>
    public required string Description { get; init; }

    /// <summary>Text content (body) of the post.</summary>
    public required string Content { get; init; }

    /// <summary>Id of the Identity user who authored the post.</summary>
    public required string AuthorId { get; init; }

    /// <summary>Display name of the post's author.</summary>
    public required string Author { get; init; }

    /// <summary>Date and time the post was first published.</summary>
    public DateTime PublishDate { get; init; }

    /// <summary>Date and time the post was last modified.</summary>
    public DateTime ModifiedDate { get; init; }

    /// <summary>Maps a persisted <see cref="Post" /> (which always has an Id) to its response representation.</summary>
    public static PostResponse FromPost(Post post)
    {
        return new PostResponse
        {
            Id = post.Id!,
            Title = post.Title,
            Description = post.Description,
            Content = post.Content,
            AuthorId = post.AuthorId,
            Author = post.Author,
            PublishDate = post.PublishDate,
            ModifiedDate = post.ModifiedDate
        };
    }
}