using AGC_Management.Helpers;
using AGC_Management.Services.DatabaseHandler;
using DisCatSharp.Attributes;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Exceptions;
using Npgsql;
using System;
using System.Globalization;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;


namespace AGC_Management.Commands;

public class ExtendedModerationSystem : ModerationSystem
{
    public static string GetReportDateFromTimestamp(long timestamp)
    {
        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(timestamp);
        string formattedDate = dateTimeOffset.ToString("dd.MM.yyyy - HH:mm:ss");
        return formattedDate;
    }

    public static async Task<(bool, object)> CheckBannsystem(DiscordUser user)
    {
        using HttpClient client = new();
        client.DefaultRequestHeaders.Add("Authorization", GlobalProperties.ConfigIni["ModHQConfig"]["API_Key"]);
        HttpResponseMessage response =
            await client.GetAsync($"{GlobalProperties.ConfigIni["ModHQConfig"]["API_URL"]}{user.Id}");
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);
            if (data.reports != null && data.reports.Count > 0)
            {
                return (true, data.reports);
            }
            else
            {
                return (false, data.reports);
            }
        }
        else
        {
            return (false, null);
        }
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
        Console.WriteLine(user_status);
        try
        {
            (bool temp_bs_status, object bs) = await CheckBannsystem(user);
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
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }
        catch (Exception)
        {
            // Ignore
        }

        string bs_icon = bs_status ? "<:BannSystem:1012006073751830529>" : "";
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

            string warnQuery =
                $"SELECT userid, punisherid, datum, description, caseid FROM warns WHERE userid = '{memberID}' AND perma = '0' ORDER BY datum ASC";
            await using(NpgsqlDataReader warnReader = DatabaseService.ExecuteQuery(warnQuery))
            {
                while (warnReader.Read())
                {
                    var warn = new
                    {
                        UserId = warnReader.GetString(0),
                        PunisherId = warnReader.GetString(1),
                        Datum = warnReader.GetDateTime(2),
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
                        UserId = flagReader.GetString(0),
                        PunisherId = flagReader.GetString(1),
                        Datum = flagReader.GetDateTime(2),
                        Description = flagReader.GetString(3),
                        CaseId = flagReader.GetInt32(4)
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
                        UserId = permawarnReader.GetString(0),
                        PunisherId = permawarnReader.GetString(1),
                        Datum = permawarnReader.GetDateTime(2),
                        Description = permawarnReader.GetString(3),
                        CaseId = permawarnReader.GetInt32(4)
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
                booster_seit = member.PremiumSince.Value.ToString("dd.MM.yyyy");
            }
            else
            {
                booster_icon = "";
                booster_seit = "";
            }
            var teamler_ico = Teamler ? "<:staff:1012027870455005357>" : "";
            // discord native format
            string userinfostring = $"**Das Mitglied**\n\n{member.UsernameWithDiscriminator}\n``{member.Id}``\n{booster_seit}\n\n";
            userinfostring += $"**Erstellung, Beitritt und mehr**\n";
            userinfostring += $"**Erstellt:** {member.CreationTimestamp.ToString("dd.MM.yyyy - HH:mm")}\n";
            userinfostring += $"**Beitritt:** {member.JoinedAt.ToString("dd.MM.yyyy - HH:mm")}\n";
            userinfostring += $"**Infobadges:** {bs_icon} {booster_icon} {teamler_ico} {bot_indicator} {vc_icon} {timeout_icon}\n\n";
            userinfostring += $"**Der Online-Status**\n{status_indicator}\n\n";
            userinfostring += $"**Kommunikations-Timeout**\n";
            userinfostring += $"{(member.CommunicationDisabledUntil.HasValue ? $"Nutzer getimeouted bis: {member.CommunicationDisabledUntil.Value.ToString("dd.MM.yyyy - HH:mm")}" : "Nutzer nicht getimeouted")}\n\n";
            userinfostring += $"**Aktueller Voice-Channel**\n{(member.VoiceState != null && member.VoiceState.Channel != null ? member.VoiceState.Channel.Mention : "Mitglied nicht in einem Voice-Channel")}\n\n";
            userinfostring += $"**Alle Verwarnungen ({warncount})**\n";
            userinfostring += (warnlist.Count == 0 ? "Es wurden keine gefunden.\n" : string.Join("\n", warnlist.Select(warn => $"[{warn.PunisherId}, ``{warn.CaseId}``] {warn.Datum.ToString("dd.MM.yyyy - HH:mm")} - {warn.Description}")));
            userinfostring += $"\n\n**Alle Perma-Verwarnungen ({permawarncount})**\n";
            userinfostring += (permawarnlist.Count == 0 ? "Es wurden keine gefunden.\n" : string.Join("\n", permawarnlist.Select(permawarn => $"[{permawarn.PunisherId}, ``{permawarn.CaseId}``] {permawarn.Datum.ToString("dd.MM.yyyy - HH:mm")} - {permawarn.Description}\n")));
            userinfostring += $"\n\n**Alle Markierungen ({flagcount})**\n";
            userinfostring += (flaglist.Count == 0 ? "Es wurden keine gefunden.\n" : string.Join("\n", flaglist.Select(flag => $"[{flag.PunisherId}, ``{flag.CaseId}``] {flag.Datum.ToString("dd.MM.yyyy - HH:mm")} - {flag.Description}\n")));
            userinfostring += $"\n\n**BannSystem-Status**\n";
            userinfostring += (bs_status ? "**__Nutzer ist gemeldet - Siehe BS-Bericht__**" : "Nutzer ist nicht gemeldet");
            string teamler_icon = Teamler ? "<:staff:1012027870455005357>" : "";
            DiscordEmbedBuilder embedbuilder = new DiscordEmbedBuilder();
            embedbuilder.WithTitle($"Infos über ein {GlobalProperties.ServerNameInitals} Mitglied");
            embedbuilder.WithDescription("Ich konnte folgende Informationen über den User finden.\n" + userinfostring);
            embedbuilder.WithColor(bs_status ? DiscordColor.Red : GlobalProperties.EmbedColor);
            embedbuilder.WithThumbnail(member.AvatarUrl);
            embedbuilder.WithFooter($"Bericht angefordert von {ctx.User.UsernameWithDiscriminator}", ctx.User.AvatarUrl);
            await ctx.RespondAsync(embed: embedbuilder.Build());

            // TODO: NOT FINISHED

        }
    }
}
