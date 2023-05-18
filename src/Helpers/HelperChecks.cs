using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;


namespace AGC_Management.Helper.Checks
{
    public class HelperChecks
    {
        public static async Task<bool> CheckForReason(CommandContext ctx, string reason)
        {
            if (reason == null)
            {
                DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder().WithTitle("Fehler: Kein Grund angegeben!")
                    .WithDescription("Bitte gebe einen Grund an")
                    .WithColor(DiscordColor.Red).WithFooter($"{ctx.User.UsernameWithDiscriminator}");
                DiscordMessageBuilder msg = new DiscordMessageBuilder().WithEmbed(embedBuilder.Build()).WithReply(ctx.Message.Id, false);
                await ctx.Channel.SendMessageAsync(msg);


                return true;
            }
            else
            {
                return false;
            }
        }


        public static string GenerateCaseID()
        // Generate CaseID with mix from current time and random number
        {
            Random rnd = new Random();
            string CaseID = DateTime.Now.ToString("yyyyMMddHHmmss") + rnd.Next(1000, 9999);
            return CaseID;
        }
        public static async Task<bool> TicketUrlCheck(CommandContext ctx, string reason)
        {
            string TicketUrl = "modtickets.animegamingcafe.de";
            if (reason == null)
            {
                return false;
            }
            if (reason.ToLower().Contains(TicketUrl.ToLower()))
            {
                Console.WriteLine("Ticket-URL enthalten");
                DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder().WithTitle("Fehler: Ticket-URL enthalten").
                    WithDescription("Bitte schreibe den Grund ohne Ticket-URL").
                    WithColor(DiscordColor.Red);
                DiscordEmbed embed = embedBuilder.Build();
                DiscordMessageBuilder msg_e = new DiscordMessageBuilder().WithEmbed(embed).WithReply(ctx.Message.Id, false);
                await ctx.Channel.SendMessageAsync(msg_e);

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

