#region

using AGC_Management.Components;
using AGC_Management.Enums;
using AGC_Management.Helpers;
using AGC_Management.Services.DatabaseHandler;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using Npgsql;

#endregion

namespace AGC_Management.Managers;

public class TicketManager
{
    public static async Task<DiscordChannel?> OpenTicket(CommandContext context, TicketType ticketType,
        TicketCreator ticketCreator, DiscordMember discordMember)
    {
        long memberid = (long)discordMember.Id;
        long guildid = (long)context.Guild.Id;
        string ticketid = TicketManagerHelper.GenerateTicketID();
        bool existing_ticket = await TicketManagerHelper.CheckForOpenTicket(memberid);
        if (existing_ticket)
        {
            long tchannelId = await TicketManagerHelper.GetOpenTicketChannel(memberid);
            var tbutton = new DiscordLinkButtonComponent("https://discord.com/channels/" + guildid + "/" + tchannelId,
                "Zum Ticket");
            var eb = new DiscordEmbedBuilder
            {
                Title = "Fehler | Bereits ein Ticket geöffnet!",
                Description =
                    $"Der User hat bereits ein geöffnetes Ticket! -> <#{await TicketManagerHelper.GetOpenTicketChannel((long)context.Member.Id)}>",
                Color = DiscordColor.Red
            };
            var mb = new DiscordMessageBuilder().AddComponents(tbutton).AddEmbed(eb);
            await context.RespondAsync(mb);
            return null;
        }

        DiscordChannel Ticket_category =
            context.Guild.GetChannel(ulong.Parse(BotConfig.GetConfig()["TicketConfig"]["SupportCategoryId"]));
        DiscordChannel? ticket_channel = null;
        int ticket_number = await TicketManagerHelper.GetPreviousTicketCount(ticketType) + 1;
        var con = TicketDatabaseService.GetConnection();
        await using NpgsqlCommand cmd =
            new(
                $"INSERT INTO ticketstore (ticket_id, ticket_owner, tickettype, closed) VALUES ('{ticketid}', '{memberid}', '{ticketType.ToString().ToLower()}', False)",
                con);
        await cmd.ExecuteNonQueryAsync();

        ticket_channel = await context.Guild.CreateChannelAsync($"support-{ticket_number}", ChannelType.Text,
            Ticket_category,
            $"Ticket erstellt von {context.User.UsernameWithDiscriminator} zu {discordMember.UsernameWithDiscriminator}");

        await using NpgsqlCommand cmd2 =
            new(
                $"INSERT INTO ticketcache (ticket_id, ticket_owner, tchannel_id, claimed) VALUES ('{ticketid}', '{memberid}', '{ticket_channel.Id}', False)",
                con);
        await cmd2.ExecuteNonQueryAsync();
        await Task.Delay(TimeSpan.FromSeconds(2));
        await TicketManagerHelper.AddUserToTicket(context, ticket_channel, discordMember);
        await TicketManagerHelper.InsertHeaderIntoTicket(context, ticket_channel, discordMember);
        await TicketManagerHelper.SendStaffNotice(context, ticket_channel, discordMember);
        return ticket_channel;
    }

