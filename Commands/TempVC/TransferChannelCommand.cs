#region

using AGC_Management.Attributes;
using AGC_Management.Services;
using AGC_Management.TempVoice;

#endregion

namespace AGC_Management.Commands.TempVC;

public sealed class TransferChannelCommand : TempVoiceHelper
{
    [Command("transfer")]
    [Aliases("transferowner", "transferownership")]
    [RequireDatabase]
    public async Task TransferOwner(CommandContext ctx, DiscordMember member)
    {
        if (SelfCheck(ctx, member)) return;
        var my_channels = await GetChannelIDFromDB(ctx);
        var db_channels = await GetAllChannelIDsFromDB();
        var userchannel = (long?)ctx.Member?.VoiceState?.Channel?.Id;
        var userchannelobj = ctx.Member?.VoiceState?.Channel;
        var channelownerid = await GetChannelOwnerID(ctx);
        if (userchannelobj == null)
        {
            await NoChannel(ctx);
            return;
        }

        DiscordUser owner = await ctx.Client.GetUserAsync((ulong)channelownerid);
        var conv_to_member = await ctx.Guild.GetMemberAsync(owner.Id);
        DiscordMember mowner = conv_to_member;
        var orig_owner = mowner;
        var new_owner = member;
        if (!my_channels.Contains((long)userchannel))
        {
            await NoChannel(ctx);
            return;
        }

        var msg = await ctx.RespondAsync(
            "<a:loading_agc:1084157150747697203> **Lade...** Versuche Channel zu übertragen...");
        if (userchannelobj.Users.Contains(orig_owner) && db_channels.Contains((long)userchannel) &&
            userchannelobj.Users.Contains(new_owner))
        {
            await using (NpgsqlConnection conn = new(DatabaseService.GetConnectionString()))
            {
                await conn.OpenAsync();
                string sql = "UPDATE tempvoice SET ownerid = @userid WHERE channelid = @channelid";
                await using (NpgsqlCommand command = new(sql, conn))
                {
                    command.Parameters.AddWithValue("@userid", (long)new_owner.Id);
                    command.Parameters.AddWithValue("@channelid", (long)userchannel);
                    int affected = await command.ExecuteNonQueryAsync();
                }
            }

            var overwrites = userchannelobj.PermissionOverwrites.Select(x => x.ConvertToBuilder()).ToList();
            overwrites = overwrites.Merge(new_owner,
                Permissions.AccessChannels | Permissions.UseVoice | Permissions.ManageChannels |
                Permissions.MoveMembers, Permissions.None);
            overwrites = overwrites.Merge(orig_owner, Permissions.AccessChannels | Permissions.UseVoice,
                Permissions.None, Permissions.ManageChannels | Permissions.MoveMembers);
            await userchannelobj.ModifyAsync(x => { x.PermissionOverwrites = overwrites; });
            await ResetChannelMods(userchannelobj);
            await msg.ModifyAsync(
                $"<:success:1085333481820790944> **Erfolg!** Channel wurde erfolgreich an {new_owner.Mention} übertragen.");
        }
        else if (userchannelobj.Users.Contains(orig_owner) && db_channels.Contains((long)userchannel) &&
                 !userchannelobj.Users.Contains(new_owner))

        {
            await msg.ModifyAsync(
                $"<:attention:1085333468688433232> **Fehler!** Der Channel wurde nicht übertragen da der Zielnutzer {new_owner} **nicht** in {userchannelobj.Mention} ist.");
        }
    }
}