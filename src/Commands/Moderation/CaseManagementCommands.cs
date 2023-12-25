#region

using System.Net.Http.Headers;
using AGC_Management.Attributes;
using AGC_Management.Services;
using AGC_Management.Utils;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;

#endregion

namespace AGC_Management.Commands.Moderation;

[Group("case")]
public sealed class CaseManagement : BaseCommandModule
{
    private async Task<string> UploadToCatBox(CommandContext ctx, List<DiscordAttachment> imgAttachments)
    {
        await ctx.Message.CreateReactionAsync(DiscordEmoji.FromGuildEmote(ctx.Client, 1084157150747697203));
        var httpClient = new HttpClient();
        string urls = "";

        foreach (DiscordAttachment att in imgAttachments)
        {
            var bytesImage = await httpClient.GetByteArrayAsync(att.Url);
            using var stream = new MemoryStream(bytesImage);

            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(fileContent, "fileToUpload", att.Filename);


            content.Add(new StringContent("fileupload"), "reqtype");

            var response = await httpClient.PostAsync("https://catbox.moe/user/api.php", content);
            var responseString = await response.Content.ReadAsStringAsync();
            urls += $" {responseString}";
        }

        await ctx.Message.DeleteOwnReactionAsync(DiscordEmoji.FromGuildEmote(ctx.Client, 1084157150747697203));
        return urls;
    }

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
        bool wcase = false;
        bool fcase = false;
        try
        {
            warn = wlist[0];
            wcase = true;
        }
        catch (Exception)
        {
            warn = null;
        }

        try
        {
            flag = flist[0];
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


        string reason = newreason;
        string sql;
        if (wcase)
        {
            if (await Helpers.CheckForReason(ctx, reason)) return;
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
            var imgExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
            var imgAttachments = ctx.Message.Attachments
                .Where(att => imgExtensions.Contains(Path.GetExtension(att.Filename).ToLower()))
                .ToList();
            string urls = "";
            if (imgAttachments.Count > 0)
            {
                urls = await UploadToCatBox(ctx, imgAttachments);
            }

            if (await Helpers.CheckForReason(ctx, reason)) return;
            await using (NpgsqlConnection conn = new(DatabaseService.GetConnectionString()))
            {
                await conn.OpenAsync();
                sql = "UPDATE flags SET description = @description WHERE caseid = @caseid";
                await using (NpgsqlCommand command = new(sql, conn))
                {
                    command.Parameters.AddWithValue("@description", newreason + urls);
                    command.Parameters.AddWithValue("@caseid", caseid);

                    int affected = await command.ExecuteNonQueryAsync();
                    DiscordEmbed ue = new DiscordEmbedBuilder()
                        .WithTitle("Case Update").WithDescription(
                            $"Der Case mit der ID ``{caseid}`` wurde erfolgreich bearbeitet.\n" +
                            $"Case-Typ: {ctyp}\n" +
                            $"Neuer Grund: ```{reason + urls}```").WithColor(BotConfig.GetEmbedColor()).Build();
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


        string reason = deletereason;
        string sql;
        if (wcase)
        {
            if (await Helpers.CheckForReason(ctx, reason)) return;
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
            if (await Helpers.CheckForReason(ctx, reason)) return;
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