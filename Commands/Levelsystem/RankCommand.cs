#region

using AGC_Management.Utils;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using SkiaSharp;

#endregion

namespace AGC_Management.Commands.Levelsystem;

public class RankCommand : ApplicationCommandsModule
{
    [SlashCommand("rank", "Zeigt den aktuellen Rang eines Nutzers an.")]
    public static async Task Rank(InteractionContext ctx,
        [Option("user", "Der Nutzer von dem der Rang angezeigt werden soll.")]
        DiscordUser user = null)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent(
                "<a:loading_agc:1084157150747697203> Rang wird geladen..."));
        if (user == null) user = ctx.User;

        if (user.IsBot)
        {
            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent(
                    "<:attention:1085333468688433232> **Fehler!** Dies ist ein Bot!"));
            return;
        }

        var embed = new DiscordEmbedBuilder
        {
            Title = "Rang von " + user.Username,
            Color = BotConfig.GetEmbedColor()
        };

        DiscordMember member = null;
        try
        {
            member = await ctx.Guild.GetMemberAsync(user.Id);
            await LevelUtils.UpdateLevelRoles(member);
        }
        catch (Exception)
        {
            // ignored
        }

        await LevelUtils.RecalculateUserLevel(user.Id);


        var rank = await LevelUtils.GetRank(user.Id);
        var level = rank[user.Id].Level;
        var totalxp = rank[user.Id].Xp;
        var display_xp = Converter.FormatWithCommas(totalxp);
        var xpForCurrentLevel = LevelUtils.XpForLevel(level);
        var xpForNextLevel = LevelUtils.XpForLevel(level + 1);
        var xpForThisLevel = xpForNextLevel - xpForCurrentLevel;
        var xpForThisLevelUntilNow = totalxp - xpForCurrentLevel;
        var percentage = (int)(xpForThisLevelUntilNow / (float)xpForThisLevel * 100);
        var userRank = await LevelUtils.GetUserRankAsync(user.Id);
        var errored = false;
        var errorMessage = "";

        var httpsEnabled = bool.Parse(BotConfig.GetConfig()["WebUI"]["UseHttps"]);
        var dashboardUrl = BotConfig.GetConfig()["WebUI"]["DashboardURL"];
        var protocol = httpsEnabled ? "https" : "http";
        var baseurl = $"{protocol}://{dashboardUrl}";

        try
        {
            var imagedata = await ImageUtils.GenerateRankCard(user, xpForThisLevelUntilNow, level, userRank, percentage,
                totalxp,
                xpForThisLevel);
            var imgstream = imagedata.AsStream();
            var button = new DiscordLinkButtonComponent($"{baseurl}/benutzereinstellungen/levelsystem/adjustrankcard",
                "Rangkarte anpassen", false, new DiscordComponentEmoji("🖼️"));
            var button2 = new DiscordLinkButtonComponent($"{baseurl}/leaderboard",
                "Online Rangliste ansehen", false, new DiscordComponentEmoji("🏆"));
            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().AddFile("rank.png", imgstream).AddComponents(button, button2));
            return;
        }
        catch (Exception e)
        {
            CurrentApplication.Logger.Error(e, "Failed to generate rank card");
            errorMessage = e.Message;
            errored = true;
        }


        using var bar = ImageUtils.CreateProgressBar(200, 20, percentage / 100f, $"{percentage}%");
        using var image = SKImage.FromBitmap(bar);
        await using var stream = image.Encode(SKEncodedImageFormat.Png, 100).AsStream();
        stream.Position = 0;

        embed.WithTitle("Rang von " + user.Username);
        embed.WithDescription($"**Level:** {level}\n" +
                              $"**Rang:** {userRank}\n\n" +
                              $"**Fortschritt:** {Converter.FormatWithCommas(xpForThisLevelUntilNow)} / {Converter.FormatWithCommas(xpForThisLevel)} **XP**\n" +
                              $"**Gesamt XP:** {display_xp} XP");
        embed.WithThumbnail(user.AvatarUrl);

        if (errored) embed.WithFooter("Fallback Rangkarte | Fehler: " + errorMessage);


        embed.WithImageUrl("attachment://progress.png");

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed).AddFile("progress.png", stream));
    }
}