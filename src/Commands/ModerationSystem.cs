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
using Sentry;
using AGC_Management.Helper.Checks;

namespace AGC_Management.Commands.Moderation
{
    public class ModerationSystem : BaseCommandModule
    {
        [Command("kick")]
        [RequirePermissions(Permissions.KickMembers)]
        public async Task KickMember(CommandContext ctx, DiscordMember member, [RemainingText] string reason)
        {
            if (await HelperChecks.CheckForReason(ctx, reason))
            {
                return;
            }

            if (await HelperChecks.TicketUrlCheck(ctx, reason))
            {
                return;
            }

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .WithTitle($"Du wurdest von {ctx.Guild.Name} gekickt")
                .WithDescription($"Grund: ```{reason}```")
                .WithColor(DiscordColor.Red);
            DiscordEmbed embed = embedBuilder.Build();

            bool sent;
            string SentEmoji;
            string sentString;
            string ReasonString = $"Grund {reason} | Von Moderator: {ctx.User.UsernameWithDiscriminator} | Datum: {DateTime.Now:dd.MM.yyyy - HH:mm}";

            try
            {
                await member.SendMessageAsync(embed: embed);
                sent = true;
                sentString = "Ja";
                SentEmoji = "<:yes:861266772665040917>";
            }
            catch (UnauthorizedException)
            {
                sentString = "Nein. Nutzer hat DMs deaktiviert oder den Bot blockiert.";
                sent = false;
                SentEmoji = "<:no:861266772724023296>";
            }

            try
            {
                await member.RemoveAsync(ReasonString);
            }
            catch (UnauthorizedException e)
            {
                DiscordEmbedBuilder failsuccessEmbedBuilder = new DiscordEmbedBuilder()
                    .WithTitle($"Punish result")
                    .WithDescription($"**Grund:** ```{reason}```\n" +
                                     $"**Action:** Kick\n" +
                                     $"**Moderator:** {ctx.User.Mention} {ctx.User.UsernameWithDiscriminator}\n\n" +
                                     $"**Punished User:** **Kein User gepunisched!**\n" +
                                     $"**Not Punished User:** {member.Mention} {member.UsernameWithDiscriminator}\n\n" +
                                     $"**Fehler:** <:counting_warning:962007085426556989><:counting_warning:962007085426556989><:counting_warning:962007085426556989>```{e.Message}```\n" +
                                     $"**User DM'd:** {SentEmoji} {sentString}")
                    .WithFooter($"{GlobalProperties.ServerNameInitals} Moderation System")
                    .WithColor(DiscordColor.Red);

                DiscordEmbed failsuccessEmbed = failsuccessEmbedBuilder.Build();
                DiscordMessageBuilder failSuccessMessage = new DiscordMessageBuilder()
                    .WithEmbed(failsuccessEmbed)
                    .WithReply(ctx.Message.Id, false);

                await ctx.Channel.SendMessageAsync(failSuccessMessage);
                return;
            }

            DiscordEmbedBuilder successEmbedBuilder = new DiscordEmbedBuilder()
                .WithTitle($"Punish result")
                .WithDescription($"**Grund:** ```{reason}```\n" +
                                 $"**Action**: Kick\n" +
                                 $"**Moderator:** {ctx.User.Mention} {ctx.User.UsernameWithDiscriminator}\n\n" +
                                 $"**Punished User:** {member.Mention} {member.UsernameWithDiscriminator}\n" +
                                 $"**Not Punished User:** **Alle User gepunished**\n" +
                                 $"**User DM'd:** {SentEmoji} {sentString}")
                .WithFooter($"{GlobalProperties.ServerNameInitals} Moderation System")
                .WithColor(DiscordColor.Green);

            DiscordEmbed successEmbed = successEmbedBuilder.Build();
            DiscordMessageBuilder SuccessMessage = new DiscordMessageBuilder()
                .WithEmbed(successEmbed)
                .WithReply(ctx.Message.Id, false);

            await ctx.Channel.SendMessageAsync(SuccessMessage);
        }

