using System.Drawing;
using System.Net.Mime;
using AGC_Management.Utils;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using System.Drawing;
using SkiaSharp;

namespace AGC_Management.Commands.Levelsystem;

public class RankCommand : ApplicationCommandsModule
{
    [SlashCommand("rank", "Zeigt den aktuellen Rang eines Nutzers an.")]
    public static async Task Rank(InteractionContext ctx, 
        [Option("user", "Der Nutzer von dem der Rang angezeigt werden soll.")] DiscordUser user = null)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("<a:loading_agc:1084157150747697203> Rang wird geladen..."));
        if (user == null)
        {
            user = ctx.User;
        }
        
        var embed = new DiscordEmbedBuilder
        {
            Title = "Rang von " + user.Username,
            Color = BotConfig.GetEmbedColor()
        };
        
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
        using var bar = ImageUtils.CreateProgressBar(200, 20, percentage / 100f, $"{percentage}%");
        using var image = SKImage.FromBitmap(bar);
        using var stream = image.Encode(SKEncodedImageFormat.Png, 100).AsStream();
        stream.Position = 0;
        
        embed.WithTitle("Rang von " + user.Username);
        embed.WithDescription($"**Level:** {level}\n" +
                              $"**Rang:** {userRank}\n\n" +
                              $"**Fortschritt:** {Converter.FormatWithCommas(xpForThisLevelUntilNow)} / {Converter.FormatWithCommas(xpForThisLevel)} **XP**\n" +
                              $"**Gesamt XP:** {display_xp} XP");
        embed.WithThumbnail(user.AvatarUrl);
                              
        
        embed.WithImageUrl("attachment://progress.png");
        
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed).AddFile("progress.png", stream));
    }

}