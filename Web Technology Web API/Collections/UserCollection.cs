using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

public class UserCollection {
    private readonly IMongoCollection<UserModel> userCollection;

    public UserCollection(IMongoCollection<UserModel> userCollection) {
        this.userCollection = userCollection;

        //make sure username is unique
        userCollection.Indexes.CreateOne(Builders<UserModel>.IndexKeys.Ascending("username"),
            new CreateIndexOptions {Unique = true, Sparse = true});
    }

    /// <summary>
    /// get all users from the database
    /// </summary>
    public async Task<IEnumerable<BsonDocument>> GetAllUsers() {
        var projection =
            Builders<UserModel>.Projection.Include(user => user.username).Include(user => user.id)
                .Include(user => user.loginStatus);
        IAsyncCursor<BsonDocument> document =
            await userCollection.Find(new BsonDocument()).Project(projection).ToCursorAsync();
        return document.ToEnumerable();
    }

    /// <summary>
    /// insert a user into the database
    /// </summary>
    public async Task InsertUser(UserModel user) {
        await userCollection.InsertOneAsync(user);
    }

    /// <summary>
    /// insert multiple users into the database
    /// </summary>
    public async Task InsertUsers(IEnumerable<UserModel> users) {
        await userCollection.InsertManyAsync(users);
    }

    /// <summary>
    /// update a single user in the database
    /// </summary>
    public async Task UpdateUser(ObjectId id, UpdateDefinition<UserModel> update, UpdateOptions options) {
        await userCollection.UpdateOneAsync(user => user.id == id, update, options);
    }

    /// <summary>
    /// delete a user from the database
    /// </summary>
    public async Task DeleteUser(ObjectId id) {
        await userCollection.FindOneAndDeleteAsync(user => user.id == id);
    }

    /// <summary>
    /// get the information of a user
    /// </summary>
    public async Task<UserModel> GetUserInfo(ObjectId id) {
        return await userCollection.Find(user => user.id == id).FirstOrDefaultAsync();
    }

    /// <summary>
    /// get the user from its username
    /// </summary>
    public async Task<ObjectId> GetByName(string username) {
        var projection = Builders<UserModel>.Projection.Expression(user => user.id);
        return await userCollection.Find(user => user.username == username).Project(projection).FirstOrDefaultAsync();
    }
}