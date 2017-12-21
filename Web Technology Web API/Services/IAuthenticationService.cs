using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public enum AuthenticationResult {
    WrongPassword,
    UserDoesNotExist,
    UserAlreadyExists,
    Succes,
    NotLoggedIn,
    InvalidToken,
    UserAlreadyLoggedIn
}

public interface IAuthenticationService {
    Task<(AuthenticationResult result, string userId, string token)> Login(string username, string pwd);
    Task<AuthenticationResult> Logout(string userId);
    Task<AuthenticationResult> CreateAccount(string username, string pwd);
    Task<AuthenticationResult> IsLoggedIn(string userId, string token);
}