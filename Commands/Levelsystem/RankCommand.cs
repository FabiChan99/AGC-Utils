using AGC_Management.Utils;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;

namespace AGC_Management.Commands.Levelsystem;

public class RankCommand : ApplicationCommandsModule
{
    [SlashCommand("rank", "Zeigt den aktuellen Rang eines Nutzers an.")]
    public static async Task Rank(InteractionContext ctx, [Option("user", "Der Nutzer von dem der Rang angezeigt werden soll.")] DiscordUser user = null)
    {
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
        var xp = rank[user.Id].Xp;
        var display_xp = Converter.FormatWithCommas(xp);
        var xpForCurrentLevel = LevelUtils.XpForLevel(level);
        var xpForNextLevel = LevelUtils.XpForLevel(level + 1);
        var xpForThisLevel = xpForNextLevel - xpForCurrentLevel;
        // TODO: Add levelcompletionprogress bspw 58000 / 80620 (72%)
        var xpForThisLevelUntilNow = xp - xpForCurrentLevel;
        var percentage = (int)(xpForThisLevelUntilNow / (float)xpForThisLevel * 100);
        var percentageForProgressBar = (int)(percentage / 5.0);
        string progressBar = "";
        for (int i = 0; i < 20; i++)
        {
            if (i < percentageForProgressBar)
            {
                progressBar += "#";
            }
            else
            {
                progressBar += "-";
            }
        }
        embed.AddField(new DiscordEmbedField("Level", level.ToString()));
        embed.AddField(new DiscordEmbedField("XP", display_xp));
        embed.AddField(new DiscordEmbedField("XP Fortschritt", progressBar));
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed));
    }
}