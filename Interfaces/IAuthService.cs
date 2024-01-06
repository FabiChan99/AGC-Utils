using AGC_Management.Enums.Web;

namespace AGC_Management.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string AccessLevel)> Login(string nutzername, string passwort);
    Task Logout();
    bool IsUserAuthenticated();
    string GetUserAccessLevel();
    bool isAuthorized(AccessLevel accessLevel);
    string GetUsername();
}