    public static async Task OpenTicket(DiscordInteraction interaction, TicketType ticketType, DiscordClient client,
        TicketCreator ticketCreator)
    {
        if (ticketCreator == TicketCreator.User)
        {
            long memberid = (long)interaction.User.Id;
            long guildid = (long)interaction.Guild.Id;
            string ticketid = TicketManagerHelper.GenerateTicketID();
            bool existing_ticket = await TicketManagerHelper.CheckForOpenTicket(memberid);
            if (existing_ticket)
            {
                long tchannelId = await TicketManagerHelper.GetOpenTicketChannel(memberid);
                var tbutton = new DiscordLinkButtonComponent(
                    "https://discord.com/channels/" + guildid + "/" + tchannelId,
                    "Zum Ticket");
                var eb = new DiscordEmbedBuilder
                {
                    Title = "Fehler | Bereits ein Ticket geöffnet!",
                    Description =
                        $"Du hast bereits ein geöffnetes Ticket! -> <#{await TicketManagerHelper.GetOpenTicketChannel((long)interaction.User.Id)}>",
                    Color = DiscordColor.Red
                };
                await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().AddEmbed(eb).AddComponents(tbutton).AsEphemeral());
                return;
            }

            var cre_emb = new DiscordEmbedBuilder
            {
                Title = "Ticket erstellen",
                Description = "Du hast ein Ticket erstellt! Bitte warte einen Augenblick...",
                Color = DiscordColor.Blurple
            };
            await interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(cre_emb).AsEphemeral());
            int ticket_number = await TicketManagerHelper.GetPreviousTicketCount(ticketType) + 1;
            DiscordChannel Ticket_category =
                interaction.Guild.GetChannel(ulong.Parse(BotConfig.GetConfig()["TicketConfig"]["SupportCategoryId"]));
            DiscordChannel? ticket_channel = null;
            if (ticketType == TicketType.Report)
            {
                var con = TicketDatabaseService.GetConnection();
                await using NpgsqlCommand cmd =
                    new(
                        $"INSERT INTO ticketstore (ticket_id, ticket_owner, tickettype, closed) VALUES ('{ticketid}', '{memberid}', '{ticketType.ToString().ToLower()}', False)",
                        con);
                await cmd.ExecuteNonQueryAsync();

                ticket_channel = await interaction.Guild.CreateChannelAsync($"report-{ticket_number}", ChannelType.Text,
                    Ticket_category, $"Ticket erstellt von {interaction.User.UsernameWithDiscriminator}");

                await using NpgsqlCommand cmd2 =
                    new(
                        $"INSERT INTO ticketcache (ticket_id, ticket_owner, tchannel_id, claimed) VALUES ('{ticketid}', '{memberid}', '{ticket_channel.Id}', False)",
                        con);
                await cmd2.ExecuteNonQueryAsync();
                await Task.Delay(TimeSpan.FromSeconds(2));
                await TicketManagerHelper.AddUserToTicket(interaction, ticket_channel, interaction.User);
                await TicketManagerHelper.InsertHeaderIntoTicket(interaction, ticket_channel, TicketCreator.User,
                    TicketType.Report);
            }
            else if (ticketType == TicketType.Support)
            {
                var con = TicketDatabaseService.GetConnection();
                await using NpgsqlCommand cmd =
                    new(
                        $"INSERT INTO ticketstore (ticket_id, ticket_owner, tickettype, closed) VALUES ('{ticketid}', '{memberid}', '{ticketType.ToString().ToLower()}', False)",
                        con);
                await cmd.ExecuteNonQueryAsync();

                ticket_channel = await interaction.Guild.CreateChannelAsync($"support-{ticket_number}",
                    ChannelType.Text,
                    Ticket_category, $"Ticket erstellt von {interaction.User.UsernameWithDiscriminator}");

                await using NpgsqlCommand cmd2 =
                    new(
                        $"INSERT INTO ticketcache (ticket_id, ticket_owner, tchannel_id, claimed) VALUES ('{ticketid}', '{memberid}', '{ticket_channel.Id}', False)",
                        con);
                await cmd2.ExecuteNonQueryAsync();
                await Task.Delay(TimeSpan.FromSeconds(2));
                await TicketManagerHelper.AddUserToTicket(interaction, ticket_channel, interaction.User);
                await TicketManagerHelper.InsertHeaderIntoTicket(interaction, ticket_channel, TicketCreator.User,
                    TicketType.Support);
            }

