using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BlogMVC.Models;

public class Post
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Author { get; set; }
    public DateTime? PublishDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}