using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Exceptions;



namespace AGC_Management.Commands
{
    public class ModerationSystem : BaseCommandModule
    {
        public static async Task<bool> TicketUrlCheck(CommandContext ctx, string reason)
        {
            string TicketUrl = "modtickets.animegamingcafe.de";
            if (reason.ToLower().Contains(TicketUrl))
            {
                DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder().WithTitle("Fehler: Ticket-URL enthalten").
                    WithDescription("Bitte schreibe den Grund ohne Ticket-URL").
                    WithColor(DiscordColor.Red);
                DiscordEmbed embed = embedBuilder.Build();
                await ctx.Channel.SendMessageAsync(embed: embed);

                return true;
            }
            else
            {
                return false;
            }
        }
            


        [Command("kick")]
        [RequirePermissions(Permissions.KickMembers)]
        public async Task KickMembers(CommandContext ctx, DiscordMember member, string reason)
        {
            if (await TicketUrlCheck(ctx, reason))
            {
                return;
            }
            else
            {
                DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder().WithTitle($"Du wurdest von {ctx.Guild.Name} gekickt").WithDescription($"Grund: {reason}").
                    WithColor(DiscordColor.Red);
                DiscordEmbed embed = embedBuilder.Build();
                bool sent = false;
                try
                {
                    await member.SendMessageAsync(embed: embed);
                    sent = true;
                }
                catch (UnauthorizedException)
                {
                    await ctx.Channel.SendMessageAsync("Der User hat seine DMs deaktiviert");
                    sent = false;
                    // TODO: add kick logic and finish the command
                }
            }
        }
    }
}
