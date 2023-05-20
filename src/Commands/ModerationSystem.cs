using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Exceptions;
using DisCatSharp.Interactivity.Extensions;
using AGC_Management.Helper;
using Sentry;

namespace AGC_Management.Commands
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
                    .WithTitle($"{member.UsernameWithDiscriminator} nicht gekickt")
                    .WithDescription($"Der User ``{member.UsernameWithDiscriminator} ({member.Id})`` konnte nicht gekickt werden!\n\n")
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithColor(DiscordColor.Red);

                DiscordEmbed failsuccessEmbed = failsuccessEmbedBuilder.Build();
                DiscordMessageBuilder failSuccessMessage = new DiscordMessageBuilder()
                    .WithEmbed(failsuccessEmbed)
                    .WithReply(ctx.Message.Id, false);

                await ctx.Channel.SendMessageAsync(failSuccessMessage);
                return;
            }

            DiscordEmbedBuilder successEmbedBuilder = new DiscordEmbedBuilder()
                                   .WithTitle($"{member.UsernameWithDiscriminator} wurde erfolgreich gekickt")
                    .WithDescription($"Der User ``{member.UsernameWithDiscriminator} ({member.Id})`` wurde erfolgreich gekickt!\n\n" +
                                          $"Grund: ``{reason}``\n\n" +
                                          $"User wurde über den kick benachrichtigt? {SentEmoji}")
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithColor(DiscordColor.Green);

            DiscordEmbed successEmbed = successEmbedBuilder.Build();
            DiscordMessageBuilder SuccessMessage = new DiscordMessageBuilder()
                .WithEmbed(successEmbed)
                .WithReply(ctx.Message.Id, false);

            await ctx.Channel.SendMessageAsync(SuccessMessage);
        }

        [Command("ban")]
        [RequireDatabase]
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
                    .WithTitle($"{user.UsernameWithDiscriminator} nicht gebannt")
                    .WithDescription($"Der User ``{user.UsernameWithDiscriminator} ({user.Id})`` konnte nicht gebannt werden!\n\n")
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithColor(DiscordColor.Red);

                DiscordEmbed failsuccessEmbed = failsuccessEmbedBuilder.Build();
                DiscordMessageBuilder failSuccessMessage = new DiscordMessageBuilder()
                    .WithEmbed(failsuccessEmbed)
                    .WithReply(ctx.Message.Id, false);

                await ctx.Channel.SendMessageAsync(failSuccessMessage);
                return;
            }

            DiscordEmbedBuilder successEmbedBuilder = new DiscordEmbedBuilder()
                                                   .WithTitle($"{user.UsernameWithDiscriminator} wurde erfolgreich gebannt")
                    .WithDescription($"Der User ``{user.UsernameWithDiscriminator} ({user.Id})`` wurde erfolgreich gebannt!\n\n" +
                                          $"Grund: ``{reason}``\n\n" +
                                          $"User wurde über den kick benachrichtigt? {SentEmoji}")
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithColor(DiscordColor.Green);

            DiscordEmbed successEmbed = successEmbedBuilder.Build();
            DiscordMessageBuilder SuccessMessage = new DiscordMessageBuilder()
                .WithEmbed(successEmbed)
                .WithReply(ctx.Message.Id, false);

            await ctx.Channel.SendMessageAsync(SuccessMessage);
        }


        [Command("multiban")]
        [RequirePermissions(Permissions.BanMembers)]
        public async Task MultiBan(CommandContext ctx, [RemainingText] string ids_and_reason)
        {
            List<ulong> ids = new List<ulong>();
            string reason = "";

            string[] parts = ids_and_reason.Split(' ');
            bool isReasonStarted = false;

            foreach (string part in parts)
            {
                if (!isReasonStarted)
                {
                    if (part.StartsWith("<@") && part.EndsWith(">"))
                    {
                        string idString = part.Substring(2, part.Length - 3);
                        if (ulong.TryParse(idString, out ulong id))
                        {
                            ids.Add(id);
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if (ulong.TryParse(part, out ulong id))
                    {
                        ids.Add(id);
                    }
                    else
                    {
                        isReasonStarted = true;
                        reason += part + " ";
                    }
                }
                else
                {
                    reason += part + " ";
                }
            }
            if (await HelperChecks.CheckForReason(ctx, reason))
            {
                return;
            }
            if (await HelperChecks.TicketUrlCheck(ctx, reason))
            {
                return;
            }
            List<DiscordUser> users_to_ban = new List<DiscordUser>();
            string reasonString = $"Grund: {reason} | Von Moderator: {ctx.User.UsernameWithDiscriminator} | Datum: {DateTime.Now:dd.MM.yyyy - HH:mm}";
            foreach (ulong id in ids)
            {
                DiscordUser? user = await ctx.Client.TryGetUserAsync(id);
                if (user != null)
                {
                    users_to_ban.Add(user);
                }
            }
            string busers = "\n";
            string busers_formatted = string.Join("\n", users_to_ban.Select(buser => buser.UsernameWithDiscriminator));
            var caseid = HelperChecks.GenerateCaseID();
            DiscordEmbedBuilder confirmEmbedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Überprüfe deine Eingabe").WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                .WithDescription($"Bitte überprüfe deine Eingabe und bestätige mit ✅, um fortzufahren.\n\n" +
                $"Users:\n" +
                $"```{busers_formatted}```\nGrund:```{reason}```")
                .WithColor(GlobalProperties.EmbedColor);
            DiscordEmbed confirmEmbed = confirmEmbedBuilder.Build();
            List<DiscordButtonComponent> buttons = new(2)
    {
        new DiscordButtonComponent(ButtonStyle.Success, $"multiban_accept_{caseid}", "✅"),
        new DiscordButtonComponent(ButtonStyle.Danger, $"multiban_deny_{caseid}", "❌")
    };
            var builder = new DiscordMessageBuilder()
                            .WithEmbed(confirmEmbed)
                            .AddComponents(buttons)
                            .WithReply(ctx.Message.Id, false);
            var interactivity = ctx.Client.GetInteractivity();
            var message = await ctx.Channel.SendMessageAsync(builder);

            var result = await interactivity.WaitForButtonAsync(message, ctx.User, TimeSpan.FromSeconds(10));
            if (result.TimedOut)
            {
                var embed_ = new DiscordMessageBuilder()
                    .WithEmbed(confirmEmbedBuilder.WithTitle("Bannanfrage abgebrochen").WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithDescription($"Der Multiban wurde abgebrochen.\n\nGrund: Zeitüberschreitung. <:counting_warning:962007085426556989>").WithColor(DiscordColor.Red).Build());
                await message.ModifyAsync(embed_);
                return;
            }



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
            }, TimeSpan.FromHours(6));

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
                    .WithTitle($"{user.UsernameWithDiscriminator} nicht gebannt")
                    .WithDescription($"Der User ``{user.UsernameWithDiscriminator} ({user.Id})`` konnte nicht gebannt werden!\n\n")
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
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
                                              .WithTitle($"{user.UsernameWithDiscriminator} wurde erfolgreich gebannt")
                    .WithDescription($"Der User ``{user.UsernameWithDiscriminator} ({user.Id})`` wurde erfolgreich gebannt!\n\n" +
                                          $"Grund: ``{reason}``\n\n" +
                                          $"User wurde über den kick benachrichtigt? {SentEmoji}")
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
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
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

