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
using Microsoft.CodeAnalysis.Operations;
using Npgsql;
using System.Threading.Channels;

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
                if ((e.Guild.Id != ulong.Parse(BotConfig.GetConfig()["ServerConfig"]["ServerId"])) |
                    (e.Guild.Id != 818699057878663168)) return;
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
                                catch (NotFoundException)
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
                                    .Replace("{discriminator}", m.Discriminator)
                                    .Replace("{userid}", m.Id.ToString())
                                    .Replace("{fullname}", m.UsernameWithDiscriminator);


                                DiscordChannel voice = await e.After?.Guild.CreateVoiceChannelAsync
                                (defaultVcName, e.After.Channel.Parent,
                                    96000, 0, qualityMode: VideoQualityMode.Full);
                                Dictionary<string, object> data = new()
                                {
                                    { "ownerid", (long)e.User.Id },
                                    { "channelid", (long)voice.Id },
                                    { "lastedited", (long)0 }
                                };
                                await DatabaseService.InsertDataIntoTable("tempvoice", data);
                                await voice.ModifyAsync(async x =>
                                {
                                    x.PermissionOverwrites = new List<DiscordOverwriteBuilder>
                                    {
                                        new DiscordOverwriteBuilder()
                                            .For(m)
                                            .Allow(Permissions.MoveMembers)
                                            .Allow(Permissions.ManageChannels)
                                            .Allow(Permissions.AccessChannels)
                                            .Allow(Permissions.UseVoice)
                                    };
                                    x.Position = e.After.Channel.Position + 1;
                                    x.UserLimit = voice.UserLimit;
                                });
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
                                }
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
                                catch (NotFoundException)
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
                        var overwrites = e.After?.Channel.PermissionOverwrites.Select(x => x.ConvertToBuilder()).ToList();
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
                                    hidden = item["hidden"] != null ? (bool)item["hidden"] : false;
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
                                await voice.ModifyAsync(async x =>
                                {
                                    x.PermissionOverwrites = new List<DiscordOverwriteBuilder>
                                    {
                                        new DiscordOverwriteBuilder()
                                            .For(m)
                                            .Allow(Permissions.MoveMembers)
                                            .Allow(Permissions.ManageChannels)
                                            .Allow(Permissions.AccessChannels)
                                            .Allow(Permissions.UseVoice)
                                    };
                                    x.Position = e.After.Channel.Position + 1;
                                    x.UserLimit = voice.UserLimit;
                                });
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
                                }
                                if (locked)
                                {
                                    overwrites = overwrites.Merge(voice.Guild.EveryoneRole, Permissions.None, Permissions.UseVoice);
                                }

                                if (hidden)
                                {
                                    overwrites = overwrites.Merge(voice.Guild.EveryoneRole, Permissions.None, Permissions.AccessChannels);
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


                                            overwrites = overwrites.Merge(userid, Permissions.UseVoice | Permissions.AccessChannels,
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
        if (userChannel == null || !dbChannels.Contains((long)userChannel?.Id))
        {
            await NoChannel(ctx);
            return;
        }

        if (userChannel != null && dbChannels.Contains((long)userChannel.Id))
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
        if (userChannel == null || !dbChannels.Contains((long)userChannel?.Id))
        {
            await NoChannel(ctx);
            return;
        }

        if (userChannel != null && dbChannels.Contains((long)userChannel.Id))
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
        if (userChannel == null || !dbChannels.Contains((long)userChannel?.Id))
        {
            await NoChannel(ctx);
            return;
        }

        if (userChannel != null && dbChannels.Contains((long)userChannel.Id))
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
        if (userChannel == null || !dbChannels.Contains((long)userChannel?.Id))
        {
            await NoChannel(ctx);
            return;
        }

        if (userChannel != null && dbChannels.Contains((long)userChannel.Id))
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
        if (userChannel == null || !dbChannels.Contains((long)userChannel?.Id))
        {
            await NoChannel(ctx);
            return;
        }

        if (userChannel != null && dbChannels.Contains((long)userChannel.Id))
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
        if (userChannel == null || !dbChannels.Contains((long)userChannel?.Id))
        {
            await NoChannel(ctx);
            return;
        }

        if (userChannel != null && dbChannels.Contains((long)userChannel.Id))
        {
            if (limit < 0 || limit > 99)
            {
                await ctx.RespondAsync(
                    "<:attention:1085333468688433232> **Fehler!** Der Limit-Wert muss zwischen 0 und 99 liegen.");
                return;
            }
        }

        if (limit == 0)
        {
            await ctx.RespondAsync(
                "<:success:1085333481820790944> Du hast das Userlimit erfolgreich **entfernt**.");
            return;
        }

        await userChannel.ModifyAsync(x => x.UserLimit = limit);
        await ctx.RespondAsync(
            $"<:success:1085333481820790944> Du hast {userChannel.Mention} erfolgreich ein Userlimit von **{limit}** gesetzt.");
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


            await userChannel.ModifyAsync(x => x.PermissionOverwrites = overwrites);
            await msg.ModifyAsync("<:success:1085333481820790944> Du hast den Channel erfolgreich **geclaimt**!");
        }

        if (channel.Users.Contains(orig_owner) && all_dbChannels.Contains((long)userChannel.Id))
        {
            await msg.ModifyAsync(
                $"<:attention:1085333468688433232> Du kannst dein Channel nicht Claimen, da der Channel-Owner ``{orig_owner.UsernameWithDiscriminator}`` noch im Channel ist.");
        }
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

                if (userChannel == null || !dbChannels.Contains((long)userChannel?.Id))
                {
                    await NoChannel(ctx);
                    return;
                }

                if (userChannel != null && dbChannels.Contains((long)userChannel.Id))
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

                            if (user.Roles.Contains(staffrole) || user.Id == ctx.User.Id)
                            {
                                continue;
                            }

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

                if (userChannel == null || !dbChannels.Contains((long)userChannel?.Id))
                {
                    await NoChannel(ctx);
                    return;
                }

                if (userChannel != null && dbChannels.Contains((long)userChannel.Id))
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

                if (userChannel == null || !dbChannels.Contains((long)userChannel?.Id))
                {
                    await NoChannel(ctx);
                    return;
                }

                if (userChannel != null && dbChannels.Contains((long)userChannel.Id))
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

                if (userChannel == null || !dbChannels.Contains((long)userChannel?.Id))
                {
                    await NoChannel(ctx);
                    return;
                }

                if (userChannel != null && dbChannels.Contains((long)userChannel.Id))
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


                            overwrites = overwrites.Merge(user, Permissions.AccessChannels | Permissions.UseVoice,
                                Permissions.None);


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
                        $"<a:loading_agc:1084157150747697203> **Lade...** Versuche Channel zu speichern...");
                    List<string> Query = new()
                    {
                        "userid"
                    };
                    bool hasSession = false;
                    var usersession = await DatabaseService.SelectDataFromTable("tempvoicesession", Query, null);
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

                        int rowsDeleted = await DatabaseService.DeleteDataFromTable("tempvoicesession", whereConditions);
                    }
                    var overwrite = userChannel.PermissionOverwrites.FirstOrDefault(o => o.Id == ctx.Guild.EveryoneRole.Id);
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
                    if (overwrite == null || overwrite?.CheckPermission(Permissions.AccessChannels) == PermissionLevel.Unset)
                    {
                        hidden = false;
                    }
                   
                    string blocklist = string.Empty;
                    string permitlist = string.Empty;
                    var buserow = userChannel.PermissionOverwrites.Where(x => x.CheckPermission(Permissions.UseVoice) == PermissionLevel.Denied).Select(x => x.Id).ToList();
                    
                    var puserow = userChannel.PermissionOverwrites
                        .Where(x => x.CheckPermission(Permissions.UseVoice) == PermissionLevel.Allowed)
                        .Where(x => x.Id != ctx.User.Id)
                        .Select(x => x.Id)
                        .ToList(); foreach (var user in buserow)
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
                    await msg.ModifyAsync($"<:success:1085333481820790944> **Erfolg!** Die Kanaleinstellungen wurden erfolgreich **gespeichert**!");
                }
            });
        }
    }
}

public class TempVoicePanel : TempVoiceHelper
{
    private static List<ulong> LevelRoleIDs = new()
    {
        750402390691152005, 798562254408777739, 750450170189185024, 798555933089071154,
        750450342474416249, 750450621492101280, 798555135071617024, 751134108893184072,
        776055585912389673, 750458479793274950, 798554730988306483, 757683142894157904,
        810231454985486377, 810232899713630228, 810232892386705418
    };

    private static List<string> lookup = new()
    {
        "5+", "10+", "15+", "20+", "25+", "30+", "35+", "40+", "45+", "50+", "60+", "70+", "80+", "90+", "100+"
    };
}