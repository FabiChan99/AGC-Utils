using AGC_Management.Services.DatabaseHandler;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using DisCatSharp.Exceptions;
using DisCatSharp.Interactivity.Extensions;
using Npgsql;

namespace AGC_Management.Helpers.TempVoice;

public class TempVoiceHelper : BaseCommandModule
{
    private static readonly Dictionary<ulong, string> levelroles = new()
    {
        { 750402390691152005, "5+" },
        { 798562254408777739, "10+" },
        { 750450170189185024, "15+" },
        { 798555933089071154, "20+" },
        { 750450342474416249, "25+" },
        { 750450621492101280, "30+" },
        { 798555135071617024, "35+" },
        { 751134108893184072, "40+" },
        { 776055585912389673, "45+" },
        { 750458479793274950, "50+" },
        { 798554730988306483, "60+" },
        { 757683142894157904, "70+" },
        { 810231454985486377, "80+" },
        { 810232899713630228, "90+" },
        { 810232892386705418, "100+" }
    };

    private static readonly Dictionary<ulong, string> debuglevelroles = new()
    {
        { 1116778938073616545, "001" },
        { 1116778981442723870, "002" }
    };


    protected static string GetVCConfig(string key)
    {
        return BotConfig.GetConfig()["TempVC"][$"{key}"];
    }

