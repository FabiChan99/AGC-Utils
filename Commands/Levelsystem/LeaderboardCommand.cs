#region

using System.Text;
using AGC_Management.Utils;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;

#endregion

namespace AGC_Management.Commands.Levelsystem;

public class LeaderboardCommand : ApplicationCommandsModule
{
    [SlashCommand("leaderboard", "XP Leaderboard")]
    public static async Task Leaderboard(InteractionContext ctx,
        [Option("user", "Der Nutzer von dem das Leaderboard angezeigt werden soll.")]
        DiscordUser user = null)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent(
                "<a:loading_agc:1084157150747697203> Leaderboard wird geladen..."));
        if (user == null)
        {
            user = ctx.User;
        }

        if (user.IsBot)
        {
            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent(
                    "<:attention:1085333468688433232> **Fehler!** Dies ist ein Bot!"));
            return;
        }

        var leaderboardData = await LevelUtils.FetchLeaderboardData();
        var userRank = await LevelUtils.GetUserRankAsync(user.Id);

        var descriptionBuilder = new StringBuilder();

        for (int i = 0; i < 5; i++)
        {
            if (i < leaderboardData.Count)
            {
                var user_ = leaderboardData[i];
                var user__ = await ctx.Client.GetUserAsync(user_.UserId);
                var Uname = user__.GetFormattedUserName();
                string lineText =
                    $"**#{i + 1} - {Uname}** - {Converter.FormatWithCommas(user_.XP)} XP / Level {user_.Level}";
                if (user.Id == user_.UserId)
                {
                    lineText =
                        $"__**#{i + 1} - {Uname}** - {Converter.FormatWithCommas(user_.XP)} XP / Level {user_.Level}__"; // Underline for invoking user
                }

                descriptionBuilder.AppendLine(lineText);
            }
        }

        descriptionBuilder.AppendLine("..................");


        int startRange = Math.Max(userRank - 5, 5);
        int endRange = Math.Min(userRank + 5, leaderboardData.Count);

        if (userRank <= 5)
        {
            startRange = 5;
        }


        for (int i = startRange; i < endRange; i++)
        {
            var user_ = leaderboardData[i];
            var user__ = await ctx.Client.GetUserAsync(user_.UserId);
            var Uname = user__.GetFormattedUserName();
            string lineText =
                $"**#{i + 1} - {Uname}** - {Converter.FormatWithCommas(user_.XP)} XP / Level {user_.Level}";
            if (user.Id == user_.UserId)
            {
                lineText =
                    $"__**#{i + 1} - {Uname}** - {Converter.FormatWithCommas(user_.XP)} XP / Level {user_.Level}__"; // Underline for invoking user
            }

            descriptionBuilder.AppendLine(lineText);
        }

        var embedBuilder = new DiscordEmbedBuilder
        {
            Title = $"XP Bestenliste für {ctx.Guild.Name}",
            Description = descriptionBuilder.ToString(),
            Color = DiscordColor.Blurple
        };

        embedBuilder.WithFooter("AGC Leveling System");

        var emb = embedBuilder.Build();
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(emb));
    }
}