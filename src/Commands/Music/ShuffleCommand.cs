using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Lavalink;
using AGC_Management.Attributes;
using AGC_Management.Helpers;
using AGC_Management.LavaManager;
using LavaSharp.LavaManager;

namespace AGC_Management.Commands;

public class ShuffleCommand : ApplicationCommandsModule
{
    [RequireConnectedLavalink]
    [EnsureGuild]
    [EnsureMatchGuildId]
    [ApplicationRequireExecutorInVoice]
    [RequireRunningPlayer]
    [CheckDJ]
    [SlashCommand("shuffle", "Shuffled die Queue.")]
    public static async Task Shuffle(InteractionContext ctx)
    {
        var queue = LavaQueue.queue;
        var lava = ctx.Client.GetLavalink();
        var node = lava.ConnectedSessions.First().Value;
        var player = node?.GetGuildPlayer(ctx.Guild);
        var channel = ctx.Member?.VoiceState?.Channel;

        if (player?.Channel.Id != channel?.Id)
        {
            var errorEmbed = EmbedGenerator.GetErrorEmbed("Du musst mit mir in einem Voice Channel sein.");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
            return;
        }

        if (queue.Count < 2)
        {
            var errorEmbed = EmbedGenerator.GetErrorEmbed("Es müssen mindestens 2 Songs in der Queue sein.");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
            return;
        }

        try
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("🔀 | Shuffling..."));
            var newQueue = queue.OrderBy(a => Guid.NewGuid()).ToList();
            queue.Clear();
            foreach (var item in newQueue)
            {
                queue.Enqueue(item);
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("🔀 | Die Queue wurde geshuffled!"));
        }
        catch (Exception e)
        {
            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent(
                    $"🔀 | Es ist ein Fehler aufgetreten: {e.Message}"));
        }
    }
}