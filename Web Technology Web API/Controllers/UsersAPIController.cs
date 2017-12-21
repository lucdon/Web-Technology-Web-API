using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

public struct UserTemplate {
    public string id;
    public string username;
    public bool loginStatus;
}

[Produces("application/json")]
[Route("api/users")]
public class UsersAPIController : Controller {
    private readonly IMongoDbService database;

    public UsersAPIController(IMongoDbService database) {
        this.database = database;
    }

    [HttpGet]
    public async Task<IActionResult> Get() {
        IEnumerable<BsonDocument> users = await database.GetUserCollection().GetAllUsers();
        return Json(users.Select(doc => new UserTemplate {
            id = doc.GetValue("_id").AsObjectId.ToString(),
            username = doc.GetValue("username").AsString,
            loginStatus = doc.GetValue("loginStatus").AsBoolean
        }).ToArray());
    }

    [HttpGet("{name}")]
    public async Task<IActionResult> Get(string name) {
        ObjectId id = await database.GetUserCollection().GetByName(name);
        if (id != ObjectId.Empty) {
            UserModel model = await database.GetUserCollection().GetUserInfo(id);
            UserTemplate template = new UserTemplate {
                id = model.id.ToString(),
                username = model.username,
                loginStatus = model.loginStatus
            };

            return Json(template);
        }

        return BadRequest($"user with name: {name}, does not exist");
    }
}