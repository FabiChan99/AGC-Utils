using AGC_Management.Helper;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Exceptions;
using DisCatSharp.Interactivity.Extensions;

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

            string SentEmoji;
            string ReasonString = $"Grund {reason} | Von Moderator: {ctx.User.UsernameWithDiscriminator} | Datum: {DateTime.Now:dd.MM.yyyy - HH:mm}";

            try
            {
                await member.SendMessageAsync(embed: embed);
                SentEmoji = "<:yes:861266772665040917>";
            }
            catch (UnauthorizedException)
            {
                SentEmoji = "<:no:861266772724023296>";
            }

            try
            {
                await member.RemoveAsync(ReasonString);
            }
            catch (UnauthorizedException)
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
                .WithDescription($"**Begründung:**```{reason}```\n" +
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
            catch (UnauthorizedException)
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
            List<ulong> ids;
            string reason;
            Converter.SeperateIdsAndReason(ids_and_reason, out ids, out reason);
            if (await HelperChecks.CheckForReason(ctx, reason))
            {
                return;
            }
            if (await HelperChecks.TicketUrlCheck(ctx, reason))
            {
                return;
            }
            reason = reason.TrimEnd(' ');
            List<DiscordUser> users_to_ban = new List<DiscordUser>();
            string reasonString = $"Grund: {reason} | Von Moderator: {ctx.User.UsernameWithDiscriminator} | Datum: {DateTime.Now:dd.MM.yyyy - HH:mm}";
            List<ulong> setids = ids.ToHashSet().ToList();
            if (setids.Count < 2)
            {
                DiscordEmbedBuilder failsuccessEmbedBuilder = new DiscordEmbedBuilder()
                .WithTitle($"Fehler")
                .WithDescription($"Du musst mindestens 2 User angeben!")
                .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                .WithColor(DiscordColor.Red);
                DiscordEmbed failsuccessEmbed = failsuccessEmbedBuilder.Build();
                DiscordMessageBuilder failSuccessMessage = new DiscordMessageBuilder()
                    .WithEmbed(failsuccessEmbed)
                    .WithReply(ctx.Message.Id, false);
                await ctx.Channel.SendMessageAsync(failSuccessMessage);
                return;
            }
            foreach (ulong id in setids)
            {
                DiscordUser? user = await ctx.Client.TryGetUserAsync(id);
                if (user != null)
                {
                    users_to_ban.Add(user);
                }
            }
            string busers_formatted = string.Join("\n", users_to_ban.Select(buser => buser.UsernameWithDiscriminator));
            var caseid = HelperChecks.GenerateCaseID();
            DiscordEmbedBuilder confirmEmbedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Überprüfe deine Eingabe").WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                .WithDescription($"Bitte überprüfe deine Eingabe und bestätige mit ✅ um fortzufahren.\n\n" +
                $"__Users:__\n" +
                $"```{busers_formatted}```\n__Grund:__```{reason}```")
                .WithColor(GlobalProperties.EmbedColor);
            DiscordEmbed confirmEmbed = confirmEmbedBuilder.Build();

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
            .WithTitle($"Du wurdest von {ctx.Guild.Name} gebannt!")
            .WithDescription($"**Begründung:**```{reason}```\n" +
                             $"**Du möchtest einen Entbannungsantrag stellen?**\n" +
                             $"Dann kannst du eine Entbannung beim [Entbannportal](https://unban.animegamingcafe.de) beantragen")
            .WithColor(DiscordColor.Red);
            DiscordEmbed UserEmbed = embedBuilder.Build();
            List<DiscordButtonComponent> buttons = new(2)
    {
        new DiscordButtonComponent(ButtonStyle.Secondary, $"multiban_accept_{caseid}", "✅"),
        new DiscordButtonComponent(ButtonStyle.Secondary, $"multiban_deny_{caseid}", "❌")
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
                    .WithEmbed(confirmEmbedBuilder.WithTitle("Multiban abgebrochen").WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithDescription($"Der Multiban wurde abgebrochen.\n\nGrund: Zeitüberschreitung. <:counting_warning:962007085426556989>").WithColor(DiscordColor.Red).Build());
                await message.ModifyAsync(embed_);
                return;
            }
            if (result.Result.Id == $"multiban_deny_{caseid}")
            {
                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                var embed_ = new DiscordMessageBuilder()
                    .WithEmbed(confirmEmbedBuilder.WithTitle("Multiban abgebrochen").WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                                       .WithDescription($"Der Multiban wurde abgebrochen.\n\nGrund: Abgebrochen. <:counting_warning:962007085426556989>").WithColor(DiscordColor.Red).Build());
                await message.ModifyAsync(embed_);
                return;
            }
            if (result.Result.Id == $"multiban_accept_{caseid}")
            {
                var disbtn = buttons;
                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                disbtn.ForEach(x => x.Disable());
                DiscordEmbedBuilder loadingEmbedBuilder = new DiscordEmbedBuilder()
                    .WithTitle("Multiban wird bearbeitet").WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithDescription($"Der Multiban wird bearbeitet. Bitte warten...")
                    .WithColor(DiscordColor.Yellow);
                DiscordEmbed loadingEmbed = loadingEmbedBuilder.Build();
                var loadingMessage = new DiscordMessageBuilder()
                            .WithEmbed(loadingEmbed).AddComponents(disbtn)
                            .WithReply(ctx.Message.Id, false);
                await message.ModifyAsync(loadingMessage);


                string b_users = "";
                string n_users = "";
                foreach (DiscordUser user in users_to_ban)
                {
                    bool sent = false;
                    try
                    {
                        await user.SendMessageAsync(UserEmbed);
                        sent = true;
                    }
                    catch
                    {
                        sent = false;
                    }
                    string semoji = sent ? "<:yes:861266772665040917>" : "<:no:861266772724023296>";
                    try
                    {
                        await ctx.Guild.BanMemberAsync(user.Id, 7, reasonString);
                        string dm = sent ? "✅" : "❌";
                        b_users += $"{user.UsernameWithDiscriminator} | DM: {dm}\n";
                    }
                    catch (UnauthorizedException)
                    {
                        /*   DiscordEmbedBuilder failsuccessEmbedBuilder = new DiscordEmbedBuilder()
                           .WithTitle($"{user.UsernameWithDiscriminator} nicht gebannt")
                           .WithDescription($"Der User ``{user.UsernameWithDiscriminator} ({user.Id})`` konnte nicht gebannt werden!\n\n")
                           .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                           .WithColor(DiscordColor.Red);

                           DiscordEmbed failsuccessEmbed = failsuccessEmbedBuilder.Build();
                           DiscordMessageBuilder failSuccessMessage = new DiscordMessageBuilder()
                               .WithEmbed(failsuccessEmbed)
                               .WithReply(ctx.Message.Id, false);
                           await ctx.Channel.SendMessageAsync(failSuccessMessage); */
                        n_users += $"{user.UsernameWithDiscriminator}\n";
                        continue;
                    }


                    /* DiscordEmbedBuilder successEmbedBuilder = new DiscordEmbedBuilder()
                                                  .WithTitle($"{user.UsernameWithDiscriminator} wurde erfolgreich gebannt")
                        .WithDescription($"Der User ``{user.UsernameWithDiscriminator} ({user.Id})`` wurde erfolgreich gebannt!\n\n" +
                                              $"Grund: ``{reason}``\n\n" +
                                              $"User wurde über den ban benachrichtigt? {semoji}")
                        .WithFooter($"{GlobalProperties.ServerNameInitals} Moderation System")
                        .WithColor(DiscordColor.Green);
                    DiscordEmbed successEmbed = successEmbedBuilder.Build();
                    DiscordMessageBuilder successMessage = new DiscordMessageBuilder()
                        .WithEmbed(successEmbed)
                        .WithReply(ctx.Message.Id, false);
                    await ctx.Channel.SendMessageAsync(successMessage); */
                }
                string e_string;
                var ec = DiscordColor.Red;
                if (n_users != "")
                {
                    e_string = $"Der Multiban wurde mit Fehlern abgeschlossen.\n" +
                    $"__Grund:__ ```{reason}```\n" +
                    $"__Gebannte User:__\n" +
                    $"```{b_users}```";
                    e_string += $"__Nicht gebannte User:__\n" +
                    $"```{n_users}```";
                    ec = DiscordColor.Yellow;
                }
                else
                {
                    e_string = $"Der Multiban wurde erfolgreich abgeschlossen.\n" +
                    $"__Grund:__ ```{reason}```\n" +
                    $"__Gebannte User:__\n" +
                    $"```{b_users}```";
                    ec = DiscordColor.Green;
                }
                DiscordEmbedBuilder discordEmbedBuilder = new DiscordEmbedBuilder()
                    .WithTitle("Multiban abgeschlossen")
                    .WithDescription(e_string)
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithColor(ec);
                DiscordEmbed discordEmbed = discordEmbedBuilder.Build();
                await message.ModifyAsync(new DiscordMessageBuilder().WithEmbed(discordEmbed));
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
                    .WithDescription($"**Begründung:**```{reason}```\n" +
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
                catch (UnauthorizedException)
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
                                          $"User wurde über den ban benachrichtigt? {SentEmoji}")
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithColor(DiscordColor.Green);

                DiscordEmbed successEmbed = successEmbedBuilder.Build();
                DiscordMessageBuilder SuccessMessage = new DiscordMessageBuilder()
                    .WithEmbed(successEmbed)
                    .WithReply(ctx.Message.Id, false);

                await message.ModifyAsync(SuccessMessage);
            }
            else if (result.Result.Id == $"banrequest_deny_{caseid}")
            {
                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                buttons.ForEach(x => x.Disable());
                DiscordEmbedBuilder declineEmbedBuilder = new DiscordEmbedBuilder()
                    .WithTitle($"Bannanfrage abgebrochen")
                    .WithDescription($"Die Bannanfrage für {user} (``{user.Id}``) wurde abgebrochen.\n\n" + 
                                                                                   $"Grund: Ban wurde abgelehnt von `{result.Result.User.UsernameWithDiscriminator}`")
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

