using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

public class UserModel {
    [BsonId]
    public ObjectId id { get; set; }

    [BsonIgnoreIfNull]
    public string autherizationToken { get; set; }

    [BsonRequired]
    public string username { get; set; }
    
    [BsonRequired]
    public string password { get; set; }

    public bool loginStatus { get; set; }
    
    public TaskModel[] tasks { get; set; }
}