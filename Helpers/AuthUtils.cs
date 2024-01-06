#region

using System.Security.Cryptography;
using System.Text;
using AGC_Management.Enums.Web;
using AGC_Management.Interfaces;
using AGC_Management.Services;
using AGC_Management.Services.Web;
using Microsoft.AspNetCore.Components;

#endregion

namespace AGC_Management.Utils;

public sealed class AuthUtils
{
    public static bool VerifyPassword(string username, string password)
    {
        var constring = DatabaseService.GetConnectionString();
        using var conn = new NpgsqlConnection(constring);
        conn.Open();
        using var cmd = new NpgsqlCommand("SELECT hashed_password FROM web_users WHERE username = @username", conn);
        cmd.Parameters.AddWithValue("username", username);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var storedHashedPassword = reader.GetString(0);
            
            return BCrypt.Net.BCrypt.Verify(password, storedHashedPassword);
        }

        return false;
    }

    /// <summary>
    /// Checks if access is allowed based on the given access level.
    /// </summary>
    /// <param name="AccessLevel">The access level to check for.</param>
    /// <param name="_NavigationManager">The navigation manager used for redirecting unauthorized or unauthenticated users.</param>
    /// <param name="_AuthService">The service used for authentication and authorization.</param>
    /// <returns>True if access is allowed, False otherwise.</returns>
    public static bool isAccessAllowed(AccessLevel AccessLevel, NavigationManager _NavigationManager, IAuthService _AuthService)
    {
        var isAuthenticated = _AuthService.IsUserAuthenticated();
        if (!isAuthenticated)
        {
            _NavigationManager.NavigateTo("/401");
            return false;
        }
        var isAuthorized = _AuthService.isAuthorized(AccessLevel);
        if (!isAuthorized)
        {
            _NavigationManager.NavigateTo("/403");
            return false;
        }
        return true;
    }

}