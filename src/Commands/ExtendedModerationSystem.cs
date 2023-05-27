using AGC_Management.Helpers;
using AGC_Management.Services.DatabaseHandler;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Exceptions;
using DisCatSharp.Interactivity.Extensions;
using Newtonsoft.Json;
using Npgsql;

namespace AGC_Management.Commands;

public class ExtendedModerationSystem : ModerationSystem
{
    private static async Task<(bool, object, bool)> CheckBannsystem(DiscordUser user)
    {
        using HttpClient client = new();

        string apiKey = GlobalProperties.DebugMode
            ? BotConfig.GetConfig()["ModHQConfigDBG"]["API_Key"]
            : BotConfig.GetConfig()["ModHQConfig"]["API_Key"];

        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", apiKey);

        string apiUrl = GlobalProperties.DebugMode
            ? BotConfig.GetConfig()["ModHQConfigDBG"]["API_URL"]
            : BotConfig.GetConfig()["ModHQConfig"]["API_URL"];

        HttpResponseMessage response = await client.GetAsync($"{apiUrl}{user.Id}");

        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(json);

            if (data.reports != null && data.reports.Count > 0)
                return (true, data.reports, true);

            return (false, data.reports, true);
        }

        return (false, null, false);
    }


    [Command("userinfo")]
    [RequireDatabase]
    [RequireStaffRole]
    [Description("Zeigt Informationen über einen User an.")]
    [RequireTeamCat]
    public async Task UserInfoCommand(CommandContext ctx, DiscordUser user)
    {
        var isMember = false;
        DiscordMember member = null;
        try
        {
            member = await ctx.Guild.GetMemberAsync(user.Id);
            isMember = true;
        }
        catch (NotFoundException)
        {
            isMember = false;
        }

        string bot_indicator = user.IsBot ? "<:bot:1012035481573265458>" : "";
        string user_status = member?.Presence?.Status.ToString() ?? "Offline";
        string status_indicator = user_status switch
        {
            "Online" => "<:online:1012032516934352986>",
            "Idle" => "<:abwesend:1012032002771406888>",
            "DoNotDisturb" => "<:do_not_disturb:1012031711263064104>",
            "Invisible" or "Offline" => "<:offline:946831431798227056>",
            "Streaming" => "<:twitch_streaming:1012033234080632983>",
            _ => "<:offline:946831431798227056>"
        };
        string platform;
        if (isMember)
        {
            var clientStatus = member?.Presence?.ClientStatus;
            platform = clientStatus switch
            {
                { Desktop: { HasValue: true } } => "User verwendet Discord am Computer",
                { Mobile: { HasValue: true } } => "User verwendet Discord am Handy",
                { Web: { HasValue: true } } => "User verwendet Discord im Browser",
                _ => "Nicht ermittelbar"
            };
        }
        else
        {
            platform = "Nicht ermittelbar. User nicht auf Server";
        }

        bool bs_status = false;
        bool bs_success = false;
        bool bs_enabled = false;

        try
        {
            if (GlobalProperties.DebugMode)
                bs_enabled = bool.Parse(BotConfig.GetConfig()["ModHQConfigDBG"]["API_ACCESS_ENABLED"]);
            if (!GlobalProperties.DebugMode)
                bs_enabled = bool.Parse(BotConfig.GetConfig()["ModHQConfig"]["API_ACCESS_ENABLED"]);
        }
        catch (Exception)
        {
            bs_enabled = false;
        }

        if (bs_enabled)
            try
            {
                (bool temp_bs_status, object bs, bs_success) = await CheckBannsystem(user);
                bs_status = temp_bs_status;
                if (bs_status)
                    try
                    {
                        DiscordColor clr = DiscordColor.Red;
                        var report_data = (List<object>)bs;
                    }
                    catch (Exception)
                    {
                    }
            }
            catch (Exception)
            {
            }


        string bs_icon = bs_status ? "<:BannSystem:1012006073751830529>" : "";
        if (isMember)
        {
            var Teamler = false;
            List<DiscordMember> staffuser = ctx.Guild.Members
                .Where(x => x.Value.Roles.Any(y => y.Id == GlobalProperties.StaffRoleId))
                .Select(x => x.Value)
                .ToList();
            string userindicator;
            if (staffuser.Any(x => x.Id == user.Id))
            {
                Teamler = true;
                userindicator = "Teammitglied";
            }
            else
            {
                userindicator = "Mitglied";
            }

            var warnlist = new List<dynamic>();
            var flaglist = new List<dynamic>();
            var permawarnlist = new List<dynamic>();

            ulong memberID = member.Id;
            List<string> WarnQuery = new()
            {
                "*"
            };
            Dictionary<string, object> warnWhereConditions = new()
            {
                { "perma", false },
                { "userid", (long)memberID }
            };
            List<Dictionary<string, object>> WarnResults =
                await DatabaseService.SelectDataFromTable("warns", WarnQuery, warnWhereConditions);
            foreach (var result in WarnResults) warnlist.Add(result);


            List<string> FlagQuery = new()
            {
                "*"
            };
            Dictionary<string, object> flagWhereConditions = new()
            {
                { "userid", (long)memberID }
            };
            List<Dictionary<string, object>> FlagResults =
                await DatabaseService.SelectDataFromTable("flags", FlagQuery, flagWhereConditions);
            foreach (var result in FlagResults) flaglist.Add(result);

            List<string> pWarnQuery = new()
            {
                "*"
            };
            Dictionary<string, object> pWarnWhereConditions = new()
            {
                { "userid", (long)memberID },
                { "perma", true }
            };
            List<Dictionary<string, object>> pWarnResults =
                await DatabaseService.SelectDataFromTable("warns", pWarnQuery, pWarnWhereConditions);
            foreach (var result in pWarnResults) permawarnlist.Add(result);

            int warncount = warnlist.Count;
            int flagcount = flaglist.Count;
            int permawarncount = permawarnlist.Count;

            string booster_icon = member.PremiumSince.HasValue ? "<:Booster:995060205178060960>" : "";
            string timeout_icon = member.CommunicationDisabledUntil.HasValue
                ? "<:timeout:1012036258857226752>"
                : "";
            string vc_icon = member.VoiceState?.Channel != null
                ? "<:voiceuser:1012037037148360815>"
                : "";
            if (member.PremiumSince.HasValue)
                booster_icon = "<:Booster:995060205178060960>";
            else
                booster_icon = "";

            string teamler_ico = Teamler ? "<:staff:1012027870455005357>" : "";
            var warnResults = new List<string>();
            var permawarnResults = new List<string>();
            var flagResults = new List<string>();

            foreach (dynamic flag in flaglist)
            {
                long intValue = flag["punisherid"];
                var ulongValue = (ulong)intValue;
                DiscordUser? puser = await ctx.Client.TryGetUserAsync(ulongValue, false);
                var FlagStr =
                    $"[{(puser != null ? puser.Username : "Unbekannt")}, ``{flag["caseid"]}``]  {Formatter.Timestamp(Converter.ConvertUnixTimestamp(flag["datum"]), TimestampFormat.RelativeTime)}  -  {flag["description"]}";
                flagResults.Add(FlagStr);
            }

            foreach (dynamic warn in warnlist)
            {
                long intValue = warn["punisherid"];
                var ulongValue = (ulong)intValue;
                DiscordUser? puser = await ctx.Client.TryGetUserAsync(ulongValue, false);
                var FlagStr =
                    $"[{(puser != null ? puser.Username : "Unbekannt")}, ``{warn["caseid"]}``] {Formatter.Timestamp(Converter.ConvertUnixTimestamp(warn["datum"]), TimestampFormat.RelativeTime)} - {warn["description"]}";
                warnResults.Add(FlagStr);
            }

            foreach (dynamic pwarn in permawarnlist)
            {
                long intValue = pwarn["punisherid"];
                var ulongValue = (ulong)intValue;
                DiscordUser? puser = await ctx.Client.TryGetUserAsync(ulongValue, false);
                var FlagStr =
                    $"[{(puser != null ? puser.Username : "Unbekannt")}, ``{pwarn["caseid"]}``] {Formatter.Timestamp(Converter.ConvertUnixTimestamp(pwarn["datum"]), TimestampFormat.RelativeTime)} - {pwarn["description"]}";
                permawarnResults.Add(FlagStr);
            }


            // if booster_seit
            string boost_string = member.PremiumSince.HasValue
                ? $"Boostet seit: {member.PremiumSince.Value.Timestamp()}\n"
                : "";
            // discord native format
            string servernick = member.Nickname != null ? $" \n*Aka. **{member.Nickname}***" : "";
            var userinfostring =
                $"**Das Mitglied**" + $"\n{member.UsernameWithDiscriminator} ``{member.Id}``{servernick}\n" +
                $"{boost_string}\n";
            userinfostring += "**Erstellung, Beitritt und mehr**\n";
            userinfostring += $"**Erstellt:** {member.CreationTimestamp.Timestamp()}\n";
            userinfostring += $"**Beitritt:** {member.JoinedAt.Timestamp()}\n";
            userinfostring +=
                $"**Infobadges:**  {booster_icon} {teamler_ico} {bot_indicator} {vc_icon} {timeout_icon} {bs_icon}\n\n";
            userinfostring += "**Der Online-Status und die Plattform**\n";
            userinfostring += $"{status_indicator} | {platform}\n\n";
            userinfostring += "**Kommunikations-Timeout**\n";
            userinfostring +=
                $"{(member.CommunicationDisabledUntil.HasValue ? $"Nutzer getimeouted bis: {member.CommunicationDisabledUntil.Value.Timestamp()}" : "Nutzer nicht getimeouted")}\n\n";
            userinfostring +=
                $"**Aktueller Voice-Channel**\n{(member.VoiceState != null && member.VoiceState.Channel != null ? member.VoiceState.Channel.Mention : "Mitglied nicht in einem Voice-Channel")}\n\n";
            userinfostring += $"**__Alle Verwarnungen ({warncount})__**\n";
            userinfostring += warnlist.Count == 0
                ? "Es wurden keine gefunden.\n"
                : string.Join("\n\n", warnResults) + "\n";
            userinfostring += $"\n**__Alle Perma-Verwarnungen ({permawarncount})__**\n";
            userinfostring += permawarnlist.Count == 0
                ? "Es wurden keine gefunden.\n"
                : string.Join("\n\n", permawarnResults) + "\n";
            userinfostring += $"\n**__Alle Markierungen ({flagcount})__**\n";
            userinfostring += flaglist.Count == 0
                ? "Es wurden keine gefunden.\n"
                : string.Join("\n\n", flagResults) + "\n";


            if (bs_success)
            {
                userinfostring += "\n**BannSystem-Status**\n";
                userinfostring += bs_status
                    ? "**__Nutzer ist gemeldet - Siehe BS-Bericht__**"
                    : "Nutzer ist nicht gemeldet";
            }

            var embedbuilder = new DiscordEmbedBuilder();
            embedbuilder.WithTitle(
                $"Infos über ein {BotConfig.GetConfig()["ServerConfig"]["ServerNameInitials"]} Mitglied");
            embedbuilder.WithDescription($"Ich konnte folgende Informationen über {userindicator} finden.\n\n" +
                                         userinfostring);
            embedbuilder.WithColor(bs_status ? DiscordColor.Red : BotConfig.GetEmbedColor());
            embedbuilder.WithThumbnail(member.AvatarUrl);
            embedbuilder.WithFooter($"Bericht angefordert von {ctx.User.UsernameWithDiscriminator}",
                ctx.User.AvatarUrl);
            await ctx.RespondAsync(embedbuilder.Build());
        }

        if (!isMember)
        {
            var warnlist = new List<dynamic>();
            var flaglist = new List<dynamic>();
            var permawarnlist = new List<dynamic>();
            ulong memberID = user.Id;
            List<string> WarnQuery = new()
            {
                "*"
            };
            Dictionary<string, object> warnWhereConditions = new()
            {
                { "perma", false },
                { "userid", (long)memberID }
            };
            List<Dictionary<string, object>> WarnResults =
                await DatabaseService.SelectDataFromTable("warns", WarnQuery, warnWhereConditions);
            foreach (var result in WarnResults) warnlist.Add(result);


            List<string> FlagQuery = new()
            {
                "*"
            };
            Dictionary<string, object> flagWhereConditions = new()
            {
                { "userid", (long)memberID }
            };
            List<Dictionary<string, object>> FlagResults =
                await DatabaseService.SelectDataFromTable("flags", FlagQuery, flagWhereConditions);
            foreach (var result in FlagResults) flaglist.Add(result);

            List<string> pWarnQuery = new()
            {
                "*"
            };
            Dictionary<string, object> pWarnWhereConditions = new()
            {
                { "userid", (long)memberID },
                { "perma", true }
            };
            List<Dictionary<string, object>> pWarnResults =
                await DatabaseService.SelectDataFromTable("warns", pWarnQuery, pWarnWhereConditions);
            foreach (var result in pWarnResults) permawarnlist.Add(result);

            int warncount = warnlist.Count;
            int flagcount = flaglist.Count;
            int permawarncount = permawarnlist.Count;

            var warnResults = new List<string>();
            var permawarnResults = new List<string>();
            var flagResults = new List<string>();

            foreach (dynamic flag in flaglist)
            {
                long intValue = flag["punisherid"];
                var ulongValue = (ulong)intValue;
                DiscordUser? puser = await ctx.Client.TryGetUserAsync(ulongValue, false);
                var FlagStr =
                    $"[{(puser != null ? puser.Username : "Unbekannt")}, ``{flag["caseid"]}``]  {Formatter.Timestamp(Converter.ConvertUnixTimestamp(flag["datum"]), TimestampFormat.RelativeTime)}  -  {flag["description"]}";
                flagResults.Add(FlagStr);
            }

            foreach (dynamic warn in warnlist)
            {
                long intValue = warn["punisherid"];
                var ulongValue = (ulong)intValue;
                DiscordUser? puser = await ctx.Client.TryGetUserAsync(ulongValue, false);
                var FlagStr =
                    $"[{(puser != null ? puser.Username : "Unbekannt")}, ``{warn["caseid"]}``] {Formatter.Timestamp(Converter.ConvertUnixTimestamp(warn["datum"]), TimestampFormat.RelativeTime)} - {warn["description"]}";
                warnResults.Add(FlagStr);
            }

            foreach (dynamic pwarn in permawarnlist)
            {
                long intValue = pwarn["punisherid"];
                var ulongValue = (ulong)intValue;
                DiscordUser? puser = await ctx.Client.TryGetUserAsync(ulongValue, false);
                var FlagStr =
                    $"[{(puser != null ? puser.Username : "Unbekannt")}, ``{pwarn["caseid"]}``] {Formatter.Timestamp(Converter.ConvertUnixTimestamp(pwarn["datum"]), TimestampFormat.RelativeTime)} - {pwarn["description"]}";
                permawarnResults.Add(FlagStr);
            }

            bool isBanned = false;
            string banStatus;
            try
            {
                var ban_entry = await ctx.Guild.GetBanAsync(user.Id);
                banStatus = $"**Nutzer ist Lokal gebannt!** ```{ban_entry.Reason}```\n";
                isBanned = true;
            }
            catch (NotFoundException)
            {
                banStatus = "Nutzer nicht Lokal gebannt.";
            }
            catch (Exception)
            {
                banStatus = "Ban-Status konnte nicht abgerufen werden.";
            }

            string banicon = isBanned ? "<:banicon:1012003595727671337>" : "";


            var userinfostring =
                $"**Der User**\n{user.UsernameWithDiscriminator} ``{user.Id}``\n\n";
            userinfostring += "**Erstellung, Beitritt und mehr**\n";
            userinfostring += $"**Erstellt:** {user.CreationTimestamp.Timestamp()}\n";
            userinfostring += "**Beitritt:** *User nicht auf dem Server*\n";
            userinfostring += $"**Infobadges:**  {bot_indicator} {bs_icon} {banicon}\n\n";
            userinfostring += "**Der Online-Status und die Plattform**\n";
            userinfostring += $"{status_indicator} | Nicht ermittelbar - User ist nicht auf dem Server\n\n";
            userinfostring += $"**__Alle Verwarnungen ({warncount})__**\n";
            userinfostring += warnlist.Count == 0
                ? "Es wurden keine gefunden.\n"
                : string.Join("\n\n", warnResults) + "\n";
            userinfostring += $"\n**__Alle Perma-Verwarnungen ({permawarncount})__**\n";
            userinfostring += permawarnlist.Count == 0
                ? "Es wurden keine gefunden.\n"
                : string.Join("\n\n", permawarnResults) + "\n";
            userinfostring += $"\n**__Alle Markierungen ({flagcount})__**\n";
            userinfostring += flaglist.Count == 0
                ? "Es wurden keine gefunden.\n"
                : string.Join("\n\n", flagResults) + "\n";
            userinfostring += "\n**Lokaler Bannstatus**\n";
            userinfostring += banStatus + "";

            if (bs_success)
            {
                userinfostring += "\n**BannSystem-Status**\n";
                userinfostring += bs_status
                    ? "**__Nutzer ist gemeldet - Siehe BS-Bericht__**"
                    : "Nutzer ist nicht gemeldet";
            }

            var embedbuilder = new DiscordEmbedBuilder();
            embedbuilder.WithTitle(
                $"Infos über ein {BotConfig.GetConfig()["ServerConfig"]["ServerNameInitials"]} Mitglied");
            embedbuilder.WithDescription("Ich konnte folgende Informationen über den User finden.\n\n" +
                                         userinfostring);
            embedbuilder.WithColor(bs_status ? DiscordColor.Red : BotConfig.GetEmbedColor());
            embedbuilder.WithThumbnail(user.AvatarUrl);
            embedbuilder.WithFooter($"Bericht angefordert von {ctx.User.UsernameWithDiscriminator}",
                ctx.User.AvatarUrl);
            await ctx.RespondAsync(embedbuilder.Build());
        }
    }

    [Command("multiuserinfo")]
    [RequireDatabase]
    [RequireStaffRole]
    [Description("Zeigt Informationen über mehrere User an.")]
    [RequireTeamCat]
    public async Task MultiUserInfo(CommandContext ctx, [RemainingText] string users)
    {
        string[] usersToCheck = users.Split(' ');
        var uniqueUserIds = new HashSet<ulong>();

        foreach (string member in usersToCheck)
        {
            if (!ulong.TryParse(member, out ulong memberId)) continue;

            uniqueUserIds.Add(memberId);
        }

        if (uniqueUserIds.Count == 0)
        {
            await ctx.RespondAsync("Keine gültigen User-IDs gefunden.");
            return;
        }

        if (uniqueUserIds.Count > 6)
        {
            await ctx.RespondAsync("Maximal 6 User können gleichzeitig abgefragt werden.");
            return;
        }

        foreach (ulong memberId in uniqueUserIds)
        {
            //await Task.Delay(1000);
            var us = await ctx.Client.TryGetUserAsync(memberId, false);
            if (us == null) continue;
            await UserInfoCommand(ctx, us);
        }
    }


    [Command("flag")]
    [Description("Flaggt einen Nutzer")]
    [RequireDatabase]
    [RequireStaffRole]
    [RequireTeamCat]
    public async Task FlagUser(CommandContext ctx, DiscordUser user, [RemainingText] string reason)
    {
        if (await Helpers.Helpers.CheckForReason(ctx, reason)) return;
        var caseid = Helpers.Helpers.GenerateCaseID();
        Dictionary<string, object> data = new()
        {
            { "userid", (long)user.Id },
            { "punisherid", (long)ctx.User.Id },
            { "datum", DateTimeOffset.Now.ToUnixTimeSeconds() },
            { "description", reason },
            { "caseid", caseid }
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
        foreach (var result in results) flaglist.Add(result);


        var flagcount = flaglist.Count;

        var embed = new DiscordEmbedBuilder()
            .WithTitle("Nutzer geflaggt")
            .WithDescription(
                $"Der Nutzer {user.UsernameWithDiscriminator} `{user.Id}` wurde geflaggt!\n Grund: ```{reason}```Der User hat nun __{flagcount} Flag(s)__. \nID des Flags: ``{caseid}``")
            .WithColor(BotConfig.GetEmbedColor())
            .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl).Build();
        await ctx.RespondAsync(embed);
    }

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

        var busers_formatted = string.Join("\n", users_to_flag.Select(buser => buser.UsernameWithDiscriminator));
        var caseid = Helpers.Helpers.GenerateCaseID();
        var confirmEmbedBuilder = new DiscordEmbedBuilder()
            .WithTitle("Überprüfe deine Eingabe | Aktion: MultiFlag")
            .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
            .WithDescription($"Bitte überprüfe deine Eingabe und bestätige mit ✅ um fortzufahren.\n\n" +
                             $"__Users:__\n" +
                             $"```{busers_formatted}```\n__Grund:__```{reason}```")
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
            List<DiscordMember> users_to_flag_obj = new();
            foreach (var id in setids)
            {
                var user = await ctx.Guild.GetMemberAsync(id);
                if (user != null) users_to_flag_obj.Add(user);
            }

            foreach (var user in users_to_flag_obj)
            {
                var caseid_ = Helpers.Helpers.GenerateCaseID();
                Dictionary<string, object> data = new()
                {
                    { "userid", (long)user.Id },
                    { "punisherid", (long)ctx.User.Id },
                    { "datum", DateTimeOffset.Now.ToUnixTimeSeconds() },
                    { "description", reason },
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
                              $"__Grund:__ ```{reason}```\n" +
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


    [Command("warn")]
    [Description("Verwarnt einen Nutzer")]
    [RequireDatabase]
    [RequireStaffRole]
    [RequireTeamCat]
    public async Task WarnUser(CommandContext ctx, DiscordUser user, [RemainingText] string reason)
    {
        if (await Helpers.Helpers.CheckForReason(ctx, reason)) return;
        if (await Helpers.Helpers.TicketUrlCheck(ctx, reason)) return;
        var (warnsToKick, warnsToBan) = await ModerationHelper.GetWarnKickValues();
        var caseid = Helpers.Helpers.GenerateCaseID();
        Dictionary<string, object> data = new()
        {
            { "userid", (long)user.Id },
            { "punisherid", (long)ctx.User.Id },
            { "datum", DateTimeOffset.Now.ToUnixTimeSeconds() },
            { "description", reason },
            { "caseid", caseid },
            { "perma", false }
        };

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
        foreach (var result in results) warnlist.Add(result);


        var warncount = warnlist.Count + 1;

        await DatabaseService.InsertDataIntoTable("warns", data);
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

        var dmsent = sent ? "✅" : "❌";
        string uAction = "Keine";

        var (KickEnabled, BanEnabled) = await ModerationHelper.UserActioningEnabled();

        if (warncount >= warnsToBan)
            try
            {
                if (BanEnabled)
                {
                    await ctx.Guild.BanMemberAsync(user, 7, reasonString);
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


        var sembed = new DiscordEmbedBuilder()
            .WithTitle("Nutzer verwarnt")
            .WithDescription(
                $"Der Nutzer {user.UsernameWithDiscriminator} `{user.Id}` wurde verwarnt!\n Grund: ```{reason}```Der User hat nun __{warncount} Verwarnung(en)__. \nUser benachrichtigt: {dmsent} \nSekundäre ausgeführte Aktion: **{uAction}** \nID des Warns: ``{caseid}``")
            .WithColor(BotConfig.GetEmbedColor())
            .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl).Build();
        await ctx.RespondAsync(sembed);
    }


    [Command("permawarn")]
    [Description("Verwarnt einen Nutzer permanent")]
    [RequireDatabase]
    [RequireStaffRole]
    [RequireTeamCat]
    public async Task PermaWarn(CommandContext ctx, DiscordUser user, [RemainingText] string reason)
    {
        if (await Helpers.Helpers.CheckForReason(ctx, reason)) return;
        if (await Helpers.Helpers.TicketUrlCheck(ctx, reason)) return;
        var (warnsToKick, warnsToBan) = await ModerationHelper.GetWarnKickValues();
        var caseid = Helpers.Helpers.GenerateCaseID();
        Dictionary<string, object> data = new()
        {
            { "userid", (long)user.Id },
            { "punisherid", (long)ctx.User.Id },
            { "datum", DateTimeOffset.Now.ToUnixTimeSeconds() },
            { "description", reason },
            { "caseid", caseid },
            { "perma", true }
        };

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
        foreach (var result in results) warnlist.Add(result);


        var warncount = warnlist.Count + 1;

        await DatabaseService.InsertDataIntoTable("warns", data);
        DiscordEmbed uembed =
            await ModerationHelper.GeneratePermaWarnEmbed(ctx, user, ctx.User, warncount, caseid, true, reason);
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

        var dmsent = sent ? "✅" : "❌";
        string uAction = "Keine";

        var (KickEnabled, BanEnabled) = await ModerationHelper.UserActioningEnabled();

        if (warncount >= warnsToBan)
            try
            {
                if (BanEnabled)
                {
                    await ctx.Guild.BanMemberAsync(user, 7, reasonString);
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


        var sembed = new DiscordEmbedBuilder()
            .WithTitle("Nutzer permanent verwarnt")
            .WithDescription(
                $"Der Nutzer {user.UsernameWithDiscriminator} `{user.Id}` wurde permanent verwarnt!\n Grund: ```{reason}```Der User hat nun __{warncount} Verwarnung(en)__. \nUser benachrichtigt: {dmsent} \nSekundäre ausgeführte Aktion: **{uAction}** \nID des Warns: ``{caseid}``")
            .WithColor(BotConfig.GetEmbedColor())
            .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl).Build();
        await ctx.RespondAsync(sembed);
    }
}

[Group("case")]
public class CaseManagement : BaseCommandModule
{
    [Command("info")]
    [RequireDatabase]
    [RequireStaffRole]
    [RequireTeamCat]
    public async Task CaseInfo(CommandContext ctx, string caseid)
    {
        List<dynamic> wlist = new();
        List<dynamic> flist = new();
        List<string> selectedWarns = new()
        {
            "*"
        };

        Dictionary<string, object> whereConditions = new()
        {
            { "caseid", caseid }
        };
        List<Dictionary<string, object>> wresult =
            await DatabaseService.SelectDataFromTable("warns", selectedWarns, whereConditions);
        List<Dictionary<string, object>> fresult =
            await DatabaseService.SelectDataFromTable("flags", selectedWarns, whereConditions);


        foreach (var result in wresult) wlist.Add(result);
        foreach (var result in fresult) flist.Add(result);
        dynamic warn;
        dynamic flag;
        string ctyp = null;
        bool wcase = false;
        bool fcase = false;
        try
        {
            warn = wlist[0];
            ctyp = "Verwarnung";
            wcase = true;
        }
        catch (Exception)
        {
            warn = null;
        }

        try
        {
            flag = flist[0];
            ctyp = "Markierung";
            fcase = true;
        }
        catch (Exception)
        {
            flag = null;
        }

        string case_type;
        DiscordUser user;
        DiscordUser punisher;
        DateTime datum;
        string reason;
        bool perma;

        if (wcase)
        {
            case_type = "Verwarnung";
            user = await ctx.Client.GetUserAsync((ulong)warn["userid"]);
            punisher = await ctx.Client.GetUserAsync((ulong)warn["punisherid"]);
            datum = DateTimeOffset.FromUnixTimeSeconds(warn["datum"]).DateTime;
            reason = warn["description"];
            perma = warn["perma"];
        }
        else if (fcase)
        {
            case_type = "Markierung";
            user = await ctx.Client.GetUserAsync((ulong)flag["userid"]);
            punisher = await ctx.Client.GetUserAsync((ulong)flag["punisherid"]);
            datum = DateTimeOffset.FromUnixTimeSeconds(flag["datum"]).DateTime;
            reason = flag["description"];
            perma = false;
        }
        else
        {
            DiscordEmbed embed = new DiscordEmbedBuilder()
                .WithTitle("Fehler")
                .WithDescription($"Es wurde kein Case mit der ID ``{caseid}`` gefunden.")
                .WithColor(DiscordColor.Red)
                .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl).Build();
            await ctx.RespondAsync(embed);
            return;
        }


        if (wcase)
        {
            DiscordEmbedBuilder discordEmbedbuilder = new DiscordEmbedBuilder()
                .WithTitle("Case Informationen").WithColor(BotConfig.GetEmbedColor())
                .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                .AddField(new DiscordEmbedField("Case-Typ:", case_type)).WithThumbnail(user.AvatarUrl)
                .AddField(new DiscordEmbedField("Case-ID:", $"``{caseid}``"))
                .AddField(new DiscordEmbedField("Der betroffene Nutzer:",
                    user.UsernameWithDiscriminator + "\n" + $"``{user.Id}``"))
                .AddField(new DiscordEmbedField("Ausgeführt von:",
                    punisher.UsernameWithDiscriminator + "\n" + $"``{punisher.Id}``"))
                .AddField(new DiscordEmbedField("Datum:", datum.Timestamp()))
                .AddField(new DiscordEmbedField("Grund:", $"```{reason}```"));
            if (wcase) discordEmbedbuilder.AddField(new DiscordEmbedField("Permanent:", perma ? "✅" : "❌"));
            await ctx.RespondAsync(discordEmbedbuilder.Build());
            return;
        }

        if (fcase)
        {
            DiscordEmbedBuilder discordEmbedbuilder = new DiscordEmbedBuilder()
                .WithTitle("Case Informationen").WithColor(BotConfig.GetEmbedColor())
                .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
                .AddField(new DiscordEmbedField("Case-Typ:", case_type)).WithThumbnail(user.AvatarUrl)
                .AddField(new DiscordEmbedField("Case-ID:", $"``{caseid}``"))
                .AddField(new DiscordEmbedField("Der betroffene Nutzer:",
                    user.UsernameWithDiscriminator + "\n" + $"``{user.Id}``"))
                .AddField(new DiscordEmbedField("Ausgeführt von::",
                    punisher.UsernameWithDiscriminator + "\n" + $"``{punisher.Id}``"))
                .AddField(new DiscordEmbedField("Datum:", datum.Timestamp()))
                .AddField(new DiscordEmbedField("Grund:", $"```{reason}```"));
            await ctx.RespondAsync(discordEmbedbuilder.Build());
        }
    }

    [Command("edit")]
    [RequireDatabase]
    [RequireStaffRole]
    [RequireTeamCat]
    public async Task CaseEdit(CommandContext ctx, string caseid, [RemainingText] string newreason)
    {
        List<dynamic> wlist = new();
        List<dynamic> flist = new();
        List<string> selectedWarns = new()
        {
            "*"
        };

        Dictionary<string, object> whereConditions = new()
        {
            { "caseid", caseid }
        };
        List<Dictionary<string, object>> wresult =
            await DatabaseService.SelectDataFromTable("warns", selectedWarns, whereConditions);
        List<Dictionary<string, object>> fresult =
            await DatabaseService.SelectDataFromTable("flags", selectedWarns, whereConditions);


        foreach (var result in wresult) wlist.Add(result);
        foreach (var result in fresult) flist.Add(result);
        dynamic warn;
        dynamic flag;
        string ctyp = null;
        bool wcase = false;
        bool fcase = false;
        try
        {
            warn = wlist[0];
            ctyp = "Verwarnung";
            wcase = true;
        }
        catch (Exception)
        {
            warn = null;
        }

        try
        {
            flag = flist[0];
            ctyp = "Markierung";
            fcase = true;
        }
        catch (Exception)
        {
            flag = null;
        }

        string case_type;
        DiscordUser user;
        DiscordUser punisher;
        DateTime datum;
        string reason = newreason;
        bool perma;
        string sql;
        if (wcase)
        {
            if (await Helpers.Helpers.CheckForReason(ctx, reason)) return;
            await using (NpgsqlConnection conn = new(DatabaseService.GetConnectionString()))
            {
                await conn.OpenAsync();
                sql = "UPDATE warns SET description = @description WHERE caseid = @caseid";
                await using (NpgsqlCommand command = new(sql, conn))
                {
                    command.Parameters.AddWithValue("@description", newreason);
                    command.Parameters.AddWithValue("@caseid", caseid);

                    int affected = await command.ExecuteNonQueryAsync();

                    DiscordEmbed ue = new DiscordEmbedBuilder()
                        .WithTitle("Case Update").WithDescription(
                            $"Der Case mit der ID ``{caseid}`` wurde erfolgreich bearbeitet.\n" +
                            $"Case-Typ: {ctyp}\n" +
                            $"Neuer Grund: ```{reason}```").WithColor(BotConfig.GetEmbedColor()).Build();
                    await ctx.RespondAsync(ue);
                }
            }

            return;
        }

        if (fcase)
        {
            if (await Helpers.Helpers.CheckForReason(ctx, reason)) return;
            await using (NpgsqlConnection conn = new(DatabaseService.GetConnectionString()))
            {
                await conn.OpenAsync();
                sql = "UPDATE flags SET description = @description WHERE caseid = @caseid";
                await using (NpgsqlCommand command = new(sql, conn))
                {
                    command.Parameters.AddWithValue("@description", newreason);
                    command.Parameters.AddWithValue("@caseid", caseid);

                    int affected = await command.ExecuteNonQueryAsync();
                    DiscordEmbed ue = new DiscordEmbedBuilder()
                        .WithTitle("Case Update").WithDescription(
                            $"Der Case mit der ID ``{caseid}`` wurde erfolgreich bearbeitet.\n" +
                            $"Case-Typ: {ctyp}\n" +
                            $"Neuer Grund: ```{reason}```").WithColor(BotConfig.GetEmbedColor()).Build();
                    await ctx.RespondAsync(ue);
                }
            }

            return;
        }

        DiscordEmbed embed = new DiscordEmbedBuilder()
            .WithTitle("Fehler")
            .WithDescription($"Es wurde kein Case mit der ID ``{caseid}`` gefunden.")
            .WithColor(DiscordColor.Red)
            .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl).Build();
        await ctx.RespondAsync(embed);
    }

    [Command("delete")]
    [RequireDatabase]
    [RequireStaffRole]
    [RequireTeamCat]
    public async Task CaseDelete(CommandContext ctx, string caseid, [RemainingText] string deletereason)
    {
        List<dynamic> wlist = new();
        List<dynamic> flist = new();
        List<string> selectedWarns = new()
        {
            "*"
        };

        Dictionary<string, object> whereConditions = new()
        {
            { "caseid", caseid }
        };
        List<Dictionary<string, object>> wresult =
            await DatabaseService.SelectDataFromTable("warns", selectedWarns, whereConditions);
        List<Dictionary<string, object>> fresult =
            await DatabaseService.SelectDataFromTable("flags", selectedWarns, whereConditions);


        foreach (var result in wresult) wlist.Add(result);
        foreach (var result in fresult) flist.Add(result);
        dynamic warn;
        dynamic flag;
        string ctyp = null;
        bool wcase = false;
        bool fcase = false;
        try
        {
            warn = wlist[0];
            ctyp = "Verwarnung";
            wcase = true;
        }
        catch (Exception)
        {
            warn = null;
        }

        try
        {
            flag = flist[0];
            ctyp = "Markierung";
            fcase = true;
        }
        catch (Exception)
        {
            flag = null;
        }

        string case_type;
        DiscordUser user;
        DiscordUser punisher;
        DateTime datum;
        string reason = deletereason;
        bool perma;
        string sql;
        if (wcase)
        {
            if (await Helpers.Helpers.CheckForReason(ctx, reason)) return;
            await using (NpgsqlConnection conn = new(DatabaseService.GetConnectionString()))
            {
                await conn.OpenAsync();
                sql = "DELETE FROM warns WHERE caseid = @caseid";
                await using (NpgsqlCommand command = new(sql, conn))
                {
                    command.Parameters.AddWithValue("@caseid", caseid);

                    int affected = await command.ExecuteNonQueryAsync();

                    DiscordEmbed ue = new DiscordEmbedBuilder()
                        .WithTitle("Case Gelöscht").WithDescription(
                            $"Der Case mit der ID ``{caseid}`` wurde gelöscht.\n" +
                            $"Case-Typ: {ctyp}\n").WithColor(BotConfig.GetEmbedColor()).Build();
                    await ctx.RespondAsync(ue);
                }
            }

            return;
        }

        if (fcase)
        {
            if (await Helpers.Helpers.CheckForReason(ctx, reason)) return;
            await using (NpgsqlConnection conn = new(DatabaseService.GetConnectionString()))
            {
                await conn.OpenAsync();
                sql = "DELETE FROM flags WHERE caseid = @caseid";
                await using (NpgsqlCommand command = new(sql, conn))
                {
                    command.Parameters.AddWithValue("@caseid", caseid);

                    int affected = await command.ExecuteNonQueryAsync();

                    DiscordEmbed ue = new DiscordEmbedBuilder()
                        .WithTitle("Case Gelöscht").WithDescription(
                            $"Der Case mit der ID ``{caseid}`` wurde gelöscht.\n" +
                            $"Case-Typ: {ctyp}\n").WithColor(BotConfig.GetEmbedColor()).Build();
                    await ctx.RespondAsync(ue);
                }
            }

            return;
        }

        DiscordEmbed embed = new DiscordEmbedBuilder()
            .WithTitle("Fehler")
            .WithDescription($"Es wurde kein Case mit der ID ``{caseid}`` gefunden.")
            .WithColor(DiscordColor.Red)
            .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl).Build();
        await ctx.RespondAsync(embed);
    }
}