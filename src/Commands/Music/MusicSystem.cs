using AGC_Management.Helpers;
using AGC_Management.Helpers.Lavalink;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Lavalink;
using DisCatSharp.Lavalink.EventArgs;

namespace AGC_Management.Commands.Music;

public class MusicSystem : LavalinkHelper
{
    private readonly Dictionary<ulong, bool> _loopStates = new();
    private readonly Dictionary<ulong, Queue<LavalinkTrack>> _queues = new();

    private async Task DisconnectAndReset(LavalinkGuildConnection connection, ulong guildId)
    {
        await connection.DisconnectAsync();

        if (_queues.ContainsKey(guildId))
        {
            _queues.Remove(guildId);
        }

        if (_loopStates.ContainsKey(guildId))
        {
            _loopStates.Remove(guildId);
        }
    }

    private async Task PlaybackFinished(LavalinkGuildConnection sender, TrackFinishEventArgs e, CommandContext ctx)
    {
        if (_loopStates.TryGetValue(sender.Guild.Id, out var loop) && loop)
        {
            await sender.PlayAsync(e.Track);
            var eb = new DiscordEmbedBuilder()
                .WithDescription($"Loop aktiviert: Spielt den aktuellen Titel erneut: \n{GetTrackInfo(e.Track)}")
                .WithColor(BotConfig.GetEmbedColor())
                .WithAuthor(e.Track.Title, iconUrl: ctx.Client.CurrentUser.AvatarUrl, url: e.Track.Title);
            await ctx.RespondAsync($"Loop aktiviert: Spielt den aktuellen Titel erneut: {GetTrackInfo(e.Track)}");
        }
        else if (_queues.TryGetValue(sender.Guild.Id, out var queue) && queue.Count > 0)
        {
            var nextTrack = queue.Dequeue();
            await sender.PlayAsync(nextTrack);

            int remainingTracks = queue.Count;
            string message = $"Spiele nächsten Titel: {GetTrackInfo(nextTrack)}\n";
            if (remainingTracks > 0)
            {
                message += $"Verbleibende Songs in der Warteschlange: {remainingTracks}";
            }
            else
            {
                message += "Keine weiteren Songs in der Warteschlange.";
            }

            await ctx.RespondAsync(message);
        }
        else if (sender.CurrentState.CurrentTrack == null)
        {
            await DisconnectAndReset(sender, ctx.Guild.Id);
        }
    }

    private static async Task<(bool, DiscordChannel)> ChannelMatchWithBot(CommandContext ctx, DiscordChannel channel)
    {
        DiscordChannel botVoiceChannel = ctx.Guild.CurrentMember.VoiceState?.Channel;
        if (botVoiceChannel != null && botVoiceChannel.Id != channel.Id)
        {
            await ctx.RespondAsync("Du musst im gleichen Voice Channel wie der Bot sein.");
            return (true, null);
        }


        return (false, botVoiceChannel);
    }

    private static async Task<(bool, DiscordChannel)> _ChannelMatchWithBot(CommandContext ctx, DiscordChannel channel)
    {
        DiscordChannel botVoiceChannel = ctx.Guild.CurrentMember.VoiceState?.Channel;
        if (botVoiceChannel != null && botVoiceChannel.Id != channel.Id)
        {
            await ctx.RespondAsync("Du musst im gleichen Voice Channel wie der Bot sein.");
            return (true, null);
        }

        if (botVoiceChannel == null)
        {
            await ctx.RespondAsync("Der Bot ist in keinem Voice Channel.");
            return (true, null);
        }

        return (false, botVoiceChannel);
    }

    private void ShowCurrentTrack(CommandContext ctx)
    {
        if (_queues.TryGetValue(ctx.Guild.Id, out var queue) && queue.Count > 0)
        {
            var currentTrack = queue.Peek();
            // Zeige das aktuelle Lied in der Warteschlange an
            ctx.RespondAsync($"Nächstes Lied in der Warteschlange: {GetTrackInfo(currentTrack)}");
        }
    }

    [RequireVoiceChannel]
    [Command("loop")]
    public async Task ToggleLoop(CommandContext ctx)
    {
        ulong guildId = ctx.Guild.Id;
        var channel = ctx.Member?.VoiceState?.Channel;
        (bool match, DiscordChannel botChannel) = await _ChannelMatchWithBot(ctx, channel);
        if (match)
        {
            return;
        }

        if (_loopStates.ContainsKey(guildId))
        {
            _loopStates[guildId] = !_loopStates[guildId];
        }
        else
        {
            _loopStates[guildId] = true;
        }

        await ctx.RespondAsync($"Loop-Status geändert: {(_loopStates[guildId] ? "Aktiviert" : "Deaktiviert")}");
    }


    [RequireVoiceChannel]
    [RequireLavalink]
    [Command("play")]
    public async Task PlayMusic(CommandContext ctx, [RemainingText] string query)
    {
        await PlayMusicInternal(ctx, query);
    }

    [RequireVoiceChannel]
    [RequireLavalink]
    [Command("play")]
    public async Task PlayMusic(CommandContext ctx, Uri url)
    {
        await PlayMusicInternal(ctx, url.ToString());
    }


    private async Task PlayMusicInternal(CommandContext ctx, string query)
    {
        var channel = ctx.Member?.VoiceState?.Channel;
        (bool match, DiscordChannel botChannel) = await ChannelMatchWithBot(ctx, channel);
        if (match)
        {
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

        if (trackLoadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
        {
            DiscordEmbedBuilder embed = new()
            {
                Title = "Fehler",
                Description = "Es konnte kein Titel gefunden werden.",
                Color = DiscordColor.Red
            };

            await ctx.RespondAsync(embed.Build());
            await DisconnectAndReset(player, ctx.Guild.Id);
            return;
        }

        if (trackLoadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
        {
            DiscordEmbedBuilder embed = new()
            {
                Title = "Fehler",
                Description = "Das Abspielen ist fehlgeschlagen.",
                Color = DiscordColor.Red
            };

            await ctx.RespondAsync(embed.Build());
            await DisconnectAndReset(player, ctx.Guild.Id);
            return;
        }


        player.PlaybackFinished += (sender, e) => PlaybackFinished(sender, e, ctx);


        var track = trackLoadResult.Tracks.First();

        if (player.CurrentState.CurrentTrack != null)
        {
            if (!_queues.ContainsKey(ctx.Channel.Guild.Id))
            {
                _queues[channel.Guild.Id] = new Queue<LavalinkTrack>();
            }

            _queues[channel.Guild.Id].Enqueue(track);
            int queueSize = _queues[channel.Guild.Id].Count;
            await ctx.RespondAsync(
                $"In die Warteschlange eingereiht: {GetTrackInfo(track)}\nWarteschlange: {queueSize} Titel");
        }
        else
        {
            await player.PlayAsync(track);
            await ctx.RespondAsync($"Wiedergabe: {GetTrackInfo(track)}");
        }
    }
}