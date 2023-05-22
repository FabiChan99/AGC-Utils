using AGC_Management.Helpers;
using AGC_Management.Services.DatabaseHandler;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Exceptions;
using Npgsql;

namespace AGC_Management.Commands;

public class ExtendedModerationSystem : ModerationSystem
{
    public static string GetReportDateFromTimestamp(long timestamp)
    {
        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(timestamp);
        string formattedDate = dateTimeOffset.ToString("dd.MM.yyyy - HH:mm:ss");
        return formattedDate;
    }

    private static async Task<(bool, object, bool)> CheckBannsystem(DiscordUser user)
    {
        using HttpClient client = new();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization",
            GlobalProperties.ConfigIni["ModHQConfig"]["API_Key"]);
        HttpResponseMessage response =
            await client.GetAsync($"{GlobalProperties.ConfigIni["ModHQConfig"]["API_URL"]}{user.Id}");
        Console.WriteLine(response.StatusCode);
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);
            if (data.reports != null && data.reports.Count > 0)
            {
                return (true, data.reports, true);
            }

            return (false, data.reports, true);
        }

        return (false, null, false);
    }



    [Command("userinfo")]
    [RequireDatabase]
    [RequireStaffRole]
    [RequireTeamCat]
    public async Task UserInfoCommand(CommandContext ctx, DiscordUser user)
    {
        bool isMember = false;
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
            _ => "<:offline:946831431798227056>",
        };
        bool bs_status = false; // Default value
        bool bs_success = false;
        /*
        try
        {
            (bool temp_bs_status, object bs, bs_success) = await CheckBannsystem(user);
            bs_status = temp_bs_status;
            if (bs_status == true)
            {
                try
                {
                    DiscordColor clr = DiscordColor.Red;
                    var report_data = (List<object>)bs;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        Console.WriteLine(bs_success);
        string bs_icon = bs_status ? "<:BannSystem:1012006073751830529>" : "";
        */
        Console.WriteLine(isMember);
        if (isMember)
        {
            bool Teamler = false;
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

            List<dynamic> warnlist = new List<dynamic>();
            List<dynamic> flaglist = new List<dynamic>();
            List<dynamic> permawarnlist = new List<dynamic>();

            ulong memberID = member.Id;
            Console.WriteLine(memberID);
            NpgsqlConnection conn = DatabaseService.dbConnection;

            try
            {
                string warnQuery =
                    $"SELECT userid, punisherid, datum, description, caseid FROM warns WHERE userid = '{memberID}' AND perma = '0' ORDER BY datum ASC";
                await using (NpgsqlDataReader warnReader = DatabaseService.ExecuteQuery(warnQuery))
                {
                    while (warnReader.Read())
                    {
                        var warn = new
                        {
                            UserId = warnReader.GetInt64(0),
                            PunisherId = warnReader.GetInt64(1),
                            Datum = warnReader.GetInt32(2),
                            Description = warnReader.GetString(3),
                            CaseId = warnReader.GetInt32(4)
                        };
                        warnlist.Add(warn);
                    }
                }

                Console.WriteLine(memberID + "11111djkafsg");

                string flagQuery =
                    $"SELECT userid, punisherid, datum, description, caseid FROM flags WHERE userid = '{memberID}' ORDER BY datum ASC";
                await using (NpgsqlDataReader flagReader = DatabaseService.ExecuteQuery(flagQuery))
                {
                    while (flagReader.Read())
                    {
                        var flag = new
                        {
                            UserId = flagReader.GetInt64(0),
                            PunisherId = flagReader.GetInt64(1),
                            Datum = flagReader.GetInt32(2),
                            Description = flagReader.GetString(3),
                            CaseId = flagReader.GetString(4)
                        };
                        flaglist.Add(flag);
                    }
                }

                Console.WriteLine(memberID + "djkafsg");

                string permawarnQuery =
                    $"SELECT userid, punisherid, datum, description, caseid FROM warns WHERE userid = '{memberID}' AND perma = '1' ORDER BY datum ASC";
                await using (NpgsqlDataReader permawarnReader = DatabaseService.ExecuteQuery(permawarnQuery))
                {
                    while (permawarnReader.Read())
                    {
                        var permawarn = new
                        {
                            UserId = permawarnReader.GetInt64(0),
                            PunisherId = permawarnReader.GetInt64(1),
                            Datum = permawarnReader.GetInt32(2),
                            Description = permawarnReader.GetString(3),
                            CaseId = permawarnReader.GetString(4)
                        };
                        permawarnlist.Add(permawarn);
                    }
                }

                Console.WriteLine("0 work");

                int warncount = warnlist.Count;
                int flagcount = flaglist.Count;
                int permawarncount = permawarnlist.Count;

                string booster_icon = member.PremiumSince.HasValue ? "<:Booster:995060205178060960>" : "";
                string booster_seit = member.PremiumSince.HasValue
                    ? member.PremiumSince.Value.ToString("dd.MM.yyyy")
                    : "Kein Booster";

                // if null
                string timeouted = member.CommunicationDisabledUntil.HasValue
                    ? member.CommunicationDisabledUntil.Value.ToString("dd.MM.yyyy")
                    : "Kein Timeout";
                string timeout_icon = member.CommunicationDisabledUntil.HasValue
                    ? "<:timeout:1012036258857226752>"
                    : "";
                string vc_icon = (member.VoiceState?.Channel != null)
                    ? "<:voiceuser:1012037037148360815>"
                    : "";
                if (member.PremiumSince.HasValue)
                {
                    booster_icon = "<:Booster:995060205178060960>";
                }
                else
                {
                    booster_icon = "";
                }

                var teamler_ico = Teamler ? "<:staff:1012027870455005357>" : "";
                List<string> warnResults = new List<string>();
                List<string> permawarnResults = new List<string>();
                List<string> flagResults = new List<string>();

                foreach (var flag in flaglist)
                {
                    long intValue = flag.PunisherId;
                    ulong ulongValue = (ulong)intValue;
                    Console.WriteLine(ulongValue);
                    var puser = await ctx.Client.TryGetUserAsync(ulongValue, fetch: false);
                    var FlagStr =
                        $"[{(puser != null ? puser.Username : "Unbekannt")}, ``{flag.CaseId}``] {DisCatSharp.Formatter.Timestamp(Converter.ConvertUnixTimestamp(flag.Datum), DisCatSharp.Enums.TimestampFormat.RelativeTime)} - {flag.Description}";
                    flagResults.Add(FlagStr);
                    Console.WriteLine(FlagStr);
                }

                foreach (var warn in warnlist)
                {
                    long intValue = warn.PunisherId;
                    ulong ulongValue = (ulong)intValue;
                    Console.WriteLine(ulongValue);
                    var puser = await ctx.Client.TryGetUserAsync(ulongValue, fetch: false);
                    var FlagStr =
                        $"[{(puser != null ? puser.Username : "Unbekannt")}, ``{warn.CaseId}``] {DisCatSharp.Formatter.Timestamp(Converter.ConvertUnixTimestamp(warn.Datum), DisCatSharp.Enums.TimestampFormat.RelativeTime)} - {warn.Description}";
                    flagResults.Add(FlagStr);
                    Console.WriteLine(FlagStr);
                }

                foreach (var pwarn in permawarnlist)
                {
                    long intValue = pwarn.PunisherId;
                    ulong ulongValue = (ulong)intValue;
                    Console.WriteLine(ulongValue);
                    var puser = await ctx.Client.TryGetUserAsync(ulongValue, fetch: false);
                    var FlagStr =
                        $"[{(puser != null ? puser.Username : "Unbekannt")}, ``{pwarn.CaseId}``] {DisCatSharp.Formatter.Timestamp(Converter.ConvertUnixTimestamp(pwarn.Datum), DisCatSharp.Enums.TimestampFormat.RelativeTime)} - {pwarn.Description}";
                    flagResults.Add(FlagStr);
                    Console.WriteLine(FlagStr);
                }



                // if booster_seit
                string boost_string = member.PremiumSince.HasValue
                    ? $"\nBoostet seit: {DisCatSharp.Formatter.Timestamp(member.PremiumSince.Value)}"
                    : "";
                // discord native format
                string userinfostring =
                    $"**Das Mitglied**\n{member.UsernameWithDiscriminator} ``{member.Id}``{boost_string}\n\n";
                userinfostring += $"**Erstellung, Beitritt und mehr**\n";
                userinfostring += $"**Erstellt:** {DisCatSharp.Formatter.Timestamp(member.CreationTimestamp)}\n";
                userinfostring += $"**Beitritt:** {DisCatSharp.Formatter.Timestamp(member.JoinedAt)}\n";
                userinfostring +=
                    $"**Infobadges:**  {booster_icon} {teamler_ico} {bot_indicator} {vc_icon} {timeout_icon}\n\n";
                userinfostring += $"**Kommunikations-Timeout**\n";
                userinfostring +=
                    $"{(member.CommunicationDisabledUntil.HasValue ? $"Nutzer getimeouted bis: {DisCatSharp.Formatter.Timestamp(member.CommunicationDisabledUntil.Value)}" : "Nutzer nicht getimeouted")}\n\n";
                userinfostring +=
                    $"**Aktueller Voice-Channel**\n{(member.VoiceState != null && member.VoiceState.Channel != null ? member.VoiceState.Channel.Mention : "Mitglied nicht in einem Voice-Channel")}\n\n";
                userinfostring += $"**Alle Verwarnungen ({warncount})**\n";
                userinfostring += (warnlist.Count == 0
                    ? "Es wurden keine gefunden.\n"
                    : string.Join("\n", warnResults) + "\n");
                userinfostring += $"\n**Alle Perma-Verwarnungen ({permawarncount})**\n";
                userinfostring += (permawarnlist.Count == 0
                    ? "Es wurden keine gefunden.\n"
                    : string.Join("\n", permawarnResults) + "\n");
                userinfostring += $"\n**Alle Markierungen ({flagcount})**\n";
                userinfostring += (flaglist.Count == 0
                    ? "Es wurden keine gefunden.\n"
                    : string.Join("\n", flagResults) + "\n");

                /*
                if (bs_success)
                {
                    userinfostring += $"\n**BannSystem-Status**\n";
                    userinfostring += (bs_status ? "**__Nutzer ist gemeldet - Siehe BS-Bericht__**" : "Nutzer ist nicht gemeldet");
                } */
                DiscordEmbedBuilder embedbuilder = new DiscordEmbedBuilder();
                embedbuilder.WithTitle($"Infos über ein {GlobalProperties.ServerNameInitals} Mitglied");
                embedbuilder.WithDescription("Ich konnte folgende Informationen über den User finden.\n\n" +
                                             userinfostring);
                embedbuilder.WithColor(bs_status ? DiscordColor.Red : GlobalProperties.EmbedColor);
                embedbuilder.WithThumbnail(member.AvatarUrl);
                embedbuilder.WithFooter($"Bericht angefordert von {ctx.User.UsernameWithDiscriminator}",
                    ctx.User.AvatarUrl);
                await ctx.RespondAsync(embed: embedbuilder.Build());
                Console.WriteLine(bs_success);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
            }

            // TODO: NOT FINISHED
        }

        if (!isMember)
        {

            List<dynamic> warnlist = new List<dynamic>();
            List<dynamic> flaglist = new List<dynamic>();
            List<dynamic> permawarnlist = new List<dynamic>();
            ulong memberID = user.Id;
            Console.WriteLine(memberID);
            NpgsqlConnection conn = DatabaseService.dbConnection;

            try
            {
                string warnQuery =
                    $"SELECT userid, punisherid, datum, description, caseid FROM warns WHERE userid = '{memberID}' AND perma = '0' ORDER BY datum ASC";
                await using (NpgsqlDataReader warnReader = DatabaseService.ExecuteQuery(warnQuery))
                {
                    while (warnReader.Read())
                    {
                        var warn = new
                        {
                            UserId = warnReader.GetInt64(0),
                            PunisherId = warnReader.GetInt64(1),
                            Datum = warnReader.GetInt32(2),
                            Description = warnReader.GetString(3),
                            CaseId = warnReader.GetInt32(4)
                        };
                        warnlist.Add(warn);
                    }
                }

                Console.WriteLine(memberID + "11111djkafsg");

                string flagQuery =
                    $"SELECT userid, punisherid, datum, description, caseid FROM flags WHERE userid = '{memberID}' ORDER BY datum ASC";
                await using (NpgsqlDataReader flagReader = DatabaseService.ExecuteQuery(flagQuery))
                {
                    while (flagReader.Read())
                    {
                        var flag = new
                        {
                            UserId = flagReader.GetInt64(0),
                            PunisherId = flagReader.GetInt64(1),
                            Datum = flagReader.GetInt32(2),
                            Description = flagReader.GetString(3),
                            CaseId = flagReader.GetString(4)
                        };
                        flaglist.Add(flag);
                    }
                }

                Console.WriteLine(memberID + "djkafsg");

                string permawarnQuery =
                    $"SELECT userid, punisherid, datum, description, caseid FROM warns WHERE userid = '{memberID}' AND perma = '1' ORDER BY datum ASC";
                await using (NpgsqlDataReader permawarnReader = DatabaseService.ExecuteQuery(permawarnQuery))
                {
                    while (permawarnReader.Read())
                    {
                        var permawarn = new
                        {
                            UserId = permawarnReader.GetInt64(0),
                            PunisherId = permawarnReader.GetInt64(1),
                            Datum = permawarnReader.GetInt32(2),
                            Description = permawarnReader.GetString(3),
                            CaseId = permawarnReader.GetString(4)
                        };
                        permawarnlist.Add(permawarn);
                    }
                }

                Console.WriteLine("0 work");

                int warncount = warnlist.Count;
                int flagcount = flaglist.Count;
                int permawarncount = permawarnlist.Count;

                List<string> warnResults = new List<string>();
                List<string> permawarnResults = new List<string>();
                List<string> flagResults = new List<string>();

                foreach (var flag in flaglist)
                {
                    long intValue = flag.PunisherId;
                    ulong ulongValue = (ulong)intValue;
                    Console.WriteLine(ulongValue);
                    var puser = await ctx.Client.TryGetUserAsync(ulongValue, fetch: false);
                    var FlagStr =
                        $"[{(puser != null ? puser.Username : "Unbekannt")}, ``{flag.CaseId}``] {DisCatSharp.Formatter.Timestamp(Converter.ConvertUnixTimestamp(flag.Datum), DisCatSharp.Enums.TimestampFormat.RelativeTime)} - {flag.Description}";
                    flagResults.Add(FlagStr);
                    Console.WriteLine(FlagStr);
                }

                foreach (var warn in warnlist)
                {
                    long intValue = warn.PunisherId;
                    ulong ulongValue = (ulong)intValue;
                    Console.WriteLine(ulongValue);
                    var puser = await ctx.Client.TryGetUserAsync(ulongValue, fetch: false);
                    var FlagStr =
                        $"[{(puser != null ? puser.Username : "Unbekannt")}, ``{warn.CaseId}``] {DisCatSharp.Formatter.Timestamp(Converter.ConvertUnixTimestamp(warn.Datum), DisCatSharp.Enums.TimestampFormat.RelativeTime)} - {warn.Description}";
                    flagResults.Add(FlagStr);
                    Console.WriteLine(FlagStr);
                }

                foreach (var pwarn in permawarnlist)
                {
                    long intValue = pwarn.PunisherId;
                    ulong ulongValue = (ulong)intValue;
                    Console.WriteLine(ulongValue);
                    var puser = await ctx.Client.TryGetUserAsync(ulongValue, fetch: false);
                    var FlagStr =
                        $"[{(puser != null ? puser.Username : "Unbekannt")}, ``{pwarn.CaseId}``] {DisCatSharp.Formatter.Timestamp(Converter.ConvertUnixTimestamp(pwarn.Datum), DisCatSharp.Enums.TimestampFormat.RelativeTime)} - {pwarn.Description}";
                    flagResults.Add(FlagStr);
                    Console.WriteLine(FlagStr);
                }




                string userinfostring =
                    $"**Das Mitglied**\nUsername: {user.UsernameWithDiscriminator} ``{user.Id}``\n\n";
                userinfostring += $"**Erstellung, Beitritt und mehr**\n";
                userinfostring += $"**Erstellt:** {DisCatSharp.Formatter.Timestamp(user.CreationTimestamp)}\n";
                userinfostring += $"**Beitritt:** *User nicht auf dem Server*\n";
                userinfostring += $"**Infobadges:**  {bot_indicator}\n\n";
                userinfostring += $"**Alle Verwarnungen ({warncount})**\n";
                userinfostring += (warnlist.Count == 0
                    ? "Es wurden keine gefunden.\n"
                    : string.Join("\n", warnResults) + "\n");
                userinfostring += $"\n**Alle Perma-Verwarnungen ({permawarncount})**\n";
                userinfostring += (permawarnlist.Count == 0
                    ? "Es wurden keine gefunden.\n"
                    : string.Join("\n", permawarnResults) + "\n");
                userinfostring += $"\n**Alle Markierungen ({flagcount})**\n";
                userinfostring += (flaglist.Count == 0
                    ? "Es wurden keine gefunden.\n"
                    : string.Join("\n", flagResults) + "\n");

                /*
                if (bs_success)
                {
                    userinfostring += $"\n**BannSystem-Status**\n";
                    userinfostring += (bs_status ? "**__Nutzer ist gemeldet - Siehe BS-Bericht__**" : "Nutzer ist nicht gemeldet");
                } */
                DiscordEmbedBuilder embedbuilder = new DiscordEmbedBuilder();
                embedbuilder.WithTitle($"Infos über ein {GlobalProperties.ServerNameInitals} Mitglied");
                embedbuilder.WithDescription("Ich konnte folgende Informationen über den User finden.\n\n" +
                                             userinfostring);
                embedbuilder.WithColor(bs_status ? DiscordColor.Red : GlobalProperties.EmbedColor);
                embedbuilder.WithThumbnail(user.AvatarUrl);
                embedbuilder.WithFooter($"Bericht angefordert von {ctx.User.UsernameWithDiscriminator}",
                    ctx.User.AvatarUrl);
                await ctx.RespondAsync(embed: embedbuilder.Build());
                Console.WriteLine(bs_success);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
            }
        }
    }


}
