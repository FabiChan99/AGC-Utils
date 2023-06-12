using AGC_Management.Helpers;
using AGC_Management.Helpers.TempVoice;
using AGC_Management.Services.DatabaseHandler;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using DisCatSharp.Exceptions;
using DisCatSharp.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Npgsql;

namespace AGC_Management.Commands.TempVC;

[EventHandler]
public class TempVCEventHandler : TempVoiceHelper
{
    [Event]
    private Task VoiceStateUpdated(DiscordClient sender, VoiceStateUpdateEventArgs e)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if ((e.Guild.Id != ulong.Parse(BotConfig.GetConfig()["ServerConfig"]["ServerId"]))) return;
                var sessionresult = new List<Dictionary<string, object>>();
                var usersession = new List<dynamic>();
                List<string> Query = new()
                {
                    "userid", "channelname", "channelbitrate", "channellimit",
                    "blockedusers", "permitedusers", "locked", "hidden"
                };
                Dictionary<string, object> WhereCondiditons = new()
                {
                    { "userid", (long)e.User.Id }
                };
                sessionresult = await DatabaseService.SelectDataFromTable("tempvoicesession", Query, WhereCondiditons);
                if (sessionresult.Count == 0)
                {
                    List<long> all_channels = await GetAllTempChannels();
                    if ((e.Before?.Channel != null && e.After?.Channel == null) ||
                        (e.Before?.Channel != null && e.After?.Channel != null))
                        if (all_channels.Contains((long)e.Before.Channel.Id))
                            if (e.Before.Channel.Users.Count == 0)
                                try
                                {
                                    Dictionary<string, (object value, string comparisonOperator)>
                                        DeletewhereConditions = new()
                                        {
                                            { "channelid", ((long)e.Before.Channel.Id, "=") }
                                        };

                                    await e.Before.Channel.DeleteAsync();
                                    await DatabaseService.DeleteDataFromTable("tempvoice", DeletewhereConditions);
                                }
                                catch (Exception)
                                {
                                    Dictionary<string, (object value, string comparisonOperator)>
                                        DeletewhereConditions = new()
                                        {
                                            { "channelid", ((long)e.Before.Channel.Id, "=") }
                                        };

                                    await DatabaseService.DeleteDataFromTable("tempvoice", DeletewhereConditions);
                                }

                    if ((e.After?.Channel != null && e.Before?.Channel == null) ||
                        (e.Before?.Channel != null && e.After?.Channel != null))
                    {
                        ulong creationChannelId;
                        if (ulong.TryParse(GetVCConfig("Creation_Channel_ID"), out creationChannelId))
                            if (e.After.Channel.Id == creationChannelId)
                            {
                                DiscordMember m = await e.Guild.GetMemberAsync(e.User.Id);

                                string defaultVcName = GetVCConfig("Default_VC_Name") ?? $"{m.Username}'s Channel";
                                defaultVcName = string.IsNullOrWhiteSpace(defaultVcName)
                                    ? $"{m.Username}'s Channel"
                                    : defaultVcName;
                                defaultVcName = defaultVcName.Replace("{username}", m.Username)
                                    .Replace("{userid}", m.Id.ToString())
                                    .Replace("{fullname}", m.UsernameWithDiscriminator);


                                DiscordChannel voice = await e.After?.Guild.CreateVoiceChannelAsync
                                (defaultVcName, e.After.Channel.Parent,
                                    96000, 0, qualityMode: VideoQualityMode.Full);
                                var overwrites = voice.PermissionOverwrites.Select(x => x.ConvertToBuilder()).ToList();
                                Dictionary<string, object> data = new()
                                {
                                    { "ownerid", (long)e.User.Id },
                                    { "channelid", (long)voice.Id },
                                    { "lastedited", (long)0 }
                                };
                                await DatabaseService.InsertDataIntoTable("tempvoice", data);
                                try
                                {
                                    await m.ModifyAsync(x => x.VoiceChannel = voice);
                                }
                                catch (Exception)
                                {
                                    Dictionary<string, (object value, string comparisonOperator)>
                                        DeletewhereConditions = new()
                                        {
                                            { "channelid", ((long)voice.Id, "=") }
                                        };
                                    await voice.DeleteAsync();
                                    await DatabaseService.DeleteDataFromTable("tempvoice", DeletewhereConditions);
                                    return;
                                }

                                overwrites = overwrites.Merge(m,
                                    Permissions.ManageChannels | Permissions.MoveMembers | Permissions.UseVoice |
                                    Permissions.AccessChannels, Permissions.None);
                                await voice.ModifyPositionInCategoryAsync(e.After.Channel.Position + 1);
                                await voice.ModifyAsync(async x =>
                                {
                                    x.PermissionOverwrites = overwrites;
                                });
                            }
                    }
                }
                else if (sessionresult.Count == 1)
                {
                    List<long> all_channels = await GetAllTempChannels();
                    if ((e.Before?.Channel != null && e.After?.Channel == null) ||
                        (e.Before?.Channel != null && e.After?.Channel != null))
                        if (all_channels.Contains((long)e.Before.Channel.Id))
                            if (e.Before.Channel.Users.Count == 0)
                                try
                                {
                                    Dictionary<string, (object value, string comparisonOperator)>
                                        DeletewhereConditions = new()
                                        {
                                            { "channelid", ((long)e.Before.Channel.Id, "=") }
                                        };

                                    await e.Before.Channel.DeleteAsync();
                                    await DatabaseService.DeleteDataFromTable("tempvoice", DeletewhereConditions);
                                }
                                catch (Exception)
                                {
                                    Dictionary<string, (object value, string comparisonOperator)>
                                        DeletewhereConditions = new()
                                        {
                                            { "channelid", ((long)e.Before.Channel.Id, "=") }
                                        };

                                    await DatabaseService.DeleteDataFromTable("tempvoice", DeletewhereConditions);
                                }

                    if ((e.After?.Channel != null && e.Before?.Channel == null) ||
                        (e.Before?.Channel != null && e.After?.Channel != null))
                    {
                        ulong creationChannelId;
                        if (ulong.TryParse(GetVCConfig("Creation_Channel_ID"), out creationChannelId))
                            if (e.After.Channel.Id == creationChannelId)
                            {
                                long userId = 0;
                                string channelName = string.Empty;
                                int channelBitrate = 0;
                                int channelLimit = 0;
                                string blockedusers = string.Empty;
                                string permitedusers = string.Empty;
                                bool locked = false;
                                bool hidden = false;
                                string channelMods = string.Empty;
                                foreach (var item in sessionresult)
                                {
                                    userId = (long)item["userid"];
                                    channelName = (string)item["channelname"];
                                    channelBitrate = (int)item["channelbitrate"];
                                    channelLimit = (int)item["channellimit"];
                                    blockedusers = item["blockedusers"] != null
                                        ? (string)item["blockedusers"]
                                        : string.Empty;
                                    permitedusers = item["permitedusers"] != null
                                        ? (string)item["permitedusers"]
                                        : string.Empty;
                                    locked = (bool)item["locked"];
                                    hidden = (bool)item["hidden"];
                                    break;
                                }

                                List<string> blockeduserslist =
                                    blockedusers.Split(new[] { ", " }, StringSplitOptions.None).ToList();
                                List<string> permiteduserslist =
                                    permitedusers.Split(new[] { ", " }, StringSplitOptions.None).ToList();

                                DiscordMember m = await e.Guild.GetMemberAsync((ulong)userId);
                                DiscordChannel voice = await e.After?.Guild.CreateVoiceChannelAsync(channelName,
                                    e.After.Channel.Parent, channelBitrate, channelLimit,
                                    qualityMode: VideoQualityMode.Full);
                                Dictionary<string, object> data = new()
                                {
                                    { "ownerid", (long)e.User.Id },
                                    { "channelid", (long)voice.Id },
                                    { "lastedited", (long)0 }
                                };
                                await DatabaseService.InsertDataIntoTable("tempvoice", data);
                                try
                                {
                                    await m.ModifyAsync(x => x.VoiceChannel = voice);
                                }
                                catch (Exception)
                                {
                                    Dictionary<string, (object value, string comparisonOperator)>
                                        DeletewhereConditions = new()
                                        {
                                            { "channelid", ((long)voice.Id, "=") }
                                        };
                                    await voice.DeleteAsync();
                                    await DatabaseService.DeleteDataFromTable("tempvoice", DeletewhereConditions);
                                    return;
                                }

                                var overwrites = voice.PermissionOverwrites.Select(x => x.ConvertToBuilder()).ToList();
                                await voice.ModifyPositionInCategoryAsync(e.After.Channel.Position + 1);
                                overwrites = overwrites.Merge(m,
                                    Permissions.ManageChannels | Permissions.MoveMembers | Permissions.UseVoice |
                                    Permissions.AccessChannels, Permissions.None);
                                if (locked)
                                {
                                    overwrites = overwrites.Merge(voice.Guild.EveryoneRole, Permissions.None,
                                        Permissions.UseVoice);
                                }

                                if (hidden)
                                {
                                    overwrites = overwrites.Merge(voice.Guild.EveryoneRole, Permissions.None,
                                        Permissions.AccessChannels);
                                }

                                foreach (string user in blockeduserslist)
                                {
                                    if (ulong.TryParse(user, out ulong blockeduser))
                                    {
                                        try
                                        {
                                            var userid = await e.After.Guild.GetMemberAsync(blockeduser);


                                            overwrites = overwrites.Merge(userid, Permissions.None,
                                                Permissions.UseVoice);
                                        }
                                        catch (NotFoundException)
                                        {
                                        }
                                    }
                                }

                                foreach (string user in permiteduserslist)
                                {
                                    if (ulong.TryParse(user, out ulong permiteduser))
                                    {
                                        try
                                        {
                                            var userid = await e.After.Guild.GetMemberAsync(permiteduser);


                                            overwrites = overwrites.Merge(userid,
                                                Permissions.UseVoice | Permissions.AccessChannels,
                                                Permissions.None);
                                        }
                                        catch (NotFoundException)
                                        {
                                        }
                                    }
                                }

                                await voice.ModifyAsync(x => x.PermissionOverwrites = overwrites);
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        });
        _ = Task.Run(async () =>
        {
            try
            {
                if (e.Before == null && e.After == null) return;
                if (e.Before == e.After) return;
                var ChID = ulong.Parse(GetVCConfig("Creation_Channel_ID"));
                var PaID = ulong.Parse(GetVCConfig("Creation_Category_ID"));
                DiscordChannel beforeChannel = e.Before?.Channel;
                DiscordChannel afterChannel = e.After?.Channel;

                if (beforeChannel == null)
                {
                    if (afterChannel.Id == ChID) return;
                    if (afterChannel.ParentId == PaID)
                    {
                        DiscordMember member = await e.User.ConvertToMember(e.Guild);
                        await afterChannel.SendMessageAsync($"<:vcjoin:1117480571917049966> {GetBetterUsernameWithID(member)}");
                        return;
                    }
                }

                if ((beforeChannel?.ParentId == PaID || afterChannel?.ParentId == PaID) &&
                    beforeChannel != afterChannel)
                {
                    if (beforeChannel != null && beforeChannel.Users.Count() > 0)
                    {
                        if (beforeChannel.Id == ChID) return;
                        if (beforeChannel.ParentId != PaID) return;
                        DiscordMember member = await e.User.ConvertToMember(e.Guild);
                        await beforeChannel.SendMessageAsync($"<:vcleave:1117480573414412339> {GetBetterUsernameWithID(member)}");
                        return;
                    }

                    if (afterChannel != null && afterChannel.Users.Count() > 0)
                    {
                        if (afterChannel.Id == ChID) return;
                        if (afterChannel.ParentId != PaID) return;
                        DiscordMember member = await e.User.ConvertToMember(e.Guild);
                        await afterChannel.SendMessageAsync($"<:vcjoin:1117480571917049966> {GetBetterUsernameWithID(member)}``");
                        return;
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        });

        return Task.CompletedTask;
    }
}

public class TempVoiceCommands : TempVoiceHelper
{
    [Command("lock")]
    [RequireDatabase]
    //[RequireVoiceChannel]
    public async Task VoiceLock(CommandContext ctx)
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
                "<a:loading_agc:1084157150747697203> **Lade...** Versuche Channel zu sperren...");
            DiscordRole default_role = ctx.Guild.EveryoneRole;
            DiscordChannel channel = ctx.Member.VoiceState.Channel;
            var overwrite = channel.PermissionOverwrites.FirstOrDefault(o => o.Id == default_role.Id);
            if (overwrite?.CheckPermission(Permissions.UseVoice) == PermissionLevel.Denied)
            {
                await msg.ModifyAsync("<:attention:1085333468688433232> Der Channel ist bereits **gesperrt**!");
                return;
            }

            var overwrites = userChannel.PermissionOverwrites.Select(x => x.ConvertToBuilder()).ToList();
            overwrites = overwrites.Merge(default_role, Permissions.None, Permissions.UseVoice);
            await userChannel.ModifyAsync(x => x.PermissionOverwrites = overwrites);

            await msg.ModifyAsync("<:success:1085333481820790944> Du hast den Channel erfolgreich **gesperrt**!");
        }
    }

