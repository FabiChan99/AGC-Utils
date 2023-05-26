using AGC_Management.Helpers;
using AGC_Management.Services.DatabaseHandler;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Exceptions;
using Newtonsoft.Json;

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
                ? $"\nBoostet seit: {member.PremiumSince.Value.Timestamp()}"
                : "";
            // discord native format
            var userinfostring =
                $"**Das Mitglied**\n{member.UsernameWithDiscriminator} ``{member.Id}``{boost_string}\n\n";
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
            embedbuilder.WithTitle($"Infos über ein {BotConfig.GetConfig()["ServerConfig"]["ServerNameInitials"]} Mitglied");
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
                banStatus = $"**Nutzer ist Lokal gebannt!** ```{ban_entry.Reason}```";
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
            embedbuilder.WithTitle($"Infos über ein {BotConfig.GetConfig()["ServerConfig"]["ServerNameInitials"]} Mitglied");
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
        List<Dictionary<string, object>> results =
            await DatabaseService.SelectDataFromTable("flags", selectedFlags, null);
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

    [Command("warn")]
    [Description("Verwarnt einen Nutzer")]
    [RequireDatabase]
    [RequireStaffRole]
    [RequireTeamCat]
    public async Task WarnUser(CommandContext ctx, DiscordUser user, [RemainingText] string reason)
    {
        if (await Helpers.Helpers.CheckForReason(ctx, reason)) return;
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
                    Console.WriteLine("Banning");
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
                    Console.WriteLine("Banning");
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