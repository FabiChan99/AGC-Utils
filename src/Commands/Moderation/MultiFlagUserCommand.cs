using AGC_Management.Helpers;
using AGC_Management.Services.DatabaseHandler;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Interactivity.Extensions;
using AGC_Management.Attributes;

namespace AGC_Management.Commands.Moderation;

public sealed class MultiFlagUserCommand : BaseCommandModule
{
        [Command("multiflag")]
        [Description("Flaggt mehrere Nutzer")]
        [RequireDatabase]
        [RequireStaffRole]
        [RequireTeamCat]
        public async Task MultiFlagUser(CommandContext ctx, [RemainingText] string ids_and_reason)
        {
            List<ulong> ids;
            string reason;
            Converter.SeperateIdsAndReason(ids_and_reason, out ids, out reason);
            if (await Helpers.Helpers.CheckForReason(ctx, reason)) return;
            if (await Helpers.Helpers.TicketUrlCheck(ctx, reason)) return;
            reason = reason.TrimEnd(' ');
            var users_to_flag = new List<DiscordUser>();
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
                if (user != null) users_to_flag.Add(user);
            }

            var imgExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
            var imgAttachments = ctx.Message.Attachments
                .Where(att => imgExtensions.Contains(Path.GetExtension(att.FileName).ToLower()))
                .ToList();
            string urls = "";
            if (imgAttachments.Count > 0)
            {
                urls = await Helpers.Helpers.UploadToCatBox(ctx, imgAttachments);
            }

            var busers_formatted = string.Join("\n", users_to_flag.Select(buser => buser.UsernameWithDiscriminator));
            var caseid = Helpers.Helpers.GenerateCaseID();
            var confirmEmbedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Überprüfe deine Eingabe | Aktion: MultiFlag")
                .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                .WithDescription($"Bitte überprüfe deine Eingabe und bestätige mit ✅ um fortzufahren.\n\n" +
                                 $"__Users:__\n" +
                                 $"```{busers_formatted}```\n__Grund:__```{reason + urls}```")
                .WithColor(BotConfig.GetEmbedColor());
            var embed = confirmEmbedBuilder.Build();
            List<DiscordButtonComponent> buttons = new(2)
            {
                new DiscordButtonComponent(ButtonStyle.Success, $"multiflag_accept_{caseid}", "Bestätigen"),
                new DiscordButtonComponent(ButtonStyle.Danger, $"multiflag_deny_{caseid}", "Abbrechen")
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

            if (result.Result.Id == $"multiflag_deny_{caseid}")
            {
                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                var loadingEmbedBuilder = new DiscordEmbedBuilder()
                    .WithTitle("MultiFlag abgebrochen")
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithDescription("Der MultiFlag wurde abgebrochen.")
                    .WithColor(DiscordColor.Red);
                var loadingEmbed = loadingEmbedBuilder.Build();
                var loadingMessage = new DiscordMessageBuilder()
                    .WithEmbed(loadingEmbed)
                    .WithReply(ctx.Message.Id);
                await message.ModifyAsync(loadingMessage);
                return;
            }

            if (result.Result.Id == $"multiflag_accept_{caseid}")
            {
                var disbtn = buttons;
                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                disbtn.ForEach(x => x.Disable());
                var loadingEmbedBuilder = new DiscordEmbedBuilder()
                    .WithTitle("Multiflag wird bearbeitet")
                    .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                    .WithDescription("Der Multiflag wird bearbeitet. Bitte warten...")
                    .WithColor(DiscordColor.Yellow);
                var loadingEmbed = loadingEmbedBuilder.Build();
                var loadingMessage = new DiscordMessageBuilder()
                    .WithEmbed(loadingEmbed).AddComponents(disbtn)
                    .WithReply(ctx.Message.Id);
                await message.ModifyAsync(loadingMessage);
                string for_str = "";
                List<DiscordUser> users_to_flag_obj = new();
                foreach (var id in setids)
                {
                    var user = await ctx.Client.GetUserAsync(id);
                    if (user != null) users_to_flag_obj.Add(user);
                }

                foreach (var user in users_to_flag_obj)
                {
                    var caseid_ = Helpers.Helpers.GenerateCaseID();
                    caseid_ = $"{caseid}-{caseid_}";
                    Dictionary<string, object> data = new()
                    {
                        { "userid", (long)user.Id },
                        { "punisherid", (long)ctx.User.Id },
                        { "datum", DateTimeOffset.Now.ToUnixTimeSeconds() },
                        { "description", reason + urls },
                        { "caseid", caseid_ }
                    };
                    await DatabaseService.InsertDataIntoTable("flags", data);
                    var flaglist = new List<dynamic>();

                    List<string> selectedFlags = new()
                    {
                        "*"
                    };

                    Dictionary<string, object> whereConditions = new()
                    {
                        { "userid", (long)user.Id }
                    };
                    List<Dictionary<string, object>> results =
                        await DatabaseService.SelectDataFromTable("flags", selectedFlags, whereConditions);
                    foreach (var lresult in results) flaglist.Add(lresult);
                    var flagcount = flaglist.Count;
                    string stringtoadd =
                        $"{user.UsernameWithDiscriminator} {user.Id} | Case-ID: {caseid_} | {flagcount} Flag(s)\n\n";
                    for_str += stringtoadd;
                }

                string e_string = $"Der Multiflag wurde erfolgreich abgeschlossen.\n" +
                                  $"__Grund:__ ```{reason + urls}```\n" +
                                  $"__Geflaggte User:__\n" +
                                  $"```{for_str}```";
                DiscordColor ec = DiscordColor.Green;
                var embedBuilder = new DiscordEmbedBuilder()
                    .WithTitle("Multiflag abgeschlossen")
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