            var button = new DiscordLinkButtonComponent(
                "https://discord.com/channels/" + guildid + "/" + ticket_channel.Id,
                "Zum Ticket");
            var teb = new DiscordEmbedBuilder
            {
                Title = "Ticket erstellt",
                Description = $"Dein Ticket wurde erfolgreich erstellt! -> <#{ticket_channel.Id}>",
                Color = DiscordColor.Green
            };
            // inset header later
            await interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(teb)
                .AddComponents(button));
            await TicketManagerHelper.SendUserNotice(interaction, ticket_channel, ticketType);
        }
    }

    public static async Task CloseTicket(CommandContext ctx, DiscordChannel ticket_channel)
    {
        // fetch first message of this channel
        await NotificationManager.ClearMode(ticket_channel.Id);
        var channelmessages = await ctx.Channel.GetMessagesAsync();
        var message = channelmessages.LastOrDefault();
        var umb = new DiscordMessageBuilder();
        umb.WithContent(message.Content).WithEmbed(message.Embeds[0]);
        var components = TicketComponents.GetClosedTicketActionRow();
        List<DiscordActionRowComponent> row = new()
        {
            new DiscordActionRowComponent(components)
        };
        umb.AddComponents(row);
        await message.ModifyAsync(umb);
        var ceb = new DiscordEmbedBuilder
        {
            Description = "Ticket wird geschlossen..",
            Color = DiscordColor.Yellow
        };
        await ticket_channel.SendMessageAsync(ceb);

        var eb1 = new DiscordEmbedBuilder
        {
            Description = "Transcript wird gespeichert....",
            Color = DiscordColor.Yellow
        };
        var msg = await ctx.Channel.SendMessageAsync(eb1.Build());

        eb1 = new DiscordEmbedBuilder
        {
            Description = "Transcript wurde gespeichert",
            Color = DiscordColor.Green
        };

        string transcriptURL = await TicketManagerHelper.GenerateTranscript(ticket_channel);
        await TicketManagerHelper.InsertTransscriptIntoDB(ticket_channel, TranscriptType.User, transcriptURL);

        await msg.ModifyAsync(eb1.Build());

        var con = TicketDatabaseService.GetConnection();
        string query = $"SELECT ticket_id FROM ticketcache where tchannel_id = '{(long)ticket_channel.Id}'";
        await using NpgsqlCommand cmd = new(query, con);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
        string ticket_id = "";
        while (reader.Read())
        {
            ticket_id = reader.GetString(0);
        }

        await reader.CloseAsync();

        await using NpgsqlCommand cmd2 = new($"UPDATE ticketstore SET closed = True WHERE ticket_id = '{ticket_id}'",
            con);
        await cmd2.ExecuteNonQueryAsync();

        var con2 = TicketDatabaseService.GetConnection();
        string query2 = $"SELECT ticket_users FROM ticketcache where tchannel_id = '{(long)ticket_channel.Id}'";
        await using NpgsqlCommand cmd3 = new(query2, con2);
        await using NpgsqlDataReader reader2 = await cmd3.ExecuteReaderAsync();
        List<List<long>> ticket_usersList = new();

        while (reader2.Read())
        {
            long[] ticketUsersArray = (long[])reader2.GetValue(0);
            List<long> ticket_users = new(ticketUsersArray);
            ticket_usersList.Add(ticket_users);
        }

        await reader2.CloseAsync();


        var del_ticketbutton = new DiscordButtonComponent(ButtonStyle.Danger, "ticket_delete", "Ticket löschen ❌");
        var teb = new DiscordEmbedBuilder
        {
            Title = "Ticket geschlossen",
            Description =
                $"Das Ticket wurde erfolgreich geschlossen!\n Geschlossen von {ctx.User.UsernameWithDiscriminator} ``{ctx.User.Id}``",
            Color = DiscordColor.Green
        };
        var tname = ticket_channel.Name;
        await ticket_channel.ModifyAsync(x => x.Name = $"closed-{ticket_channel.Name}");
        var mb = new DiscordMessageBuilder();
        mb.WithContent(ctx.User.Mention);
        mb.WithEmbed(teb.Build());
        mb.AddComponents(del_ticketbutton);
        await ctx.Channel.SendMessageAsync(mb);

        foreach (var users in ticket_usersList)
        {
            foreach (var user in users)
            {
                var member = await ctx.Guild.GetMemberAsync((ulong)user);
                await TicketManagerHelper.RemoveUserFromTicket(ctx, ticket_channel, member);
                await TicketManagerHelper.SendTranscriptsToUser(member, transcriptURL, RemoveType.Closed, tname);
            }
        }
    }


    public static async Task CloseTicket(DiscordChannel ticket_channel, DiscordClient client)
    {
        // fetch first message of this channel
        await NotificationManager.ClearMode(ticket_channel.Id);
        var channelmessages = await ticket_channel.GetMessagesAsync();
        var message = channelmessages.LastOrDefault();
        var umb = new DiscordMessageBuilder();
        umb.WithContent(message.Content).WithEmbed(message.Embeds[0]);
        var components = TicketComponents.GetClosedTicketActionRow();
        List<DiscordActionRowComponent> row = new()
        {
            new DiscordActionRowComponent(components)
        };
        umb.AddComponents(row);
        await message.ModifyAsync(umb);
        var ceb = new DiscordEmbedBuilder
        {
            Description = "Ticket wird geschlossen..",
            Color = DiscordColor.Yellow
        };
        await ticket_channel.SendMessageAsync(ceb);

        var eb1 = new DiscordEmbedBuilder
        {
            Description = "Transcript wird gespeichert....",
            Color = DiscordColor.Yellow
        };
        var msg = await ticket_channel.SendMessageAsync(eb1.Build());

        eb1 = new DiscordEmbedBuilder
        {
            Description = "Transcript wurde gespeichert",
            Color = DiscordColor.Green
        };

        string transcriptURL = await TicketManagerHelper.GenerateTranscript(ticket_channel);
        await TicketManagerHelper.InsertTransscriptIntoDB(ticket_channel, TranscriptType.User, transcriptURL);

        await msg.ModifyAsync(eb1.Build());

        var con = TicketDatabaseService.GetConnection();
        string query = $"SELECT ticket_id FROM ticketcache where tchannel_id = '{(long)ticket_channel.Id}'";
        await using NpgsqlCommand cmd = new(query, con);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
        string ticket_id = "";
        while (reader.Read())
        {
            ticket_id = reader.GetString(0);
        }

        await reader.CloseAsync();

        await using NpgsqlCommand cmd2 = new($"UPDATE ticketstore SET closed = True WHERE ticket_id = '{ticket_id}'",
            con);
        await cmd2.ExecuteNonQueryAsync();

        var con2 = TicketDatabaseService.GetConnection();
        string query2 = $"SELECT ticket_users FROM ticketcache where tchannel_id = '{(long)ticket_channel.Id}'";
        await using NpgsqlCommand cmd3 = new(query2, con2);
        await using NpgsqlDataReader reader2 = await cmd3.ExecuteReaderAsync();
        List<List<long>> ticket_usersList = new();

        while (reader2.Read())
        {
            long[] ticketUsersArray = (long[])reader2.GetValue(0);
            List<long> ticket_users = new(ticketUsersArray);
            ticket_usersList.Add(ticket_users);
        }

        await reader2.CloseAsync();


        var del_ticketbutton = new DiscordButtonComponent(ButtonStyle.Danger, "ticket_delete", "Ticket löschen ❌");
        var botu = await client.GetUserAsync(ulong.Parse(BotConfig.GetConfig()["MainConfig"]["BotUserID"]));
        var teb = new DiscordEmbedBuilder
        {
            Title = "Ticket geschlossen",
            Description =
                $"Das Ticket wurde erfolgreich geschlossen!\n Geschlossen von {botu.UsernameWithDiscriminator} " +
                $"``{BotConfig.GetConfig()["MainConfig"]["BotUserID"]}`` " +
                $"\nLetzter Ticketuser nicht mehr auf dem Server",
            Color = DiscordColor.Green
        };
        var tname = ticket_channel.Name;
        await ticket_channel.ModifyAsync(x => x.Name = $"closed-{ticket_channel.Name}");
        var mb = new DiscordMessageBuilder();
        mb.WithContent($"<@{BotConfig.GetConfig()["MainConfig"]["BotUserID"]}>");
        mb.WithEmbed(teb.Build());
        mb.AddComponents(del_ticketbutton);
        await ticket_channel.SendMessageAsync(mb);

        foreach (var users in ticket_usersList)
        {
            foreach (var user in users)
            {
                var member = await client.GetUserAsync((ulong)user);
                await TicketManagerHelper.RemoveUserFromTicket(ticket_channel, member, client);
                //await TicketManagerHelper.SendTranscriptsToUser(member, transcriptURL, RemoveType.Closed, tname);
            }
        }
    }


    public static async Task CloseTicket(ComponentInteractionCreateEventArgs interaction, DiscordChannel ticket_channel)
    {
        var teamler =
            TeamChecker.IsSupporter(await interaction.Interaction.User.ConvertToMember(interaction.Interaction.Guild));
        if (!teamler)
        {
            await interaction.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Du bist kein Teammitglied!").AsEphemeral());
            return;
        }

        await interaction.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        var message = await ticket_channel.GetMessageAsync(interaction.Message.Id);
        var umb = new DiscordMessageBuilder();
        umb.WithContent(message.Content);
        umb.WithEmbed(message.Embeds[0]);
        List<DiscordButtonComponent> components = TicketComponents.GetClosedTicketActionRow();
        List<DiscordActionRowComponent> row = new()
        {
            new DiscordActionRowComponent(components)
        };
        umb.AddComponents(row);
        await message.ModifyAsync(umb);
        DiscordEmbedBuilder ceb = new()
        {
            Description = "Ticket wird geschlossen..",
            Color = DiscordColor.Yellow
        };
        await ticket_channel.SendMessageAsync(ceb);

        DiscordEmbedBuilder eb1 = new()
        {
            Description = "Transcript wird gespeichert....",
            Color = DiscordColor.Yellow
        };
        DiscordMessage msg = await interaction.Channel.SendMessageAsync(eb1.Build());

        eb1 = new DiscordEmbedBuilder
        {
            Description = "Transcript wurde gespeichert",
            Color = DiscordColor.Green
        };

        string transcriptURL = await TicketManagerHelper.GenerateTranscript(ticket_channel);
        await TicketManagerHelper.InsertTransscriptIntoDB(ticket_channel, TranscriptType.User, transcriptURL);

        await msg.ModifyAsync(eb1.Build());

        NpgsqlConnection con = TicketDatabaseService.GetConnection();
        string query = $"SELECT ticket_id FROM ticketcache where tchannel_id = '{(long)ticket_channel.Id}'";
        await using NpgsqlCommand cmd = new(query, con);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
        string ticket_id = "";
        while (reader.Read())
        {
            ticket_id = reader.GetString(0);
        }

        await reader.CloseAsync();

        await using NpgsqlCommand cmd2 = new($"UPDATE ticketstore SET closed = True WHERE ticket_id = '{ticket_id}'",
            con);
        await cmd2.ExecuteNonQueryAsync();

        NpgsqlConnection con2 = TicketDatabaseService.GetConnection();
        string query2 = $"SELECT ticket_users FROM ticketcache where tchannel_id = '{(long)ticket_channel.Id}'";
        await using NpgsqlCommand cmd3 = new(query2, con2);
        await using NpgsqlDataReader reader2 = await cmd3.ExecuteReaderAsync();
        List<List<long>> ticket_usersList = new();

        while (reader2.Read())
        {
            long[] ticketUsersArray = (long[])reader2.GetValue(0);
            List<long> ticket_users = new(ticketUsersArray);
            ticket_usersList.Add(ticket_users);
        }

        await reader2.CloseAsync();

        var button = new DiscordLinkButtonComponent(
            "https://discord.com/channels/" + interaction.Guild.Id + "/" + interaction.Channel.Id, "Zum Ticket");

        DiscordButtonComponent del_ticketbutton =
            new(ButtonStyle.Danger, "ticket_delete", "Ticket löschen ❌");
        DiscordEmbed teb = new DiscordEmbedBuilder
        {
            Title = "Ticket geschlossen",
            Description =
                $"Das Ticket wurde erfolgreich geschlossen!\n Geschlossen von {interaction.User.UsernameWithDiscriminator} ``{interaction.User.Id}``",
            Color = DiscordColor.Green
        }.Build();
        var tname = ticket_channel.Name;
        await ticket_channel.ModifyAsync(x => x.Name = $"closed-{ticket_channel.Name}");
        DiscordMessageBuilder mb = new();
        mb.WithContent(interaction.User.Mention);
        mb.WithEmbed(teb);
        mb.AddComponents(del_ticketbutton);
        await interaction.Channel.SendMessageAsync(mb);

        foreach (var users in ticket_usersList)
        {
            foreach (var user in users)
            {
                var member = await interaction.Guild.GetMemberAsync((ulong)user);
                await TicketManagerHelper.RemoveUserFromTicket(interaction.Interaction, ticket_channel, member);
                await TicketManagerHelper.SendTranscriptsToUser(member, transcriptURL, RemoveType.Closed, tname);
            }
        }
    }
}