#region

using AGC_Management.Attributes;
using AGC_Management.Helpers;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Lavalink;
using DisCatSharp.Lavalink.Entities;
using LavaSharp.LavaManager;

#endregion

namespace AGC_Management.Commands.Music;

public sealed class SkipCommand : ApplicationCommandsModule
{
    [RequireConnectedLavalink]
    [EnsureGuild]
    [EnsureMatchGuildId]
    [ApplicationRequireExecutorInVoice]
    [RequireRunningPlayer]
    [CheckDJ]
    [SlashCommand("skip", "Überspringt den aktuellen Song")]
    public async Task Skip(InteractionContext ctx,
        [Option("number_of_tracks", "[Optional] Die Anzahl der zu überspringenden Songs")]
        int tracksToSkip = 1)
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

        if (queue.Count == 0)
        {
            var errorEmbed = EmbedGenerator.GetErrorEmbed("Es sind keine Songs in der Queue.");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
            return;
        }

        if (tracksToSkip > queue.Count || tracksToSkip < 1)
        {
            var errorEmbed =
                EmbedGenerator.GetErrorEmbed(
                    $"Die Anzahl der zu überspringenden Songs muss zwischen 1 und der Queue-Länge ({queue.Count}) liegen.");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
            return;
        }

        if (tracksToSkip == 1)
        {
            var track = queue.Dequeue();

            CurrentPlayData.track = track.Item1;
            CurrentPlayData.user = track.Item2;
            await player.PlayAsync(track.Item1);
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("⏭️ | Der aktuelle Song wurde übersprungen."));
            await NowPlaying.sendNowPlayingTrack(ctx, track.Item1);
            return;
        }

        if (tracksToSkip >= 2)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent($"⏭️ | Überspringe ``{tracksToSkip}`` Songs....."));
            LavalinkTrack targettrack = null;
            for (int i = 0; i < tracksToSkip; i++)
            {
                var track = queue.Dequeue();
                CurrentPlayData.track = track.Item1;
                CurrentPlayData.user = track.Item2;
                targettrack = track.Item1;
            }

            await player.PlayAsync(targettrack);
            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent($"⏭️ | ``{tracksToSkip}`` Songs wurden übersprungen."));
            await NowPlaying.sendNowPlayingTrack(ctx, targettrack);
        }
    }
}