        [Command("ban")]
        [RequirePermissions(Permissions.BanMembers)]
        public async Task BanMember(CommandContext ctx, DiscordUser user, [RemainingText] string reason)
        {
            if (await HelperChecks.CheckForReason(ctx, reason))
            {
                return;
            }
            if (await HelperChecks.TicketUrlCheck(ctx, reason))
            {
                return;
            }

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .WithTitle($"Du wurdest von {ctx.Guild.Name} gebannt!")
                .WithDescription($"**Begründung:**\n```\n{reason}\n```\n\n" +
                                 $"**Du möchtest einen Entbannungsantrag stellen?**\n" +
                                 $"Dann kannst du eine Entbannung beim [Entbannportal](https://unban.animegamingcafe.de) beantragen")
                .WithColor(DiscordColor.Red);

            DiscordEmbed embed = embedBuilder.Build();
            bool sent;
            string ReasonString = $"Grund {reason} | Von Moderator: {ctx.User.UsernameWithDiscriminator} | Datum: {DateTime.Now:dd.MM.yyyy - HH:mm}";
            string sentString;
            var username = user.UsernameWithDiscriminator;

            try
            {
                await user.SendMessageAsync(embed: embed);
                sent = true;
                sentString = "Ja";
            }
            catch (Exception e)
            {
                sentString = $"Nein. Fehlergrund: ```{e.Message}```";
                sent = false;
            }

            string SentEmoji = sent ? "<:yes:861266772665040917>" : "<:no:861266772724023296>";

            try
            {
                await ctx.Guild.BanMemberAsync(user.Id, 7, ReasonString);
            }
            catch (UnauthorizedException e)
            {
                DiscordEmbedBuilder failsuccessEmbedBuilder = new DiscordEmbedBuilder()
                    .WithTitle($"Punish result")
                    .WithDescription($"**Grund:** ```{reason}```\n" +
                                     $"**Action:** Ban\n" +
                                     $"**Moderator:** {ctx.User.Mention} {ctx.User.UsernameWithDiscriminator}\n\n" +
                                     $"**Punished User:** **Kein User gepunisched!**\n" +
                                     $"**Not Punished User:** {user.Mention} {user.UsernameWithDiscriminator}\n\n" +
                                     $"**Fehler:** <:counting_warning:962007085426556989><:counting_warning:962007085426556989><:counting_warning:962007085426556989>```{e.Message}```\n" +
                                     $"**User DM'd:** {SentEmoji} {sentString}")
                    .WithFooter($"{GlobalProperties.ServerNameInitals} Moderation System")
                    .WithColor(DiscordColor.Red);

                DiscordEmbed failsuccessEmbed = failsuccessEmbedBuilder.Build();
                DiscordMessageBuilder failSuccessMessage = new DiscordMessageBuilder()
                    .WithEmbed(failsuccessEmbed)
                    .WithReply(ctx.Message.Id, false);

                await ctx.Channel.SendMessageAsync(failSuccessMessage);
                return;
            }

            DiscordEmbedBuilder successEmbedBuilder = new DiscordEmbedBuilder()
                .WithTitle($"Punish result")
                .WithDescription($"**Grund:** ```{reason}```\n" +
                                 $"**Action**: Ban\n" +
                                 $"**Moderator:** {ctx.User.Mention} {ctx.User.UsernameWithDiscriminator}\n\n" +
                                 $"**Punished User:** {user.Mention} {user.UsernameWithDiscriminator}\n" +
                                 $"**Not Punished User:** **Alle User gepunished**\n" +
                                 $"**User DM'd:** {SentEmoji} {sentString}")
                .WithFooter($"{GlobalProperties.ServerNameInitals} Moderation System")
                .WithColor(DiscordColor.Green);

            DiscordEmbed successEmbed = successEmbedBuilder.Build();
            DiscordMessageBuilder SuccessMessage = new DiscordMessageBuilder()
                .WithEmbed(successEmbed)
                .WithReply(ctx.Message.Id, false);

            await ctx.Channel.SendMessageAsync(SuccessMessage);
        }


        [Command("banrequest")]
        [Aliases("banreq")]
        [RequireStaffRole]
        public async Task BanRequest(CommandContext ctx, DiscordUser user, [RemainingText] string reason)
        {
            if (await HelperChecks.CheckForReason(ctx, reason))
            {
                return;
            }
            if (await HelperChecks.TicketUrlCheck(ctx, reason))
            {
                return;
            }

            string caseid = HelperChecks.GenerateCaseID();
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
                if (!GlobalProperties.DebugMode)
                {
                    staffMentionString = $"Kein Moderator online | {GlobalProperties.ConfigIni["ServerConfig"]["AdminRoleId"]} | {GlobalProperties.ConfigIni["ServerConfig"]["ModRoleId"]}";
                }
                else
                {
                    staffMentionString = "Kein Moderator online | DEBUG MODE AKTIV";
                }
                
            }

            DiscordEmbed embed = embedBuilder.Build();
            List<DiscordButtonComponent> buttons = new(2)
    {
        new DiscordButtonComponent(ButtonStyle.Success, $"banrequest_accept_{caseid}", "Annehmen"),
        new DiscordButtonComponent(ButtonStyle.Danger, $"banrequest_deny_{caseid}", "Ablehnen")
    };

            var builder = new DiscordMessageBuilder()
                .WithEmbed(embed)
                .AddComponents(buttons)
                .WithContent(staffMentionString)
                .WithReply(ctx.Message.Id, false);

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

