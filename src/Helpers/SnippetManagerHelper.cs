#region

using AGC_Management.Services.DatabaseHandler;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using Npgsql;

#endregion

namespace AGC_Ticket_System.Helper;

public class SnippetManagerHelper
{
    public static async Task<string?> GetSnippetAsync(string snippetId)
    {
        if (string.IsNullOrEmpty(snippetId))
        {
            return null;
        }

        var con = TicketDatabaseService.GetConnection();
        await using var cmd = new NpgsqlCommand("SELECT snipped_text FROM snippets WHERE snip_id = @snippetId", con);
        cmd.Parameters.AddWithValue("@snippetId", snippetId);

        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

        while (reader.Read())
        {
            return reader.GetString(0);
        }

        await reader.CloseAsync();

        return null;
    }

    public static async Task SendSnippetAsync(DiscordInteraction e)
    {
        var irb = new DiscordInteractionResponseBuilder();
        irb.WithContent("Sende snippet...");
        await e.CreateResponseAsync(InteractionResponseType.UpdateMessage, irb);
        var snipId = e.Data.Values[0];
        var snippet = await GetSnippetAsync(snipId);
        if (string.IsNullOrEmpty(snippet))
        {
            return;
        }

        var eb = new DiscordEmbedBuilder()
            .WithDescription(snippet)
            .WithColor(DiscordColor.Gold)
            .WithTitle("Hinweis").WithFooter("AGC Support-System", e.Guild.IconUrl);
        var users_in_ticket = await TicketManagerHelper.GetTicketUsers(e.Channel);
        var ping = "";
        foreach (var user in users_in_ticket)
        {
            ping = ping + $" {user.Mention}";
        }

        DiscordMessageBuilder mb = new();
        mb.WithContent(ping).WithEmbed(eb);
        await e.Channel.SendMessageAsync(mb);
        var nrb = new DiscordWebhookBuilder();
        nrb.WithContent("Snippet gesendet!");
        await e.EditOriginalResponseAsync(nrb);
    }


    public static async Task<List<(string snipId, string snippedText)>> GetAllSnippetsAsync()
    {
        var newcon = TicketDatabaseService.GetConnectionString();
        var resultList = new List<(string, string)>();

        await using var con = new NpgsqlConnection(newcon);
        await con.OpenAsync();

        await using var cmd = new NpgsqlCommand("SELECT snip_id, snipped_text FROM snippets", con);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            resultList.Add((reader.GetString(0), reader.GetString(1)));
        }

        await reader.CloseAsync();

        return resultList;
    }
}