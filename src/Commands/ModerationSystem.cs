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
using IniParser;
using IniParser.Model;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Extensions;
using AGC_Management.Helper.AttributeHelper;


namespace AGC_Management.Commands.Moderation
{
    public class ModerationSystem : BaseCommandModule
    {


        private static string GenerateCaseID()
        // Generate CaseID with mix from current time and random number
        {
            Random rnd = new Random();
            string CaseID = DateTime.Now.ToString("yyyyMMddHHmmss") + rnd.Next(1000, 9999);
            return CaseID;
        }
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
                DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder().WithTitle($"Du wurdest von {ctx.Guild.Name} gebannt!")
                        .WithDescription($"**Begründung:**\n```\n{reason}\n```\n\n" +
                                         $"**Du möchtest einen Entbannungsantrag stellen?**\n" +
                                         $"Dann kannst du eine Entbannung beim [Entbannportal](https://unban.animegamingcafe.de) beantragen").WithColor(DiscordColor.Red);
                DiscordEmbed embed = embedBuilder.Build();
                bool sent;
                string ReasonString = $"Grund {reason} | Von Moderator: {ctx.User.UsernameWithDiscriminator} | Datum: {DateTime.Now:dd.MM.yyyy - HH:mm}";
                string sentEmoji;
                string sentString;
                try
                {

                    await user.SendMessageAsync(embed:embed);
                    sent = true;
                    sentString = "Ja";
                }
                catch (Exception e)
                {
                    sentString = $"Nein. Fehlergrund: ```{e.Message}```";
                    sent = false;
                }

                string SentEmoji;

