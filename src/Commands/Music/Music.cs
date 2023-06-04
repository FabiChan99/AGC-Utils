using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Common.Utilities;
using DisCatSharp.Enums;
using DisCatSharp.Lavalink;
using System.Threading.Channels;
using AGC_Management.Helpers.Lavalink;
using DisCatSharp.ApplicationCommands.Attributes;
using AGC_Management.Helpers;
using System.Xml.Linq;
using System.Numerics;
using DisCatSharp.Lavalink.EventArgs;
using System.Web;

namespace AGC_Management.Commands.Music;

public class MusicSystem : LavalinkHelper
{
    private Dictionary<ulong, Queue<LavalinkTrack>> _queues = new Dictionary<ulong, Queue<LavalinkTrack>>();

    private async Task PlaybackFinished(LavalinkGuildConnection sender, TrackFinishEventArgs e)
    {
        if (_queues.TryGetValue(sender.Guild.Id, out var queue) && queue.Count > 0)
        {
            var nextTrack = queue.Dequeue();
            await sender.PlayAsync(nextTrack);
        }
        else if (sender.CurrentState.CurrentTrack == null)
        {
            await sender.DisconnectAsync();
        }
    }



    [RequireLavalink]
    [Command("play")]
    public async Task PlayMusic(CommandContext ctx, [RemainingText] string query)
    {
        await PlayMusicInternal(ctx, query);
    }
    [RequireLavalink]
    [Command("play")]
    public async Task PlayMusic(CommandContext ctx, Uri url)
    {
        await PlayMusicInternal(ctx, url.ToString());
    }


    private async Task PlayMusicInternal(CommandContext ctx, string query)
    {
        var channel = ctx.Member?.VoiceState?.Channel;
        if (channel == null)
        {
            await ctx.RespondAsync("Du musst in einem Voice Channel sein.");
            return;
        }

        var botVoiceChannel = ctx.Guild.CurrentMember.VoiceState.Channel;
        if (botVoiceChannel != null && botVoiceChannel.Id != channel.Id)
        {
            await ctx.RespondAsync("Du musst im gleichen Voice Channel wie der Bot sein.");
            return;
        }

        (LavalinkExtension lava, bool connected) = await ConnectToVoice(ctx);
        if (!connected)
        {
            // TODO: Add error message
            return;
        }


        var lavalinkNode = lava.ConnectedNodes.Values.First();


        query = HandleYoutubeLink(query);

        var trackLoadResult = await lavalinkNode.Rest.GetTracksAsync(query);
        var player = lavalinkNode.GetGuildConnection(ctx.Guild);

        if (trackLoadResult.LoadResultType == LavalinkLoadResultType.LoadFailed ||
            trackLoadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
        {
            await ctx.RespondAsync("Failed to load the track.");
            await player.DisconnectAsync();
            return;
        }

        
        player.PlaybackFinished += PlaybackFinished;

        var track = trackLoadResult.Tracks.First();

        if (player.CurrentState.CurrentTrack != null)
        {
            if (!_queues.ContainsKey(ctx.Channel.Guild.Id))
            {
                _queues[channel.Guild.Id] = new Queue<LavalinkTrack>();
            }
            _queues[channel.Guild.Id].Enqueue(track);
            int queueSize = _queues[channel.Guild.Id].Count;
            await ctx.RespondAsync($"In die Warteschlange eingereiht: {GetTrackInfo(track)}\nWarteschlange: {queueSize} Titel");
        }
        else
        {
            await player.PlayAsync(track);
            await ctx.RespondAsync($"Wiedergabe: {GetTrackInfo(track)}");
        }
    }


}
