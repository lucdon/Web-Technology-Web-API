using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

[Produces("application/json")]
[Route("api/tasks")]
public class TasksAPIController : Controller {
    private readonly IMongoDbService database;

    public TasksAPIController(IMongoDbService database) {
        this.database = database;
    }

    /// <summary>
    /// Get all the tasks for a specific user
    /// </summary>
    [HttpGet("{name}")]
    public async Task<IActionResult> Get(string name) {
        ObjectId id = await database.GetUserCollection().GetByName(name);

        if (id == ObjectId.Empty) {
            return BadRequest($"user with name: {name}, does not exist!");
        }

        UserModel model = await database.GetUserCollection().GetUserInfo(id);
        return Json(model.tasks);
    }

    /// <summary>
    /// Get all the tasks for a specific user
    /// </summary>
    [HttpGet("{name}/{id}")]
    public async Task<IActionResult> Get(string name, int id) {
        ObjectId userId = await database.GetUserCollection().GetByName(name);

        if (userId == ObjectId.Empty) {
            return BadRequest($"user with name: {name}, does not exist!");
        }

        UserModel model = await database.GetUserCollection().GetUserInfo(userId);
        foreach (TaskModel task in model.tasks) {
            if (task.id == id) {
                return Json(task);
            }
        }

        return BadRequest($"user with name: {name} does not have task with id: {id}!");
    }
}