using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text;

public class AuthenticationService : IAuthenticationService {
    private readonly UserCollection users;

    public AuthenticationService(IMongoDbService database) {
        users = database.GetUserCollection();
    }

    public async Task<(AuthenticationResult result, string userId, string token)> Login(string username, string pwd) {
        ObjectId userId = await users.GetByName(username);
        UserModel user = await users.GetUserInfo(userId);

        if (userId == ObjectId.Empty || user == null) {
            return (AuthenticationResult.UserDoesNotExist, ObjectId.Empty.ToString(), default(string));
        }

        if (!VerifyHash(userId.ToString(), pwd, user.password)) {
            return (AuthenticationResult.WrongPassword, ObjectId.Empty.ToString(), default(string));
        }

        string token = Guid.NewGuid().ToString();
        var builder = Builders<UserModel>.Update;
        var update = builder.Set(old => old.autherizationToken, token).Set(old => old.loginStatus, true);
        UpdateOptions options = new UpdateOptions {IsUpsert = false};
        await users.UpdateUser(userId, update, options);
        return (AuthenticationResult.Succes, userId.ToString(), token);
    }

    public async Task<AuthenticationResult> Logout(string userId) {
        UserModel user = await users.GetUserInfo(ObjectId.Parse(userId));
        if (userId == ObjectId.Empty.ToString() || user == null) {
            return AuthenticationResult.UserDoesNotExist;
        }

        if (!user.loginStatus) {
            return AuthenticationResult.NotLoggedIn;
        }

        var builder = Builders<UserModel>.Update;
        var update = builder.Unset(old => old.autherizationToken).Set(old => old.loginStatus, false);
        UpdateOptions options = new UpdateOptions {IsUpsert = false};
        await users.UpdateUser(ObjectId.Parse(userId), update, options);

        return AuthenticationResult.Succes;
    }

    public async Task<AuthenticationResult> CreateAccount(string username, string pwd) {
        ObjectId userId = await users.GetByName(username);

        if (userId != ObjectId.Empty) {
            return AuthenticationResult.UserAlreadyExists;
        }

        ObjectId id = ObjectId.GenerateNewId();
        UserModel user = new UserModel {
            id = id,
            username = username,
            password = EncryptPassword(id.ToString(), pwd)
        };

        await users.InsertUser(user);
        return AuthenticationResult.Succes;
    }

    public async Task<AuthenticationResult> IsLoggedIn(string userId, string token) {
        UserModel user = await users.GetUserInfo(ObjectId.Parse(userId));
        if (userId == ObjectId.Empty.ToString() || user == null) {
            return AuthenticationResult.UserDoesNotExist;
        }

        if (!user.loginStatus) {
            return AuthenticationResult.NotLoggedIn;
        }

        return user.autherizationToken != token ? AuthenticationResult.InvalidToken : AuthenticationResult.Succes;
    }

    private static string EncryptPassword(string id, string password) {
        MD5 fastHash = MD5.Create();

        string salt = GetHash(fastHash, id);
        string pwd = GetHash(fastHash, password);

        return GetHash(fastHash, salt + pwd);
    }

    private static string GetHash(HashAlgorithm algo, string input) {
        byte[] data = algo.ComputeHash(Encoding.UTF8.GetBytes(input));

        StringBuilder sBuilder = new StringBuilder();

        foreach (byte b in data) {
            sBuilder.Append(b.ToString("x2"));
        }

        return sBuilder.ToString();
    }

    private static bool VerifyHash(string id, string password, string hash) {
        string hashOfInput = EncryptPassword(id, password);
        StringComparer comparer = StringComparer.OrdinalIgnoreCase;
        return comparer.Compare(hashOfInput, hash) == 0;
    }
}