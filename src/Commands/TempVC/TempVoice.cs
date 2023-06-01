using AGC_Management.Helpers;
using AGC_Management.Helpers.TempVoice;
using AGC_Management.Services.DatabaseHandler;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using DisCatSharp.Exceptions;
using Microsoft.Extensions.Logging;
using Npgsql;
using Sentry;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Threading.Channels;

namespace AGC_Management.Commands.TempVC;

[EventHandler]
public class TempVCEventHandler : TempVoiceHelper
{
    [Event]
    private async Task VoiceStateUpdated(object sender, VoiceStateUpdateEventArgs e)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                //if (e.Guild.Id != ulong.Parse(BotConfig.GetConfig()["ServerConfig"]["ServerId"])) return;
                if (e.Guild.Id != 818699057878663168) return;

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
                                    channelMods = (string)item["channelmods"] != null
                                        ? (string)item["channelmods"]
                                        : string.Empty;
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
                                await m.ModifyAsync(x => x.VoiceChannel = voice);
                                
                                if (locked)
                                {
                                    voice.ModifyAsync(x =>
                                        x.PermissionOverwrites =
                                            voice.PermissionOverwrites.ConvertToBuilderWithNewOverwrites(e.Guild.EveryoneRole,
                                                Permissions.None, Permissions.UseVoice));
                                }

                                if (hidden)
                                {
                                    voice.ModifyAsync(x =>
                                        x.PermissionOverwrites =
                                            voice.PermissionOverwrites.ConvertToBuilderWithNewOverwrites(e.Guild.EveryoneRole,
                                                Permissions.None, Permissions.AccessChannels));
                                }
                                
                                foreach (string user in blockeduserslist)
                                {
                                    if (ulong.TryParse(user, out ulong blockeduser))
                                    {
                                        try
                                        {
                                            DiscordMember blockedmember = await e.Guild.GetMemberAsync(blockeduser);
                                            await voice.AddOverwriteAsync(blockedmember, deny: Permissions.UseVoice);
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
                                            DiscordMember permitedmember = await e.Guild.GetMemberAsync(permiteduser);
                                            await voice.AddOverwriteAsync(permitedmember, Permissions.UseVoice);
                                            await voice.AddOverwriteAsync(permitedmember, Permissions.AccessChannels);
                                        }
                                        catch (NotFoundException)
                                        {
                                        }
                                    }
                                }
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        });
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

            await channel.ModifyAsync(x =>
                x.PermissionOverwrites =
                    channel.PermissionOverwrites.ConvertToBuilderWithNewOverwrites(ctx.Guild.EveryoneRole,
                        Permissions.None, Permissions.UseVoice));

            await msg.ModifyAsync("<:success:1085333481820790944> Du hast den Channel erfolgreich **gesperrt**!");
        }
    }

    /*

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

            await channel.ModifyAsync(x => x.PermissionOverwrites = channel.PermissionOverwrites.ConvertToBuilder()
                .Where(x =>
                {
                    if (x.Target.Id == ctx.Guild.EveryoneRole.Id)
                        x.Denied = x.Denied.Revoke(Permissions.UseVoice);
                    return true;
                }));


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

            await channel.ModifyAsync(x =>
                x.PermissionOverwrites =
                    channel.PermissionOverwrites.ConvertToBuilderWithNewOverwrites(ctx.Guild.EveryoneRole,
                        Permissions.None, Permissions.AccessChannels));


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

            await channel.ModifyAsync(x => x.PermissionOverwrites = channel.PermissionOverwrites.ConvertToBuilder()
                .Where(
                    x =>
                    {
                        if (x.Target.Id == ctx.Guild.EveryoneRole.Id)
                            x.Denied = x.Denied.Revoke(Permissions.AccessChannels);
                        return true;
                    }));
            await msg.ModifyAsync("<:success:1085333481820790944> Der Channel ist nun **sichtbar**!");
        }
    }
   */

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
            msg.ModifyAsync("<:attention:1085333468688433232> Du bist in keinem Voice-Channel.");
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
        var overwrites = new List<DiscordOverwriteBuilder>();

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

            await channel.ModifyAsync(x => x.PermissionOverwrites = channel.PermissionOverwrites.ConvertToBuilder()
                .Where(x =>
                {
                    if (x.Target == orig_owner.Id)
                    {
                        x.Allowed = x.Allowed.Revoke(Permissions.ManageChannels)
                            .Revoke(Permissions.UseVoice).Revoke(Permissions.MoveMembers)
                            .Revoke(Permissions.AccessChannels);
                    }
                    return true;
                }));
            await channel.ModifyAsync(x =>
                x.PermissionOverwrites =
                    channel.PermissionOverwrites.ConvertToBuilderWithNewOverwrites(ctx.Member,
                        Permissions.ManageChannels | Permissions.UseVoice | Permissions.MoveMembers | Permissions.AccessChannels, Permissions.None));

            await msg.ModifyAsync("<:success:1085333481820790944> Du hast den Channel erfolgreich **geclaimt**!");
        }
        if (channel.Users.Contains(orig_owner) && all_dbChannels.Contains((long)userChannel.Id))
        {
            await msg.ModifyAsync(
                $"<:attention:1085333468688433232> Du kannst dein Channel nicht Claimen, da der Channel-Owner ``{orig_owner.UsernameWithDiscriminator}`` noch im Channel ist.");
            return;
        }


    }
   
    

    [Command("block")]
    [RequireDatabase]
    [Aliases("vcban", "multiblock")]
    public async Task VoiceBlock(CommandContext ctx, [RemainingText] string users)
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
            "<a:loading_agc:1084157150747697203> **Lade...** Versuche Nutzer zu blockieren...");
            //var overwrites = new List<DiscordOverwriteBuilder>();
            var blockedlist = new List<ulong>();
            List<ulong> ids = new List<ulong>();
            ids = Converter.ExtractUserIDsFromString(users);
            var staffrole = ctx.Guild.GetRole(GlobalProperties.StaffRoleId);
            foreach (ulong id in ids)
            {
                try
                {
                    var user = await ctx.Guild.GetMemberAsync(id);
                    
                    if (user.Roles.Contains(staffrole) || user.Id == ctx.User.Id)
                    {
                        continue;
                    }

                    await userChannel.AddOverwriteAsync(user, deny: Permissions.UseVoice);

                    //overwrites.Add(new DiscordOverwriteBuilder(id).Deny(Permissions.UseVoice));
                    if (userChannel.Users.Contains(user) && !user.Roles.Contains(staffrole))
                    {
                        //await user.DisconnectFromVoiceAsync();
                    }
                    blockedlist.Add(user.Id);
                }
                catch (NotFoundException)
                {
                }
            }

            int successCount = blockedlist.Count;
            string endstring = $"<:success:1085333481820790944> **Erfolg!** Es {(successCount == 1 ? "wurde" : "wurden")} {successCount} Nutzer erfolgreich **blockiert**!";

            await msg.ModifyAsync(endstring);
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