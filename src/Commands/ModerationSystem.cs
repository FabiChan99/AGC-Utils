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
using IniParser.Model;

namespace AGC_Management.Commands
{
    public class ModerationSystem : BaseCommandModule
    {
        public static async Task<bool> TicketUrlCheck(CommandContext ctx, string reason)
        {
            string TicketUrl = "modtickets.animegamingcafe.de";
            Console.WriteLine($"Ticket-URL Check {reason}");
            if (reason.ToLower().Contains(TicketUrl.ToLower()))
            {
                Console.WriteLine("Ticket-URL enthalten");
                DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder().WithTitle("Fehler: Ticket-URL enthalten").
                    WithDescription("Bitte schreibe den Grund ohne Ticket-URL").
                    WithColor(DiscordColor.Red);
                DiscordEmbed embed = embedBuilder.Build();
                await ctx.Channel.SendMessageAsync(embed:embed);

                return true;
            }
            else
            {
                Console.WriteLine("Keine URL gefunden");
                return false;
            }
        }
            


        [Command("kick")]
        [RequirePermissions(Permissions.KickMembers)]
        public async Task KickMember(CommandContext ctx, DiscordMember member,[RemainingText] string reason)
        {
            if (await TicketUrlCheck(ctx, reason))
            {
                return;
            }
            else
            {
                DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder().WithTitle($"Du wurdest von {ctx.Guild.Name} gekickt")
                    .WithDescription($"Grund: {reason}")
                    .WithColor(DiscordColor.Red);
                DiscordEmbed embed = embedBuilder.Build();
                string sent = "Nein";
                string ReasonString = $"Grund {reason} | Von Moderator: {ctx.User.UsernameWithDiscriminator} | Datum: {DateTime.Now:dd.MM.yyyy - HH:mm}";
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
        [Command("ban")]
        [RequirePermissions(Permissions.BanMembers)]
        public async Task BanMember(CommandContext ctx, DiscordUser user, [RemainingText] string reason)
        {
            if (await TicketUrlCheck(ctx, reason))
            {
                return;
            }
            else
            {
                DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder().WithTitle($"Du wurdest von {ctx.Guild.Name} gebannt")
                    .WithDescription($"Grund: {reason}")
                    .WithColor(DiscordColor.Red);
                DiscordEmbed embed = embedBuilder.Build();
                string sent = "Nein";
                string ReasonString = $"Grund {reason} | Von Moderator: {ctx.User.UsernameWithDiscriminator} | Datum: {DateTime.Now:dd.MM.yyyy - HH:mm}";
                try
                {
                    await user.SendMessageAsync(embed: embed);
                    sent = "Ja";
                }
                catch (UnauthorizedException)
                {
                    sent = "Nein. Nutzer hat DMs deaktiviert oder den Bot blockiert.";
                }

                await ctx.Guild.BanMemberAsync(user.Id, 7, ReasonString);
                DiscordEmbedBuilder discordEmbedBuilder = new DiscordEmbedBuilder()
                                    .WithTitle($"{user.UsernameWithDiscriminator} wurde gebannt")
                                    .WithDescription($"User: {user.UsernameWithDiscriminator}\n" +
                                                     $"Begründung: {reason}\n" +
                                                     $"Nutzer benachrichtigt: {sent}")
                                    .WithColor(GlobalProperties.EmbedColor);
            }
        }

    }
}
