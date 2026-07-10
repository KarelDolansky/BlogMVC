namespace BlogMVC.Models;

/// <summary>
/// Configuration model for the MongoDB connection, bound from the "MongoDb"
/// section in appsettings.json via IOptions&lt;MongoDbSettings&gt;.
/// </summary>
public class MongoDbSettings
{
    /// <summary>Connection string to the MongoDB server/cluster.</summary>
    public string ConnectionString { get; set; } = null!;

    /// <summary>Name of the database that stores the posts.</summary>
    public string DatabaseName { get; set; } = null!;

    /// <summary>Name of the collection containing <see cref="Post"/> documents.</summary>
    public string PostsCollectionName { get; set; } = null!;
}