#region

using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using AGC_Management.Entities;
using DisCatSharp.Net;
using Newtonsoft.Json;
using RestSharp;
using NpgsqlDataSource = Npgsql.NpgsqlDataSource;

#endregion

namespace AGC_Management.Utils;

public static class ToolSet
{
    public static string GetFaviconUrl()
    {
        return CurrentApplication.TargetGuild.IconUrl != null
            ? CurrentApplication.TargetGuild.IconUrl
            : "favicon.png";
    }


    public static async Task<bool> IsUserInCache(ulong userId)
    {
        try
        {
            var cachedmembers = CurrentApplication.DiscordClient.UserCache.Values.ToList();

            return cachedmembers.Any(member => member.Id == userId);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Fehler beim Abrufen der Servermitglieder: " + ex.Message);
            return false;
        }
    }


    public static string GetBuildNumber(Assembly assembly)
    {
        const string BuildVersionMetadataPrefix = "+build";

        var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (attribute?.InformationalVersion != null)
        {
            var value = attribute.InformationalVersion;
            var index = value.IndexOf(BuildVersionMetadataPrefix);
            if (index > 0)
            {
                return value.Substring(index + BuildVersionMetadataPrefix.Length);
            }
        }

        return string.Empty;
    }

    public static string RemoveWhitespace(string input)
    {
        return new string(input.ToCharArray()
            .Where(c => !char.IsWhiteSpace(c))
            .ToArray());
    }

    public static long GetBuildDateToUnixTime(Assembly assembly)
    {
        const string BuildVersionMetadataPrefix = "+build";

        var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (attribute?.InformationalVersion != null)
        {
            var value = attribute.InformationalVersion;
            var index = value.IndexOf(BuildVersionMetadataPrefix);
            if (index > 0)
            {
                value = value.Substring(index + BuildVersionMetadataPrefix.Length);
                if (DateTime.TryParseExact(value, "yyyyMMddHHmmss", CultureInfo.CurrentCulture,
                        DateTimeStyles.AssumeLocal, out var result))
                {
                    return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(result, TimeZoneInfo.Local))
                        .ToUnixTimeSeconds();
                }
            }
        }

