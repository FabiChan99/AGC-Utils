

namespace AGC_Management.ApplicationSystem;

public sealed class SendApplyPanelCommand : BaseCommandModule
{
    [RequirePermissions(Permissions.Administrator)]
    [Command("sendapplypanel")]
    [Description("Sends the apply panel to the channel.")]
    public async Task SendPanel(CommandContext ctx)
    {
        // impl
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

