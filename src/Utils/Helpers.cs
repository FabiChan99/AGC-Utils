#region

using AGC_Management.Entities;
using AGC_Management.Services;
using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using Newtonsoft.Json;
using Npgsql;
using RestSharp;

#endregion

namespace AGC_Management.Utils;

public static class Helpers
{
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
            var dbConfigSection = GlobalProperties.DebugMode ? "DatabaseCfgDBG" : "DatabaseCfg";
            var DbHost = BotConfig.GetConfig()[dbConfigSection]["Database_Host"];
            var DbUser = BotConfig.GetConfig()[dbConfigSection]["Database_User"];
            var DbPass = BotConfig.GetConfig()[dbConfigSection]["Database_Password"];
            var DbName = BotConfig.GetConfig()[dbConfigSection]["Ticket_Database"];

            await using var con =
                new NpgsqlConnection($"Host={DbHost};Username={DbUser};Password={DbPass};Database={DbName}");
            await con.OpenAsync();
            await using var cmd =
                new NpgsqlCommand($"SELECT COUNT(*) FROM ticketstore WHERE ticket_owner = {userid}", con);
            var result = await cmd.ExecuteScalarAsync();
            await con.CloseAsync();
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
        string con = TicketDatabaseService.GetConnectionString();
        await using var connection = new NpgsqlConnection(con);
        await connection.OpenAsync();
        string query = $"SELECT COUNT(*) FROM ticketcache WHERE ticket_owner = '{(long)UserId}'";
        await using NpgsqlCommand cmd = new(query, connection);
        int rowCount = Convert.ToInt32(cmd.ExecuteScalar());
        if (rowCount > 0)
        {
            await connection.CloseAsync();
            return true;
        }

        await connection.CloseAsync();
        return false;
    }

    public static async Task<bool> UserHasOpenTicket(ulong UserId)
    {
        string con = TicketDatabaseService.GetConnectionString();
        await using var connection = new NpgsqlConnection(con);
        await connection.OpenAsync();
        string query = $"SELECT COUNT(*) FROM ticketcache WHERE ticket_users @> ARRAY[{(long)UserId}::bigint]";
        await using NpgsqlCommand cmd = new(query, connection);
        int rowCount = Convert.ToInt32(cmd.ExecuteScalar());
        if (rowCount > 0)
        {
            await connection.CloseAsync();
            return true;
        }

        await connection.CloseAsync();
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