                string SentEmoji = sent ? "<:yes:861266772665040917>" : "<:no:861266772724023296>";

                try
                {
                    await ctx.Guild.BanMemberAsync(user, reason: $"Grund: {reason} | Banrequestor: {ctx.User} | Banapprover: {result.Result.User} | Datum: {now}");
                }
                catch (UnauthorizedException e)
                {
                    buttons.ForEach(x => x.Disable());
                    await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                    DiscordEmbedBuilder failsuccessEmbedBuilder = new DiscordEmbedBuilder()
                        .WithTitle($"Punish result <:counting_warning:962007085426556989>")
                        .WithDescription($"**Grund:** ```{reason}```\n" +
                        $"**Action:** Ban\n" +
                        $"**Banrequestor:** {ctx.User.Mention} {ctx.User.UsernameWithDiscriminator}\n" +
                        $"**Banapprover:** {result.Result.Interaction.User.Mention} {result.Result.Interaction.User.UsernameWithDiscriminator}\n\n" +
                        $"**Punished User:** **Kein User gepunisched!**\n" +
                        $"**Not Punished User:** {user.Mention} {user.UsernameWithDiscriminator}\n\n" +
                        $"**Fehler:** <:counting_warning:962007085426556989><:counting_warning:962007085426556989><:counting_warning:962007085426556989>```{e.Message}```\n" +
                        $"**User DM'd:** {SentEmoji} {sentString}")
                        .WithFooter($"{GlobalProperties.ServerNameInitals} Moderation System")
                        .WithColor(DiscordColor.Red);

                    DiscordEmbed failsuccessEmbed = failsuccessEmbedBuilder.Build();
                    DiscordMessageBuilder failSuccessMessage = new DiscordMessageBuilder()
                        .WithEmbed(failsuccessEmbed)
                        .WithReply(ctx.Message.Id, false);

                    await message.ModifyAsync(failSuccessMessage);
                }

                buttons.ForEach(x => x.Disable());
                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                DiscordEmbedBuilder successEmbedBuilder = new DiscordEmbedBuilder()
                    .WithTitle($"Punish result")
                    .WithDescription($"**Grund:** ```{reason}```\n" +
                    $"**Action:** Ban\n" +
                    $"**Banrequestor:** {ctx.User.Mention} {ctx.User.UsernameWithDiscriminator}\n" +
                    $"**Banapprover:** {result.Result.Interaction.User.Mention} {result.Result.Interaction.User.UsernameWithDiscriminator}\n\n" +
                    $"**Punished User:** {user.Mention} {user.UsernameWithDiscriminator}\n" +
                    $"**Not Punished User:** **Alle User gepunished**\n" +
                    $"**User DM'd:** {SentEmoji} {sentString}")
                    .WithFooter($"{GlobalProperties.ServerNameInitals} Moderation System")
                    .WithColor(DiscordColor.Green);

                DiscordEmbed successEmbed = successEmbedBuilder.Build();
                DiscordMessageBuilder SuccessMessage = new DiscordMessageBuilder()
                    .WithEmbed(successEmbed)
                    .WithReply(ctx.Message.Id, false);

                await message.ModifyAsync(SuccessMessage);
            }
            else if (result.Result.Id == $"banrequest_deny_{caseid}")
            {
                buttons.ForEach(x => x.Disable());
                DiscordEmbedBuilder declineEmbedBuilder = new DiscordEmbedBuilder()
                    .WithTitle($"Punish result")
                    .WithDescription($"**Grund:** ```{reason}```\n" +
                    $"**Action:** Ban\n" +
                    $"**Banrequestor:** {ctx.User.Mention} {ctx.User.UsernameWithDiscriminator}\n" +
                    //$"**Banapprover:** {result.Result.Interaction.User.Mention} {result.Result.Interaction.User.UsernameWithDiscriminator}" +
                    $"**<:counting_warning:962007085426556989>BAN ABGELEHNT von {result.Result.Interaction.User.Mention}<:counting_warning:962007085426556989>\n\n" +
                    $"**Punished User:** **Kein User gepunisched!**\n" +
                    $"**Not Punished User:** {user.Mention} {user.UsernameWithDiscriminator}\n" +
                    $"**User DM'd:** /-/")
                    .WithFooter($"{GlobalProperties.ServerNameInitals} Moderation System")
                    .WithColor(DiscordColor.Red);

                DiscordEmbed declineEmbed = declineEmbedBuilder.Build();
                DiscordMessageBuilder DeclineMessage = new DiscordMessageBuilder()
                    .WithEmbed(declineEmbed)
                    .WithReply(ctx.Message.Id, false);

                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                await message.ModifyAsync(DeclineMessage);
            }
        }











    }
}

