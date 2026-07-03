using MongoDB.Bson;

namespace BlogMVC.Helpers;

public static class MongoDbHelper
{
    public static bool IsValidObjectId(string id)
    {
        return ObjectId.TryParse(id, out _);
    }
}