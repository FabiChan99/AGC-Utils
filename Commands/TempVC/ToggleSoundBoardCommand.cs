﻿#region

using AGC_Management.TempVoice;

#endregion

namespace AGC_Management.Commands.TempVC;

public sealed class ToggleSoundBoardCommand : TempVoiceHelper
{
    [Command("togglesoundboard")]
    [Aliases("vcsoundboard")]
    public async Task ToggleVcSoundboard(CommandContext ctx)
    {
        var dbChannels = await GetChannelIDFromDB(ctx);
        var userChannel = ctx.Member?.VoiceState?.Channel;

        var isMod = await IsChannelMod(userChannel, ctx.Member);

        if (userChannel == null || (!dbChannels.Contains((long)userChannel?.Id) && !isMod))
        {
            await NoChannel(ctx);
            return;
        }

        if ((userChannel != null && dbChannels.Contains((long)userChannel.Id)) || (userChannel != null && isMod))
        {
            var msg = await ctx.RespondAsync("Status des Soundboards wird geändert");
            var SBState = GetSoundboardState(userChannel);
            if (SBState)
            {
                await SetSoundboardState(userChannel, false);
                await msg.ModifyAsync(
                    "<:success:1085333481820790944> **Erfolg!** Das Soundboard ist nun **deaktiviert**!");
                return;
            }

            if (!SBState)
            {
                await SetSoundboardState(userChannel, true);
                await msg.ModifyAsync(
                    "<:success:1085333481820790944> **Erfolg!** Das Soundboard ist nun **aktiviert**!");
            }
        }
    }
}