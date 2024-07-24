#region

using AGC_Management.Attributes;
using AGC_Management.TempVoice;

#endregion

namespace AGC_Management.Commands.TempVC;

public sealed class SetChannelLimitStatus : TempVoiceHelper
{
    [Command("limit")]
    [RequireDatabase]
    [Aliases("vclimit")]
    public async Task VoiceLimit(CommandContext ctx, int limit)
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
            if (limit < 0 || limit > 99)
            {
                await ctx.RespondAsync(
                    "<:attention:1085333468688433232> **Fehler!** Der Limit-Wert muss zwischen 0 und 99 liegen.");
                return;
            }

            if (limit == 0)
                await ctx.RespondAsync(
                    "<:success:1085333481820790944> Du hast das Userlimit erfolgreich **entfernt**.");

            await userChannel.ModifyAsync(x => x.UserLimit = limit);
            await ctx.RespondAsync(
                $"<:success:1085333481820790944> Du hast {userChannel.Mention} erfolgreich ein Userlimit von **{limit}** gesetzt.");
        }
    }
}