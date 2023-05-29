using AGC_Management.Commands.TempVC;
using DisCatSharp;
using AGC_Management.Services.DatabaseHandler;
using Microsoft.Extensions.Logging;
using DisCatSharp.Entities;

namespace AGC_Management.Tasks;

public class TempVoiceTasks : TempVoice
{
    public async Task StartRemoveEmptyTempVoices(DiscordClient discord)
    {

        if (DatabaseService.IsConnected())
        {
            while (true)
            {
                await RemoveEmptyTempVoices(discord);
                await Task.Delay(TimeSpan.FromMinutes(2));
            }
        }

        discord.Logger.LogWarning(
            "Datenbank nicht verbunden. Deaktiviere automatische überprüfung auf leere TempVoices.");
    }

    private async Task RemoveEmptyTempVoices(DiscordClient discord)
    {
        List<string> Query = new()
        {
            "channelid"
        };
        List<Dictionary<string, object>> all_channels =
            await DatabaseService.SelectDataFromTable("tempvoice", Query, null);
        foreach (var listed_channel in all_channels)
        {
            ulong channelid = (ulong)listed_channel["channelid"];
            DiscordChannel channel = await discord.GetChannelAsync(channelid);
            try
            {
                if (channel.Users.Count == 0)
                {
                    Dictionary<string, (object value, string comparisonOperator)>
                        DeletewhereConditions = new()
                        {
                            { "channelid", ((long)channel.Id, "=") }
                        };
                    await channel.DeleteAsync();
                    await DatabaseService.DeleteDataFromTable("tempvoice", DeletewhereConditions);
                }
            }
            catch (Exception ex)
            {
                Dictionary<string, (object value, string comparisonOperator)>
                    DeletewhereConditions = new()
                    {
                        { "channelid", ((long)channel.Id, "=") }
                    };
                //await channel.DeleteAsync();
                await DatabaseService.DeleteDataFromTable("tempvoice", DeletewhereConditions);
            }
        }
    }
}