    [Command("unlock")]
    [RequireDatabase]
    //[RequireVoiceChannel]
    public async Task VoiceUnlock(CommandContext ctx)
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
                "<a:loading_agc:1084157150747697203> **Lade...** Versuche Channel zu entsperren...");
            DiscordRole default_role = ctx.Guild.EveryoneRole;
            DiscordChannel channel = ctx.Member.VoiceState.Channel;
            var overwrite = channel.PermissionOverwrites.FirstOrDefault(o => o.Id == default_role.Id);
            if (overwrite == null || overwrite?.CheckPermission(Permissions.UseVoice) == PermissionLevel.Unset)
            {
                await msg.ModifyAsync("<:attention:1085333468688433232> Der Channel ist bereits **entsperrt**!");
                return;
            }

            var overwrites = userChannel.PermissionOverwrites.Select(x => x.ConvertToBuilder()).ToList();
            overwrites = overwrites.Merge(default_role, Permissions.None, Permissions.None, Permissions.UseVoice);
            await userChannel.ModifyAsync(x => x.PermissionOverwrites = overwrites);


            await msg.ModifyAsync("<:success:1085333481820790944> Du hast den Channel erfolgreich **entsperrt**!");
        }
    }


    [Command("hide")]
    [RequireDatabase]
    public async Task VoiceHide(CommandContext ctx)
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
                "<a:loading_agc:1084157150747697203> **Lade...** Versuche Channel zu verstecken...");
            DiscordRole default_role = ctx.Guild.EveryoneRole;
            DiscordChannel channel = ctx.Member.VoiceState.Channel;
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

    [Command("rename")]
    [RequireDatabase]
    [Aliases("vcname")]
    public async Task VoiceRename(CommandContext ctx, [RemainingText] string name)
    {
        var current_timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
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
                "<a:loading_agc:1084157150747697203> **Lade...** Versuche Channel umzubenennen...");
            DiscordChannel channel = ctx.Member.VoiceState.Channel;
            long timestampdata = 0;
            List<string> Query = new()
            {
                "lastedited"
            };
            Dictionary<string, object> WhereCondiditons = new()
            {
                { "channelid", (long)channel.Id }
            };
            var dbtimestampdata = await DatabaseService.SelectDataFromTable("tempvoice", Query, WhereCondiditons);
            foreach (var data in dbtimestampdata)
            {
                timestampdata = (long)data["lastedited"];
            }

            long edit_timestamp = timestampdata;
            long math = current_timestamp - edit_timestamp;
            if (math < 300)
            {
                long calc = edit_timestamp + 300;
                await msg.ModifyAsync(
                    $"<:attention:1085333468688433232> **Fehler!** Der Channel wurde in den letzten 5 Minuten schon einmal umbenannt. Bitte warte noch etwas, bevor du den Channel erneut umbenennen kannst. __Beachte:__ Auf diese Aktualisierung haben wir keinen Einfluss und dies Betrifft nur Bots. Erneut umbenennen kannst du den Channel <t:{calc}:R>.");
                return;
            }

            string oldname = channel.Name;
            await channel.ModifyAsync(x => x.Name = name);
            await using (NpgsqlConnection conn = new(DatabaseService.GetConnectionString()))
            {
                await conn.OpenAsync();
                string sql = "UPDATE tempvoice SET lastedited = @timestamp WHERE channelid = @channelid";
                await using (NpgsqlCommand command = new(sql, conn))
                {
                    command.Parameters.AddWithValue("@timestamp", current_timestamp);
                    command.Parameters.AddWithValue("@channelid", (long)channel.Id);
                    int affected = await command.ExecuteNonQueryAsync();
                }
            }

            await msg.ModifyAsync(
                "<:success:1085333481820790944> **Erfolg!** Der Channel wurde erfolgreich umbenannt.");
        }
    }


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

            else if (limit == 0)
            {
                await ctx.RespondAsync(
                    "<:success:1085333481820790944> Du hast das Userlimit erfolgreich **entfernt**.");
                return;
            }

            await userChannel.ModifyAsync(x => x.UserLimit = limit);
            await ctx.RespondAsync(
                $"<:success:1085333481820790944> Du hast {userChannel.Mention} erfolgreich ein Userlimit von **{limit}** gesetzt.");
        }

    }

    [Command("claim")]
    [RequireDatabase]
    [Aliases("claimvc")]
    public async Task ClaimVoice(CommandContext ctx)
    {
        List<long> dbChannels = await GetChannelIDFromDB(ctx);
        List<long> all_dbChannels = await GetAllChannelIDsFromDB();
        DiscordChannel userChannel = ctx.Member?.VoiceState?.Channel;
        long? channelownerid = await GetChannelOwnerID(ctx);
        var msg = await ctx.RespondAsync(
            "<a:loading_agc:1084157150747697203> **Lade...** Versuche Channel zu übernehmen...");
        if (channelownerid == (long)ctx.User.Id)
        {
            await msg.ModifyAsync("<:attention:1085333468688433232> Du bist bereits der Besitzer des Channels.");
            return;
        }

        if (ctx.Member.VoiceState?.Channel == null)
        {
            await msg.ModifyAsync("<:attention:1085333468688433232> Du bist in keinem Voice-Channel.");
            return;
        }

        if (channelownerid == null)
        {
            await msg.ModifyAsync("<:attention:1085333468688433232> Du bist in keinem TempVC Channel.");
            return;
        }

        var channelowner = await ctx.Client.GetUserAsync((ulong)channelownerid);
        DiscordMember channelownermember = await ctx.Guild.GetMemberAsync(channelowner.Id);
        var orig_owner = channelownermember;
        DiscordMember new_owner = ctx.Member;
        DiscordChannel channel = ctx.Member.VoiceState?.Channel;
        var overwrites = userChannel.PermissionOverwrites.Select(x => x.ConvertToBuilder()).ToList();

        if (!channel.Users.Contains(orig_owner) && all_dbChannels.Contains((long)userChannel.Id))
        {
            await using (NpgsqlConnection conn = new(DatabaseService.GetConnectionString()))
            {
                await conn.OpenAsync();
                string sql = "UPDATE tempvoice SET ownerid = @owner WHERE channelid = @channelid";
                await using (NpgsqlCommand command = new(sql, conn))
                {
                    command.Parameters.AddWithValue("@owner", (long)new_owner.Id);
                    command.Parameters.AddWithValue("@channelid", (long)channel.Id);
                    int affected = await command.ExecuteNonQueryAsync();
                }
            }

            overwrites = overwrites.Merge(orig_owner, Permissions.None, Permissions.None,
                Permissions.ManageChannels | Permissions.UseVoice | Permissions.MoveMembers |
                Permissions.AccessChannels);
            overwrites = overwrites.Merge(new_owner,
                Permissions.ManageChannels | Permissions.UseVoice | Permissions.MoveMembers |
                Permissions.AccessChannels, Permissions.None);

            await ResetChannelMods(channel);
            await userChannel.ModifyAsync(x => x.PermissionOverwrites = overwrites);
            await msg.ModifyAsync("<:success:1085333481820790944> Du hast den Channel erfolgreich **geclaimt**!");
        }

        if (channel.Users.Contains(orig_owner) && all_dbChannels.Contains((long)userChannel.Id))
        {
            await msg.ModifyAsync(
                $"<:attention:1085333468688433232> Du kannst dein Channel nicht Claimen, da der Channel-Owner ``{orig_owner.UsernameWithDiscriminator}`` noch im Channel ist.");
        }
    }


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

                            if (userChannel.Users.Contains(user) && !user.Roles.Contains(staffrole))
                            {
                                await user.DisconnectFromVoiceAsync();
                            }

                            kicklist.Add(user.Id);
                        }
                        catch (NotFoundException)
                        {
                        }
                    }

                    await userChannel.ModifyAsync(x => x.PermissionOverwrites = overwrites);
                    try
                    {
                        var currentmods = await RetrieveChannelMods(userChannel);
                        currentmods.Remove(ctx.User.Id);
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


    [Command("block")]
    [RequireDatabase]
    [Aliases("vcban", "multiblock")]
    public async Task VoiceBlock(CommandContext ctx, [RemainingText] string users)
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
                    var blockedlist = new List<ulong>();
                    List<ulong> ids = new();
                    ids = Converter.ExtractUserIDsFromString(users);
                    var staffrole = ctx.Guild.GetRole(GlobalProperties.StaffRoleId);
                    var msg = await ctx.RespondAsync(
                        $"<a:loading_agc:1084157150747697203> **Lade...** Versuche {ids.Count} Nutzer zu blockieren...");
                    var overwrites = userChannel.PermissionOverwrites.Select(x => x.ConvertToBuilder()).ToList();

                    foreach (ulong id in ids)
                    {
                        try
                        {
                            var user = await ctx.Guild.GetMemberAsync(id);

                            if (user.Roles.Contains(staffrole))
                            {
                                continue;
                            }
                            var channelowner = await GetChannelOwnerID(userChannel);
                            if (channelowner == (long)user.Id)
                            {
                                continue;
                            }
                            List<ulong> mods = await RetrieveChannelMods(userChannel);
                            if (id == ctx.User.Id || mods.Contains(id))
                            {
                                continue;
                            }
                            try
                            {
                                var currentmods = await RetrieveChannelMods(userChannel);
                                currentmods.Remove(id);
                                await UpdateChannelMods(userChannel, currentmods);
                            }
                            catch (Exception)
                            {
                            }
                            overwrites = overwrites.Merge(user, Permissions.None, Permissions.None, Permissions.UseVoice | Permissions.AccessChannels);
                            overwrites = overwrites.Merge(user, Permissions.None, Permissions.UseVoice);
                            if (userChannel.Users.Contains(user) && !user.Roles.Contains(staffrole))
                            {
                                await user.DisconnectFromVoiceAsync();
                            }

                            blockedlist.Add(user.Id);
                        }
                        catch (NotFoundException)
                        {
                        }
                    }
                    await userChannel.ModifyAsync(x => x.PermissionOverwrites = overwrites);


                    int successCount = blockedlist.Count;
                    string endstring =
                        $"<:success:1085333481820790944> **Erfolg!** Es {(successCount == 1 ? "wurde" : "wurden")} {successCount} Nutzer erfolgreich **blockiert**!";

                    await msg.ModifyAsync(endstring);
                }
            }
        );
    }


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

    [Command("permit")]
    [RequireDatabase]
    [Aliases("allow", "whitelist")]
    public async Task VoicePermit(CommandContext ctx, [RemainingText] string users)
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
                    var permitusers = new List<ulong>();
                    List<ulong> ids = new();
                    ids = Converter.ExtractUserIDsFromString(users);
                    var staffrole = ctx.Guild.GetRole(GlobalProperties.StaffRoleId);
                    var msg = await ctx.RespondAsync(
                        $"<a:loading_agc:1084157150747697203> **Lade...** Versuche {ids.Count} Nutzer zuzulassen...");
                    var overwrites = userChannel.PermissionOverwrites.Select(x => x.ConvertToBuilder()).ToList();

                    foreach (ulong id in ids)
                    {
                        try
                        {
                            var user = await ctx.Guild.GetMemberAsync(id);

                            var channelmods = await RetrieveChannelMods(userChannel);
                            if (channelmods.Contains(user.Id))
                            {
                                var buserow = userChannel.PermissionOverwrites
                                    .Where(x => x.Type == OverwriteType.Member)
                                    .Where(x => x.CheckPermission(Permissions.UseVoice) == PermissionLevel.Denied).Select(x => x.Id)
                                    .ToList();

                                if (buserow.Contains(user.Id))
                                {
                                    continue;
                                }


                            }

                            overwrites = overwrites.Merge(user, Permissions.AccessChannels | Permissions.UseVoice,
                                Permissions.None);


                            permitusers.Add(user.Id);
                        }
                        catch (NotFoundException)
                        {
                        }
                    }

                    await userChannel.ModifyAsync(x => x.PermissionOverwrites = overwrites);

                    int successCount = permitusers.Count;
                    string endstring =
                        $"<:success:1085333481820790944> **Erfolg!** Es {(successCount == 1 ? "wurde" : "wurden")} {successCount} Nutzer erfolgreich **zugelassen**!";

                    await msg.ModifyAsync(endstring);
                }
            }
        );
    }

    [Command("unpermit")]
    [RequireDatabase]
    [Aliases("unwhitelist")]
    public async Task VoiceUnpermit(CommandContext ctx, [RemainingText] string users)
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
                    var unpermitlist = new List<ulong>();
                    List<ulong> ids = new();
                    ids = Converter.ExtractUserIDsFromString(users);
                    var staffrole = ctx.Guild.GetRole(GlobalProperties.StaffRoleId);
                    var msg = await ctx.RespondAsync(
                        $"<a:loading_agc:1084157150747697203> **Lade...** Versuche {ids.Count} Nutzer unzupermitten...");
                    var overwrites = userChannel.PermissionOverwrites.Select(x => x.ConvertToBuilder()).ToList();

                    foreach (ulong id in ids)
                    {
                        try
                        {
                            var user = await ctx.Guild.GetMemberAsync(id);

                            var channelowner = await GetChannelOwnerID(userChannel);
                            if (channelowner == (long)user.Id)
                            {
                                continue;
                            }

                            overwrites = overwrites.Merge(user, Permissions.None,
                                Permissions.None, Permissions.AccessChannels | Permissions.UseVoice);




                            unpermitlist.Add(user.Id);
                        }
                        catch (NotFoundException)
                        {
                        }
                    }

                    await userChannel.ModifyAsync(x => x.PermissionOverwrites = overwrites);

                    int successCount = unpermitlist.Count;
                    string endstring =
                        $"<:success:1085333481820790944> **Erfolg!** Es {(successCount == 1 ? "wurde" : "wurden")} {successCount} Nutzer erfolgreich **unpermitted**!";

                    await msg.ModifyAsync(endstring);
                }
            }
        );
    }

    [Command("joinrequest")]
    [Aliases("joinreq")]
    public async Task JoinRequest(CommandContext ctx, DiscordMember user)
    {
        if (SelfCheck(ctx, user)) return;
        var caseid = Helpers.Helpers.GenerateCaseID();
        var db_channels = await GetAllChannelIDsFromDB();
        var userchannel = user.VoiceState?.Channel;
        var userchannelid = userchannel?.Id;
        var channelownerid = await GetChannelOwnerID(user);
        DiscordChannel channel = userchannel;
        DiscordMember? TargetUser = null;
        var msg = await ctx.RespondAsync(
            "<a:loading_agc:1084157150747697203> **Lade...** Versuche eine Beitrittsanfrage zu stellen...");
        if (user.IsBot)
        {
            await msg.ModifyAsync(
                "<:attention:1085333468688433232> **Fehler!** Dieser User ist ein Bot!");
            return;
        }

        Console.WriteLine(userchannel);
        if (userchannel == null)
        {
            await msg.ModifyAsync("<:attention:1085333468688433232> **Fehler!** Der user ist in keinem Voice Channel.");
            return;
        }

        if (userchannel != null && !db_channels.Contains((long)userchannelid))
        {
            await msg.ModifyAsync(
                "<:attention:1085333468688433232> **Fehler!** Der User ist nicht in einem Custom Voice Channel.");
            return;
        }

        TargetUser = user;
        if (db_channels.Contains((long)userchannelid) && channelownerid != (long)user.Id)
        {
            DiscordMember Owner = await ctx.Guild.GetMemberAsync((ulong)channelownerid);
            await msg.ModifyAsync(
                $"<:attention:1085333468688433232> **Fehler!** Der User ist nicht der Besitzer des Channels. Der Besitzer ist ``{Owner.UsernameWithDiscriminator}`` \nJoinanfrage wird umgeleitet...");
            await Task.Delay(3000);
            TargetUser = Owner;
            if (TargetUser.VoiceState?.Channel == null)
            {
                await msg.ModifyAsync(
                    "<:attention:1085333468688433232> **Fehler!** Der Besitzer des Channels ist in keinem Voice Channel.");
                return;
            }

            Console.WriteLine(TargetUser.VoiceState?.Channel.Id);
            Console.WriteLine(userchannelid);
            if (TargetUser.VoiceState?.Channel != null && TargetUser.VoiceState?.Channel.Id != userchannelid)
            {
                await msg.ModifyAsync(
                    "<:attention:1085333468688433232> **Fehler!** Der Besitzer des Channels ist nicht in dem gewünschten Channel.");
                return;
            }
        }

        if (db_channels.Contains((long)userchannelid) && channelownerid == (long)TargetUser.Id)
        {
            var ebb = new DiscordEmbedBuilder();
            ebb.WithTitle("Beitrittsanfrage");
            ebb.WithDescription(
                $"{ctx.Member.UsernameWithDiscriminator} {ctx.Member.Mention} möchte gerne deinem Channel beitreten. Möchtest du die Beitrittsanfage annehmen?\n Du hast 300 Sekunden Zeit");
            ebb.WithFooter($"{ctx.Member.UsernameWithDiscriminator}", ctx.Member.AvatarUrl);
            List<DiscordButtonComponent> buttons = new(2)
            {
                new DiscordButtonComponent(ButtonStyle.Success, $"jr_accept_{caseid}", "Ja"),
                new DiscordButtonComponent(ButtonStyle.Danger, $"jr_deny_{caseid}", "Nein")
            };
            ebb.WithColor(BotConfig.GetEmbedColor());
            var eb = ebb.Build();
            DiscordMessageBuilder mb = new();
            mb.WithEmbed(eb);
            mb.WithContent($"{TargetUser.Mention}");
            mb.AddComponents(buttons);

            msg = await msg.ModifyAsync(mb);
            var pingmsg = await ctx.RespondAsync($"{TargetUser.Mention}");
            await pingmsg.DeleteAsync();
            var interactivity = ctx.Client.GetInteractivity();
            var channelmods = await RetrieveChannelMods(userchannel);
            var result = await interactivity.WaitForButtonAsync(msg, predicate: interaction =>
            {
                if (interaction.User.Id == TargetUser.Id)
                {
                    return true;
                };
                if (channelmods.Contains(interaction.User.Id))
                {
                    var buserow = userchannel.PermissionOverwrites
                        .Where(x => x.Type == OverwriteType.Member)
                        .Where(x => x.CheckPermission(Permissions.UseVoice) == PermissionLevel.Denied).Select(x => x.Id)

                        .ToList();
                    return !buserow.Contains(ctx.User.Id);
                }
                return false;

            }, TimeSpan.FromSeconds(300));
            if (!userchannel.Users.Contains(TargetUser) && TargetUser != user)
            {
                DiscordMessageBuilder msgb = new();
                msgb.WithEmbed(null);
                msgb.WithContent(
                    "<:attention:1085333468688433232> **Fehler!** Der User ist nicht mehr in deinem Channel.");
                await msg.ModifyAsync(msgb);
                return;
            }

            if (result.TimedOut)
            {
                DiscordEmbedBuilder eb_ = new();
                eb_.WithTitle("Beitrittsanfrage abgelaufen");
                eb_.WithDescription(
                    $"Sorry {ctx.User.Username}, aber {TargetUser.Username} hat nicht in der benötigten Zeit reagiert");
                eb_.WithFooter($"{ctx.Member.UsernameWithDiscriminator}", ctx.Member.AvatarUrl);
                eb_.WithColor(DiscordColor.Red);
                eb_.Build();

                DiscordMessageBuilder msgb = new();
                msgb.WithEmbed(eb_);

                await msg.ModifyAsync(msgb);
                return;
            }

            if (result.Result.Id == $"jr_accept_{caseid}")
            {
                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                var invite = await userchannel.CreateInviteAsync(300, 1, false, true);
                DiscordEmbedBuilder eb_ = new();


                var overwrites = userchannel.PermissionOverwrites.Select(x => x.ConvertToBuilder()).ToList();
                overwrites = overwrites.Merge(ctx.Member, Permissions.AccessChannels | Permissions.UseVoice,
                    Permissions.None);
                int? channellimit = userchannel.UserLimit;
                if (userchannel.UserLimit < userchannel.Users.Count() && userchannel.UserLimit != 0 || userchannel.UserLimit == userchannel.Users.Count())
                {
                    channellimit = channellimit + 1;
                }


                await userchannel.ModifyAsync(x => { x.PermissionOverwrites = overwrites;
                    x.UserLimit = channellimit;
                });
                eb_.WithTitle("Beitrittsanfrage angenommen");
                eb_.WithDescription(
                    $"Deine Beitrittsanfrage wurde von {result.Result.User.UsernameWithDiscriminator} akzeptiert. Du kannst nun beitreten. \nÜber den Button kannst du dem Kanal beitreten.");
                eb_.WithFooter($"{ctx.Member.UsernameWithDiscriminator}", ctx.Member.AvatarUrl);
                eb_.WithColor(BotConfig.GetEmbedColor());
                eb_.Build();
                List<DiscordLinkButtonComponent> urlb = new(1)
                {
                    new DiscordLinkButtonComponent(invite.ToString(), "Kanal betreten")
                };
                DiscordMessageBuilder msgb = new();
                msgb.AddComponents(urlb);
                msgb.WithEmbed(eb_);
                await msg.ModifyAsync(msgb);
            }

            if (result.Result.Id == $"jr_deny_{caseid}")
            {
                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                DiscordEmbedBuilder eb_ = new();
                eb_.WithTitle("Beitrittsanfrage abgelehnt");
                eb_.WithDescription(
                    $"{result.Result.User.UsernameWithDiscriminator} hat deine Beitrittsanfrage abgelehnt.");
                eb_.WithFooter($"{ctx.Member.UsernameWithDiscriminator}", ctx.Member.AvatarUrl);
                eb_.WithColor(DiscordColor.Red);
                eb_.Build();

                DiscordMessageBuilder msgb = new();
                msgb.WithEmbed(eb_);
                await msg.ModifyAsync(msgb);
            }
        }
    }


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

    [Command("vchelp")]
    [Aliases("voicehelp", "voice-help", "vc-help")]
    public async Task VoiceHelp(CommandContext ctx)
    {
        var eb = new DiscordEmbedBuilder();
        string prefix = BotConfig.GetConfig()["MainConfig"]["BotPrefix"];
        string helpstring = "__**Grundbefehle:**__\n" +
                            $"> ``{prefix}hide`` - Macht den aktuellen Channel unsichtbar¹\n" +
                            $"> ``{prefix}unhide`` - Macht den aktuellen Channel sichtbar¹\n" +
                            $"> ``{prefix}lock`` - Sperrt den aktuellen Channel¹\n" +
                            $"> ``{prefix}unlock`` - Entsperrt den aktuellen Channel¹\n" +
                            $"> ``{prefix}vckick @user/id`` - Kickt einen User aus dem Channel¹\n" +
                            $"> ``{prefix}block @user/id`` - Blockt einen User aus dem Channel¹\n" +
                            $"> ``{prefix}claim`` - Claimt den aktuellen Channel, wenn sich der Besitzer nicht mehr im Channel befindet\n" +
                            $"> ``{prefix}unblock @user/id`` - Macht die Blockierung eines Users rückgängig¹\n" +
                            $"> ``{prefix}transfer @user/id`` - Transferiert den Channeleigentümer\n" +
                            $"> ``{prefix}permit @user/id`` - Whitelistet einen User für einen Channel¹\n" +
                            $"> ``{prefix}unpermit @user/id`` - Macht das Whitelisting eines Users rückgängig¹\n" +
                            $"> ``{prefix}limit 0 - 99`` -  Setzt das Userlimit für den Channel (0 = Unlimited)¹\n" +
                            $"> ``{prefix}rename name`` - Verändert den Namen des Channels¹\n" +
                            $"> ``{prefix}vcinfo [optional <channelid>]`` - Zeigt ausführliche Infos über einen Channel an wie z.b. Eigentümer\n" +
                            $"> ``{prefix}joinrequest @user/id`` - Stellt eine Beitrittsanfrage an einen User\n" +
                            $"\n" +
                            $"**__Sitzungsverwaltung:__** (Persistente Kanäle)\n" +
                            $"> ``{prefix}session save`` - Speichert ein Abbild des Channels in der Datenbank\n" +
                            $"> ``{prefix}session read`` - Zeigt die aktuell gespeicherte Sitzung an\n" +
                            $"> ``{prefix}session delete`` -  Löscht die gespeicherte Sitzung\n" +
                            $"\n" +
                            $"**__Kanalmodverwaltung:__** (Mehrere Channelowner)\n" +
                            $"> ``{prefix}channelmod add @user/id`` - Ernennnt einen User zu einem Kanalmoderator\n" +
                            $"> ``{prefix}channelmod remove @user/id`` - Entfernt einen Kanalmoderator\n" +
                            $"> ``{prefix}channelmod reset`` - Entfernt alle Kanalmoderatoren\n" +
                            $"> ``{prefix}channelmod list`` - Listet alle Kanalmoderatoren auf\n\n"
                            + "¹ Funktion kann auch von einem Kanalmoderator ausgeführt werden.\n\n" +
                            "*Sollte etwas unklar sein, kannst du ein Ticket in <#826083443489636372> öffnen.*";

        eb.WithTitle("Temp-VC Commands");
        eb.WithDescription(helpstring);
        eb.WithColor(BotConfig.GetEmbedColor());
        await ctx.Channel.SendMessageAsync(embed: eb);
    }

    [Command("vcinfo")]
    [Aliases("voiceinfo", "voice-info", "vc-info")]
    [RequireDatabase]
    public async Task VoiceInfo(CommandContext ctx, DiscordChannel channel = null)
    {
        if (channel == null)
        {
            channel = ctx.Member?.VoiceState?.Channel;
        }
        var userchannel = (long?)channel?.Id;
        var db_channels = await GetAllChannelIDsFromDB();
        if (userchannel == null)
        {
            await ctx.RespondAsync("<:attention:1085333468688433232> Du musst in einem Channel sein, um diesen Befehl auszuführen!");
            return;
        }

        if (!db_channels.Contains((long)userchannel) && userchannel != null)
        {
            await ctx.RespondAsync(
                $"<:attention:1085333468688433232> Der Aktuelle Voice Channel ist kein Custom Channel");
            return;
        }

        if (db_channels.Contains((long)userchannel) && userchannel != null)
        {
            long? channelownerid = await GetChannelOwnerID(channel);
            var channellimit = channel.UserLimit;
            DiscordMember channelowner = await ctx.Guild.GetMemberAsync((ulong)channelownerid);
            string channelname = channel.Name;
            var channel_timestamp = channel.CreationTimestamp;
            var channel_created = channel_timestamp.UtcDateTime;
            var rendered_channel_timestamp = channel_created.Timestamp();
            DiscordRole default_role = ctx.Guild.EveryoneRole;
            var yesemote = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
            var noemote = DiscordEmoji.FromName(ctx.Client, ":x:");
            var overwrites = channel.PermissionOverwrites.Select(x => x.ConvertToBuilder()).ToList();
            bool locked = false;
            bool hidden = false;
            var overwrite =
                channel.PermissionOverwrites.FirstOrDefault(o => o.Id == default_role.Id);
            if (overwrite?.CheckPermission(Permissions.UseVoice) == PermissionLevel.Denied)
            {
                locked = true;
            }

            if (overwrite == null || overwrite?.CheckPermission(Permissions.UseVoice) == PermissionLevel.Unset)
            {
                locked = false;
            }

            if (overwrite?.CheckPermission(Permissions.AccessChannels) == PermissionLevel.Denied)
            {
                hidden = true;
            }

            if (overwrite == null ||
                overwrite?.CheckPermission(Permissions.AccessChannels) == PermissionLevel.Unset)
            {
                hidden = false;
            }
            var hiddenemote = hidden ? yesemote : noemote;
            var lockedemote = locked ? yesemote : noemote;


            string climit = (channellimit == 0) ? "∞" : channellimit.ToString();

            string lreach = "";
            if (channellimit == channel.Users.Count && channellimit != 0)
            {
                lreach = yesemote;
            }

            if (channellimit < channel.Users.Count && channellimit != 0)
            {
                lreach = yesemote;
            }

            if (channellimit > channel.Users.Count)
            {
                lreach = noemote;
            }

            if (channellimit == 0)
            {
                lreach = "Kein Limit gesetzt";
            }

            List<string> Query = new()
            {
                "userid"
            };
            Dictionary<string, object> WhereCondiditons = new()
            {
                { "userid", (long)channelownerid }
            };

            string sessionemote = noemote;
            var usersession = await DatabaseService.SelectDataFromTable("tempvoicesession", Query, WhereCondiditons);
            if (usersession.Count > 0)
            {
                sessionemote = yesemote;
            }

            var ebb = new DiscordEmbedBuilder()
                .WithDescription(
                    $"**• Name des Channels** = ``{channelname}``\n" +
                    $"**• ID des Channels** = ``{channel.Id}``\n" +
                    $"**• Eigentümer** = {channelowner.Mention} ``({channelowner.Id})``\n" +
                    $"**• Useranzahl im VC** = ``{channel.Users.Count}``\n" +
                    $"**• Userlimit des VC's** = ``{climit}``\n" +
                    $"**• Limit des Channels erreicht** = {lreach}\n" +
                    $"**• Erstellzeit** = {rendered_channel_timestamp}\n" +
                    $"**• Aktuelle Bitrate** = ``{channel.Bitrate} kbps``\n" +
                    $"**• Channel Versteckt** = {hiddenemote}\n" +
                    $"**• Channel Gesperrt** = {lockedemote}\n" +
                    $"**• Channelowner hat Session** = {sessionemote}")
                .WithColor(BotConfig.GetEmbedColor()).WithTitle("Voice Channel Informationen")
                //.WithThumbnail("https://cdn3.emoji.gg/emojis/2378-discord-voice-channel.png")
                .WithFooter($"{ctx.User.UsernameWithDiscriminator}");
            var caseid = Helpers.Helpers.GenerateCaseID();
            List<DiscordButtonComponent> buttons = new(2)
            {
                new DiscordButtonComponent(ButtonStyle.Secondary, $"get_vcinfo_{caseid}", "Info über Zugelassene oder Blockierte User (Nur Channelowner)"),
            };


            var mb = new DiscordMessageBuilder()
                .WithEmbed(ebb).AddComponents(buttons);
            var omb = new DiscordMessageBuilder()
                .WithEmbed(ebb);
            DiscordMessage msg = await ctx.RespondAsync(mb);
            var interactivity = ctx.Client.GetInteractivity();
            var results = await interactivity.WaitForButtonAsync(msg, channelowner, TimeSpan.FromMinutes(3));
            if (results.TimedOut)
            {
                await msg.ModifyAsync(omb);
                return;
            }

            if (results.Result.Id == $"get_vcinfo_{caseid}")
            {
                string blocklist = string.Empty;
                string permitlist = string.Empty;
                var buserow = channel.PermissionOverwrites
                    .Where(x => x.Type == OverwriteType.Member)
                    .Where(x => x.CheckPermission(Permissions.UseVoice) == PermissionLevel.Denied).Select(x => x.Id)

                    .ToList();

                var puserow = channel.PermissionOverwrites
                    .Where(x => x.CheckPermission(Permissions.UseVoice) == PermissionLevel.Allowed)
                    .Where(x => x.Type == OverwriteType.Member)
                    .Where(x => x.Id != ctx.User.Id)
                    .Select(x => x.Id)
                    .ToList();
                foreach (var user in buserow)
                {
                    var member = await ctx.Guild.GetMemberAsync(user);
                    blocklist += $"{member.UsernameWithDiscriminator} {member.Mention} ``({member.Id})``\n";
                }

                foreach (var user in puserow)
                {
                    var member = await ctx.Guild.GetMemberAsync(user);
                    permitlist += $"{member.UsernameWithDiscriminator} {member.Mention} ``({member.Id})``\n";
                }

                if (buserow.Count == 0)
                {
                    blocklist = "Keine User blockiert";
                }

                if (puserow.Count == 0)
                {
                    permitlist = "Keine User zugelassen";
                }

                var emb = new DiscordEmbedBuilder().WithDescription("__Zugelassene User:__\n" +
                                                                    $"{permitlist}\n\n" +
                                                                    "__Blockierte User:__\n" +
                                                                    $"{blocklist}").WithColor(BotConfig.GetEmbedColor()).WithTitle("Kanal Permit und Block Liste").WithFooter($"{ctx.User.UsernameWithDiscriminator}");
                var mbb = new DiscordInteractionResponseBuilder()
                {
                    IsEphemeral = true
                }.AddEmbed(emb);

                await results.Result.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, mbb);
                await msg.ModifyAsync(omb);
                return;
            }
        }

        
    }


    [Group("channelmod")]
    [Aliases("cmod")]
    public class ChannelModManagement : TempVoiceCommands
    {
        [RequireDatabase]
        [Command("add")]
        public async Task ChannelModAdd(CommandContext ctx, [RemainingText] DiscordMember user)
        {
            List<ulong> channelmods = new();
            List<long> dbChannels = await GetChannelIDFromDB(ctx);

            DiscordChannel userChannel = ctx.Member?.VoiceState?.Channel;

            if (userChannel == null || !dbChannels.Contains((long)userChannel?.Id))
            {
                await NoChannel(ctx);
                return;
            }

            if (userChannel != null && dbChannels.Contains((long)userChannel.Id))
            {
                bool isMod = await IsChannelMod(userChannel, user);
                if (isMod)
                {
                    await ctx.RespondAsync("Dieser User ist bereits Kanalmoderator");
                    return;
                }
                var currentmods = await RetrieveChannelMods(userChannel);
                currentmods.Add(user.Id);
                await UpdateChannelMods(userChannel, currentmods);
                await ctx.RespondAsync($"Der User ``{user.UsernameWithDiscriminator}`` ``{user.Id}`` wurde als Kanalmoderator hinzugefügt.");
            }
        }

        [RequireDatabase]
        [Command("reset")]
        public async Task ChannelModReset(CommandContext ctx)
        {
            List<long> dbChannels = await GetChannelIDFromDB(ctx);
            DiscordChannel userChannel = ctx.Member?.VoiceState?.Channel;

            if (userChannel == null || !dbChannels.Contains((long)userChannel?.Id))
            {
                await NoChannel(ctx);
                return;
            }

            if (userChannel != null && dbChannels.Contains((long)userChannel.Id))
            {
                if (!ChannelHasMods(userChannel).Result)
                {
                    await ctx.RespondAsync("Dieser Kanal hat keine Kanalmoderatoren.");
                    return;
                }
                await ResetChannelMods(userChannel);
                await ctx.RespondAsync("Die Kanalmoderatoren wurden zurückgesetzt.");
            }
        }

        [RequireDatabase]
        [Command("remove")]
        public async Task ChannelModRemove(CommandContext ctx, [RemainingText] DiscordMember user)
        {
            List<ulong> channelmods = new();
            List<long> dbChannels = await GetChannelIDFromDB(ctx);

            DiscordChannel userChannel = ctx.Member?.VoiceState?.Channel;

            if (userChannel == null || !dbChannels.Contains((long)userChannel?.Id))
            {
                await NoChannel(ctx);
                return;
            }

            if (userChannel != null && dbChannels.Contains((long)userChannel.Id))
            {
                bool isMod = await IsChannelMod(userChannel, user);
                if (!isMod)
                {
                    await ctx.RespondAsync("Dieser User ist kein Kanalmoderator");
                    return;
                }
                var currentmods = await RetrieveChannelMods(userChannel);
                currentmods.Remove(user.Id);
                await UpdateChannelMods(userChannel, currentmods);
                await ctx.RespondAsync($"Der User ``{user.UsernameWithDiscriminator}`` ``{user.Id}`` wurde als Kanalmoderator entfernt.");
            }
        }

        [RequireDatabase]
        [Command("list")]
        public async Task ChannelModList(CommandContext ctx)
        {
            List<ulong> channelmods = new();
            List<long> dbChannels = await GetChannelIDFromDB(ctx);

            DiscordChannel userChannel = ctx.Member?.VoiceState?.Channel;

            if (userChannel == null || !dbChannels.Contains((long)userChannel?.Id))
            {
                await NoChannel(ctx);
                return;
            }

            if (userChannel != null && dbChannels.Contains((long)userChannel.Id))
            {
                if (!ChannelHasMods(userChannel).Result)
                {
                    await ctx.RespondAsync("Dieser Kanal hat keine Kanalmoderatoren.");
                    return;
                }
                var currentmods = await RetrieveChannelMods(userChannel);
                string modlist = string.Empty;
                foreach (var mod in currentmods)
                {
                    var member = await ctx.Guild.GetMemberAsync(mod);
                    modlist += $"{member.UsernameWithDiscriminator} ``({member.Id})``\n";
                }
                var emb = new DiscordEmbedBuilder().WithDescription(modlist).WithColor(BotConfig.GetEmbedColor()).WithTitle("Kanalmoderatoren").WithFooter($"{ctx.User.UsernameWithDiscriminator} | {userChannel.Name}");
                await ctx.RespondAsync(embed: emb);
            }
        }
    }

    [Group("session")]
    public class SessionManagement : TempVoiceCommands
    {
        [Command("save")]
        [Aliases("safe")]
        [RequireDatabase]
        public async Task SessionSave(CommandContext ctx)
        {
            _ = Task.Run(async () =>
            {
                List<ulong> blockedusers = new();
                List<ulong> permittedusers = new();
                List<long> dbChannels = await GetChannelIDFromDB(ctx);
                bool hidden = false;
                bool locked = false;
                DiscordChannel userChannel = ctx.Member?.VoiceState?.Channel;

                if (userChannel == null || !dbChannels.Contains((long)userChannel?.Id))
                {
                    await NoChannel(ctx);
                    return;
                }

                if (userChannel != null && dbChannels.Contains((long)userChannel.Id))
                {
                    var msg = await ctx.RespondAsync(
                        "<a:loading_agc:1084157150747697203> **Lade...** Versuche Channel zu speichern...");
                    List<string> Query = new()
                    {
                        "userid"
                    };
                    Dictionary<string, object> WhereCondiditons = new()
                    {
                        { "userid", (long)ctx.User.Id }
                    };
                    bool hasSession = false;
                    var usersession = await DatabaseService.SelectDataFromTable("tempvoicesession", Query, WhereCondiditons);
                    foreach (var user in usersession)
                    {
                        hasSession = true;
                    }


                    if (hasSession)
                    {
                        // if session is there delete it
                        Dictionary<string, (object value, string comparisonOperator)> whereConditions = new()
                        {
                            { "userid", ((long)ctx.User.Id, "=") }
                        };

                        int rowsDeleted =
                            await DatabaseService.DeleteDataFromTable("tempvoicesession", whereConditions);
                    }

                    var overwrite =
                        userChannel.PermissionOverwrites.FirstOrDefault(o => o.Id == ctx.Guild.EveryoneRole.Id);
                    if (overwrite?.CheckPermission(Permissions.UseVoice) == PermissionLevel.Denied)
                    {
                        locked = true;
                    }

                    if (overwrite == null || overwrite?.CheckPermission(Permissions.UseVoice) == PermissionLevel.Unset)
                    {
                        locked = false;
                    }

                    if (overwrite?.CheckPermission(Permissions.AccessChannels) == PermissionLevel.Denied)
                    {
                        hidden = true;
                    }

                    if (overwrite == null ||
                        overwrite?.CheckPermission(Permissions.AccessChannels) == PermissionLevel.Unset)
                    {
                        hidden = false;
                    }

                    string blocklist = string.Empty;
                    string permitlist = string.Empty;
                    var buserow = userChannel.PermissionOverwrites
                        .Where(x => x.Type == OverwriteType.Member)
                        .Where(x => x.CheckPermission(Permissions.UseVoice) == PermissionLevel.Denied).Select(x => x.Id)

                        .ToList();

                    var puserow = userChannel.PermissionOverwrites
                        .Where(x => x.CheckPermission(Permissions.UseVoice) == PermissionLevel.Allowed)
                        .Where(x => x.Type == OverwriteType.Member)
                        .Where(x => x.Id != ctx.User.Id)
                        .Select(x => x.Id)
                        .ToList();
                    foreach (var user in buserow)
                    {
                        blockedusers.Add(user);
                    }

                    foreach (var user in puserow)
                    {
                        permittedusers.Add(user);
                    }

                    blocklist = string.Join(", ", blockedusers);
                    permitlist = string.Join(", ", permittedusers);
                    Dictionary<string, object> data = new()
                    {
                        { "userid", (long)ctx.User.Id },
                        { "channelname", userChannel.Name },
                        { "channelbitrate", userChannel.Bitrate },
                        { "channellimit", userChannel.UserLimit },
                        { "blockedusers", blocklist },
                        { "permitedusers", permitlist },
                        { "locked", locked },
                        { "hidden", hidden }
                    };
                    await DatabaseService.InsertDataIntoTable("tempvoicesession", data);
                    await msg.ModifyAsync(
                        "<:success:1085333481820790944> **Erfolg!** Die Kanaleinstellungen wurden erfolgreich **gespeichert**!");
                }
            });
        }


        [Command("delete")]
        [RequireDatabase]
        public async Task SessionReset(CommandContext ctx)
        {
            _ = Task.Run(async () =>
            {
                var msg = await ctx.RespondAsync(
                    "<a:loading_agc:1084157150747697203> **Lade...** Versuche Kanaleinstellungen zu löschen...");
                List<string> Query = new()
                {
                    "userid"
                };
                Dictionary<string, object> WhereCondiditons = new()
                {
                    { "userid", (long)ctx.User.Id }
                };
                bool hasSession = false;
                var usersession = await DatabaseService.SelectDataFromTable("tempvoicesession", Query, WhereCondiditons);
                foreach (var user in usersession)
                {
                    hasSession = true;
                }

                Dictionary<string, (object value, string comparisonOperator)> whereConditions = new()
                {
                    { "userid", ((long)ctx.User.Id, "=") }
                };

                int rowsDeleted =
                    await DatabaseService.DeleteDataFromTable("tempvoicesession", whereConditions);

                if (!hasSession)
                {
                    await msg.ModifyAsync(
                        "\u274c Du hast keine gespeicherte Sitzung.");
                    return;
                }


                await msg.ModifyAsync(
                    "<:success:1085333481820790944> **Erfolg!** Die Kanaleinstellungen wurden erfolgreich **gelöscht**!");
            });
        }


        [Command("read")]
        [RequireDatabase]
        public async Task SessionRead(CommandContext ctx)
        {
            _ = Task.Run(async () =>
            {
                var msg = await ctx.RespondAsync(
                    "<a:loading_agc:1084157150747697203> **Lade...** Versuche Kanaleinstellungen zu lesen...");
                List<string> Query = new()
                {
                    "userid"
                };

                Dictionary<string, object> WhereCondiditons_ = new()
                {
                    { "userid", (long)ctx.User.Id }
                };
                bool hasSession = false;
                var usersession = await DatabaseService.SelectDataFromTable("tempvoicesession", Query, WhereCondiditons_);
                foreach (var user in usersession)
                {
                    hasSession = true;
                }

                if (!hasSession)
                {
                    await msg.ModifyAsync(
                        "\u274c Du hast keine gespeicherte Sitzung.");
                    return;
                }

                List<string> dataQuery = new()
                {
                    "*"
                };

                Dictionary<string, object> WhereCondiditons = new()
                {
                    { "userid", (long)ctx.User.Id }
                };

                var session = await DatabaseService.SelectDataFromTable("tempvoicesession", dataQuery, WhereCondiditons);
                foreach (var user in session)
                {
                    if (user["userid"].ToString() == ctx.User.Id.ToString())
                    {
                        string channelname = user["channelname"].ToString();
                        string channelbitrate = user["channelbitrate"].ToString();
                        string channellimit = user["channellimit"].ToString();

                        if (channellimit == "0")
                        {
                            channellimit = "Kein Limit";
                        }
                        var caseid = Helpers.Helpers.GenerateCaseID();
                        string blockedusers = user["blockedusers"].ToString();
                        string permitedusers = user["permitedusers"].ToString();
                        string locked = user["locked"].ToString();
                        string hidden = user["hidden"].ToString();
                        string pu = string.IsNullOrEmpty(permitedusers) ? "Keine" : permitedusers;
                        string bu = string.IsNullOrEmpty(blockedusers) ? "Keine" : blockedusers;

                        DiscordEmbedBuilder ebb = new()
                        {
                            Title =
                                $"{BotConfig.GetConfig()["ServerConfig"]["ServerNameInitials"]} TempVC Kanaleinstellungen",
                            Description = $"**Kanalname:** {channelname}\n" +
                                          $"**Kanalbitrate:** {channelbitrate} kbps\n" +
                                          $"**Kanallimit:** {channellimit}\n\n" +
                                          $"**Gesperrte Benutzer:** ```{bu}```\n" +
                                          $"**Zugelassene Benutzer:** ```{pu}```\n" +
                                          $"**Gesperrt:** {locked}\n" +
                                          $"**Versteckt:** {hidden}\n",
                            Color = BotConfig.GetEmbedColor()
                        };
                        var mb = new DiscordMessageBuilder();
                        List<DiscordButtonComponent> buttons = new(2)
                        {
                            new DiscordButtonComponent(ButtonStyle.Secondary, $"close_msg_{caseid}", "Nachricht schließen"),
                        };
                        mb.WithEmbed(ebb);
                        mb.AddComponents(buttons);
                        var nmb = new DiscordMessageBuilder();
                        nmb.WithEmbed(ebb);
                        msg = await msg.ModifyAsync(mb);
                        var interactiviy = ctx.Client.GetInteractivity();
                        var response = await interactiviy.WaitForButtonAsync(msg, ctx.User,
                                                       TimeSpan.FromMinutes(5));
                        if (response.TimedOut)
                        {
                            await msg.ModifyAsync(nmb);
                        }

                        if (response.Result.Id == $"close_msg_{caseid}")
                        {
                            await msg.DeleteAsync();
                            try
                            {
                                await ctx.Message.DeleteAsync();
                            }
                            catch (NotFoundException)
                            {
                                // ignored
                            }
                        }

                    }
                }
            });
        }
    }
}

