#region

using AGC_Management.Attributes;
using AGC_Management.Helpers;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Lavalink;

#endregion

namespace AGC_Management.Commands.Music;

public sealed class VolumeCommand : ApplicationCommandsModule
{
    [RequireConnectedLavalink]
    [EnsureGuild]
    [EnsureMatchGuildId]
    [RequireRunningPlayer]
    [ApplicationRequireExecutorInVoice]
    [CheckDJ]
    [SlashCommand("Volume", "Changes the volume of the player.")]
    public static async Task Volume(InteractionContext ctx, [Option("percentage", "The volume to set.")] int volume)
    {
        var lava = ctx.Client.GetLavalink();
        var node = lava.ConnectedSessions.First().Value;
        var player = node.GetGuildPlayer(ctx.Guild);
        var channel = ctx.Member?.VoiceState?.Channel;
        int maxvol = 150;

        if (player?.Channel.Id != channel?.Id)
        {
            var errorEmbed = EmbedGenerator.GetErrorEmbed("Du musst mit mir in einem Voice Channel sein.");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
            return;
        }

        if (volume > maxvol || volume < 0)
        {
            var errorEmbed = EmbedGenerator.GetErrorEmbed($"Volume must be between 0 and {maxvol}.");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
            return;
        }

        CurrentPlayData.CurrentVolume = volume;
        await player.SetVolumeAsync(volume);
        var volstr = $"🔊 | Volume set to ``{volume}%``.";
        if (volume > 100)
        {
            volstr =
                $"🔊 | Volume set to ``{volume}%``. Warning: Volume is above 100%. This may cause audio distortion.";
        }

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent(volstr));
    }
}