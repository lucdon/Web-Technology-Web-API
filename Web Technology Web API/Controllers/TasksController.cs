using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

public struct TaskInfo {
    public int id { get; set; }
    public string title { get; set; }
    public string description { get; set; }
    public long startDate { get; set; }
    public long endDate { get; set; }
}

[Produces("application/json")]
[Route("[controller]/[action]")]
public class TasksController : Controller {
    private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly IAuthenticationService authentication;
    private readonly ILogger<AuthController> logger;
    private readonly IMongoDbService database;

    public TasksController(IAuthenticationService authentication, IMongoDbService database,
        ILogger<AuthController> logger) {
        this.authentication = authentication;
        this.database = database;
        this.logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> UpdateTask(string id, string token, [FromBody] TaskInfo info) {
        logger.LogInformation("update task attempt: " + id + " for task: " + info.id);

        AuthenticationResult result = await authentication.IsLoggedIn(id, token);
        
        if (result != AuthenticationResult.Succes) {
            return Result(result); 
        }

        UserModel model = await database.GetUserCollection().GetUserInfo(ObjectId.Parse(id));
        DateTime startDate = DateTimeOffset.FromUnixTimeSeconds(info.startDate).LocalDateTime;
        DateTime endDate = DateTimeOffset.FromUnixTimeSeconds(info.endDate).LocalDateTime;

        int pos = -1;
        for (int i = 0; i < model.tasks.Length; i++) {
            if (model.tasks[i].id == info.id) {
                pos = i;
                break;
            }
        }

        if (pos == -1) {
            return NotFound("task with id: " + info.id + " does not exist");
        }

        model.tasks[pos].title = info.title;
        model.tasks[pos].description = info.description;
        model.tasks[pos].startDate = startDate;
        model.tasks[pos].endDate = endDate;

        var builder = Builders<UserModel>.Update;
        var update = builder.Set(old => old.tasks, model.tasks);
        UpdateOptions options = new UpdateOptions {IsUpsert = false};
        await database.GetUserCollection().UpdateUser(ObjectId.Parse(id), update, options);
        return Accepted();
    }

    [HttpPut]
    public async Task<IActionResult> CreateTask(string id, string token, [FromBody] TaskInfo info) {
        logger.LogInformation("create task attempt: " + id);
        logger.LogInformation(info.title);
        AuthenticationResult result = await authentication.IsLoggedIn(id, token);
        if (result != AuthenticationResult.Succes) {
            return Result(result);
        }

        UserModel model = await database.GetUserCollection().GetUserInfo(ObjectId.Parse(id));
        DateTime startDate = DateTimeOffset.FromUnixTimeSeconds(info.startDate).LocalDateTime;
        DateTime endDate = DateTimeOffset.FromUnixTimeSeconds(info.endDate).LocalDateTime;
        TaskModel task = new TaskModel {
            id = model.tasks?.Length ?? 0,
            title = info.title,
            description = info.description,
            startDate = startDate,
            endDate = endDate
        };

        TaskModel[] tasks;
        if (model.tasks == null) {
            tasks = new[] {task};
        } else {
            tasks = new TaskModel[model.tasks.Length + 1];
            for (int i = 0; i < model.tasks.Length; i++) {
                tasks[i] = model.tasks[i];
            }
            tasks[tasks.Length - 1] = task;
        }

        var builder = Builders<UserModel>.Update;
        var update = builder.Set(old => old.tasks, tasks);
        UpdateOptions options = new UpdateOptions {IsUpsert = false};
        await database.GetUserCollection().UpdateUser(ObjectId.Parse(id), update, options);
        return Accepted();
    }

    [HttpDelete("{taskid:int}")]
    public async Task<IActionResult> DeleteTask(string id, string token, int taskid) {
        logger.LogInformation("delete task attempt: " + id + " for task: " + taskid);
        AuthenticationResult result = await authentication.IsLoggedIn(id, token);
        if (result != AuthenticationResult.Succes) {
            return Result(result);
        }

        UserModel model = await database.GetUserCollection().GetUserInfo(ObjectId.Parse(id));
        if (model.tasks == null) {
            return NotFound("task with id: " + taskid + " does not exist");
        }
        int pos = -1;
        for (int i = 0; i < model.tasks.Length; i++) {
            if (model.tasks[i].id == taskid) {
                pos = i;
                break;
            }
        }

        if (pos == -1) {
            return NotFound("task with id: " + taskid + " does not exist");
        }

        TaskModel[] tasks = new TaskModel[model.tasks.Length - 1];
        int j = 0;
        for (int i = 0; i < model.tasks.Length; i++) {
            if (i == pos) {
                continue;
            }
            model.tasks[i].id = j;
            tasks[j] = model.tasks[i];
            j++;
        }

        var builder = Builders<UserModel>.Update;
        var update = builder.Set(old => old.tasks, tasks);
        UpdateOptions options = new UpdateOptions {IsUpsert = false};
        await database.GetUserCollection().UpdateUser(ObjectId.Parse(id), update, options);
        return Accepted();
    }

    [HttpGet]
    public async Task<IActionResult> Get(string id, string token) {
        logger.LogInformation("get task attempt: " + id);
        AuthenticationResult result = await authentication.IsLoggedIn(id, token);
        if (result != AuthenticationResult.Succes) {
            return Result(result);
        }

        UserModel model = await database.GetUserCollection().GetUserInfo(ObjectId.Parse(id));
        List<TaskInfo> tasks = new List<TaskInfo>();
        foreach (TaskModel task in model.tasks) {
            tasks.Add(new TaskInfo {
                id = task.id,
                title = task.title,
                description = task.description,
                startDate = (long) task.startDate.Subtract(epoch).TotalMilliseconds,
                endDate = (long) task.endDate.Subtract(epoch).TotalMilliseconds
            });
        }
        return Json(tasks.ToArray());
    }

    [HttpGet("{taskid:int}")]
    public async Task<IActionResult> Get(string id, string token, int taskid) {
        logger.LogInformation("get specific task attempt: " + id + " for task: " + taskid);
        AuthenticationResult result = await authentication.IsLoggedIn(id, token);
        if (result != AuthenticationResult.Succes) {
            return Result(result);
        }

        UserModel model = await database.GetUserCollection().GetUserInfo(ObjectId.Parse(id));
        TaskInfo info = new TaskInfo();
        foreach (TaskModel task in model.tasks) {
            if (task.id == taskid) {
                info = new TaskInfo {
                    id = task.id,
                    title = task.title,
                    description = task.description,
                    startDate = (long) task.startDate.Subtract(epoch).TotalSeconds,
                    endDate = (long) task.endDate.Subtract(epoch).TotalSeconds
                };
                break;
            }
        }
        return Json(info);
    }

    private IActionResult Result(AuthenticationResult result) {
        switch (result) {
            case AuthenticationResult.Succes: return Accepted();
            case AuthenticationResult.WrongPassword:
                return BadRequest("wrong password");
            case AuthenticationResult.UserDoesNotExist:
                return BadRequest("user does not exist");
            case AuthenticationResult.UserAlreadyExists:
                return BadRequest("user already exists");
            case AuthenticationResult.NotLoggedIn:
                return BadRequest("user is not logged in");
            case AuthenticationResult.InvalidToken:
                return BadRequest("the token is invalid");
            case AuthenticationResult.UserAlreadyLoggedIn:
                return BadRequest("user is already logged in");
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}