[EventHandler]
public class TempVoicePanelEventHandler : TempVoiceHelper
{
    [Event]
    private static Task ComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        _ = Task.Run(async () =>
        {

            var Interaction = e.Interaction;
            var PanelMsgId = ulong.Parse(BotConfig.GetConfig()["TempVC"]["VCPanelMessageID"]);
            var PanelMsgChannelId = ulong.Parse(BotConfig.GetConfig()["TempVC"]["VCPanelChannelID"]);
            if (PanelMsgChannelId == 0 && PanelMsgId == 0)
            {
                sender.Logger.LogWarning(
                    $"Panel is not Initialized! Consider initializing with {BotConfig.GetConfig()["MainConfig"]["BotPrefix"]}initpanel");
                return;
            }

            if (Interaction.Channel.Id != PanelMsgChannelId)
            {
                return;
            }

            if (Interaction.Channel.Id == PanelMsgChannelId)
            {
                Console.WriteLine(Interaction.Data.CustomId);
                var customid = Interaction.Data.CustomId;
                //await Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                if (customid == "channel_lock")
                {
                    await PanelLockChannel(Interaction);
                }
                else if (customid == "unlock_lock")
                {
                    await PanelUnlockChannel(Interaction);
                }
                else if (customid == "channel_rename")
                {
                    await PanelChannelRename(Interaction, sender);
                }
                else if (customid == "channel_hide")
                {
                    await PanelHideChannel(Interaction);
                }
                else if (customid == "channel_show")
                {
                    await PanelUnhideChannel(Interaction);
                }
                else if (customid == "channel_invite")
                {
                    await PanelChannelInvite(Interaction);
                }
                else if (customid == "invite_selector")
                {
                    await PanelChannelInviteCallback(Interaction, sender);
                }
                else if (customid == "channel_limit")
                {
                    await PanelChannelLimit(Interaction, sender);
                }
                else if (customid == "channel_delete")
                {
                    await PanelChannelDelete(Interaction, sender, e);
                }
                else if (customid == "channel_permit")
                {
                    await PanelPermitVoiceSelector(Interaction, sender, e);
                }
                else if (customid == "channel_permit")
                {
                    await PanelPermitVoiceSelector(Interaction, sender, e);
                }
                else if (customid == "permit_selector")
                {
                    await PanelPermitVoiceSelectorCallback(Interaction, sender, e);
                }
                else if (customid == "role_permit_button")
                {
                    await PanelPermitVoiceRole(Interaction, sender, e);
                }
                else if (customid == "role_permit_selector")
                {
                    await PanelPermitVoiceRoleCallback(Interaction, sender, e);
                }
                else if (customid == "channel_unpermit")
                {
                    await PanelChannelUnpermit(Interaction, sender, e);
                }
                else if (customid == "unpermit_levelrole")
                {
                    await PanelChannelUnpermitRoleCallback(Interaction, sender, e);
                }
                else if (customid == "unpermit_selector")
                {
                    await PanelChannelUnpermitUserCallback(Interaction, sender, e);
                }
                else if (customid == "channel_claim")
                {
                    await PanelChannelClaim(Interaction, sender);
                }
                else if (customid == "channel_transfer")
                {
                    await PanelChannelTransfer(Interaction);
                }
                else if (customid == "transfer_selector")
                {
                    await PanelChannelTransferCallback(Interaction, sender, e);
                }
                else if (customid == "channel_kick")
                {
                    await PanelChannelKick(Interaction);
                }
                else if (customid == "kick_selector")
                {
                    await PanelChannelKickCallback(Interaction, sender, e);
                }
                else if (customid == "channel_ban")
                {
                    await PanelChannelBlock(Interaction);
                }
                else if (customid == "ban_selector")
                {
                    await PanelChannelBlockCallback(Interaction, sender, e);
                }
                else if (customid == "channel_unban")
                {
                    await PanelChannelUnblock(Interaction);
                }
                else if (customid == "unban_selector")
                {
                    await PanelChannelUnblockCallback(Interaction, sender, e);
                }
            }
        });
        return Task.CompletedTask;
    }
}

