using AGC_Management.Helpers;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Exceptions;
using DisCatSharp.Interactivity.Extensions;

namespace AGC_Management.Commands;

public class ModerationSystem : BaseCommandModule
{
    [Command("kick")]
    [RequireTeamCat]
    [RequirePermissions(Permissions.KickMembers)]
    public async Task KickMember(CommandContext ctx, DiscordMember user, [RemainingText] string reason)
    {
        if (await Helpers.Helpers.CheckForReason(ctx, reason)) return;
        if (await Helpers.Helpers.TicketUrlCheck(ctx, reason)) return;
        var caseid = Helpers.Helpers.GenerateCaseID();
        var embedBuilder = new DiscordEmbedBuilder()
            .WithTitle($"Du wurdest von {ctx.Guild.Name} gekickt!")
            .WithDescription($"**Begründung:**```{reason}```")
            .WithColor(DiscordColor.Red);

        var embed = embedBuilder.Build();
        bool sent;
        var ReasonString =
            $"Grund {reason} | Von Moderator: {ctx.User.UsernameWithDiscriminator} | Datum: {DateTime.Now:dd.MM.yyyy - HH:mm}";
        var interactivity = ctx.Client.GetInteractivity();
        var confirmEmbedBuilder = new DiscordEmbedBuilder()
            .WithTitle("Überprüfe deine Eingabe | Aktion: Kick")
            .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
            .WithDescription($"Bitte überprüfe deine Eingabe und bestätige mit ✅ um fortzufahren.\n\n" +
                             $"__Users:__\n" +
                             $"```{user.UsernameWithDiscriminator}```\n__Grund:__```{reason}```")
            .WithColor(GlobalProperties.EmbedColor);
        var embed__ = confirmEmbedBuilder.Build();
        List<DiscordButtonComponent> buttons = new(2)
        {
            new DiscordButtonComponent(ButtonStyle.Secondary, $"kick_accept_{caseid}", "✅"),
            new DiscordButtonComponent(ButtonStyle.Secondary, $"kick_deny_{caseid}", "❌")
        };
        var confirmMessage = new DiscordMessageBuilder()
            .WithEmbed(embed__).AddComponents(buttons).WithReply(ctx.Message.Id);
        var confirm = await ctx.Channel.SendMessageAsync(confirmMessage);
        var interaction = await interactivity.WaitForButtonAsync(confirm, ctx.User, TimeSpan.FromSeconds(60));
        buttons.ForEach(x => x.Disable());
        if (interaction.TimedOut)
        {
            var embed_ = new DiscordMessageBuilder()
                .WithEmbed(confirmEmbedBuilder.WithTitle("Kick abgebrochen")
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithDescription(
                        "Der Kick wurde abgebrochen.\n\nGrund: Zeitüberschreitung. <:counting_warning:962007085426556989>")
                    .WithColor(DiscordColor.Red).Build());
            await confirm.ModifyAsync(embed_);
            return;
        }

        if (interaction.Result.Id == $"kick_deny_{caseid}")
        {
            await interaction.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            var embed_ = new DiscordMessageBuilder()
                .WithEmbed(confirmEmbedBuilder.WithTitle("Kick abgebrochen")
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithDescription(
                        "Der Kick wurde abgebrochen.\n\nGrund: Abgebrochen. <:counting_warning:962007085426556989>")
                    .WithColor(DiscordColor.Red).Build());
            await confirm.ModifyAsync(embed_);
            return;
        }

        if (interaction.Result.Id == $"kick_accept_{caseid}")
        {
            await interaction.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            var loadingEmbedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Kick wird bearbeitet")
                .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                .WithDescription("Der Kick wird bearbeitet. Bitte warten...")
                .WithColor(DiscordColor.Yellow);
            var loadingEmbed = loadingEmbedBuilder.Build();
            var loadingMessage = new DiscordMessageBuilder()
                .WithEmbed(loadingEmbed).AddComponents(buttons)
                .WithReply(ctx.Message.Id);
            await confirm.ModifyAsync(loadingMessage);

            var b_users = "";
            var n_users = "";
            string e_string;
            var ec = DiscordColor.Red;
            DiscordMessage? umsg = null;
            try
            {
                umsg = await user.SendMessageAsync(embed);
                sent = true;
            }
            catch
            {
                sent = false;
            }

            var semoji = sent ? "<:yes:861266772665040917>" : "<:no:861266772724023296>";
            try
            {
                await user.RemoveAsync(ReasonString);
                var dm = sent ? "✅" : "❌";
                b_users += $"{user.UsernameWithDiscriminator} | DM: {dm}\n";
            }
            catch (UnauthorizedException)
            {
                n_users += $"{user.UsernameWithDiscriminator}\n";
            }

            if (n_users != "")
            {
                e_string = $"Der Kick war nicht erfolgreich.\n" +
                           $"__Grund:__ ```{reason}```\n";
                e_string += $"__Nicht gekickte User:__\n" +
                            $"```{n_users}```";
                ec = DiscordColor.Red;
                if (sent)
                    try
                    {
                        await umsg.DeleteAsync();
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
            }
            else
            {
                e_string = $"Der Kick wurde erfolgreich abgeschlossen.\n" +
                           $"__Grund:__ ```{reason}```\n" +
                           $"__Gekickte User:__\n" +
                           $"```{b_users}```";
                ec = DiscordColor.Green;
            }

            var discordEmbedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Kick abgeschlossen")
                .WithDescription(e_string)
                .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                .WithColor(ec);
            var discordEmbed = discordEmbedBuilder.Build();
            await confirm.ModifyAsync(new DiscordMessageBuilder().WithEmbed(discordEmbed));
        }
    }

    [Command("ban")]
    [RequirePermissions(Permissions.BanMembers)]
    public async Task BanMember(CommandContext ctx, DiscordUser user, [RemainingText] string reason)
    {
        if (await Helpers.Helpers.CheckForReason(ctx, reason)) return;
        if (await Helpers.Helpers.TicketUrlCheck(ctx, reason)) return;
        var caseid = Helpers.Helpers.GenerateCaseID();
        var embedBuilder = new DiscordEmbedBuilder()
            .WithTitle($"Du wurdest von {ctx.Guild.Name} gebannt!")
            .WithDescription($"**Begründung:**```{reason}```\n" +
                             $"**Du möchtest einen Entbannungsantrag stellen?**\n" +
                             $"Dann kannst du eine Entbannung beim [Entbannportal](https://unban.animegamingcafe.de) beantragen")
            .WithColor(DiscordColor.Red);

        var embed = embedBuilder.Build();
        bool sent;
        var ReasonString =
            $"Grund {reason} | Von Moderator: {ctx.User.UsernameWithDiscriminator} | Datum: {DateTime.Now:dd.MM.yyyy - HH:mm}";
        // abfrage
        var interactivity = ctx.Client.GetInteractivity();
        var confirmEmbedBuilder = new DiscordEmbedBuilder()
            .WithTitle("Überprüfe deine Eingabe | Aktion: Ban")
            .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
            .WithDescription($"Bitte überprüfe deine Eingabe und bestätige mit ✅ um fortzufahren.\n\n" +
                             $"__Users:__\n" +
                             $"```{user.UsernameWithDiscriminator}```\n__Grund:__```{reason}```")
            .WithColor(GlobalProperties.EmbedColor);
        var embed__ = confirmEmbedBuilder.Build();
        List<DiscordButtonComponent> buttons = new(2)
        {
            new DiscordButtonComponent(ButtonStyle.Secondary, $"ban_accept_{caseid}", "✅"),
            new DiscordButtonComponent(ButtonStyle.Secondary, $"ban_deny_{caseid}", "❌")
        };
        var confirmMessage = new DiscordMessageBuilder()
            .WithEmbed(embed__).AddComponents(buttons).WithReply(ctx.Message.Id);
        var confirm = await ctx.Channel.SendMessageAsync(confirmMessage);
        var interaction = await interactivity.WaitForButtonAsync(confirm, ctx.User, TimeSpan.FromSeconds(60));
        buttons.ForEach(x => x.Disable());
        if (interaction.TimedOut)
        {
            var embed_ = new DiscordMessageBuilder()
                .WithEmbed(confirmEmbedBuilder.WithTitle("Ban abgebrochen")
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithDescription(
                        "Der Ban wurde abgebrochen.\n\nGrund: Zeitüberschreitung. <:counting_warning:962007085426556989>")
                    .WithColor(DiscordColor.Red).Build());
            await confirm.ModifyAsync(embed_);
            return;
        }

        if (interaction.Result.Id == $"ban_deny_{caseid}")
        {
            await interaction.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            var embed_ = new DiscordMessageBuilder()
                .WithEmbed(confirmEmbedBuilder.WithTitle("Ban abgebrochen")
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithDescription(
                        "Der Ban wurde abgebrochen.\n\nGrund: Abgebrochen. <:counting_warning:962007085426556989>")
                    .WithColor(DiscordColor.Red).Build());
            await confirm.ModifyAsync(embed_);
            return;
        }

        if (interaction.Result.Id == $"ban_accept_{caseid}")
        {
            await interaction.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            var loadingEmbedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Ban wird bearbeitet")
                .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                .WithDescription("Der Ban wird bearbeitet. Bitte warten...")
                .WithColor(DiscordColor.Yellow);
            var loadingEmbed = loadingEmbedBuilder.Build();
            var loadingMessage = new DiscordMessageBuilder()
                .WithEmbed(loadingEmbed).AddComponents(buttons)
                .WithReply(ctx.Message.Id);
            await confirm.ModifyAsync(loadingMessage);

            var b_users = "";
            var n_users = "";
            string e_string;
            var ec = DiscordColor.Red;
            DiscordMessage? umsg = null;
            try
            {
                umsg = await user.SendMessageAsync(embed);
                sent = true;
            }
            catch
            {
                sent = false;
            }

            var semoji = sent ? "<:yes:861266772665040917>" : "<:no:861266772724023296>";
            try
            {
                await ctx.Guild.BanMemberAsync(user.Id, 7, ReasonString);
                var dm = sent ? "✅" : "❌";
                b_users += $"{user.UsernameWithDiscriminator} | DM: {dm}\n";
            }
            catch (UnauthorizedException)
            {
                n_users += $"{user.UsernameWithDiscriminator}\n";
            }

            if (n_users != "")
            {
                e_string = $"Der Ban war nicht erfolgreich.\n" +
                           $"__Grund:__ ```{reason}```\n";
                e_string += $"__Nicht gebannte User:__\n" +
                            $"```{n_users}```";
                ec = DiscordColor.Red;
                if (sent)
                    try
                    {
                        await umsg.DeleteAsync();
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
            }
            else
            {
                e_string = $"Der Ban wurde erfolgreich abgeschlossen.\n" +
                           $"__Grund:__ ```{reason}```\n" +
                           $"__Gebannte User:__\n" +
                           $"```{b_users}```";
                ec = DiscordColor.Green;
            }

            var discordEmbedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Ban abgeschlossen")
                .WithDescription(e_string)
                .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                .WithColor(ec);
            var discordEmbed = discordEmbedBuilder.Build();
            await confirm.ModifyAsync(new DiscordMessageBuilder().WithEmbed(discordEmbed));
        }
    }


    [Command("multiban")]
    [RequirePermissions(Permissions.BanMembers)]
    public async Task MultiBan(CommandContext ctx, [RemainingText] string ids_and_reason)
    {
        List<ulong> ids;
        string reason;
        Converter.SeperateIdsAndReason(ids_and_reason, out ids, out reason);
        if (await Helpers.Helpers.CheckForReason(ctx, reason)) return;
        if (await Helpers.Helpers.TicketUrlCheck(ctx, reason)) return;
        reason = reason.TrimEnd(' ');
        var users_to_ban = new List<DiscordUser>();
        var reasonString =
            $"Grund: {reason} | Von Moderator: {ctx.User.UsernameWithDiscriminator} | Datum: {DateTime.Now:dd.MM.yyyy - HH:mm}";
        var setids = ids.ToHashSet().ToList();
        if (setids.Count < 2)
        {
            var failsuccessEmbedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Fehler")
                .WithDescription("Du musst mindestens 2 User angeben!")
                .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                .WithColor(DiscordColor.Red);
            var failsuccessEmbed = failsuccessEmbedBuilder.Build();
            var failSuccessMessage = new DiscordMessageBuilder()
                .WithEmbed(failsuccessEmbed)
                .WithReply(ctx.Message.Id);
            await ctx.Channel.SendMessageAsync(failSuccessMessage);
            return;
        }

        foreach (var id in setids)
        {
            var user = await ctx.Client.TryGetUserAsync(id);
            if (user != null) users_to_ban.Add(user);
        }

        var busers_formatted = string.Join("\n", users_to_ban.Select(buser => buser.UsernameWithDiscriminator));
        var caseid = Helpers.Helpers.GenerateCaseID();
        var confirmEmbedBuilder = new DiscordEmbedBuilder()
            .WithTitle("Überprüfe deine Eingabe | Aktion: Multiban")
            .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
            .WithDescription($"Bitte überprüfe deine Eingabe und bestätige mit ✅ um fortzufahren.\n\n" +
                             $"__Users:__\n" +
                             $"```{busers_formatted}```\n__Grund:__```{reason}```")
            .WithColor(GlobalProperties.EmbedColor);
        var confirmEmbed = confirmEmbedBuilder.Build();

        var embedBuilder = new DiscordEmbedBuilder()
            .WithTitle($"Du wurdest von {ctx.Guild.Name} gebannt!")
            .WithDescription($"**Begründung:**```{reason}```\n" +
                             $"**Du möchtest einen Entbannungsantrag stellen?**\n" +
                             $"Dann kannst du eine Entbannung beim [Entbannportal](https://unban.animegamingcafe.de) beantragen")
            .WithColor(DiscordColor.Red);
        var UserEmbed = embedBuilder.Build();
        List<DiscordButtonComponent> buttons = new(2)
        {
            new DiscordButtonComponent(ButtonStyle.Secondary, $"multiban_accept_{caseid}", "✅"),
            new DiscordButtonComponent(ButtonStyle.Secondary, $"multiban_deny_{caseid}", "❌")
        };
        var builder = new DiscordMessageBuilder()
            .WithEmbed(confirmEmbed)
            .AddComponents(buttons)
            .WithReply(ctx.Message.Id);
        var interactivity = ctx.Client.GetInteractivity();
        var message = await ctx.Channel.SendMessageAsync(builder);

        var result = await interactivity.WaitForButtonAsync(message, ctx.User, TimeSpan.FromSeconds(10));
        if (result.TimedOut)
        {
            var embed_ = new DiscordMessageBuilder()
                .WithEmbed(confirmEmbedBuilder.WithTitle("Multiban abgebrochen")
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithDescription(
                        "Der Multiban wurde abgebrochen.\n\nGrund: Zeitüberschreitung. <:counting_warning:962007085426556989>")
                    .WithColor(DiscordColor.Red).Build());
            await message.ModifyAsync(embed_);
            return;
        }

        if (result.Result.Id == $"multiban_deny_{caseid}")
        {
            await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            var embed_ = new DiscordMessageBuilder()
                .WithEmbed(confirmEmbedBuilder.WithTitle("Multiban abgebrochen")
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithDescription(
                        "Der Multiban wurde abgebrochen.\n\nGrund: Abgebrochen. <:counting_warning:962007085426556989>")
                    .WithColor(DiscordColor.Red).Build());
            await message.ModifyAsync(embed_);
            return;
        }

        if (result.Result.Id == $"multiban_accept_{caseid}")
        {
            var disbtn = buttons;
            await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            disbtn.ForEach(x => x.Disable());
            var loadingEmbedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Multiban wird bearbeitet")
                .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                .WithDescription("Der Multiban wird bearbeitet. Bitte warten...")
                .WithColor(DiscordColor.Yellow);
            var loadingEmbed = loadingEmbedBuilder.Build();
            var loadingMessage = new DiscordMessageBuilder()
                .WithEmbed(loadingEmbed).AddComponents(disbtn)
                .WithReply(ctx.Message.Id);
            await message.ModifyAsync(loadingMessage);


            var b_users = "";
            var n_users = "";
            foreach (var user in users_to_ban)
            {
                var sent = false;
                try
                {
                    await user.SendMessageAsync(UserEmbed);
                    sent = true;
                }
                catch
                {
                    sent = false;
                }

                var semoji = sent ? "<:yes:861266772665040917>" : "<:no:861266772724023296>";
                try
                {
                    await ctx.Guild.BanMemberAsync(user.Id, 7, reasonString);
                    var dm = sent ? "✅" : "❌";
                    b_users += $"{user.UsernameWithDiscriminator} | DM: {dm}\n";
                }
                catch (UnauthorizedException)
                {
                    n_users += $"{user.UsernameWithDiscriminator}\n";
                }
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

            var discordEmbedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Multiban abgeschlossen")
                .WithDescription(e_string)
                .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                .WithColor(ec);
            var discordEmbed = discordEmbedBuilder.Build();
            await message.ModifyAsync(new DiscordMessageBuilder().WithEmbed(discordEmbed));
        }
    }


    [Command("multibanrequest")]
    [Aliases("multibanreq")]
    [RequireStaffRole]
    public async Task MultiBanRequest(CommandContext ctx, [RemainingText] string ids_and_reason)
    {
        List<ulong> ids;
        string reason;
        Converter.SeperateIdsAndReason(ids_and_reason, out ids, out reason);
        if (await Helpers.Helpers.CheckForReason(ctx, reason)) return;
        if (await Helpers.Helpers.TicketUrlCheck(ctx, reason)) return;
        reason = reason.TrimEnd(' ');
        var users_to_ban = new List<DiscordUser>();
        var reasonString =
            $"Grund: {reason} | Von Moderator: {ctx.User.UsernameWithDiscriminator} | Datum: {DateTime.Now:dd.MM.yyyy - HH:mm}";
        var setids = ids.ToHashSet().ToList();
        if (setids.Count < 2)
        {
            var failsuccessEmbedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Fehler")
                .WithDescription("Du musst mindestens 2 User angeben!")
                .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                .WithColor(DiscordColor.Red);
            var failsuccessEmbed = failsuccessEmbedBuilder.Build();
            var failSuccessMessage = new DiscordMessageBuilder()
                .WithEmbed(failsuccessEmbed)
                .WithReply(ctx.Message.Id);
            await ctx.Channel.SendMessageAsync(failSuccessMessage);
            return;
        }

        foreach (var id in setids)
        {
            var user = await ctx.Client.TryGetUserAsync(id);
            if (user != null) users_to_ban.Add(user);
        }

        var busers_formatted = string.Join("\n", users_to_ban.Select(buser => buser.UsernameWithDiscriminator));
        var caseid = Helpers.Helpers.GenerateCaseID();
        var confirmEmbedBuilder = new DiscordEmbedBuilder()
            .WithTitle("Überprüfe deine Eingabe | Aktion: Multibanrequest")
            .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
            .WithDescription($"Bitte überprüfe deine Eingabe und bestätige mit ✅ um fortzufahren.\n\n" +
                             $"__Users:__\n" +
                             $"```{busers_formatted}```\n__Grund:__```{reason}```")
            .WithColor(GlobalProperties.EmbedColor);
        var embed = confirmEmbedBuilder.Build();
        List<DiscordButtonComponent> buttons = new(2)
        {
            new DiscordButtonComponent(ButtonStyle.Success, $"multibanrequest_accept_{caseid}", "Bestätigen"),
            new DiscordButtonComponent(ButtonStyle.Danger, $"multibanrequest_deny_{caseid}", "Abbrechen")
        };
        var messageBuilder = new DiscordMessageBuilder()
            .WithEmbed(embed)
            .WithReply(ctx.Message.Id)
            .AddComponents(buttons);
        var message = await ctx.Channel.SendMessageAsync(messageBuilder);
        var Interactivity = ctx.Client.GetInteractivity();
        var result = await Interactivity.WaitForButtonAsync(message, ctx.User, TimeSpan.FromMinutes(5));
        buttons.ForEach(x => x.Disable());
        if (result.TimedOut)
        {
            var timeoutEmbedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Timeout")
                .WithDescription("Du hast zu lange gebraucht um zu antworten.")
                .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                .WithColor(DiscordColor.Red);
            var timeoutEmbed = timeoutEmbedBuilder.Build();
            var timeoutMessage = new DiscordMessageBuilder()
                .WithEmbed(timeoutEmbed).AddComponents(buttons)
                .WithReply(ctx.Message.Id);
            await message.ModifyAsync(timeoutMessage);
            return;
        }

        // handle if you cancel request
        if (result.Result.Id == $"multibanrequest_deny_{caseid}")
        {
            await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            var denyEmbedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Anfrage abgebrochen")
                .WithDescription("Du hast deinen Multibanrequest abgebrochen.")
                .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                .WithColor(DiscordColor.Red);
            var denyEmbed = denyEmbedBuilder.Build();
            var denyMessage = new DiscordMessageBuilder()
                .WithEmbed(denyEmbed).AddComponents(buttons)
                .WithReply(ctx.Message.Id);
            await message.ModifyAsync(denyMessage);
            return;
        }

        // handle if you accept 
        if (result.Result.Id == $"multibanrequest_accept_{caseid}")
        {
            await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            var now = DateTime.Now.ToString("dd.MM.yyyy - HH:mm");
            var banEmbedBuilder = new DiscordEmbedBuilder()
                .WithTitle($"Du wurdest von {ctx.Guild.Name} gebannt!").WithColor(DiscordColor.Red)
                .WithDescription($"**Begründung:**```{reason}```\n" +
                                 $"**Du möchtest einen Entbannungsantrag stellen?**\n" +
                                 $"Dann kannst du eine Entbannung beim [Entbannportal](https://unban.animegamingcafe.de) beantragen");
            var banEmbed = banEmbedBuilder.Build();
            Console.WriteLine($"[BAN] {ctx.User.UsernameWithDiscriminator} banned {busers_formatted} for {reason}");
            var staffrole = ctx.Guild.GetRole(ulong.Parse(GlobalProperties.ConfigIni["ServerConfig"]["StaffRoleId"]));
            Console.WriteLine(staffrole.Id);
            var staffmembers = ctx.Guild.Members
                .Where(x => x.Value.Roles.Any(y => y.Id == GlobalProperties.StaffRoleId))
                .Select(x => x.Value)
                .ToList();
            var staffWithBanPerms =
                staffmembers.Where(x => x.Permissions.HasPermission(Permissions.BanMembers)).ToList();
            var onlineStaffWithBanPerms = staffWithBanPerms
                .Where(member => (member.Presence?.Status ?? UserStatus.Offline) != UserStatus.Offline).ToList();
            Console.WriteLine($"[BAN] {ctx.User.UsernameWithDiscriminator} banned {busers_formatted} for {reason}");
            var embedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Bannanfrage")
                .WithDescription($"Ban-Anfrage für mehrere Benutzer: {busers_formatted}\n" +
                                 $"```{busers_formatted}```\n__Grund:__```{reason}```" +
                                 $"Bitte warte, während diese Anfrage von jemandem mit Bannberechtigung bestätigt wird <a:loading_agc:1084157150747697203>")
                .WithColor(GlobalProperties.EmbedColor)
                .WithFooter($"{ctx.User.UsernameWithDiscriminator}");

            string staffMentionString;
            if (onlineStaffWithBanPerms.Count > 0)
            {
                if (!GlobalProperties.DebugMode)
                    staffMentionString = string.Join(" ", onlineStaffWithBanPerms
                        .Where(member => member.Id != 441192596325531648)
                        .Select(member => member.Mention));
                else
                    staffMentionString = "DEBUG MODE AKTIV | Kein Ping wird ausgeführt";
            }
            else
            {
                if (!GlobalProperties.DebugMode)
                    staffMentionString =
                        $"Kein Moderator online | <@&{GlobalProperties.ConfigIni["ServerConfig"]["AdminRoleId"]}> | <@&{GlobalProperties.ConfigIni["ServerConfig"]["ModRoleId"]}>";
                else
                    staffMentionString = "Kein Moderator online | DEBUG MODE AKTIV";
            }

            var embed_ = embedBuilder.Build();
            List<DiscordButtonComponent> staffbuttons = new(2)
            {
                new DiscordButtonComponent(ButtonStyle.Success, $"modbanrequest_accept_{caseid}", "Annehmen"),
                new DiscordButtonComponent(ButtonStyle.Danger, $"modbanrequest_deny_{caseid}", "Ablehnen")
            };
            // enable buttons
            staffbuttons.ForEach(x => x.Enable());

            var builder = new DiscordMessageBuilder()
                .WithEmbed(embed_)
                .AddComponents(staffbuttons)
                .WithContent(staffMentionString)
                .WithReply(ctx.Message.Id);

            var interactivity = ctx.Client.GetInteractivity();
            await message.ModifyAsync(builder);

            var pingmsg = await ctx.Channel.SendMessageAsync(staffMentionString);
            await pingmsg.DeleteAsync();

            var staffresult = await interactivity.WaitForButtonAsync(message, interaction =>
            {
                if (interaction.User is DiscordMember guildUser)
                    return guildUser.Permissions.HasPermission(Permissions.BanMembers);

                return false;
            }, TimeSpan.FromSeconds(6));
            staffbuttons.ForEach(x => x.Disable());
            if (staffresult.TimedOut)
            {
                var denyEmbedBuilder = new DiscordEmbedBuilder()
                    .WithTitle("Anfrage abgebrochen")
                    .WithDescription(
                        "Deine Anfrage wurde abgebrochen, da sie nicht innerhalb von 6 Stunden bestätigt wurde.")
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithColor(DiscordColor.Red);
                var denyEmbed = denyEmbedBuilder.Build();
                var denyMessage = new DiscordMessageBuilder()
                    .WithEmbed(denyEmbed)
                    .WithReply(ctx.Message.Id);
                await message.ModifyAsync(denyMessage);
                return;
            }

            if (staffresult.Result.Id == $"modbanrequest_deny_{caseid}")
            {
                var denyEmbedBuilder = new DiscordEmbedBuilder()
                    .WithTitle("Anfrage abgelehnt")
                    .WithDescription(
                        $"Deine Anfrage wurde von ``{staffresult.Result.User.UsernameWithDiscriminator}`` abgelehnt.")
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithColor(DiscordColor.Red);
                var denyEmbed = denyEmbedBuilder.Build();
                var denyMessage = new DiscordMessageBuilder()
                    .WithEmbed(denyEmbed)
                    .WithReply(ctx.Message.Id);
                await message.ModifyAsync(denyMessage);
                return;
            }

            if (staffresult.Result.Id == $"modbanrequest_accept_{caseid}")
            {
                await staffresult.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                Console.WriteLine("1");
                var disbtn = buttons;
                Console.WriteLine("223");

                // disbtn.ForEach(x => x.Disable());
                Console.WriteLine("22");
                var loadingEmbedBuilder = new DiscordEmbedBuilder()
                    .WithTitle("Multiban wird bearbeitet")
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithDescription("Der Multiban wird bearbeitet. Bitte warten...")
                    .WithColor(DiscordColor.Yellow);
                var loadingEmbed = loadingEmbedBuilder.Build();
                var loadingMessage = new DiscordMessageBuilder()
                    .WithEmbed(loadingEmbed).AddComponents(disbtn)
                    .WithReply(ctx.Message.Id);
                await message.ModifyAsync(loadingMessage);


                var b_users = "";
                var n_users = "";
                foreach (var user in users_to_ban)
                {
                    Console.WriteLine("");
                    var sent = false;
                    try
                    {
                        await user.SendMessageAsync(banEmbed);
                        sent = true;
                    }
                    catch
                    {
                        sent = false;
                    }

                    var semoji = sent ? "<:yes:861266772665040917>" : "<:no:861266772724023296>";
                    try
                    {
                        await ctx.Guild.BanMemberAsync(user.Id, 7, reasonString);
                        var dm = sent ? "✅" : "❌";
                        b_users += $"{user.UsernameWithDiscriminator} | DM: {dm}\n";
                    }
                    catch (UnauthorizedException)
                    {
                        n_users += $"{user.UsernameWithDiscriminator}\n";
                    }
                }

                string e_string;
                var ec = DiscordColor.Red;
                if (n_users != "")
                {
                    e_string = $"Der Multibanrequest wurde mit Fehlern abgeschlossen.\n" +
                               $"__Grund:__ ```{reason}```\n" +
                               $"__Gebannte User:__\n" +
                               $"```{b_users}```";
                    e_string += $"__Nicht gebannte User:__\n" +
                                $"```{n_users}```\n" +
                                $"Bestätigt von {staffresult.Result.User.UsernameWithDiscriminator}";
                    ec = DiscordColor.Yellow;
                }
                else
                {
                    e_string = $"Der Multibanrequest wurde erfolgreich abgeschlossen.\n" +
                               $"__Grund:__ ```{reason}```\n" +
                               $"__Gebannte User:__\n" +
                               $"```{b_users}```\n" +
                               $"Bestätigt von {staffresult.Result.User.UsernameWithDiscriminator}";
                    ec = DiscordColor.Green;
                }

                var discordEmbedBuilder = new DiscordEmbedBuilder()
                    .WithTitle("Multibanrequest abgeschlossen")
                    .WithDescription(e_string)
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithColor(ec);
                var discordEmbed = discordEmbedBuilder.Build();
                await message.ModifyAsync(new DiscordMessageBuilder().WithEmbed(discordEmbed));
            }
        }
    }


    [Command("banrequest")]
    [Aliases("banreq")]
    [RequireStaffRole]
    public async Task BanRequest(CommandContext ctx, DiscordUser user, [RemainingText] string reason)
    {
        if (await Helpers.Helpers.CheckForReason(ctx, reason)) return;
        if (await Helpers.Helpers.TicketUrlCheck(ctx, reason)) return;
        var caseid = Helpers.Helpers.GenerateCaseID();
        Console.WriteLine(
            $"[BAN] {ctx.User.UsernameWithDiscriminator} banned {user.UsernameWithDiscriminator} for {reason}");
        var staffrole = ctx.Guild.GetRole(ulong.Parse(GlobalProperties.ConfigIni["ServerConfig"]["StaffRoleId"]));
        var staffmembers = ctx.Guild.Members
            .Where(x => x.Value.Roles.Any(y => y.Id == GlobalProperties.StaffRoleId))
            .Select(x => x.Value)
            .ToList();
        var staffWithBanPerms = staffmembers.Where(x => x.Permissions.HasPermission(Permissions.BanMembers)).ToList();
        var onlineStaffWithBanPerms = staffWithBanPerms
            .Where(member => (member.Presence?.Status ?? UserStatus.Offline) != UserStatus.Offline).ToList();
        var embedBuilder = new DiscordEmbedBuilder()
            .WithTitle("Bannanfrage")
            .WithDescription($"Ban-Anfrage für Benutzer: ``{user.UsernameWithDiscriminator}`` ``({user.Id})``\n" +
                             $"Banngrund:\n```\n{reason}\n```\n" +
                             $"Bitte warte, während diese Anfrage von jemandem mit Bannberechtigung bestätigt wird <a:loading_agc:1084157150747697203>")
            .WithColor(GlobalProperties.EmbedColor)
            .WithFooter($"{ctx.User.UsernameWithDiscriminator}");
        var interactivity_ = ctx.Client.GetInteractivity();
        var confirmEmbedBuilder = new DiscordEmbedBuilder()
            .WithTitle("Überprüfe deine Eingabe | Aktion: Banrequest")
            .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
            .WithDescription($"Bitte überprüfe deine Eingabe und bestätige mit ✅ um fortzufahren.\n\n" +
                             $"__Users:__\n" +
                             $"```{user.UsernameWithDiscriminator}```\n__Grund:__```{reason}```")
            .WithColor(GlobalProperties.EmbedColor);
        var embed__ = confirmEmbedBuilder.Build();
        List<DiscordButtonComponent> buttons_ = new(2)
        {
            new DiscordButtonComponent(ButtonStyle.Secondary, $"br_accept_{caseid}", "✅"),
            new DiscordButtonComponent(ButtonStyle.Secondary, $"br_deny_{caseid}", "❌")
        };
        var confirmMessage = new DiscordMessageBuilder()
            .WithEmbed(embed__).AddComponents(buttons_).WithReply(ctx.Message.Id);
        var confirm = await ctx.Channel.SendMessageAsync(confirmMessage);
        var interaction = await interactivity_.WaitForButtonAsync(confirm, ctx.User, TimeSpan.FromSeconds(60));
        buttons_.ForEach(x => x.Disable());
        if (interaction.TimedOut)
        {
            var embed_ = new DiscordMessageBuilder()
                .WithEmbed(confirmEmbedBuilder.WithTitle("Banrequest abgebrochen")
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithDescription(
                        "Der Banrequest wurde abgebrochen.\n\nGrund: Zeitüberschreitung. <:counting_warning:962007085426556989>")
                    .WithColor(DiscordColor.Red).Build());
            await confirm.ModifyAsync(embed_);
            return;
        }

        if (interaction.Result.Id == $"br_deny_{caseid}")
        {
            await interaction.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            var sembed_ = new DiscordMessageBuilder()
                .WithEmbed(confirmEmbedBuilder.WithTitle("Banrequest abgebrochen")
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithDescription(
                        "Der Banrequest wurde abgebrochen.\n\nGrund: Abgebrochen. <:counting_warning:962007085426556989>")
                    .WithColor(DiscordColor.Red).Build());
            await confirm.ModifyAsync(sembed_);
            return;
        }

        if (interaction.Result.Id == $"br_accept_{caseid}")
        {
            await interaction.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            string staffMentionString;
            if (onlineStaffWithBanPerms.Count > 0)
            {
                if (!GlobalProperties.DebugMode)
                    staffMentionString = string.Join(" ", onlineStaffWithBanPerms
                        .Where(member => member.Id != 441192596325531648)
                        .Select(member => member.Mention));
                else
                    staffMentionString = "DEBUG MODE AKTIV | Kein Ping wird ausgeführt";
            }
            else
            {
                if (!GlobalProperties.DebugMode)
                    staffMentionString =
                        $"Kein Moderator online | <@&{GlobalProperties.ConfigIni["ServerConfig"]["AdminRoleId"]}> | <@&{GlobalProperties.ConfigIni["ServerConfig"]["ModRoleId"]}>";
                else
                    staffMentionString = "Kein Moderator online | DEBUG MODE AKTIV";
            }

            var embed = embedBuilder.Build();
            List<DiscordButtonComponent> buttons = new(2)
            {
                new DiscordButtonComponent(ButtonStyle.Success, $"banrequest_accept_{caseid}", "Annehmen"),
                new DiscordButtonComponent(ButtonStyle.Danger, $"banrequest_deny_{caseid}", "Ablehnen")
            };

            var builder = new DiscordMessageBuilder()
                .WithEmbed(embed)
                .AddComponents(buttons)
                .WithContent(staffMentionString)
                .WithReply(ctx.Message.Id);

            var interactivity = ctx.Client.GetInteractivity();
            var message = await confirm.ModifyAsync(builder);
            buttons.ForEach(x => x.Disable());
            var result = await interactivity.WaitForButtonAsync(message, interaction =>
            {
                if (interaction.User is DiscordMember guildUser)
                    return guildUser.Permissions.HasPermission(Permissions.BanMembers);

                return false;
            }, TimeSpan.FromHours(6));

            if (result.TimedOut)
            {
                var embed_ = new DiscordMessageBuilder()
                    .WithEmbed(embedBuilder.WithTitle("Bannanfrage abgebrochen")
                        .WithDescription(
                            $"Die Bannanfrage für {user} (``{user.Id}``) wurde abgebrochen.\n\nGrund: Zeitüberschreitung. <:counting_warning:962007085426556989>")
                        .WithColor(DiscordColor.Red).Build());
                await message.ModifyAsync(embed_);
                return;
            }

            if (result.Result.Id == $"banrequest_accept_{caseid}")
            {
                var now = DateTime.Now.ToString("dd.MM.yyyy - HH:mm");
                var banEmbedBuilder = new DiscordEmbedBuilder()
                    .WithTitle($"Du wurdest von {ctx.Guild.Name} gebannt!")
                    .WithDescription($"**Begründung:**```{reason}```\n" +
                                     $"**Du möchtest einen Entbannungsantrag stellen?**\n" +
                                     $"Dann kannst du eine Entbannung beim [Entbannportal](https://unban.animegamingcafe.de) beantragen")
                    .WithColor(DiscordColor.Red);

                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                var loadingEmbedBuilder = new DiscordEmbedBuilder()
                    .WithTitle("Ban wird bearbeitet")
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithDescription("Der Ban wird bearbeitet. Bitte warten...")
                    .WithColor(DiscordColor.Yellow);
                var loadingEmbed = loadingEmbedBuilder.Build();
                var loadingMessage = new DiscordMessageBuilder()
                    .WithEmbed(loadingEmbed).AddComponents(buttons)
                    .WithReply(ctx.Message.Id);
                await confirm.ModifyAsync(loadingMessage);

                var b_users = "";
                var n_users = "";
                string e_string;
                bool sent;
                var ReasonString =
                    $"Grund {reason} | Von Moderator: {ctx.User.UsernameWithDiscriminator} | Datum: {DateTime.Now:dd.MM.yyyy - HH:mm}";
                var ec = DiscordColor.Red;
                DiscordMessage? umsg = null;
                try
                {
                    umsg = await user.SendMessageAsync(embed);
                    sent = true;
                }
                catch
                {
                    sent = false;
                }

                var semoji = sent ? "<:yes:861266772665040917>" : "<:no:861266772724023296>";
                try
                {
                    await ctx.Guild.BanMemberAsync(user.Id, 7, ReasonString);
                    var dm = sent ? "✅" : "❌";
                    b_users += $"{user.UsernameWithDiscriminator} | DM: {dm}\n";
                }
                catch (UnauthorizedException)
                {
                    n_users += $"{user.UsernameWithDiscriminator}\n";
                }

                if (n_users != "")
                {
                    e_string = $"Der Ban war nicht erfolgreich!\n" +
                               $"Bestätigt von ``{result.Result.User.UsernameWithDiscriminator}``\n\n" +
                               $"__Grund:__ ```{reason}```\n";
                    e_string += $"__Nicht gebannte User:__\n" +
                                $"```{n_users}```";
                    ec = DiscordColor.Red;
                    if (sent)
                        try
                        {
                            await umsg.DeleteAsync();
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                }
                else
                {
                    e_string = $"Der Ban wurde erfolgreich abgeschlossen.\n" +
                               $"Bestätigt von ``{result.Result.User.UsernameWithDiscriminator}``\n\n" +
                               $"__Grund:__ ```{reason}```\n" +
                               $"__Gebannte User:__\n" +
                               $"```{b_users}```";
                    ec = DiscordColor.Green;
                }

                var discordEmbedBuilder = new DiscordEmbedBuilder()
                    .WithTitle("Ban abgeschlossen")
                    .WithDescription(e_string)
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithColor(ec);
                var discordEmbed = discordEmbedBuilder.Build();
                await confirm.ModifyAsync(new DiscordMessageBuilder().WithEmbed(discordEmbed));
            }
            else if (result.Result.Id == $"banrequest_deny_{caseid}")
            {
                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                buttons.ForEach(x => x.Disable());
                var declineEmbedBuilder = new DiscordEmbedBuilder()
                    .WithTitle("Bannanfrage abgebrochen")
                    .WithDescription(
                        $"Die Bannanfrage für {user.UsernameWithDiscriminator} (``{user.Id}``) wurde abgebrochen.\n\n" +
                        $"Grund: Ban wurde abgelehnt von `{result.Result.User.UsernameWithDiscriminator}`")
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithColor(DiscordColor.Red);

                var declineEmbed = declineEmbedBuilder.Build();
                var DeclineMessage = new DiscordMessageBuilder()
                    .WithEmbed(declineEmbed)
                    .WithReply(ctx.Message.Id);
                await message.ModifyAsync(DeclineMessage);
            }
        }
    }
}