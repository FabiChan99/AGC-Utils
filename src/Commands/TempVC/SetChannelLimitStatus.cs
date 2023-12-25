using AGC_Management.Attributes;
using AGC_Management.Utils.TempVoice;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;

namespace AGC_Management.Commands.TempVC;

public sealed class SetChannelLimitStatus : TempVoiceHelper
{
    

    [Command("limit")]
    [RequireDatabase]
    [Aliases("vclimit")]
    public async Task VoiceLimit(CommandContext ctx, int limit)
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
            if (limit < 0 || limit > 99)
            {
                await ctx.RespondAsync(
                    "<:attention:1085333468688433232> **Fehler!** Der Limit-Wert muss zwischen 0 und 99 liegen.");
                return;
            }

            if (limit == 0)
            {
                await ctx.RespondAsync(
                    "<:success:1085333481820790944> Du hast das Userlimit erfolgreich **entfernt**.");
            }

            await userChannel.ModifyAsync(x => x.UserLimit = limit);
            await ctx.RespondAsync(
                $"<:success:1085333481820790944> Du hast {userChannel.Mention} erfolgreich ein Userlimit von **{limit}** gesetzt.");
        }
    }


}