using AGC_Management.Helpers;
using AGC_Management.Helpers.TempVoice;
using AGC_Management.Services.DatabaseHandler;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using DisCatSharp.Exceptions;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;

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
                if (e.Guild.Id != 826878963354959933) return;

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
                                defaultVcName = string.IsNullOrWhiteSpace(defaultVcName) ? $"{m.Username}'s Channel" : defaultVcName;
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
                                await m.ModifyAsync(x => x.VoiceChannel = voice);
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
                                    await voice.AddOverwriteAsync(e.Guild.EveryoneRole, deny: Permissions.UseVoice);
                                }

                                if (hidden)
                                {
                                    await voice.AddOverwriteAsync(e.Guild.EveryoneRole,
                                        deny: Permissions.AccessChannels);
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
        foreach (long channel in dbChannels)
        {
        }

        DiscordChannel userChannel = ctx.Member?.VoiceState?.Channel;
        if (userChannel == null || !dbChannels.Contains((long)(userChannel?.Id)))
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
            if (overwrite != null && overwrite.CheckPermission(Permissions.UseVoice).Equals(PermissionLevel.Denied))
            {
                await msg.ModifyAsync("<:attention:1085333468688433232> Der Channel ist bereits **gesperrt**!");
                return;
            }

            int vclimit = (int)channel.UserLimit;
            await channel.ModifyAsync(x =>
            {
                x.PermissionOverwrites = new List<DiscordOverwriteBuilder>
                {
                    new DiscordOverwriteBuilder()
                        .For(default_role)
                        .Deny(Permissions.UseVoice)
                };
                x.UserLimit = vclimit;
            });

            await msg.ModifyAsync("<:success:1085333481820790944> Du hast den Channel erfolgreich **gesperrt**!");

        }
    }

    [Command("unlock")]
    [RequireDatabase]
    //[RequireVoiceChannel]
    public async Task VoiceUnlock(CommandContext ctx)
    {
        List<long> dbChannels = await GetChannelIDFromDB(ctx);
        foreach (long channel in dbChannels)
        {
        }

        DiscordChannel userChannel = ctx.Member?.VoiceState?.Channel;
        if (userChannel == null || !dbChannels.Contains((long)(userChannel?.Id)))
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
            if (overwrite != null && overwrite.CheckPermission(Permissions.UseVoice).Equals(PermissionLevel.Unset))
            {
                await msg.ModifyAsync("<:attention:1085333468688433232> Der Channel ist bereits **entsperrt**!");
                return;
            }
            int vclimit = (int)channel.UserLimit;
            await channel.ModifyAsync(x =>
            {
                x.PermissionOverwrites = channel.PermissionOverwrites.ConvertToBuilderWithNewOverwrites(ctx.Guild.EveryoneRole, Permissions.None) // TODO: Fix this
                x.UserLimit = vclimit;
            });

            await msg.ModifyAsync("<:success:1085333481820790944> Du hast den Channel erfolgreich **entsperrt**!");
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