        return default;
    }

    public static async Task<bool> IsUserOnServer(ulong userId)
    {
        try
        {
            var serverMembers = CurrentApplication.TargetGuild.Members.Values.ToList();

            return serverMembers.Any(member => member.Id == userId);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Fehler beim Abrufen der Servermitglieder: " + ex.Message);
            return false;
        }
    }

    public static PartialUser GetFallbackUser(ulong userId)
    {
        return new PartialUser
        {
            UserId = userId,
            UserName = userId.ToString(),
            Avatar = GetDefaultAvatarUrlForUserId(userId)
        };
    }

    public static string GetDefaultAvatarUrlForUserId(ulong userId)
    {
        var domainUrl = DiscordDomain.GetDomain(CoreDomain.DiscordCdn).Url;
        var avatarIndex = (userId >> 22) % 6;
        string avatarUrl = $"{domainUrl}{Endpoints.EMBED}{Endpoints.AVATARS}/{avatarIndex}.png?size=1024";

        return avatarUrl;
    }


    public static string GetFormattedName(DiscordMember member)
    {
        if (!string.IsNullOrEmpty(member.Nickname))
        {
            return $"{member.Username} ({member.Nickname})";
        }

        if (!string.IsNullOrEmpty(member.DisplayName))
        {
            return $"{member.Username} ({member.DisplayName})";
        }

        return member.Username;
    }

    public static ulong GetUserIdFromHttpContext(HttpContext context)
    {
        var claimsIdentity = context.User.Identity as ClaimsIdentity;
        var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Convert.ToUInt64(userId);
    }

    public static async Task<string> UploadToCatBox(CommandContext ctx, List<DiscordAttachment> imgAttachments)
    {
        await ctx.Message.CreateReactionAsync(DiscordEmoji.FromGuildEmote(ctx.Client, 1084157150747697203));
        string apiurl = "https://catbox.moe/user/api.php";
        var client = new RestClient(apiurl);
        string urls = "";

        foreach (DiscordAttachment att in imgAttachments)
        {
            var bytesImage = await new HttpClient().GetByteArrayAsync(att.Url.Split('?')[0]);
            //using var stream = new MemoryStream(bytesImage);

            var request = new RestRequest(apiurl, Method.Post);
            request.AddParameter("reqtype", "fileupload");
            request.AddHeader("Content-Type", "multipart/form-data");
            request.AddFile("fileToUpload", bytesImage, att.Filename);


            var response = await client.ExecuteAsync(request);
            urls += $" {response.Content}";
        }

        await ctx.Message.DeleteOwnReactionAsync(DiscordEmoji.FromGuildEmote(ctx.Client, 1084157150747697203));
        return urls;
    }


    public static async Task<List<BannSystemWarn>?> GetBannsystemWarns(DiscordUser user)
    {
        using HttpClient client = new();
        string apiKey = GlobalProperties.DebugMode
            ? BotConfig.GetConfig()["ModHQConfigDBG"]["API_Key"]
            : BotConfig.GetConfig()["ModHQConfig"]["API_Key"];
        string apiUrl = GlobalProperties.DebugMode
            ? BotConfig.GetConfig()["ModHQConfigDBG"]["API_URL"]
            : BotConfig.GetConfig()["ModHQConfig"]["API_URL"];

        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", apiKey);
        HttpResponseMessage response = await client.GetAsync($"{apiUrl}{user.Id}");
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            UserInfoApiResponse apiResponse = JsonConvert.DeserializeObject<UserInfoApiResponse>(json);
            List<BannSystemWarn> data = apiResponse.warns;
            return data;
        }

        return null;
    }

    public static async Task<List<BannSystemReport?>?> GetBannsystemReports(DiscordUser user)
    {
        using HttpClient client = new();
        string apiKey = GlobalProperties.DebugMode
            ? BotConfig.GetConfig()["ModHQConfigDBG"]["API_Key"]
            : BotConfig.GetConfig()["ModHQConfig"]["API_Key"];
        string apiUrl = GlobalProperties.DebugMode
            ? BotConfig.GetConfig()["ModHQConfigDBG"]["API_URL"]
            : BotConfig.GetConfig()["ModHQConfig"]["API_URL"];

        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", apiKey);
        HttpResponseMessage response = await client.GetAsync($"{apiUrl}{user.Id}");
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            UserInfoApiResponse apiResponse = JsonConvert.DeserializeObject<UserInfoApiResponse>(json);
            List<BannSystemReport> data = apiResponse.reports;
            return data;
        }

        return null;
    }

    public static async Task<List<BannSystemReport?>?> BSReportToWarn(DiscordUser user)
    {
        try
        {
            var data = await GetBannsystemReports(user);

            return data.Select(warn => new BannSystemReport
            {
                reportId = warn.reportId,
                authorId = warn.authorId,
                reason = warn.reason,
                timestamp = warn.timestamp,
                active = warn.active
            }).ToList();
        }
        catch (Exception e)
        {
            // ignored
        }

        return new List<BannSystemReport>();
    }

    public static async Task<List<BannSystemWarn>> BSWarnToWarn(DiscordUser user)
    {
        try
        {
            var data = await GetBannsystemWarns(user);

            return data.Select(warn => new BannSystemWarn
            {
                warnId = warn.warnId,
                authorId = warn.authorId,
                reason = warn.reason,
                timestamp = warn.timestamp
            }).ToList();
        }
        catch (Exception e)
        {
            // ignored
        }

        return new List<BannSystemWarn>();
    }


    public static bool HasActiveBannSystemReport(List<BannSystemReport> reports)
    {
        return reports.Any(report => report.active);
    }

    public static async Task<bool> CheckForReason(CommandContext ctx, string? reason)
    {
        if (reason == null)
        {
            var embedBuilder = new DiscordEmbedBuilder().WithTitle("Fehler: Kein Grund angegeben!")
                .WithDescription("Bitte gebe einen Grund an")
                .WithColor(DiscordColor.Red).WithFooter($"{ctx.User.UsernameWithDiscriminator}", ctx.User.AvatarUrl);
            var msg = new DiscordMessageBuilder().WithEmbed(embedBuilder.Build()).WithReply(ctx.Message.Id);
            await ctx.Channel.SendMessageAsync(msg);


            return true;
        }

        if (reason == "")
        {
            var embedBuilder = new DiscordEmbedBuilder().WithTitle("Fehler: Kein Grund angegeben!")
                .WithDescription("Bitte gebe einen Grund an")
                .WithColor(DiscordColor.Red).WithFooter($"{ctx.User.UsernameWithDiscriminator}", ctx.User.AvatarUrl);
            var msg = new DiscordMessageBuilder().WithEmbed(embedBuilder.Build()).WithReply(ctx.Message.Id);
            await ctx.Channel.SendMessageAsync(msg);
            return true;
        }

        return false;
    }

    public static async Task<long> GetTicketCount(ulong userid)
    {
        try
        {
            var con = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
            await using var cmd =
                con.CreateCommand($"SELECT COUNT(*) FROM ticketstore WHERE ticket_owner = {userid}");
            var result = await cmd.ExecuteScalarAsync();
            return (long)result;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 0;
        }
    }

    public static IEnumerable<DiscordOverwriteBuilder> MergeOverwrites(DiscordChannel userChannel,
        List<DiscordOverwriteBuilder> overwrites,
        out IEnumerable<DiscordOverwriteBuilder> targetOverwrites)
    {
        targetOverwrites = userChannel.PermissionOverwrites.Select(x => x.ConvertToBuilder());
        foreach (var overwrite in overwrites)
        {
            targetOverwrites =
                targetOverwrites.Merge(overwrite.Type, overwrite.Target, overwrite.Allowed, overwrite.Denied);
        }

        var newOverwrites = targetOverwrites.ToList();
        return newOverwrites;
    }


    public static string GenerateCaseID()
    {
        var guid = Guid.NewGuid().ToString("N");
        var uniqueID = guid.Substring(0, 8);
        return uniqueID;
    }

    public static async Task<bool> UserHasClosedPendingTicket(ulong UserId)
    {
        var con = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
        string query = $"SELECT COUNT(*) FROM ticketcache WHERE ticket_owner = '{(long)UserId}'";
        await using NpgsqlCommand cmd = con.CreateCommand(query);
        int rowCount = Convert.ToInt32(cmd.ExecuteScalar());
        if (rowCount > 0)
        {
            return true;
        }


        return false;
    }

    public static async Task<bool> UserHasOpenTicket(ulong UserId)
    {
        var connection = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
        string query = $"SELECT COUNT(*) FROM ticketcache WHERE ticket_users @> ARRAY[{(long)UserId}::bigint]";
        await using NpgsqlCommand cmd = connection.CreateCommand(query);
        int rowCount = Convert.ToInt32(cmd.ExecuteScalar());
        if (rowCount > 0)
        {
            return true;
        }

        return false;
    }

    public static async Task<int> GenerateBannDeleteMessageDays(ulong UserId)
    {
        bool hasOpenTicket = await UserHasOpenTicket(UserId);
        bool hasClosedPendingTicket = await UserHasClosedPendingTicket(UserId);
        if (hasOpenTicket)
        {
            return 0;
        }

        if (hasClosedPendingTicket)
        {
            return 0;
        }

        return 7;
    }

    public static async Task SendWarnAsChannel(CommandContext ctx, DiscordUser user, DiscordEmbed uembed, string caseid)
    {
        DiscordEmbed userembed = uembed;
        var catid = BotConfig.GetConfig()["TicketConfig"]["SupportCategoryId"];
        var wchannel = await ctx.Guild.CreateChannelAsync($"warn-{caseid}",
            ChannelType.Text, ctx.Guild.GetChannel(Convert.ToUInt64(catid)), user.Id.ToString());
        await wchannel.AddOverwriteAsync(await user.ConvertToMember(ctx.Guild),
            Permissions.AccessChannels, Permissions.SendMessages | Permissions.AddReactions, "Warn eröffnet");
        var buttonack = new DiscordButtonComponent(ButtonStyle.Primary, "ackwarn", "Kenntnisnahme",
            emoji: new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:")));
        var mb = new DiscordMessageBuilder()
            .WithEmbed(userembed).AddComponents(buttonack).WithContent(user.Mention);
        await wchannel.SendMessageAsync(mb);
    }

    public static async Task<bool> TicketUrlCheck(CommandContext ctx, string reason)
    {
        var TicketUrl = "ticketsystem.animegamingcafe.de";
        if (reason == null) return false;
        if (reason.ToLower().Contains(TicketUrl.ToLower()))
        {
            Console.WriteLine("Ticket-URL enthalten");
            var embedBuilder = new DiscordEmbedBuilder().WithTitle("Fehler: Ticket-URL enthalten")
                .WithDescription("Bitte schreibe den Grund ohne Ticket-URL").WithColor(DiscordColor.Red);
            var embed = embedBuilder.Build();
            var msg_e = new DiscordMessageBuilder().WithEmbed(embed).WithReply(ctx.Message.Id);
            await ctx.Channel.SendMessageAsync(msg_e);

            return true;
        }

        return false;
    }
}