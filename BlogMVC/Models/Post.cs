using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BlogMVC.Models;

/// <summary>
///     Domain model representing a single blog post stored in MongoDB.
///     Used as the read/write entity via <see cref="BlogMVC.Infrastructure.Interfaces.IPostRepository" />.
/// </summary>
public class Post
{
    /// <summary>
    ///     Unique identifier of the post (MongoDB ObjectId stored as a string).
    ///     Null until the document is persisted (MongoDB assigns the id itself).
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>Title of the post.</summary>
    public required string Title { get; set; }

    /// <summary>Short description of the post.</summary>
    public required string Description { get; set; }

    /// <summary>Text content (body) of the post.</summary>
    public required string Content { get; set; }

    /// <summary>Id of the Identity user who authored the post. Used to verify ownership on edit/delete.</summary>
    public required string AuthorId { get; set; }

    /// <summary>Display name of the post's author.</summary>
    public required string Author { get; set; }

    /// <summary>Date and time the post was first published.</summary>
    public DateTime PublishDate { get; set; }

    /// <summary>Date and time the post was last modified.</summary>
    public DateTime ModifiedDate { get; set; }
}