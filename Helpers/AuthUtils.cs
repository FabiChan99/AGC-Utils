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
    
    public static async Task<bool> HasAdministrativeUsers()
    {
        var constring = DatabaseService.GetConnectionString();
        await using var conn = new NpgsqlConnection(constring);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM web_users WHERE access_level = @access_level", conn);
        cmd.Parameters.AddWithValue("access_level", AccessLevel.Administrator.ToString());

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var count = reader.GetInt32(0);
            return count > 0;
        }

        return false;
    }
    
    public static string GenerateToken(int charcount)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var stringChars = new char[charcount];
        var random = new Random();

        for (var i = 0; i < stringChars.Length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }

        return new string(stringChars);
    }

}