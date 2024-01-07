#region

using AGC_Management.Enums.Web;
using AGC_Management.Interfaces;
using AGC_Management.Utils;

#endregion

namespace AGC_Management.Services.Web
{
    public class AuthService : IAuthService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<(bool Success, string AccessLevel)> Login(string nutzername, string passwort)
        {
            if (string.IsNullOrEmpty(nutzername) || string.IsNullOrEmpty(passwort))
            {
                return (false, null);
            }

            var constring = DatabaseService.GetConnectionString();
            await using var con = new NpgsqlConnection(constring);
            await con.OpenAsync();

            await using var cmd =
                new NpgsqlCommand("SELECT hashed_password, access_level FROM web_users WHERE username = @username",
                    con);
            cmd.Parameters.AddWithValue("username", nutzername);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var storedHashedPassword = reader.GetString(0);
                var accessLevel = reader.GetString(1);

                if (AuthUtils.VerifyPassword(nutzername, passwort))
                {
                    return (true, accessLevel);
                }
            }

            return (false, null);
        }


        public bool IsUserAuthenticated()
        {
            var context = _httpContextAccessor.HttpContext;
            return context.Session.GetString("IsAuthenticated") == "true";
        }

        public string GetUserAccessLevel()
        {
            var context = _httpContextAccessor.HttpContext;
            return context.Session.GetString("AccessLevel");
        }

        public string GetUsername()
        {
            var context = _httpContextAccessor.HttpContext;
            return context.Session.GetString("Username");
        }

        public async Task Logout()
        {
            var context = _httpContextAccessor.HttpContext;
            context.Session.SetString("IsAuthenticated", "false");
            context.Session.SetString("AccessLevel", "");
            await context.Session.CommitAsync();
        }

        public bool isAuthorized(AccessLevel accessLevel)
        {
            var context = _httpContextAccessor.HttpContext;
            var userAccessLevelStr = context.Session.GetString("AccessLevel");


            if (!string.IsNullOrWhiteSpace(userAccessLevelStr) &&
                Enum.TryParse(userAccessLevelStr, out AccessLevel userAccessLevel))
            {
                return userAccessLevel >= accessLevel;
            }

            return false;
        }
    }
}