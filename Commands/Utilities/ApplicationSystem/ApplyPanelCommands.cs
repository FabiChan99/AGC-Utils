

using System.Text;

namespace AGC_Management.ApplicationSystem;

public sealed class ApplyPanelCommands : BaseCommandModule
{
    [RequirePermissions(Permissions.Administrator)]
    [Command("sendapplypanel")]
    [Description("Sends the apply panel to the channel.")]
    public async Task SendPanel(CommandContext ctx)
    {
        var categories = await GetBewerbungsCategories();
        var selectorlist = new List<DiscordStringSelectComponentOption>();
        if (categories.Count == 0)
        {
            await ctx.RespondAsync("Es sind keine Bewerbungspositionen vorhanden!");
            return;
        }
        foreach (var category in categories)
        {
            selectorlist.Add(new DiscordStringSelectComponentOption(category.PositionName, RemoveWhitespace(category.PositionId)));
        }
        
        var selector = new DiscordStringSelectComponent("select_apply_category", "Wähle die gewünschte Bewerbungsposition aus", selectorlist);
        string paneltext = "applyrequirements.txt is missing!";
        string paneltext2 = "applyrequirements2.txt is missing!";
        if (File.Exists("applyrequirements.txt"))
        {
            paneltext = await File.ReadAllTextAsync("applyrequirements.txt");
        }
        if (File.Exists("applyrequirements2.txt"))
        {
            paneltext2 = await File.ReadAllTextAsync("applyrequirements2.txt");
        }
        
        StringBuilder embstr = new StringBuilder();
        embstr.Append(paneltext);
        embstr.Append("\n\n");
        embstr.Append(paneltext2);
        
        DiscordEmbedBuilder emb = new DiscordEmbedBuilder()
            .WithTitle("Bewerbung")
            .WithDescription(embstr.ToString())
            .WithColor(DiscordColor.Gold)
            .WithFooter("AGC Bewerbungssystem", ctx.Guild.IconUrl);

        DiscordMessageBuilder msgb = new DiscordMessageBuilder()
            .WithEmbed(emb)
            .AddComponents(selector);
        
        await ctx.RespondAsync(msgb);
    }


    private class Bewerbung 
    {
        public string PositionName { get; set; }
        public string PositionId { get; set; }
    }
    
    
    private static string RemoveWhitespace(string input)
    {
        return new string(input.ToCharArray()
            .Where(c => !Char.IsWhiteSpace(c))
            .ToArray());
    }
    
    private static async Task<List<Bewerbung>> GetBewerbungsCategories()
    {
        List<Bewerbung> bewerbungen = new();
        var con = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
        await using var command = con.CreateCommand("SELECT positionname, postitionid FROM applicationcategories");
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            bewerbungen.Add(new Bewerbung
            {
                PositionName = reader.GetString(0),
                PositionId = reader.GetString(1)
            });
        }
        
        return bewerbungen;
    }
}

