using AGC_Management.Services.DatabaseHandler;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Interactivity.Extensions;
using Microsoft.VisualBasic;
using Npgsql;
using System.Xml.Linq;

namespace AGC_Management.Helpers.TempVoice;

public class TempVoiceHelper : BaseCommandModule
{
    protected static string GetVCConfig(string key)
    {
        return BotConfig.GetConfig()["TempVC"][$"{key}"];
    }


    protected static async Task<bool> NoChannel(CommandContext ctx)
    {
        string errorMessage = $"<:attention:1085333468688433232> **Fehler!** " +
                              $"Du besitzt keinen eigenen Kanal oder der Kanal gehört dir nicht. " +
                              $"Wenn du keinen Kanal hast, kannst du einen unter <#{GetVCConfig("Creation_Channel_ID")}> erstellen.";

        await ctx.RespondAsync(errorMessage);
        return true;
    }

    protected static async Task<bool> NoChannel(DiscordInteraction interaction)
    {
        string errorMessage = $"<:attention:1085333468688433232> **Fehler!** " +
                              $"Du besitzt keinen eigenen Kanal oder der Kanal gehört dir nicht. " +
                              $"Wenn du keinen Kanal hast, kannst du einen unter <#{GetVCConfig("Creation_Channel_ID")}> erstellen.";
        DiscordInteractionResponseBuilder ib = new()
        {
            IsEphemeral = true
        };
        await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            ib.WithContent(errorMessage));
        return true;
    }


    protected static async Task<bool> CheckTeam(CommandContext ctx, DiscordMember user)
    {
        DiscordRole staffRole = ctx.Guild.GetRole(ulong.Parse(BotConfig.GetConfig()["ServerConfig"]["StaffRoleId"]));
        if (staffRole.Members.Any(x => x.Key == user.Id))
        {
            await ctx.RespondAsync(
                $"<:attention:1085333468688433232> **Fehler!** Du kannst den Befehl ``{ctx.Command.Name}`` nicht auf Teammitglieder anwenden!");
            return true;
        }

        return false;
    }

    protected static ulong? GetUserChannel(CommandContext ctx)
    {
        ulong? userchannel;
        try
        {
            userchannel = ctx.Member.VoiceState?.Channel.Id;
        }
        catch
        {
            userchannel = null;
        }

        return userchannel;
    }

    protected static ulong? GetUserChannel(DiscordMember user)
    {
        ulong? userchannel;
        try
        {
            userchannel = user.VoiceState?.Channel.Id;
        }
        catch
        {
            userchannel = null;
        }

        return userchannel;
    }

    protected static DiscordChannel? GetUserChannelObj(CommandContext ctx)
    {
        DiscordChannel? channel;
        try
        {
            channel = ctx.Member.VoiceState?.Channel;
        }
        catch
        {
            channel = null;
        }

        return channel;
    }

    protected static DiscordChannel? GetUserChannelObj(DiscordMember user)
    {
        DiscordChannel? channel;
        try
        {
            channel = user.VoiceState?.Channel;
        }
        catch
        {
            channel = null;
        }

        return channel;
    }

    protected static async Task<List<long>> GetChannelIDFromDB(DiscordInteraction interaction)
    {
        List<long> dbChannels = new();

        List<string> Query = new()
        {
            "channelid"
        };
        Dictionary<string, object> QueryConditions = new()
        {
            { "ownerid", (long)interaction.User.Id }
        };
        List<Dictionary<string, object>> QueryResult =
            await DatabaseService.SelectDataFromTable("tempvoice", Query, QueryConditions);
        foreach (var result in QueryResult)
        {
            var chid = result["channelid"];
            var id = (long)chid;
            dbChannels.Add(id);
        }

        return dbChannels;
    }


    protected static async Task<List<long>> GetChannelIDFromDB(CommandContext ctx)
    {
        List<long> dbChannels = new();

        List<string> Query = new()
        {
            "channelid"
        };
        Dictionary<string, object> QueryConditions = new()
        {
            { "ownerid", (long)ctx.User.Id }
        };
        List<Dictionary<string, object>> QueryResult =
            await DatabaseService.SelectDataFromTable("tempvoice", Query, QueryConditions);
        foreach (var result in QueryResult)
        {
            var chid = result["channelid"];
            var id = (long)chid;
            dbChannels.Add(id);
        }

        return dbChannels;
    }

    protected static async Task<List<long>> GetAllChannelIDsFromDB()
    {
        List<long> dbChannels = new();

        List<string> Query = new()
        {
            "channelid"
        };
        List<Dictionary<string, object>> QueryResult =
            await DatabaseService.SelectDataFromTable("tempvoice", Query, null);
        foreach (var result in QueryResult)
        {
            var chid = result["channelid"];
            var id = (long)chid;
            dbChannels.Add(id);
        }

        return dbChannels;
    }

    protected static async Task<List<long>> GetChannelIDFromDB(DiscordMember member)
    {
        List<long> dbChannels = new();

        List<string> Query = new()
        {
            "channelid"
        };
        Dictionary<string, object> QueryConditions = new()
        {
            { "ownerid", member.Id }
        };
        List<Dictionary<string, object>> QueryResult =
            await DatabaseService.SelectDataFromTable("tempvoice", Query, QueryConditions);
        foreach (var result in QueryResult)
        {
            var chid = result["channelid"];
            var id = (long)chid;
            dbChannels.Add(id);
        }

        return dbChannels;
    }

    protected static async Task<long?> GetChannelOwnerID(CommandContext ctx)
    {
        long? channelownerid = null;
        try
        {
            List<string> query = new()
            {
                "ownerid"
            };

            Dictionary<string, object> queryConditions = new()
            {
                { "channelid", (long)ctx.Member.VoiceState?.Channel.Id }
            };

            List<Dictionary<string, object>> queryResult =
                await DatabaseService.SelectDataFromTable("tempvoice", query, queryConditions);

            foreach (var result in queryResult)
            {
                if (result.TryGetValue("ownerid", out object ownerIdValue) && ownerIdValue is long ownerId)
                {
                    channelownerid = ownerId;
                    break;
                }
            }
        }
        catch
        {
            // Handle exception
        }

        return channelownerid;
    }

    protected static async Task<long?> GetChannelOwnerID(DiscordMember user)
    {
        long? channelownerid = null;
        try
        {
            List<string> query = new()
            {
                "ownerid"
            };

            Dictionary<string, object> queryConditions = new()
            {
                { "channelid", (long)user.VoiceState?.Channel.Id }
            };

            List<Dictionary<string, object>> queryResult =
                await DatabaseService.SelectDataFromTable("tempvoice", query, queryConditions);

            foreach (var result in queryResult)
            {
                if (result.TryGetValue("ownerid", out object ownerIdValue) && ownerIdValue is long ownerId)
                {
                    channelownerid = ownerId;
                    break;
                }
            }
        }
        catch
        {
            // Handle exception
        }

        return channelownerid;
    }


    protected static async Task<long?> GetChannelOwnerID(DiscordInteraction interaction)
    {
        long? channelownerid = null;
        try
        {
            List<string> query = new()
            {
                "ownerid"
            };
            DiscordGuild discordGuild = interaction.Guild;
            DiscordMember discordMember = await discordGuild.GetMemberAsync(interaction.User.Id);
            Dictionary<string, object> queryConditions = new()
            {
                { "channelid", (long)discordMember.VoiceState?.Channel.Id }
            };

            List<Dictionary<string, object>> queryResult =
                await DatabaseService.SelectDataFromTable("tempvoice", query, queryConditions);

            foreach (var result in queryResult)
            {
                if (result.TryGetValue("ownerid", out object ownerIdValue) && ownerIdValue is long ownerId)
                {
                    channelownerid = ownerId;
                    break;
                }
            }
        }
        catch
        {
            // Handle exception
        }

        return channelownerid;
    }

    protected static async Task<long?> GetChannelOwnerID(DiscordChannel channel)
    {
        long? channelownerid = null;
        try
        {
            List<string> query = new()
            {
                "ownerid"
            };

            Dictionary<string, object> queryConditions = new()
            {
                { "channelid", channel.Id }
            };

            List<Dictionary<string, object>> queryResult =
                await DatabaseService.SelectDataFromTable("tempvoice", query, queryConditions);

            foreach (var result in queryResult)
            {
                if (result.TryGetValue("ownerid", out object ownerIdValue) && ownerIdValue is long ownerId)
                {
                    channelownerid = ownerId;
                    break;
                }
            }
        }
        catch
        {
            // Handle exception
        }

        return channelownerid;
    }

    protected static bool SelfCheck(CommandContext ctx, DiscordMember member)
    {
        if (ctx.User.Id == member.Id)
        {
            return true;
        }

        return false;
    }

    protected static async Task<List<long>> GetAllTempChannels()
    {
        var list = new List<long>();
        List<string> query = new()
        {
            "channelid"
        };
        List<Dictionary<string, object>> result = await DatabaseService.SelectDataFromTable("tempvoice", query, null);
        foreach (var item in result)
        {
            long ChannelId = (long)item["channelid"];
            list.Add(ChannelId);
        }

        return list;
    }


    protected bool NoChannelInter(DiscordInteraction interaction)
    {
        var builder = new DiscordInteractionResponseBuilder
        {
            Content = "You are not in a voice channel!",
            IsEphemeral = true
        };
        interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
        return true;
    }


    protected static async Task PanelLockChannel(DiscordInteraction interaction)
    {
        List<long> dbChannels = await GetChannelIDFromDB(interaction);
        DiscordMember member = await interaction.Guild.GetMemberAsync(interaction.User.Id);
        DiscordChannel userChannel = member?.VoiceState?.Channel;
        if (userChannel == null || !dbChannels.Contains((long)userChannel?.Id))
        {
            await NoChannel(interaction);
            return;
        }

        if (userChannel != null && dbChannels.Contains((long)userChannel.Id))
        {
            DiscordRole default_role = interaction.Guild.EveryoneRole;
            DiscordChannel channel = member.VoiceState.Channel;
            var overwrite = channel.PermissionOverwrites.FirstOrDefault(o => o.Id == default_role.Id);
            if (overwrite?.CheckPermission(Permissions.UseVoice) == PermissionLevel.Denied)
            {
                DiscordInteractionResponseBuilder builder = new()
                {
                    IsEphemeral = true,
                    Content = "Der Channel ist bereits gesperrt!"
                };
                await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                return;
            }

            var overwrites = userChannel.PermissionOverwrites.Select(x => x.ConvertToBuilder()).ToList();
            overwrites = overwrites.Merge(default_role, Permissions.None, Permissions.UseVoice);
            await userChannel.ModifyAsync(x => x.PermissionOverwrites = overwrites);

            DiscordInteractionResponseBuilder builder_ = new()
            {
                IsEphemeral = true,
                Content = $"<:success:1085333481820790944> <#{channel.Id}> ist nun **gesperrt**."
            };
            await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder_);
        }
    }

    protected static async Task PanelUnlockChannel(DiscordInteraction interaction)
    {
        List<long> dbChannels = await GetChannelIDFromDB(interaction);
        DiscordMember member = await interaction.Guild.GetMemberAsync(interaction.User.Id);
        DiscordChannel userChannel = member?.VoiceState?.Channel;
        if (userChannel == null || !dbChannels.Contains((long)userChannel?.Id))
        {
            await NoChannel(interaction);
            return;
        }

        if (userChannel != null && dbChannels.Contains((long)userChannel.Id))
        {
            DiscordRole default_role = interaction.Guild.EveryoneRole;
            DiscordChannel channel = member.VoiceState.Channel;
            var overwrite = channel.PermissionOverwrites.FirstOrDefault(o => o.Id == default_role.Id);
            if (overwrite?.CheckPermission(Permissions.UseVoice) == PermissionLevel.Unset)
            {
                DiscordInteractionResponseBuilder builder = new()
                {
                    IsEphemeral = true,
                    Content = "Der Channel ist bereits entsperrt!"
                };
                await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                return;
            }

            var overwrites = userChannel.PermissionOverwrites.Select(x => x.ConvertToBuilder()).ToList();
            overwrites = overwrites.Merge(default_role, Permissions.None, Permissions.None, Permissions.UseVoice);
            await userChannel.ModifyAsync(x => x.PermissionOverwrites = overwrites);

            DiscordInteractionResponseBuilder builder_ = new()
            {
                IsEphemeral = true,
                Content = $"<:success:1085333481820790944> <#{channel.Id}> ist nun **entsperrt**."
            };
            await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder_);
        }
    }


    protected static async Task PanelHideChannel(DiscordInteraction interaction)
    {
        List<long> dbChannels = await GetChannelIDFromDB(interaction);
        DiscordMember member = await interaction.Guild.GetMemberAsync(interaction.User.Id);
        DiscordChannel userChannel = member?.VoiceState?.Channel;
        if (userChannel == null || !dbChannels.Contains((long)userChannel?.Id))
        {
            await NoChannel(interaction);
            return;
        }

        if (userChannel != null && dbChannels.Contains((long)userChannel.Id))
        {
            DiscordRole default_role = interaction.Guild.EveryoneRole;
            DiscordChannel channel = member.VoiceState.Channel;
            var overwrite = channel.PermissionOverwrites.FirstOrDefault(o => o.Id == default_role.Id);
            if (overwrite?.CheckPermission(Permissions.AccessChannels) == PermissionLevel.Denied)
            {
                DiscordInteractionResponseBuilder builder = new()
                {
                    IsEphemeral = true,
                    Content = "Der Channel ist bereits versteckt!"
                };
                await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                return;
            }

            var overwrites = userChannel.PermissionOverwrites.Select(x => x.ConvertToBuilder()).ToList();
            overwrites = overwrites.Merge(default_role, Permissions.None, Permissions.AccessChannels);
            await userChannel.ModifyAsync(x => x.PermissionOverwrites = overwrites);

            DiscordInteractionResponseBuilder builder_ = new()
            {
                IsEphemeral = true,
                Content = $"<:success:1085333481820790944> <#{channel.Id}> ist nun **versteckt**."
            };
            await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder_);
        }
    }

    protected static async Task PanelUnhideChannel(DiscordInteraction interaction)
    {
        List<long> dbChannels = await GetChannelIDFromDB(interaction);
        DiscordMember member = await interaction.Guild.GetMemberAsync(interaction.User.Id);
        DiscordChannel userChannel = member?.VoiceState?.Channel;
        if (userChannel == null || !dbChannels.Contains((long)userChannel?.Id))
        {
            await NoChannel(interaction);
            return;
        }

        if (userChannel != null && dbChannels.Contains((long)userChannel.Id))
        {
            DiscordRole default_role = interaction.Guild.EveryoneRole;
            DiscordChannel channel = member.VoiceState.Channel;
            var overwrite = channel.PermissionOverwrites.FirstOrDefault(o => o.Id == default_role.Id);
            if (overwrite?.CheckPermission(Permissions.AccessChannels) == PermissionLevel.Unset)
            {
                DiscordInteractionResponseBuilder builder = new()
                {
                    IsEphemeral = true,
                    Content = "Der Channel ist bereits sichtbar!"
                };
                await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                return;
            }

            var overwrites = userChannel.PermissionOverwrites.Select(x => x.ConvertToBuilder()).ToList();
            overwrites = overwrites.Merge(default_role, Permissions.None, Permissions.None, Permissions.AccessChannels);
            await userChannel.ModifyAsync(x => x.PermissionOverwrites = overwrites);

            DiscordInteractionResponseBuilder builder_ = new()
            {
                IsEphemeral = true,
                Content = $"<:success:1085333481820790944> <#{channel.Id}> ist nun **sichtbar**."
            };
            await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder_);
        }
    }

    protected static async Task PanelChannelRename(DiscordInteraction interaction, DiscordClient client)
    {
        var current_timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        List<long> dbChannels = await GetChannelIDFromDB(interaction);
        DiscordMember member = await interaction.Guild.GetMemberAsync(interaction.User.Id);
        DiscordChannel userChannel = member?.VoiceState?.Channel;
        if (userChannel == null || !dbChannels.Contains((long)userChannel?.Id))
        {
            await NoChannel(interaction);
            return;
        }

        if (userChannel != null && dbChannels.Contains((long)userChannel.Id))
        {
            var caseid = Helpers.GenerateCaseID();
            var idstring = $"RenameModal-{caseid}";
            DiscordInteractionModalBuilder modal = new();
            modal.WithTitle("Channel Rename");
            modal.CustomId = idstring;
            modal.AddTextComponent(new DiscordTextComponent(TextComponentStyle.Small ,label: "Kanal umbenennen:"));
            await interaction.CreateInteractionModalResponseAsync(modal);
            var interactivity = client.GetInteractivity();
            var result = await interactivity.WaitForModalAsync(idstring, TimeSpan.FromMinutes(1));
            var name = result.Result.Interaction.Data.Components[0].Value.ToString();
            if (result.TimedOut)
            {
                return;
            }
            //Console.WriteLine(result.Result.Interaction.Data.Options.ToString());
            await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            var channel = userChannel;
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
                var build = new DiscordFollowupMessageBuilder();
                build.WithContent($"<:attention:1085333468688433232> **Fehler!** Der Channel wurde in den letzten 5 Minuten schon einmal umbenannt. Bitte warte noch etwas, bevor du den Channel erneut umbenennen kannst. __Beachte:__ Auf diese Aktualisierung haben wir keinen Einfluss und dies Betrifft nur Bots. Erneut umbenennen kannst du den Channel <t:{calc}:R>.");
                build.IsEphemeral = true;
                await result.Result.Interaction.CreateFollowupMessageAsync(build);
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
            var builder = new DiscordFollowupMessageBuilder();
            builder.WithContent($"<:success:1085333481820790944> **Erfolg!** Der Channel wurde erfolgreich umbenannt.");
            builder.IsEphemeral = true;
            await result.Result.Interaction.CreateFollowupMessageAsync(builder);
            return;
        }
    }
}