#region

using AGC_Management.Attributes;
using AGC_Management.TempVoice;

#endregion

namespace AGC_Management.Commands.TempVC;

public sealed class HideChannelCommand : TempVoiceHelper
{
    [Command("hide")]
    [RequireDatabase]
    public async Task VoiceHide(CommandContext ctx)
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
            var msg = await ctx.RespondAsync(
                "<a:loading_agc:1084157150747697203> **Lade...** Versuche Channel zu verstecken...");
            var default_role = ctx.Guild.EveryoneRole;
            var channel = ctx.Member.VoiceState.Channel;
            var overwrite = channel.PermissionOverwrites.FirstOrDefault(o => o.Id == default_role.Id);
            if (overwrite?.CheckPermission(Permissions.AccessChannels) == PermissionLevel.Denied)
            {
                await msg.ModifyAsync("<:attention:1085333468688433232> Der Channel ist bereits **versteckt**!");
                return;
            }


            var overwrites = userChannel.PermissionOverwrites.Select(x => x.ConvertToBuilder()).ToList();
            overwrites = overwrites.Merge(default_role, Permissions.None, Permissions.AccessChannels);
            await userChannel.ModifyAsync(x => x.PermissionOverwrites = overwrites);
            await msg.ModifyAsync("<:success:1085333481820790944> Du hast den Channel erfolgreich **versteckt**!");
        }
    }
}