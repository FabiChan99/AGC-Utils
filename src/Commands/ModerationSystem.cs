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
                string sent = "Nein";
                string ReasonString = $"Grund {reason} | Von Moderator: {ctx.User.UsernameWithDiscriminator} | Datum: {DateTime.Now.ToString("dd.MM.yyyy - HH:mm")}";
                try
                {
                    await member.SendMessageAsync(embed: embed);
                    sent = "Ja";
                }
                catch (UnauthorizedException)
                {
                    sent = "Nein. Nutzer hat DMs deaktiviert oder den Bot blockiert.";
                }
                try
                {
                    await member.RemoveAsync(ReasonString);
                }
                catch (UnauthorizedException) { 
                }
                DiscordEmbedBuilder discordEmbedBuilder = new DiscordEmbedBuilder()
                                    .WithTitle($"{member.UsernameWithDiscriminator} wurde gekickt")
                                    .WithDescription($"User: {member.UsernameWithDiscriminator}\n" +
                                    $"Begründung: {reason}\n" +
                                    $"Nutzer benachrichtigt: {sent}")
                                    .WithColor(GlobalProperties.EmbedColor);
                DiscordEmbed discordEmbed = discordEmbedBuilder.Build();
                await ctx.Channel.SendMessageAsync(embed: discordEmbed);                
            }
        }
    }
}
