

using System.Text;
using AGC_Management.Enums;
using AGC_Management.Services;
using AGC_Management.Utils;

namespace AGC_Management.ApplicationSystem;

public sealed class ApplyPanelCommands : BaseCommandModule
{
    [RequirePermissions(Permissions.Administrator)]
    [Command("sendapplypanel")]
    [Description("Sends the apply panel to the channel.")]
    public async Task SendPanel(CommandContext ctx)
    {
        var msgb = await BuildMessage(ctx);
        var m = await ctx.RespondAsync(msgb);
        ulong id = m.Id;
        ulong channelId = m.ChannelId;
        await CachingService.SetCacheValue(FileCacheType.ApplicationSystemIdCache, "applymessageid", id.ToString());
        await CachingService.SetCacheValue(FileCacheType.ApplicationSystemIdCache, "applychannelid", channelId.ToString());
    }


    private static async Task<DiscordMessageBuilder> BuildMessage(CommandContext ctx)
    {
        var categories = await GetBewerbungsCategories();
        var selectorlist = new List<DiscordStringSelectComponentOption>();
        

        foreach (var category in categories)
        {
            selectorlist.Add(new DiscordStringSelectComponentOption(category.PositionName, ToolSet.RemoveWhitespace(category.PositionId)));
        }
        
        var selector = new DiscordStringSelectComponent("select_apply_category", "Wähle die gewünschte Bewerbungsposition aus", selectorlist);
        string paneltext = "applyrequirements.txt is missing!";
        
        StringBuilder embstr = new StringBuilder();
        embstr.Append(paneltext);
        
        DiscordEmbedBuilder emb = new DiscordEmbedBuilder()
            .WithTitle("Bewerbung")
            .WithDescription(embstr.ToString())
            .WithColor(DiscordColor.Gold)
            .WithFooter("AGC Bewerbungssystem", ctx.Guild.IconUrl);

        DiscordMessageBuilder msgb = new DiscordMessageBuilder()
            .WithEmbed(emb);
        
        if (categories.Count == 0)
        {
            return new DiscordMessageBuilder()
                .WithContent("Es gibt keine Bewerbungspositionen!");
        }
        
        
        if (selectorlist.Count > 0)
        {
            msgb.AddComponents(selector);
        }
        
        return msgb;
    }


    private class Bewerbung 
    {
        public string PositionName { get; set; }
        public string PositionId { get; set; }
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

