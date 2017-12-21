using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using System;

public class MongoDbService : IMongoDbService {
    private readonly UserCollection users;

    public MongoDbService(MongoClientSettings settings, string databaseName) {
        ConventionPack conventionPack = new ConventionPack {new CamelCaseElementNameConvention()};
        ConventionRegistry.Register("camelCase", conventionPack, t => true);

        MongoClient client = new MongoClient(settings);
        IMongoDatabase database = client.GetDatabase(databaseName);
        users = new UserCollection(database.GetCollection<UserModel>("users"));
    }

    public UserCollection GetUserCollection() {
        return users;
    }
}