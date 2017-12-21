using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

// struct for passing user information
public struct UserInfo {
    public string username { get; set; }
    public string password { get; set; }
}

// struct for passing creation information
public struct CreateUserInfo {
    public string username { get; set; }
    public string password { get; set; }
}

[Produces("application/json")]
[Route("[controller]/[action]")]
public class AuthController : Controller {
    private readonly IAuthenticationService authentication;
    private readonly ILogger<AuthController> logger;

    public AuthController(IAuthenticationService authentication, ILogger<AuthController> logger) {
        this.authentication = authentication;
        this.logger = logger;
    }

    /// <summary>
    /// Perform a logout for a specified person
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Logout([FromBody] string id) {
        logger.LogInformation("Logout Attempt: " + id);
        AuthenticationResult result = await authentication.Logout(id);
        return Result(result);
    }

    /// <summary>
    /// Perform a login for a specified person
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] UserInfo info) {
        logger.LogInformation("Login Attempt: " + info.username);
        (AuthenticationResult result, string userId, string token) =
            await authentication.Login(info.username, info.password);
        return result == AuthenticationResult.Succes ? Accepted(new object[] {result, userId, token}) : Result(result);
    }

    /// <summary>
    /// create a new account for a user
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] CreateUserInfo info) {
        logger.LogInformation("Create Attempt: " + info.username);
        AuthenticationResult result = await authentication.CreateAccount(info.username, info.password);
        return Result(result);
    }

    /// <summary>
    /// create the correct message for a given result
    /// </summary>
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