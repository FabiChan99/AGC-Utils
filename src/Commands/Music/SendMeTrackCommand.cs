using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Lavalink;
using AGC_Management.Attributes;
using AGC_Management.Helpers;

namespace AGC_Management.Commands;

public class SendMeTrackCommand : ApplicationCommandsModule
{
    [RequireConnectedLavalink]
    [EnsureGuild]
    [EnsureMatchGuildId]
    [ApplicationRequireExecutorInVoice]
    [RequireRunningPlayer]
    [SlashCommand("sendmetrack", "Sendet dir den aktuellen Song per DM.")]
    public static async Task SendMeTrack(InteractionContext ctx)
    {
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
        
        // send the track to the user
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent(
                $"🔂 | Ich habe dir den Song per DM gesendet!"));
        // send the track to the user per dm
        var em = new DiscordEmbedBuilder()
            .WithTitle("Aktueller Song")
            .WithDescription($"[{player.CurrentTrack.Info.Title}]({player.CurrentTrack.Info.Uri})")
            .WithColor(DiscordColor.Blurple)
            .WithFooter($"Requested by {ctx.Member.DisplayName}", ctx.Member.AvatarUrl)
            .Build();
        bool success = false;
        try {
            await ctx.Member.SendMessageAsync(embed: em);
            success = true;
        } catch (Exception e) {
            Console.WriteLine(e);
        }

        if (!success)
        {
            // follow up message
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent("❌ | Ich konnte dir den Song nicht per DM senden. Hast du DMs von Servermitgliedern deaktiviert? Wenn ja, aktiviere sie bitte und versuche es erneut."));
        }
        
        
        
        
        
        
        
        
    }
}