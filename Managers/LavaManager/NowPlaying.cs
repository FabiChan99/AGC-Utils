#region

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Lavalink;
using DisCatSharp.Lavalink.Entities;
using LavaSharp.LavaManager;

#endregion

namespace AGC_Management.Utils;

public class NowPlaying
{
    public static async Task sendNowPlayingTrack(InteractionContext ctx, LavalinkTrack track)
    {
        bool buttonevent = bool.Parse(BotConfig.GetConfig()["MusicConfig"]["SkipAndStopButtons"]);
        var embed = EmbedGenerator.GetCurrentTrackEmbed(track, CurrentPlayData.player);

        var skipButton =
            new DiscordButtonComponent(ButtonStyle.Primary, "skip", null, false, new DiscordComponentEmoji("⏭️"));
        var stopButton =
            new DiscordButtonComponent(ButtonStyle.Primary, "stop", null, false, new DiscordComponentEmoji("⏹️"));
        DiscordMessageBuilder mb;
        if (buttonevent)
        {
            mb = new DiscordMessageBuilder().AddEmbed(embed).AddComponents(skipButton, stopButton);
        }
        else
        {
            mb = new DiscordMessageBuilder().AddEmbed(embed);
        }

        var msg = await ctx.Channel.SendMessageAsync(mb);
        await TryRemoveButtonsFromMessage(CurrentPlayData.CurrentNowPlayingMessageId);
        CurrentPlayData.CurrentNowPlayingMessageId = msg.Id;
    }

    public static async Task sendNowPlayingTrack(InteractionCreateEventArgs ctx, LavalinkTrack track)
    {
        bool buttonevent = bool.Parse(BotConfig.GetConfig()["MusicConfig"]["SkipAndStopButtons"]);
        var embed = EmbedGenerator.GetCurrentTrackEmbed(track, CurrentPlayData.player);

        var skipButton =
            new DiscordButtonComponent(ButtonStyle.Primary, "skip", null, false, new DiscordComponentEmoji("⏭️"));
        var stopButton =
            new DiscordButtonComponent(ButtonStyle.Primary, "stop", null, false, new DiscordComponentEmoji("⏹️"));
        DiscordMessageBuilder mb;
        if (buttonevent)
        {
            mb = new DiscordMessageBuilder().AddEmbed(embed).AddComponents(skipButton, stopButton);
        }
        else
        {
            mb = new DiscordMessageBuilder().AddEmbed(embed);
        }

        var msg = await ctx.Interaction.Channel.SendMessageAsync(mb);
        await TryRemoveButtonsFromMessage(CurrentPlayData.CurrentNowPlayingMessageId);
        CurrentPlayData.CurrentNowPlayingMessageId = msg.Id;
    }

    public static async Task TryRemoveButtonsFromMessage(ulong messageId)
    {
        try
        {
            var msg = await CurrentApplication.DiscordClient.GetChannelAsync(CurrentPlayData.CurrentExecutionChannel.Id)
                .Result.GetMessageAsync(messageId);
            var embed = msg.Embeds.First();
            // no buttons
            if (msg.Components.Count == 0)
            {
                return;
            }

            var nmb = new DiscordMessageBuilder().AddEmbed(embed);
            await msg.ModifyAsync(nmb);
        }
        catch (Exception)
        {
            // ignored
        }
    }
}

[EventHandler]
public class NowPlayingEvent : ApplicationCommandsModule
{
    [Event]
    public static async Task ComponentInteractionCreated(DiscordClient client, ComponentInteractionCreateEventArgs e)
    {
        if (e.Id == "skip")
        {
            _ = Task.Run(async () => // SkipTask
            {
                if (e.Message.Id != CurrentPlayData.CurrentNowPlayingMessageId)
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent("This menu is expired!").AsEphemeral());
                    return;
                }

                var player = CurrentApplication.DiscordClient.GetLavalink().GetGuildPlayer(e.Guild);
                if (player?.CurrentTrack is null)
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("I'm not connected to a voice channel or there is no track playing!")
                            .AsEphemeral());
                }