    protected static async Task RemoveChannelFromDB(ulong cid)
    {
        Dictionary<string, (object value, string comparisonOperator)>
            DeletewhereConditions = new()
            {
                { "channelid", ((long)cid, "=") }
            };
        await DatabaseService.DeleteDataFromTable("tempvoice", DeletewhereConditions);
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
                    Content = "<:attention:1085333468688433232> Der Channel ist bereits gesperrt!"
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
            if (overwrite == null || overwrite?.CheckPermission(Permissions.UseVoice) == PermissionLevel.Unset)
            {
                DiscordInteractionResponseBuilder builder = new()
                {
                    IsEphemeral = true,
                    Content = "<:attention:1085333468688433232> Der Channel ist bereits entsperrt!"
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
                    Content = "<:attention:1085333468688433232> Der Channel ist bereits versteckt!"
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
            if (overwrite == null || overwrite?.CheckPermission(Permissions.AccessChannels) == PermissionLevel.Unset)
            {
                DiscordInteractionResponseBuilder builder = new()
                {
                    IsEphemeral = true,
                    Content = "<:attention:1085333468688433232> Der Channel ist bereits sichtbar!"
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
            modal.AddTextComponent(new DiscordTextComponent(TextComponentStyle.Small, label: "Kanal umbenennen:"));
            await interaction.CreateInteractionModalResponseAsync(modal);
            var interactivity = client.GetInteractivity();
            var result = await interactivity.WaitForModalAsync(idstring, TimeSpan.FromMinutes(1));
            if (result.TimedOut)
            {
                return;
            }

            var name = result.Result.Interaction.Data.Components[0].Value;
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
                build.WithContent(
                    $"<:attention:1085333468688433232> **Fehler!** Der Channel wurde in den letzten 5 Minuten schon einmal umbenannt. Bitte warte noch etwas, bevor du den Channel erneut umbenennen kannst. __Beachte:__ Auf diese Aktualisierung haben wir keinen Einfluss und dies Betrifft nur Bots. Erneut umbenennen kannst du den Channel <t:{calc}:R>.");
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
                    await command.ExecuteNonQueryAsync();
                }
            }

            var builder = new DiscordFollowupMessageBuilder();
            builder.WithContent("<:success:1085333481820790944> **Erfolg!** Der Channel wurde erfolgreich umbenannt.");
            builder.IsEphemeral = true;
            await result.Result.Interaction.CreateFollowupMessageAsync(builder);
        }
    }

    protected static async Task PanelChannelLimit(DiscordInteraction interaction, DiscordClient client)
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
            var caseid = Helpers.GenerateCaseID();
            var idstring = $"LimitModal-{caseid}";
            DiscordInteractionModalBuilder modal = new();
            modal.WithTitle("Channel Limit");
            modal.CustomId = idstring;
            modal.AddTextComponent(new DiscordTextComponent(TextComponentStyle.Small, label: "Kanal Limit festlegen:",
                minLength: 1, maxLength: 2, placeholder: "Limit zwischen 0 und 99 eingeben."));
            await interaction.CreateInteractionModalResponseAsync(modal);
            var interactivity = client.GetInteractivity();
            var result = await interactivity.WaitForModalAsync(idstring, TimeSpan.FromMinutes(1));
            if (result.TimedOut)
            {
                return;
            }

            var limit = result.Result.Interaction.Data.Components[0].Value;
            await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            int climit = 0;
            try
            {
                climit = int.Parse(limit);
            }
            catch (FormatException ex)
            {
                Console.WriteLine(ex.Message);
                var errbuilder = new DiscordFollowupMessageBuilder();
                errbuilder.WithContent(
                    "<:attention:1085333468688433232> **Fehler!** Stelle sicher, dass das Limit korrekt ist. Bitte gebe nur Zahlen von 0 - 99 ein.");
                errbuilder.IsEphemeral = true;
                await result.Result.Interaction.CreateFollowupMessageAsync(errbuilder);
                return;
            }

            var channel = userChannel;
            try
            {
                await channel.ModifyAsync(x => x.UserLimit = climit);
            }
            catch (BadRequestException ex)
            {
                Console.WriteLine(ex.Message);
                var errbuilder = new DiscordFollowupMessageBuilder();
                errbuilder.WithContent(
                    "<:attention:1085333468688433232> **Fehler!** Stelle sicher, dass das Limit korrekt ist. Bitte gebe nur Zahlen von 0 - 99 ein.");
                errbuilder.IsEphemeral = true;
                await result.Result.Interaction.CreateFollowupMessageAsync(errbuilder);
                return;
            }

            var builder = new DiscordFollowupMessageBuilder();
            if (climit == 0)
            {
                builder.WithContent("<:success:1085333481820790944> **Erfolg!** Das Limit wurde erfolgreich entfernt.");
                builder.IsEphemeral = true;
                await result.Result.Interaction.CreateFollowupMessageAsync(builder);
                return;
            }

            builder.WithContent(
                $"<:success:1085333481820790944> **Erfolg!** Das Limit wurde erfolgreich auf {climit} gesetzt.");
            builder.IsEphemeral = true;
            await result.Result.Interaction.CreateFollowupMessageAsync(builder);
        }
    }

    protected static async Task PanelChannelInvite(DiscordInteraction interaction)
    {
        var db_channels = await GetAllChannelIDsFromDB();
        var my_db_channels = await GetChannelIDFromDB(interaction);
        DiscordMember member = await interaction.Guild.GetMemberAsync(interaction.User.Id);
        DiscordChannel userChannel = member?.VoiceState?.Channel;
        if (userChannel == null || !my_db_channels.Contains((long)userChannel?.Id))
        {
            await NoChannel(interaction);
            return;
        }

        if (!db_channels.Contains((long)userChannel?.Id))
        {
            string errorMessage =
                "<:attention:1085333468688433232> Du musst in einem TempVoice Kanal sein um Mitglieder einladen zu können!";
            DiscordInteractionResponseBuilder ib = new()
            {
                IsEphemeral = true
            };
            await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                ib.WithContent(errorMessage));
            return;
        }

        var options = new DiscordUserSelectComponent[]
        {
            new("Wähle den einzuladenden User aus.", "invite_selector")
        };
        DiscordInteractionResponseBuilder builder_ = new()
        {
            IsEphemeral = true,
            Content = "Wähle die User aus, die du einladen möchtest."
        };
        var builder = builder_.AddComponents(options);
        await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
    }

    protected static async Task PanelChannelInviteCallback(DiscordInteraction interaction, DiscordClient sender)
    {
        var userid = interaction.Data.Values[0];
        DiscordMember user = await interaction.Guild.GetMemberAsync(ulong.Parse(userid));
        DiscordMember executor = await interaction.Guild.GetMemberAsync(interaction.User.Id);
        bool send = false;
        if (user == interaction.User)
        {
            await interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder
                {
                    IsEphemeral = true,
                    Content = "<:attention:1085333468688433232> Du kannst dich nicht selbst einladen!"
                });
            return;
        }

        if (user.IsBot)
        {
            await interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate,
                new DiscordInteractionResponseBuilder
                {
                    IsEphemeral = true,
                    Content = "<:attention:1085333468688433232> Du kannst keine Bots einladen!"
                });
            return;
        }

        if (user.VoiceState != null && user.VoiceState.Channel.Users.Contains(user))
        {
            await interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder
                {
                    IsEphemeral = true,
                    Content = $"<:attention:1085333468688433232> Der {user.Mention} ist bereits in deinem Channel!"
                });
            return;
        }

        try
        {
            DiscordEmbedBuilder eb = new DiscordEmbedBuilder()
                .WithDescription(
                    $"Du wurdest von {interaction.User.Mention} eingeladen <#{interaction.Guild.GetMemberAsync(interaction.User.Id).Result.VoiceState?.Channel.Id}> beizutreten.")
                .WithColor(BotConfig.GetEmbedColor());
            await user.SendMessageAsync(eb);
            send = true;
        }
        catch (UnauthorizedException)
        {
            send = false;
        }

        if (send)
        {
            DiscordRole role = interaction.Guild.EveryoneRole;
            var overwrites = executor.VoiceState?.Channel.PermissionOverwrites.Select(x => x.ConvertToBuilder())
                .ToList();
            overwrites.Merge(user, Permissions.AccessChannels | Permissions.UseVoice,
                Permissions.None);
            await executor.VoiceState?.Channel.ModifyAsync(x => { x.PermissionOverwrites = overwrites; });
            await interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder
                {
                    IsEphemeral = true,
                    Content =
                        $"<:success:1085333481820790944> {user.Mention} wurde in <#{interaction.Guild.GetMemberAsync(interaction.User.Id).Result.VoiceState.Channel.Id}> eingeladen."
                });
            return;
        }

        await interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
            new DiscordInteractionResponseBuilder
            {
                IsEphemeral = true,
                Content =
                    $"<:attention:1085333468688433232> {user.Mention} konnte nicht eingeladen werden. Dieser User erlaubt keine DMs!"
            });
    }

    protected static async Task PanelChannelDelete(DiscordInteraction interaction, DiscordClient client,
        ComponentInteractionCreateEventArgs e)
    {
        var db_channel = await GetChannelIDFromDB(interaction);
        DiscordMember member = await interaction.Guild.GetMemberAsync(interaction.User.Id);
        DiscordChannel userChannel = member?.VoiceState?.Channel;
        if (userChannel == null || !db_channel.Contains((long)userChannel?.Id))
        {
            await NoChannel(interaction);
            return;
        }

        if (userChannel != null && db_channel.Contains((long)userChannel?.Id))
        {
            var caseid = Helpers.GenerateCaseID();
            DiscordChannel channel = interaction.Guild.GetChannel(userChannel.Id);
            List<DiscordButtonComponent> buttons = new(2)
            {
                new DiscordButtonComponent(ButtonStyle.Success, $"chdel_accept_{caseid}", "Ja"),
                new DiscordButtonComponent(ButtonStyle.Danger, $"chdel_deny_{caseid}", "Nein")
            };
            var interactivity = client.GetInteractivity();
            string errorMessage =
                "<:attention:1085333468688433232> Möchtest du deinen Kanal wirklich löschen?";
            DiscordInteractionResponseBuilder ib = new()
            {
                IsEphemeral = true
            };
            await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                ib.WithContent(errorMessage).AddComponents(buttons));
            var resp = await interaction.GetOriginalResponseAsync();


            var result = await interactivity.WaitForButtonAsync(resp, TimeSpan.FromSeconds(60));
            if (result.TimedOut)
            {
                await interaction.EditOriginalResponseAsync(
                    new DiscordWebhookBuilder
                    {
                        Content = "<:attention:1085333468688433232> Du hast nicht rechtzeitig reagiert."
                    });
                return;
            }

            if (result.Result.Id == $"chdel_accept_{caseid}")
            {
                var cid = channel.Id;
                await channel.DeleteAsync();
                await interaction.EditOriginalResponseAsync(
                    new DiscordWebhookBuilder
                    {
                        Content = "<:success:1085333481820790944> Dein Channel wurde gelöscht."
                    });
                await RemoveChannelFromDB(cid);
                return;
            }

            if (result.Result.Id == $"chdel_deny_{caseid}")
            {
                await interaction.EditOriginalResponseAsync(
                    new DiscordWebhookBuilder
                    {
                        Content = "<:attention:1085333468688433232> Vorgang abgebrochen."
                    });
            }
        }
    }

    protected static async Task PanelPermitVoiceSelector(DiscordInteraction interaction, DiscordClient client,
        ComponentInteractionCreateEventArgs e)
    {
        var db_channel = await GetChannelIDFromDB(interaction);
        DiscordMember member = await interaction.Guild.GetMemberAsync(interaction.User.Id);
        DiscordChannel userChannel = member?.VoiceState?.Channel;
        if (userChannel == null || !db_channel.Contains((long)userChannel?.Id))
        {
            await NoChannel(interaction);
            return;
        }

        if (userChannel != null && db_channel.Contains((long)userChannel.Id))
        {
            var channel = interaction.Guild.GetChannel(userChannel.Id);
            var interactivity = client.GetInteractivity();
            var selector = new List<DiscordComponent>
            {
                new DiscordUserSelectComponent("Wähle zuzulassende Mitglieder aus.", "permit_selector",
                    1, 8)
            };
            string message =
                "<:botpoint:1083853403316297758> Um eine Option auszuwählen, verwende das Menü und klicke darauf:";

            if (interaction.Guild.Id == 750365461945778209)
            {
                List<DiscordComponent> button = new()
                {
                    new DiscordButtonComponent(ButtonStyle.Secondary, "role_permit_button",
                        "Levelbeschränkung festlegen")
                };


                DiscordInteractionResponseBuilder ib = new()
                {
                    IsEphemeral = true,
                    Content = message
                };
                List<DiscordActionRowComponent> rowComponents = new()
                {
                    new DiscordActionRowComponent(selector),
                    new DiscordActionRowComponent(button)
                };
                await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    ib.AddComponents(rowComponents));
            }
            else
            {
                List<DiscordComponent> button = new()
                {
                    new DiscordButtonComponent(ButtonStyle.Secondary, "role_permit_button",
                        "Levelbeschränkung festlegen")
                };


                DiscordInteractionResponseBuilder ib = new()
                {
                    IsEphemeral = true,
                    Content = message
                };
                List<DiscordActionRowComponent> rowComponents = new()
                {
                    new DiscordActionRowComponent(selector),
                    new DiscordActionRowComponent(button)
                };
                await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    ib.AddComponents(rowComponents));
            }
        }
    }

    protected static async Task PanelPermitVoiceSelectorCallback(DiscordInteraction interaction, DiscordClient client,
        ComponentInteractionCreateEventArgs e)
    {
        var db_channel = await GetChannelIDFromDB(interaction);
        DiscordMember member = await interaction.Guild.GetMemberAsync(interaction.User.Id);
        DiscordChannel userChannel = member?.VoiceState?.Channel;
        if (userChannel == null || !db_channel.Contains((long)userChannel?.Id))
        {
            await NoChannel(interaction);
            return;
        }

        if (userChannel != null && db_channel.Contains((long)userChannel.Id))
        {
            DiscordChannel channel = interaction.Guild.GetChannel(userChannel.Id);

            var u = e.Values.ToList();
            var users = e.Values.Select(x => ulong.Parse(x));
            var usersList = new List<DiscordMember>();
            List<ulong> idlist = new();
            var overwrites = channel.PermissionOverwrites.Select(x => x.ConvertToBuilder())
                .ToList();
            foreach (ulong id in users)
            {
                try
                {
                    idlist.Add(id);
                    if (id == interaction.User.Id)
                    {
                        continue;
                    }

                    var user = await interaction.Guild.GetMemberAsync(id);


                    overwrites = overwrites.Merge(user, Permissions.AccessChannels | Permissions.UseVoice,
                        Permissions.None);


                    usersList.Add(user);
                }
                catch (NotFoundException)
                {
                    // ignored
                }
            }

            await channel.ModifyAsync(x => { x.PermissionOverwrites = overwrites; });
            await interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder
                {
                    IsEphemeral = true,
                    Content =
                        $"<:success:1085333481820790944> {usersList.Count} von {idlist.Count} User {(usersList.Count == 1 ? "wurde" : "wurden")} **permittet**."
                });
        }
    }

    protected static async Task PanelPermitVoiceRole(DiscordInteraction interaction, DiscordClient client,
        InteractionCreateEventArgs e)
    {
        var db_channel = await GetChannelIDFromDB(interaction);
        DiscordMember member = await interaction.Guild.GetMemberAsync(interaction.User.Id);
        DiscordChannel userChannel = member?.VoiceState?.Channel;
        if (userChannel == null || !db_channel.Contains((long)userChannel?.Id))
        {
            await NoChannel(interaction);
            return;
        }

        bool ch_locked = false;
        var overwrite = userChannel.PermissionOverwrites.FirstOrDefault(o => o.Id == interaction.Guild.EveryoneRole.Id);
        if (overwrite == null || overwrite?.CheckPermission(Permissions.UseVoice) == PermissionLevel.Unset)
        {
            ch_locked = false;
        }

        if (overwrite?.CheckPermission(Permissions.UseVoice) == PermissionLevel.Denied)
        {
            ch_locked = true;
        }

        if (!ch_locked)
        {
            await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder
                {
                    IsEphemeral = true,
                    Content =
                        "<:attention:1085333468688433232> Der Channel ist **nicht gesperrt** und eine Levelbegrenzung würde __keinen Effekt__ haben. Bitte **sperre** den Channel zuerst!"
                });
            return;
        }

        if (userChannel != null && db_channel.Contains((long)userChannel.Id))
        {
            bool role_permitted = false;
            Dictionary<ulong, string> lvlroles = debuglevelroles;
            foreach (var role in interaction.Guild.Roles)
            {
                var RoleId = role.Value.Id;
                if (lvlroles.ContainsKey(RoleId))
                {
                    var temp_ow = userChannel.PermissionOverwrites.FirstOrDefault(o => o.Id == RoleId);
                    if (temp_ow != null)
                    {
                        role_permitted = true;
                        break;
                    }
                }
            }

            if (role_permitted)
            {
                await interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder
                    {
                        IsEphemeral = true,
                        Content =
                            "<:attention:1085333468688433232> Es wurde bereits eine **Levelbegrenzung** für diesen Channel festgelegt."
                    });
                return;
            }

            var options = new List<DiscordStringSelectComponentOption>();

            foreach (var kvp in lvlroles)
            {
                ulong roleId = kvp.Key;
                string id = kvp.Value;
                options.Add(new DiscordStringSelectComponentOption(id, roleId.ToString()));
            }

            var selectComponent = new DiscordStringSelectComponent
                ("Wähle ein zuzulassendes Level aus.", options, "role_permit_selector");
            var sbuilder = new DiscordInteractionResponseBuilder
            {
                IsEphemeral = true,
                Content =
                    "<:attention:1085333468688433232> Es wurde bereits eine **Levelbegrenzung** für diesen Channel festgelegt."
            };
            sbuilder.AddComponents(selectComponent);
            await interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, sbuilder);
        }
    }

    protected static async Task PanelPermitVoiceRoleCallback(DiscordInteraction interaction, DiscordClient client,
        ComponentInteractionCreateEventArgs e)
    {
        var db_channel = await GetChannelIDFromDB(interaction);
        DiscordMember member = await interaction.Guild.GetMemberAsync(interaction.User.Id);
        DiscordChannel userChannel = member?.VoiceState?.Channel;
        if (userChannel == null || !db_channel.Contains((long)userChannel?.Id))
        {
            await NoChannel(interaction);
            return;
        }

        if (userChannel != null && db_channel.Contains((long)userChannel.Id))
        {
            var channel = userChannel;
            var sel_role = e.Values.ToList();
            var role = interaction.Guild.GetRole(ulong.Parse(sel_role[0]));
            var overwrites = channel.PermissionOverwrites.Select(x => x.ConvertToBuilder())
                .ToList();
            overwrites = overwrites.Merge(role, Permissions.AccessChannels | Permissions.UseVoice, Permissions.None);
            await channel.ModifyAsync(x => { x.PermissionOverwrites = overwrites; });
            await interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder
                {
                    IsEphemeral = true,
                    Content =
                        $"<:success:1085333481820790944> Erfolg! Es können nur noch Mitglieder den Kanal betreten, die die Rolle ``{role.Name}`` haben."
                });
        }
    }
}