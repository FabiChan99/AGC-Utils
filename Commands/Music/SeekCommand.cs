#region

using AGC_Management.Attributes;
using AGC_Management.Utils;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Lavalink;

#endregion

namespace AGC_Management.Commands.Music;

public sealed class SeekCommand : ApplicationCommandsModule
{
    [RequireConnectedLavalink]
    [EnsureGuild]
    [EnsureMatchGuildId]
    [ApplicationRequireExecutorInVoice]
    [RequireRunningPlayer]
    [CheckDJ]
    [SlashCommand("seek", "Spult zu einer bestimmten Zeit im aktuellen Song.")]
    public async Task Seek(InteractionContext ctx, [Option("time", "Zeitangabe")] string timeString)
    {
        TimeSpan time;

        // Parse the time here. Assuming timeString is in the format "hh:mm:ss" or "mm:ss"
        if (!TimeSpan.TryParseExact(timeString, new[] { "hh\\:mm\\:ss", "mm\\:ss" }, null, out time))
        {
            var errorEmbed =
                EmbedGenerator.GetErrorEmbed("Ungültige Zeitangabe. Bitte benutze ``hh:mm:ss`` oder ``mm:ss``");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
            return;
        }

        var lava = ctx.Client.GetLavalink();
        var node = lava.ConnectedSessions.First().Value;
        var player = node.GetGuildPlayer(ctx.Guild);
        var channel = ctx.Member.VoiceState?.Channel;

        if (player?.Channel.Id != channel?.Id)
        {
            var errorEmbed = EmbedGenerator.GetErrorEmbed("Du musst mit mir in einem Voice Channel sein.");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
            return;
        }

        if (player?.CurrentTrack == null)
        {
            var errorEmbed = EmbedGenerator.GetErrorEmbed("Es wird kein Song abgespielt.");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
            return;
        }

        if (time > player.CurrentTrack.Info.Length)
        {
            var errorEmbed = EmbedGenerator.GetErrorEmbed("Die Zeitangabe ist größer als die Länge des Songs.");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
            return;
        }

        await player.SeekAsync(time);
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent($"⏩ | Gespult zu ``{time}``"));
    }
}