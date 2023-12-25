#region

using AGC_Management.Attributes;
using AGC_Management.Utils.TempVoice;

#endregion

namespace AGC_Management.Commands.TempVC;

public sealed class UnhideChannelCommand : TempVoiceHelper
{
    [Command("unhide")]
    [RequireDatabase]
    public async Task VoiceUnhide(CommandContext ctx)
    {
        List<long> dbChannels = await GetChannelIDFromDB(ctx);
        DiscordChannel userChannel = ctx.Member?.VoiceState?.Channel;
        bool isMod = await IsChannelMod(userChannel, ctx.Member);

        if (userChannel == null || !dbChannels.Contains((long)userChannel?.Id) && !isMod)
        {
            await NoChannel(ctx);
            return;
        }

        if (userChannel != null && dbChannels.Contains((long)userChannel.Id) || userChannel != null && isMod)
        {
            var msg = await ctx.RespondAsync(
                "<a:loading_agc:1084157150747697203> **Lade...** Versuche Channel sichtbar zu machen...");
            DiscordRole default_role = ctx.Guild.EveryoneRole;
            DiscordChannel channel = ctx.Member.VoiceState.Channel;
            var overwrite = channel.PermissionOverwrites.FirstOrDefault(o => o.Id == default_role.Id);
            if (overwrite == null || overwrite?.CheckPermission(Permissions.AccessChannels) == PermissionLevel.Unset)
            {
                await msg.ModifyAsync("<:attention:1085333468688433232> Der Channel ist bereits **sichtbar**!");
                return;
            }

            var overwrites = userChannel.PermissionOverwrites.Select(x => x.ConvertToBuilder()).ToList();
            overwrites = overwrites.Merge(default_role, Permissions.None, Permissions.None, Permissions.AccessChannels);
            await userChannel.ModifyAsync(x => x.PermissionOverwrites = overwrites);

            await msg.ModifyAsync("<:success:1085333481820790944> Der Channel ist nun **sichtbar**!");
        }
    }
}