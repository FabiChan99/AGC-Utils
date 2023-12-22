#region

using AGC_Management.Attributes;
using AGC_Management.Services;
using AGC_Management.Utils;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Interactivity.Extensions;

#endregion

namespace AGC_Management.Commands.Moderation;

public sealed class MultiWarnCommand : BaseCommandModule
{
    // multiwarn, also wie multiflag und warn zusammen

    [Command("multiwarn")]
    [Description("Warnt mehrere Nutzer")]
    [RequireDatabase]
    [RequireStaffRole]
    [RequireTeamCat]
    public async Task MultiWarnUser(CommandContext ctx, [RemainingText] string ids_and_reason)
    {
        List<ulong> ids;
        string reason;
        Converter.SeperateIdsAndReason(ids_and_reason, out ids, out reason);
        if (await Helpers.CheckForReason(ctx, reason)) return;
        reason = reason.TrimEnd(' ');
        reason = await ReasonTemplateResolver.Resolve(reason);
        var users_to_warn = new List<DiscordUser>();
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
            if (user != null) users_to_warn.Add(user);
        }

        var busers_formatted = string.Join("\n", users_to_warn.Select(buser => buser.UsernameWithDiscriminator));
        var caseid = Helpers.GenerateCaseID();
        var confirmEmbedBuilder = new DiscordEmbedBuilder()
            .WithTitle("Überprüfe deine Eingabe | Aktion: MultiWarn")
            .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
            .WithDescription($"Bitte überprüfe deine Eingabe und bestätige mit ✅ um fortzufahren.\n\n" +
                             $"__Users:__\n" +
                             $"```{busers_formatted}```\n__Grund:__```{reason}```")
            .WithColor(BotConfig.GetEmbedColor());
        var embed = confirmEmbedBuilder.Build();
        List<DiscordButtonComponent> buttons = new(2)
        {
            new DiscordButtonComponent(ButtonStyle.Success, $"multiwarn_accept_{caseid}", "Bestätigen"),
            new DiscordButtonComponent(ButtonStyle.Danger, $"multiwarn_deny_{caseid}", "Abbrechen")
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

        if (result.Result.Id == $"multiwarn_deny_{caseid}")
        {
            await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            var loadingEmbedBuilder = new DiscordEmbedBuilder()
                .WithTitle("MultiWarn abgebrochen")
                .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                .WithDescription("Der MultiWarn wurde abgebrochen.")
                .WithColor(DiscordColor.Red);
            var loadingEmbed = loadingEmbedBuilder.Build();
            var loadingMessage = new DiscordMessageBuilder()
                .WithEmbed(loadingEmbed)
                .WithReply(ctx.Message.Id);
            await message.ModifyAsync(loadingMessage);
            return;
        }

        if (result.Result.Id == $"multiwarn_accept_{caseid}")
        {
            var disbtn = buttons;
            await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            disbtn.ForEach(x => x.Disable());
            var loadingEmbedBuilder = new DiscordEmbedBuilder()
                .WithTitle("MultiWarn wird bearbeitet")
                .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                .WithDescription("Der MultiWarn wird bearbeitet. Bitte warten...")
                .WithColor(DiscordColor.Yellow);
            var loadingEmbed = loadingEmbedBuilder.Build();
            var loadingMessage = new DiscordMessageBuilder()
                .WithEmbed(loadingEmbed).AddComponents(disbtn)
                .WithReply(ctx.Message.Id);
            await message.ModifyAsync(loadingMessage);
            string for_str = "";
            List<DiscordUser> users_to_warn_obj = new();
            foreach (var id in setids)
            {
                var user = await ctx.Client.GetUserAsync(id);
                if (user != null) users_to_warn_obj.Add(user);
            }

            foreach (var user in users_to_warn_obj)
            {
                var caseid_ = Helpers.GenerateCaseID();
                caseid_ = $"{caseid}-{caseid_}";
                Dictionary<string, object> data = new()
                {
                    { "userid", (long)user.Id },
                    { "punisherid", (long)ctx.User.Id },
                    { "datum", DateTimeOffset.Now.ToUnixTimeSeconds() },
                    { "description", reason },
                    { "caseid", caseid_ },
                    { "perma", false }
                };
                await DatabaseService.InsertDataIntoTable("warns", data);
                var warnlist = new List<dynamic>();

                List<string> selectedWarns = new()
                {
                    "*"
                };
                Dictionary<string, object> whereConditions = new()
                {
                    { "userid", (long)user.Id }
                };


                List<Dictionary<string, object>> results =
                    await DatabaseService.SelectDataFromTable("warns", selectedWarns, whereConditions);
                foreach (var lresult in results) warnlist.Add(lresult);
                var warncount = warnlist.Count;

                DiscordEmbed uembed =
                    await ModerationHelper.GenerateWarnEmbed(ctx, user, ctx.User, warncount, caseid, true, reason);
                string reasonString =
                    $"{warncount}. Verwarnung: {reason} | By Moderator: {ctx.User.UsernameWithDiscriminator} | Datum: {DateTime.Now:dd.MM.yyyy - HH:mm}";
                bool sent;
                try
                {
                    await user.SendMessageAsync(uembed);
                    sent = true;
                }
                catch (Exception)
                {
                    sent = false;
                }

                if (!sent)
                {
                    await Helpers.SendWarnAsChannel(ctx, user, uembed, caseid);
                }

                var dmsent = sent ? "✅" : "⚠️";
                string uAction = "Keine";
                var (warnsToKick, warnsToBan) = await ModerationHelper.GetWarnKickValues();
                var (KickEnabled, BanEnabled) = await ModerationHelper.UserActioningEnabled();

                if (warncount >= warnsToBan)
                    try
                    {
                        if (BanEnabled)
                        {
                            await ctx.Guild.BanMemberAsync(user, await Helpers.GenerateBannDeleteMessageDays(user.Id), reasonString);
                            uAction = "Gebannt";
                        }
                    }
                    catch (Exception)
                    {
                    }
                else if (warncount >= warnsToKick)
                    try
                    {
                        if (KickEnabled)
                        {
                            await ctx.Guild.GetMemberAsync(user.Id).Result.RemoveAsync(reasonString);
                            uAction = "Gekickt";
                        }
                    }
                    catch (Exception)
                    {
                    }

                string stringtoadd =
                    $"{user.UsernameWithDiscriminator} {user.Id} | Case-ID: {caseid_} | {warncount} Warn(s) | DM: {dmsent} | Sek. Aktion: {uAction}\n\n";
                for_str += stringtoadd;
            }

            string e_string = $"Der MultiWarn wurde erfolgreich abgeschlossen.\n" +
                              $"__Grund:__ ```{reason}```\n" +
                              $"__Gewarnte User:__\n" +
                              $"```{for_str}```";
            DiscordColor ec = DiscordColor.Green;
            var embedBuilder = new DiscordEmbedBuilder()
                .WithTitle("MultiWarn abgeschlossen")
                .WithDescription(e_string)
                .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                .WithColor(ec);
            var sembed = embedBuilder.Build();
            var smessageBuilder = new DiscordMessageBuilder()
                .WithEmbed(sembed)
                .WithReply(ctx.Message.Id);
            await message.ModifyAsync(smessageBuilder);
        }
    }
}