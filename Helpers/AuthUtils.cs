#region

using System.Security.Cryptography;
using System.Text;
using AGC_Management.Services;

#endregion

namespace AGC_Management.Utils;

public sealed class AuthUtils
{
    public static string HashPassword(string password)
    {
        using var sha512 = SHA512.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha512.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }


    public static bool VerifyPassword(string username, string password)
    {
        var hashedPassword = HashPassword(password);
        var constring = DatabaseService.GetConnectionString();
        using var conn = new NpgsqlConnection(constring);
        conn.Open();
        using var cmd =
            new NpgsqlCommand(
                "SELECT hashed_password FROM web_users WHERE hashed_password = @hashed_password AND username = @username",
                conn);
        cmd.Parameters.AddWithValue("hashed_password", hashedPassword);
        cmd.Parameters.AddWithValue("username", username);
        using var reader = cmd.ExecuteReader();
        if (reader.HasRows)
        {
            return true;
        }

        return false;
    }
}