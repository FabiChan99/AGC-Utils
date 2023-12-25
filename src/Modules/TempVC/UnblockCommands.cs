#region

using AGC_Management.Attributes;
using AGC_Management.Utils;
using AGC_Management.Utils.TempVoice;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Exceptions;

#endregion

namespace AGC_Management.Commands.TempVC;

public sealed class UnblockCommands : TempVoiceHelper
{
    [Command("unblock")]
    [RequireDatabase]
    [Aliases("vcunban", "multiunblock")]
    public async Task VoiceUnblock(CommandContext ctx, [RemainingText] string users)
    {
        _ = Task.Run(async () =>
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
                    var unblocklist = new List<ulong>();
                    List<ulong> ids = new();
                    ids = Converter.ExtractUserIDsFromString(users);
                    var staffrole = ctx.Guild.GetRole(GlobalProperties.StaffRoleId);
                    var msg = await ctx.RespondAsync(
                        $"<a:loading_agc:1084157150747697203> **Lade...** Versuche {ids.Count} Nutzer zu entsperren...");
                    var overwrites = userChannel.PermissionOverwrites.Select(x => x.ConvertToBuilder()).ToList();

                    foreach (ulong id in ids)
                    {
                        try
                        {
                            var user = await ctx.Guild.GetMemberAsync(id);


                            overwrites = overwrites.Merge(user, Permissions.None, Permissions.None,
                                Permissions.UseVoice);


                            unblocklist.Add(user.Id);
                        }
                        catch (NotFoundException)
                        {
                        }
                    }

                    await userChannel.ModifyAsync(x => x.PermissionOverwrites = overwrites);

                    int successCount = unblocklist.Count;
                    string endstring =
                        $"<:success:1085333481820790944> **Erfolg!** Es {(successCount == 1 ? "wurde" : "wurden")} {successCount} Nutzer erfolgreich **entsperrt**!";

                    await msg.ModifyAsync(endstring);
                }
            }
        );
    }
}