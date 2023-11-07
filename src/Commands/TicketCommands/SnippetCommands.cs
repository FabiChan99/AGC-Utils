#region

using System.Text;
using System.Text.RegularExpressions;
using AGC_Management.Services.DatabaseHandler;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using Npgsql;

#endregion

namespace AGC_Management.Commands;

[Group("snippetmanager")]
public class SnippetManagerCommands : BaseCommandModule
{
    [GroupCommand]
    [RequireStaffRole]
    public async Task SnippetManager_Help(CommandContext ctx)
    {
        await SnippetManager(ctx);
    }

    [Command("help")]
    [RequireStaffRole]
    public async Task SnippetManager(CommandContext ctx)
    {
        string prefix = ctx.Prefix;
        DiscordEmbed eb = new DiscordEmbedBuilder().WithTitle("Snippet Manager")
            .WithDescription("Verwaltung der Snippets")
            .WithColor(DiscordColor.Red)
            .WithFooter("AGC Support-System", ctx.Guild.IconUrl)
            .AddField(new DiscordEmbedField("Hinzufügen", $"`{prefix}snippetmanager add <name> <content>`"))
            .AddField(new DiscordEmbedField("Entfernen", $"`{prefix}snippetmanager remove <name>`"))
            .AddField(new DiscordEmbedField("Auflisten", $"`{prefix}snippetmanager list`"))
            .AddField(new DiscordEmbedField("Suchen", $"`{prefix}snippetmanager search <name>`"))
            .AddField(new DiscordEmbedField("Kürzelsuche", $"`{prefix}snippetmanager shortcutsearch <dein_text>`"))
            .AddField(new DiscordEmbedField("Hilfe", $"`{prefix}snippetmanager help`"))
            .Build();
        await ctx.RespondAsync(eb);
    }


    [Command("add")]
    [RequireStaffRole]
    public async Task AddSnippet(CommandContext ctx, string name, [RemainingText] string content)
    {
        await using var con = new NpgsqlConnection(TicketDatabaseService.GetConnectionString());
        await con.OpenAsync();


        await using var checkCmd =
            new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM snippets WHERE snip_id = @name)", con);
        checkCmd.Parameters.AddWithValue("name", name);
        var exists = (bool)await checkCmd.ExecuteScalarAsync();

        if (exists)
        {
            var ebe = new DiscordEmbedBuilder()
                .WithDescription($"Snippet `{name}` existiert bereits!")
                .WithColor(DiscordColor.Red);
            await ctx.RespondAsync(ebe.Build());
            return;
        }

        await using var cmd =
            new NpgsqlCommand("INSERT INTO snippets (snip_id, snipped_text) VALUES (@name, @content)", con);
        cmd.Parameters.AddWithValue("name", name);
        cmd.Parameters.AddWithValue("content", content);
        await cmd.ExecuteNonQueryAsync();
        var eb = new DiscordEmbedBuilder()
            .WithDescription($"Snippet `{name}` wurde hinzugfügt!")
            .WithFooter("AGC Support-System", ctx.Guild.IconUrl)
            .WithColor(DiscordColor.Green);
        await ctx.RespondAsync(eb);
    }


    [Command("remove")]
    [RequireStaffRole]
    public async Task RemoveSnippet(CommandContext ctx, string name)
    {
        await using var con = new NpgsqlConnection(TicketDatabaseService.GetConnectionString());
        await con.OpenAsync();
        await using var cmd = new NpgsqlCommand("DELETE FROM snippets WHERE snip_id = @name", con);
        cmd.Parameters.AddWithValue("name", name);
        int rowsAffected = await cmd.ExecuteNonQueryAsync();
        var eb = new DiscordEmbedBuilder();

        if (rowsAffected > 0)
        {
            eb.WithDescription($"Snippet `{name}` wurde erfolgreich entfernt!")
                .WithColor(DiscordColor.Green);
        }
        else
        {
            eb.WithDescription($"Snippet `{name}` existiert nicht!")
                .WithColor(DiscordColor.Red);
        }

        eb.WithFooter("AGC Support-System", ctx.Guild.IconUrl);
        await ctx.RespondAsync(eb.Build());
    }


    [Command("list")]
    [RequireStaffRole]
    public async Task ListSnippets(CommandContext ctx)
    {
        await using var con = new NpgsqlConnection(TicketDatabaseService.GetConnectionString());
        await con.OpenAsync();

        await using var cmd = new NpgsqlCommand("SELECT snip_id FROM snippets", con);
        await using var reader = await cmd.ExecuteReaderAsync();

        var eb = new DiscordEmbedBuilder()
            .WithTitle("Snippets")
            .WithFooter("AGC Support-System", ctx.Guild.IconUrl)
            .WithColor(DiscordColor.Gold);

        bool snippetFound = false;
        StringBuilder sb = new();

        while (await reader.ReadAsync())
        {
            sb.Append($"`{reader.GetString(0)}` ");
            snippetFound = true;
        }

        if (snippetFound)
        {
            eb.WithDescription(sb.ToString());
        }
        else
        {
            eb.WithDescription("Keine Snippets gefunden!");
            eb.WithColor(DiscordColor.Red);
        }

        await reader.CloseAsync();
        await ctx.RespondAsync(eb.Build());
    }


    [Command("search")]
    [RequireStaffRole]
    public async Task SearchSnippets(CommandContext ctx, string snippet_id)
    {
        await using var con = new NpgsqlConnection(TicketDatabaseService.GetConnectionString());
        await con.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT snipped_text FROM snippets WHERE snip_id = @snippet_id", con);
        cmd.Parameters.AddWithValue("snippet_id", snippet_id);
        await using var reader = await cmd.ExecuteReaderAsync();
        var eb = new DiscordEmbedBuilder()
            .WithTitle("Snippet Suche")
            .WithColor(DiscordColor.Gold)
            .WithFooter("AGC Support-System", ctx.Guild.IconUrl);
        if (await reader.ReadAsync())
        {
            eb.WithDescription(reader.GetString(0));
        }
        else
        {
            eb.WithDescription("Snippet nicht gefunden!");
        }

        await reader.CloseAsync();
        await ctx.RespondAsync(eb.Build());
    }

    [Command("shortcutsearch")]
    [RequireStaffRole]
    public async Task ShortcutSnippets(CommandContext ctx)
    {
        await using var con = new NpgsqlConnection(TicketDatabaseService.GetConnectionString());
        await con.OpenAsync();

        var words = Regex.Split(ctx.Message.Content, @"\W+");

        var eb = new DiscordEmbedBuilder()
            .WithTitle("Snippet Suche")
            .WithColor(DiscordColor.Gold)
            .WithFooter("AGC Support-System", ctx.Guild.IconUrl);

        bool snippetFound = false;

        foreach (var word in words)
        {
            await using var cmd =
                new NpgsqlCommand("SELECT snip_id, snipped_text FROM snippets WHERE snipped_text ~* @word", con);

            cmd.Parameters.AddWithValue("word", $"\\m{word}\\M");
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                eb.AddField(new DiscordEmbedField(reader.GetString(0), reader.GetString(1)));
                snippetFound = true;
            }

            await reader.CloseAsync();
        }

        if (!snippetFound)
        {
            eb.WithDescription("Kein passendes Snippetkürzel gefunden!");
            eb.WithColor(DiscordColor.Red);
        }

        await ctx.RespondAsync(eb.Build());
    }
}