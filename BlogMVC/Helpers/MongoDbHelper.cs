using MongoDB.Bson;

namespace BlogMVC.Helpers;

/// <summary>
///     Helper static methods for working with values specific to MongoDB.
/// </summary>
public static class MongoDbHelper
{
    /// <summary>
    ///     Checks whether the given string is a valid MongoDB ObjectId (24 hex characters).
    ///     Used in controllers before querying the database to avoid an exception
    ///     and to return a proper response (400/404) for invalid ids right away.
    /// </summary>
    /// <param name="id">Id to validate.</param>
    /// <returns>True if it's a valid ObjectId; otherwise false.</returns>
    public static bool IsValidObjectId(string id)
    {
        return ObjectId.TryParse(id, out _);
    }
}