                if (sent)
                {
                    SentEmoji = "<:yes:861266772665040917>";
                }
                else
                {
                    SentEmoji = "<:no:861266772724023296>";
                }
                try
                {
                    await ctx.Guild.BanMemberAsync(user.Id, 7, ReasonString);
                }
                catch (UnauthorizedException e)
                {
                    DiscordEmbedBuilder failsuccessEmbedBuilder = new DiscordEmbedBuilder()
                    .WithTitle($"Ban Result: <:counting_warning:962007085426556989>")
                    .WithDescription($"**Grund:** ```{reason}```\n" +
                     $"**Moderator:** {ctx.User.Mention} {ctx.User.UsernameWithDiscriminator}\n\n" +
                     $"**Punished User:** **Kein User gepunisched!**\n" +
                     $"**Not Punished User:** {user.Mention} {user.UsernameWithDiscriminator}\n\n" +
                     $"**Fehler:** <:counting_warning:962007085426556989><:counting_warning:962007085426556989><:counting_warning:962007085426556989>```{e.Message}```\n" +
                     $"**User DM'd:** {SentEmoji} {sentString}").WithFooter("AGC Moderation System")
                    .WithColor(DiscordColor.Red);
                    DiscordEmbed failsuccessEmbed = failsuccessEmbedBuilder.Build();
                    DiscordMessageBuilder failSuccessMessage = new DiscordMessageBuilder()
                        .WithEmbed(failsuccessEmbed)//.AddComponents(buttons)
                        .WithReply(ctx.Message.Id, true);

                    await ctx.Channel.SendMessageAsync(failSuccessMessage);
                    return;

                }
                DiscordEmbedBuilder successEmbedBuilder = new DiscordEmbedBuilder()
                    .WithTitle($"Ban Result:")
                    .WithDescription($"**Grund:** ```{reason}```\n" +
                                     $"**Moderator:** {ctx.User.Mention} {ctx.User.UsernameWithDiscriminator}\n" +
                                     $"**Punished User:** {user.Mention} {user.UsernameWithDiscriminator}\n" +
                                     $"**Not Punished User:** **Alle User gepunished**\n" +
                                     $"**User DM'd:** {SentEmoji} {sentString}").WithFooter("AGC Moderation System")
                    .WithColor(DiscordColor.Green);
                DiscordEmbed successEmbed = successEmbedBuilder.Build();
                DiscordMessageBuilder SuccessMessage = new DiscordMessageBuilder()
                    .WithEmbed(successEmbed)//.AddComponents(buttons)
                    .WithReply(ctx.Message.Id, true);
                await ctx.Channel.SendMessageAsync(SuccessMessage);
            }
        }

        [Command("banrequest")]
        [Aliases("banreq")]
        [RequireStaffRole]
        public async Task BanRequest(CommandContext ctx, DiscordUser user, [RemainingText] string reason)
        {
            if (await TicketUrlCheck(ctx, reason))
            {
                return;
            }
            string caseid = GenerateCaseID();
            DiscordRole staffrole = ctx.Guild.GetRole(ulong.Parse(GlobalProperties.ConfigIni["MainConfig"]["StaffRoleId"]));
            List<DiscordMember> staffmembers = ctx.Guild.Members
                .Where(x => x.Value.Roles.Any(y => y.Id == GlobalProperties.StaffRoleId))
                .Select(x => x.Value)
                .ToList();
            List<DiscordMember> staffWithBanPerms = staffmembers.Where(x => x.Permissions.HasPermission(Permissions.BanMembers)).ToList();
            List<DiscordMember> onlineStaffWithBanPerms = staffWithBanPerms.Where(member => (member.Presence?.Status ?? UserStatus.Offline) != UserStatus.Offline).ToList();

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .WithTitle($"Bannanfrage")
                .WithDescription($"Ban-Anfrage für Benutzer: ``{user.UsernameWithDiscriminator}`` ``({user.Id})``\n" +
                                 $"Banngrund:\n```\n{reason}\n```\n" +
                                 $"Bitte warte, während diese Anfrage von jemandem mit Bannberechtigung bestätigt wird <a:loading_agc:1084157150747697203>")
                .WithColor(GlobalProperties.EmbedColor)
                .WithFooter($"{ctx.User.UsernameWithDiscriminator}");
            
            
            string staffMentionString;
            if (onlineStaffWithBanPerms.Count > 0)
            {
                staffMentionString = string.Join(" ", onlineStaffWithBanPerms
                    .Where(member => member.Id != 441192596325531648)
                    .Select(member => member.UsernameWithDiscriminator));
            }
            else
            {
                staffMentionString = "Kein Moderator online | <@_&750365462235316244> | <@_&760211373921271918>";
            }

            DiscordEmbed embed = embedBuilder.Build();
            List<DiscordButtonComponent> buttons = new(2)
            {
                    new DiscordButtonComponent(ButtonStyle.Success, $"banrequest_accept_{caseid}", "Annehmen"),
                    new DiscordButtonComponent(ButtonStyle.Danger, $"banrequest_deny_{caseid}", "Ablehnen")
            };
            var builder = new DiscordMessageBuilder()
                .WithEmbed(embed)
                .AddComponents(buttons).WithContent(staffMentionString)
                .WithReply(ctx.Message.Id, true);
            var interactivity = ctx.Client.GetInteractivity();
            var message = await ctx.Channel.SendMessageAsync(builder);


            var result = await interactivity.WaitForButtonAsync(message, interaction =>
            {
                if (interaction.User is DiscordMember guildUser)
                {
                    return guildUser.Permissions.HasPermission(Permissions.BanMembers);
                }

                return false;
            }, TimeSpan.FromSeconds(10));
            if (result.TimedOut)
            {
                var embed_ = new DiscordMessageBuilder()
                    .WithEmbed(embedBuilder.WithTitle("Bannanfrage abgebrochen")
                    .WithDescription($"Die Bannanfrage für {user} (``{user.Id}``) wurde abgebrochen.\n\nGrund: Zeitüberschreitung. <:counting_warning:962007085426556989>").WithColor(DiscordColor.Red).Build());
                await message.ModifyAsync(embed_);
                return;
            }

            if (result.Result.Id == $"banrequest_accept_{caseid}")
            {
                string now = DateTime.Now.ToString("dd.MM.yyyy");
                DiscordEmbedBuilder banEmbedBuilder = new DiscordEmbedBuilder()
                        .WithTitle($"Du wurdest von {ctx.Guild.Name} gebannt!")
                        .WithDescription($"**Begründung:**\n```\n{reason}\n```\n\n" +
                                         $"**Du möchtest einen Entbannungsantrag stellen?**\n" +
                                         $"Dann kannst du eine Entbannung beim [Entbannportal](https://unban.animegamingcafe.de) beantragen");
                bool sent;
                string sentString;
                string failReason;
                try
                {

                    await user.SendMessageAsync(embed: banEmbedBuilder.Build());
                    sent = true;
                    sentString = "Ja";
                }
                catch (Exception e)
                {
                    sentString = $"Nein. Fehlergrund: ```{e.Message}```";
                    sent = false;
                }

                string SentEmoji;

                if (sent)
                {
                    SentEmoji = "<:yes:861266772665040917>";
                }
                else
                {
                    SentEmoji = "<:no:861266772724023296>";
                }

                try
                {
                    await ctx.Guild.BanMemberAsync(user, reason: $"Grund: {reason} | Banrequestor: {ctx.User} | Banapprover: {result.Result.User} | Datum: {now}");
                }
                catch (UnauthorizedException e)
                {
                    buttons.ForEach(x => x.Disable());
                    await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                    DiscordEmbedBuilder failsuccessEmbedBuilder = new DiscordEmbedBuilder()
                    .WithTitle($"Ban Result: <:counting_warning:962007085426556989>")
                    .WithDescription($"**Grund:** ```{reason}```\n" +
                     $"**Banrequestor:** {ctx.User.Mention} {ctx.User.UsernameWithDiscriminator}\n" +
                     $"**Banapprover:** {result.Result.Interaction.User.Mention} {result.Result.Interaction.User.UsernameWithDiscriminator}\n\n" +
                     $"**Punished User:** **Kein User gepunisched!**\n" +
                     $"**Not Punished User:** {user.Mention} {user.UsernameWithDiscriminator}\n\n" +
                     $"**Fehler:** <:counting_warning:962007085426556989><:counting_warning:962007085426556989><:counting_warning:962007085426556989>```{e.Message}```\n" +
                     $"**User DM'd:** {SentEmoji} {sentString}").WithFooter("AGC Moderation System")
                    .WithColor(DiscordColor.Red);
                    DiscordEmbed failsuccessEmbed = failsuccessEmbedBuilder.Build();
                    DiscordMessageBuilder failSuccessMessage = new DiscordMessageBuilder()
                        .WithEmbed(failsuccessEmbed)//.AddComponents(buttons)
                        .WithReply(ctx.Message.Id, true);

                    await message.ModifyAsync(failSuccessMessage);

                }
                buttons.ForEach(x => x.Disable());
                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                DiscordEmbedBuilder successEmbedBuilder = new DiscordEmbedBuilder()
                    .WithTitle($"Ban Result:")
                    .WithDescription($"**Grund:** ```{reason}```\n" +
                                     $"**Banrequestor:** {ctx.User.Mention} {ctx.User.UsernameWithDiscriminator}\n" +
                                     $"**Banapprover:** {result.Result.Interaction.User.Mention} {result.Result.Interaction.User.UsernameWithDiscriminator}\n\n" +
                                     $"**Punished User:** {user.Mention} {user.UsernameWithDiscriminator}\n" +
                                     $"**Not Punished User:** **Alle User gepunished**\n" +
                                     $"**User DM'd:** {SentEmoji} {sentString}").WithFooter("AGC Moderation System")
                    .WithColor(DiscordColor.Green);
                DiscordEmbed successEmbed = successEmbedBuilder.Build();
                DiscordMessageBuilder SuccessMessage = new DiscordMessageBuilder()
                    .WithEmbed(successEmbed)//.AddComponents(buttons)
                    .WithReply(ctx.Message.Id, true);

                await message.ModifyAsync(SuccessMessage);
            }
            else if (result.Result.Id == $"banrequest_deny_{caseid}")
            {
                buttons.ForEach(x => x.Disable());
                DiscordEmbedBuilder declineEmbedBuilder = new DiscordEmbedBuilder()
                .WithTitle($"Ban Result:")
                    .WithDescription($"**Grund:** ```{reason}```\n" +
                                     $"**Banrequestor:** {ctx.User.Mention} {ctx.User.UsernameWithDiscriminator}\n" +
                                     //$"**Banapprover:** {result.Result.Interaction.User.Mention} {result.Result.Interaction.User.UsernameWithDiscriminator}" +
                                     $"**<:counting_warning:962007085426556989>BAN ABGELEHNT von {result.Result.Interaction.User.Mention}<:counting_warning:962007085426556989>\n\n" +
                                     $"**Punished User:** **Kein User gepunisched!**\n" +
                                     $"**Not Punished User:** {user.Mention} {user.UsernameWithDiscriminator}\n" +
                                     $"**User DM'd:** /-/").WithFooter("AGC Moderation System")
                    .WithColor(DiscordColor.Red);
                DiscordEmbed declineEmbed = declineEmbedBuilder.Build();
                DiscordMessageBuilder DeclineMessage = new DiscordMessageBuilder()
                    .WithEmbed(declineEmbed)//.AddComponents(buttons)
                    .WithReply(ctx.Message.Id, true);
                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                
                await message.ModifyAsync(DeclineMessage);
            }










        }
    }
}