                if (!CheckIfSameVoiceChannel(e.Interaction))
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("You must be in the same voice channel as me!").AsEphemeral());
                    return;
                }

                if (!await checkDj(e))
                {
                    return;
                }

                if (LavaQueue.queue.Count == 0)
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("There are no songs in the queue! You can't skip!").AsEphemeral());
                    return;
                }

                var nextTrack = LavaQueue.queue.Dequeue();
                CurrentPlayData.track = nextTrack.Item1;
                CurrentPlayData.user = nextTrack.Item2;
                await player.PlayAsync(nextTrack.Item1);
                await e.Interaction.Channel.SendMessageAsync(
                    $"⏭️ | Skipped the current song with button. -> ``{e.Interaction.User.UsernameWithGlobalName}``");
                await NowPlaying.sendNowPlayingTrack(e, nextTrack.Item1);
            });
        }
        else if (e.Id == "stop")
        {
            _ = Task.Run(async () => // StopTask
            {
                if (e.Message.Id != CurrentPlayData.CurrentNowPlayingMessageId)
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent("This menu is expired!").AsEphemeral());
                    return;
                }

                var player = CurrentApplication.DiscordClient.GetLavalink().GetGuildPlayer(e.Guild);
                if (player?.CurrentTrack is null)
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("I'm not connected to a voice channel or there is no track playing!")
                            .AsEphemeral());
                }

                if (!CheckIfSameVoiceChannel(e.Interaction))
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("You must be in the same voice channel as me!").AsEphemeral());
                    return;
                }

                if (!await checkDj(e))
                {
                    return;
                }

                await LavaQueue.DisconnectAndReset(player);
                await e.Interaction.Channel.SendMessageAsync(
                    $"⏹️ | Stopped the player with button. -> ``{e.Interaction.User.UsernameWithGlobalName}``");
            });
        }
    }

    private static bool CheckIfSameVoiceChannel(DiscordInteraction interaction)
    {
        try
        {
            var member = interaction.User as DiscordMember;
            var channel = member?.VoiceState?.Channel;
            var player = CurrentApplication.DiscordClient.GetLavalink().GetGuildPlayer(interaction.Guild);
            if (player?.Channel.Id != channel?.Id)
            {
                return false;
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static async Task<bool> checkDj(ComponentInteractionCreateEventArgs ctx)
    {
        var member = await ctx.User.ConvertToMember(ctx.Guild);
        if (member.VoiceState?.Channel is null)
        {
            DiscordEmbedBuilder errorembed =
                EmbedGenerator.GetErrorEmbed("Du musst in einem VoiceChannel sein um diesen Command zu verwenden!");
            await ctx.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorembed).AsEphemeral());
            return false;
        }

        IReadOnlyCollection<DiscordMember> voicemembers = member.VoiceState.Channel.Users;
        bool isPlaying = false;
        try
        {
            var ct = CurrentApplication.DiscordClient.GetLavalink().ConnectedSessions!.First().Value
                .GetGuildPlayer(ctx.Guild).CurrentTrack;
            if (ct != null)
            {
                isPlaying = true;
            }
        }
        catch (Exception)
        {
            isPlaying = false;
        }

        if (!isPlaying)
        {
            if (voicemembers.Count == 1)
            {
                return true;
            }
        }
        else if (isPlaying)
        {
            if (voicemembers.Count == 2)
            {
                if (voicemembers.Any(x => x.Id == CurrentApplication.DiscordClient.CurrentUser.Id) &&
                    voicemembers.Any(x => x.Id == member.Id))
                {
                    return true;
                }
            }
        }

        bool djconf = bool.Parse(BotConfig.GetConfig()["MusicConfig"]["RequireDJRole"]);
        if (!djconf)
        {
            return true;
        }

        bool isDJ = false;
        bool isCtxAdminorManager = member.Permissions.HasPermission(Permissions.Administrator) ||
                                   member.Permissions.HasPermission(Permissions.ManageGuild);
        if (isCtxAdminorManager)
        {
            isDJ = true;
        }

        var checkRoleifuserhasdj = member.Roles.Any(x => x.Name == "DJ");
        if (checkRoleifuserhasdj)
        {
            isDJ = true;
        }

        if (!isDJ)
        {
            DiscordEmbedBuilder errorembed =
                EmbedGenerator.GetErrorEmbed("Du musst die Rolle ``DJ`` haben, um den Command zu benutzen.");
            await ctx.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorembed).AsEphemeral());
            return false;
        }

        return isDJ;
    }
}