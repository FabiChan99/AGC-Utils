#region

using System.Text;
using AGC_Management.Enums;
using AGC_Management.Services;
using AGC_Management.Utils;

#endregion

namespace AGC_Management.ApplicationSystem;

public sealed class ApplyPanelCommands : BaseCommandModule
{
    private static Queue<Task> refreshQueue = new Queue<Task>();
    private static Timer timer;
    public ApplyPanelCommands()
    {
        timer = new Timer(RefreshPanelFromQueue, null, Timeout.Infinite, Timeout.Infinite);
        var watcher = new FileSystemWatcher
        {
            Path = "./textfiles",
            NotifyFilter = NotifyFilters.LastWrite,
            Filter = "applyrequirements.txt"
        };
        watcher.Changed += async (sender, e) =>
        {
            Console.WriteLine("File changed");
            QueueRefreshPanel();
        };
        watcher.Error += (sender, e) =>
        {
            CurrentApplication.DiscordClient.Logger.LogError(e.GetException(), "Filewatcher error");
        }; 
        watcher.EnableRaisingEvents = true;
    }
    
    
    public static void QueueRefreshPanel()
    {
        refreshQueue.Clear();
        refreshQueue.Enqueue(new Task(async () => await RefreshPanel()));
        timer.Change(2000, Timeout.Infinite);
    }
    
    private static void RefreshPanelFromQueue(object state)
    {
        if (refreshQueue.Count > 0)
        {
            var task = refreshQueue.Dequeue();
            task.Start();
            
            if (refreshQueue.Count > 0)
            {
                timer.Change(5000, Timeout.Infinite);
            }
            else
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }
    }
    
    [RequirePermissions(Permissions.Administrator)]
    [Command("sendapplypanel")]
    [Description("Sends the apply panel to the channel.")]
    public async Task SendPanel(CommandContext ctx)
    {
        var msgb = await BuildMessage();
        var m = await ctx.RespondAsync(msgb);
        ulong id = m.Id;
        ulong channelId = m.ChannelId;
        await CachingService.SetCacheValue(CustomDatabaseCacheType.ApplicationSystemCache, "applymessageid", id.ToString());
        await CachingService.SetCacheValue(CustomDatabaseCacheType.ApplicationSystemCache, "applychannelid",
            channelId.ToString());
        await CachingService.SetCacheValue(CustomDatabaseCacheType.ApplicationSystemCache, "ispanelactive", "true");
    }
    
    public static async Task RefreshPanel()
    {
        var m_id = await CachingService.GetCacheValue(CustomDatabaseCacheType.ApplicationSystemCache, "applymessageid");
        var c_id = await CachingService.GetCacheValue(CustomDatabaseCacheType.ApplicationSystemCache, "applychannelid");
        if (string.IsNullOrEmpty(m_id) || m_id == "0" || string.IsNullOrEmpty(c_id) || c_id == "0")
        {
            return;
        }
        var msgb = await BuildMessage();
        var m = await CurrentApplication.DiscordClient.GetChannelAsync(ulong.Parse(c_id)).Result.GetMessageAsync(ulong.Parse(m_id));
        await m.ModifyAsync(msgb);
    }


    private static async Task<DiscordMessageBuilder> BuildMessage()
    {
        var categories = await GetBewerbungsCategories();
        var selectorlist = new List<DiscordStringSelectComponentOption>();


        foreach (var category in categories)
        {
            selectorlist.Add(new DiscordStringSelectComponentOption(category.PositionName,
                ToolSet.RemoveWhitespace(category.PositionId), description:MessageFormatter.BoolToEmoji(category.IsApplicable) + " Diese Position ist " + (category.IsApplicable ? "bewerbbar" : "nicht bewerbbar")));
        }

        var selector = new DiscordStringSelectComponent("select_apply_category",
            "Wähle die gewünschte Bewerbungsposition aus", selectorlist);
        
        var dbdata = await CachingService.GetCacheValueAsBase64(CustomDatabaseCacheType.ApplicationSystemCache, "applypaneltext");
        var embstr = new StringBuilder();
        var paneltext = string.IsNullOrEmpty(dbdata) ? "⚠️ Es wurde noch kein Text für das Bewerbungspanel festgelegt. ⚠️" : dbdata;

        DiscordEmbedBuilder emb = new DiscordEmbedBuilder()
            .WithTitle("Bewerbung")
            .WithDescription(embstr.ToString())
            .WithColor(DiscordColor.Gold)
            .WithFooter("AGC Bewerbungssystem", CurrentApplication.TargetGuild.IconUrl);

        DiscordMessageBuilder msgb = new DiscordMessageBuilder()
            .WithEmbed(emb);

        if (categories.Count == 0)
        {
            emb.WithDescription(paneltext + "\n\nEs sind keine Bewerbungspositionen verfügbar.");
        }


        if (selectorlist.Count > 0)
        {
            msgb.AddComponents(selector);
        }

        return msgb;
    }


    private static async Task<List<Bewerbung>> GetBewerbungsCategories()
    {
        List<Bewerbung> bewerbungen = new();
        var con = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
        await using var command = con.CreateCommand("SELECT positionname, positionid, applicable FROM applicationcategories");
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            bewerbungen.Add(new Bewerbung
            {
                PositionName = reader.GetString(0),
                PositionId = reader.GetString(1),
                IsApplicable = reader.GetBoolean(2)
            });
        }

        return bewerbungen;
    }


    private class Bewerbung
    {
        public string PositionName { get; set; }
        public string PositionId { get; set; }
        public bool IsApplicable { get; set; }
    }
}