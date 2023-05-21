using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using AGC_Management.Services.DatabaseHandler;
using DisCatSharp.Entities;

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
                DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder().WithTitle("Fehler: Datenbank nicht verbunden!").
                    WithDescription($"Command deaktiviert. Bitte informiere den Botentwickler ``{ctx.Client.GetUserAsync(GlobalProperties.BotOwnerId).Result.UsernameWithDiscriminator}``").
                    WithColor(DiscordColor.Red);
                DiscordEmbed embed = embedBuilder.Build();
                DiscordMessageBuilder msg_e = new DiscordMessageBuilder().WithEmbed(embed).WithReply(ctx.Message.Id, false);
                await ctx.Channel.SendMessageAsync(msg_e);
                return false;
            }
        }
    }
}