using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using AGC_Management.Services.DatabaseHandler;

namespace AGC_Management.Helper
{
    public class RequireStaffRole : CheckBaseAttribute
    {

        private ulong RoleId = ulong.Parse(GlobalProperties.ConfigIni["ServerConfig"]["StaffRoleId"]);
        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            
            // Check if user has staff role
            if (ctx.Member.Roles.Any(r => r.Id == RoleId))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    public class RequireDatabase : CheckBaseAttribute
    {
        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            // Check if database is connected
            if (DatabaseService.IsConnected() == true)
            {
                return true;
            }
            else
            {
                Console.WriteLine("Database is not connected! Command disabled.");
                return false;
            }
        }
    }
}