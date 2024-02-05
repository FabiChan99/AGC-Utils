#region

using AGC_Management.Utils;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;

#endregion

namespace AGC_Management.ApplicationSystem;

[ApplicationCommandRequirePermissions(Permissions.Administrator)]
[SlashCommandGroup("modifyapplicationpositions", "Modify the application positions.", (long)Permissions.Administrator)]
public sealed class ModifyApplicationPositions : ApplicationCommandsModule
{
    [SlashCommand("add", "Add a new application position.")]
    public static async Task AddPosition(InteractionContext ctx,
        [Option("position", "The position to add.")] string positionName)
    {
        var posId = ToolSet.RemoveWhitespace(positionName.ToLower());
        var con = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
        await using var cmd = con.CreateCommand("SELECT positionid FROM applicationcategories");
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (reader.GetString(0) == posId)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Position already exists!"));
                return;
            }
        }

        await using var cmd2 =
            con.CreateCommand(
                "INSERT INTO applicationcategories (positionid, positionname) VALUES (@positionid, @positionname)");
        cmd2.Parameters.AddWithValue("positionid", posId);
        cmd2.Parameters.AddWithValue("positionname", positionName);
        await cmd2.ExecuteNonQueryAsync();
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent("Position added!"));
        ApplyPanelCommands.QueueRefreshPanel();
    }

    [SlashCommand("remove", "Remove an application position.")]
    public static async Task RemovePosition(InteractionContext ctx,
        [Autocomplete(typeof(ApplicationAutocompleteProvider))] [Option("position", "The position to remove.", true)]
        string positionId)
    {
        var con = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
        await using var cmd = con.CreateCommand("DELETE FROM applicationcategories WHERE positionid = @positionid");
        cmd.Parameters.AddWithValue("positionid", positionId);
        var e = await cmd.ExecuteNonQueryAsync();
        if (e == 0)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Position not found!"));
            return;
        }

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent("Position removed!"));
        ApplyPanelCommands.QueueRefreshPanel();
    }


    [SlashCommand("modifyapplystatus", "Modify an application position to be applicable.")]
    public async Task ModifyApplyStatus(InteractionContext ctx,
        [Autocomplete(typeof(ApplicationAutocompleteProvider))] [Option("position", "The position to modify.", true)]
        string positionId, [Option("status", "The status to set.")] bool status)
    {
    }


    public class ApplicationAutocompleteProvider : IAutocompleteProvider
    {
        public async Task<IEnumerable<DiscordApplicationCommandAutocompleteChoice>> Provider(AutocompleteContext ctx)
        {
            var options = new List<DiscordApplicationCommandAutocompleteChoice>();

            var con = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
            await using var cmd = con.CreateCommand("SELECT positionname, positionid FROM applicationcategories");
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                options.Add(new DiscordApplicationCommandAutocompleteChoice(reader.GetString(0), reader.GetString(1)));
            }

            return await Task.FromResult(options.AsEnumerable());
        }
    }
}