public class TempVoicePanel : TempVoiceHelper
{
    [Command("initpanel")]
    [RequirePermissions(Permissions.Administrator)]
    public async Task InitVCPanel(CommandContext ctx)
    {
        List<DiscordButtonComponent> buttons = new()
        {
            new(ButtonStyle.Secondary, "channel_rename",
                emoji: new DiscordComponentEmoji(1085333479732035664)),
            new(ButtonStyle.Secondary, "channel_limit",
                emoji: new DiscordComponentEmoji(1085333471838343228)),
            new(ButtonStyle.Secondary, "channel_lock",
                emoji: new DiscordComponentEmoji(1085333475625795605)),
            new(ButtonStyle.Secondary, "unlock_lock",
                emoji: new DiscordComponentEmoji(1085518424790286346)),
            new(ButtonStyle.Secondary, "channel_invite",
                emoji: new DiscordComponentEmoji(1085333458840203314)),
            new(ButtonStyle.Secondary, "channel_delete",
                emoji: new DiscordComponentEmoji(1085333454713004182)),
            new(ButtonStyle.Secondary, "channel_hide",
                emoji: new DiscordComponentEmoji(1085333456487206973)),
            new(ButtonStyle.Secondary, "channel_show",
                emoji: new DiscordComponentEmoji(1085333489416671242)),
            new(ButtonStyle.Secondary, "channel_permit",
                emoji: new DiscordComponentEmoji(1085333477240615094)),
            new(ButtonStyle.Secondary, "channel_unpermit",
                emoji: new DiscordComponentEmoji(1085333494105919560)),
            new(ButtonStyle.Secondary, "channel_claim",
                emoji: new DiscordComponentEmoji(1085333451571466301)),
            new(ButtonStyle.Secondary, "channel_transfer",
                emoji: new DiscordComponentEmoji(1085333484731629578)),
            new(ButtonStyle.Secondary, "channel_kick",
                emoji: new DiscordComponentEmoji(1085333460366925914)),
            new(ButtonStyle.Secondary, "channel_ban",
                emoji: new DiscordComponentEmoji(1085333473893556324)),
            new(ButtonStyle.Secondary, "channel_unban",
                emoji: new DiscordComponentEmoji(1085333487587971102))
        };

        List<DiscordButtonComponent> buttons1 = buttons.Take(5).ToList();
        List<DiscordButtonComponent> buttons2 = buttons.Skip(5).Take(5).ToList();
        List<DiscordButtonComponent> buttons3 = buttons.Skip(10).ToList();

        List<DiscordActionRowComponent> rowComponents = new()
        {
            new DiscordActionRowComponent(buttons1),
            new DiscordActionRowComponent(buttons2),
            new DiscordActionRowComponent(buttons3)
        };

        DiscordEmbedBuilder eb = new()
        {
            Title = $"{BotConfig.GetConfig()["ServerConfig"]["ServerNameInitials"]} Temp-Voice Panel",
            Description =
                $"Hier kannst du die Einstellungen deines Temporären Kanals verändern. \nDu kannst auch Commands in <#{BotConfig.GetConfig()["TempVC"]["CommandChannel_ID"]}> verwenden.",
            Color = BotConfig.GetEmbedColor(),
            ImageUrl = "https://cdn.discordapp.com/attachments/764921088689438771/1115554368574476288/panel.png"
        };
        DiscordMessageBuilder dmb = new DiscordMessageBuilder().AddComponents(rowComponents).AddEmbed(eb.Build());
        var msg = await ctx.Channel.SendMessageAsync(dmb);
        BotConfig.SetConfig("TempVC", "VCPanelMessageID", msg.Id.ToString());
        BotConfig.SetConfig("TempVC", "VCPanelChannelID", ctx.Channel.Id.ToString());
    }
}