#region

using AGC_Management.Utils;
using AGC_Management.Utils.TempVoice;
using DisCatSharp.Exceptions;

#endregion

namespace AGC_Management.Commands.TempVC;

public sealed class VoiceKickCommand : TempVoiceHelper
{
    [Command("vckick")]
    public async Task VoiceKick(CommandContext ctx, [RemainingText] string users)
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
                    var kicklist = new List<ulong>();
                    List<ulong> ids = new();
                    ids = Converter.ExtractUserIDsFromString(users);
                    var staffrole = ctx.Guild.GetRole(GlobalProperties.StaffRoleId);
                    var msg = await ctx.RespondAsync(
                        $"<a:loading_agc:1084157150747697203> **Lade...** Versuche {ids.Count} Nutzer zu kicken...");
                    var overwrites = userChannel.PermissionOverwrites.Select(x => x.ConvertToBuilder()).ToList();
                    var currentmods = await RetrieveChannelMods(userChannel);
                    var channelownerid = await GetChannelOwnerID(ctx);
                    var owner = await ctx.Client.GetUserAsync((ulong)channelownerid);
                    foreach (ulong id in ids)
                    {
                        try
                        {
                            var user = await ctx.Guild.GetMemberAsync(id);

                            if (user.Roles.Contains(staffrole))
                            {
                                continue;
                            }

                            List<ulong> mods = await RetrieveChannelMods(userChannel);
                            if (id == ctx.User.Id || mods.Contains(id))
                            {
                                continue;
                            }

                            if (id == owner.Id)
                            {
                                continue;
                            }

                            if (userChannel.Users.Contains(user) && !user.Roles.Contains(staffrole))
                            {
                                await user.DisconnectFromVoiceAsync();
                            }

                            kicklist.Add(user.Id);
                            try
                            {
                                currentmods.Remove(id);
                            }
                            catch (Exception)
                            {
                            }
                        }
                        catch (NotFoundException)
                        {
                        }
                    }

                    await userChannel.ModifyAsync(x => x.PermissionOverwrites = overwrites);
                    try
                    {
                        await UpdateChannelMods(userChannel, currentmods);
                    }
                    catch (Exception)
                    {
                    }

                    int successCount = kicklist.Count;
                    string endstring =
                        $"<:success:1085333481820790944> **Erfolg!** Es {(successCount == 1 ? "wurde" : "wurden")} {successCount} Nutzer erfolgreich **gekickt**!";

                    await msg.ModifyAsync(endstring);
                }
            }
        );
    }
}