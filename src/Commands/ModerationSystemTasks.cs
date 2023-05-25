using AGC_Management.Services.DatabaseHandler;
using DisCatSharp;
using Microsoft.Extensions.Logging;

namespace AGC_Management.Commands;

public class ModerationSystemTasks
{
    public async Task StartRemovingWarnsPeriodically(DiscordClient discord)
    {
        if (DatabaseService.IsConnected())
        {
            discord.Logger.LogInformation("Starte überprüfung auf abgelaufene Warns..");
            while (true)
            {
                await RemoveWarnsOlderThan7Days(discord);
                await Task.Delay(TimeSpan.FromMinutes(5));
            }
        }

        discord.Logger.LogWarning(
            "Datenbank nicht verbunden. Deaktiviere automatische überprüfung auf abgelaufene warns.");
    }

    private async Task RemoveWarnsOlderThan7Days(DiscordClient discord)
    {
        discord.Logger.LogInformation("Prüfe auf abgelaufene Warns");
        var warnlist = new List<dynamic>();
        int expireTime = (int)DateTimeOffset.UtcNow.AddSeconds(-604800).ToUnixTimeSeconds();

        List<string> WarnQuery = new()
        {
            "*"
        };
        Dictionary<string, object> warnWhereConditions = new()
        {
            { "perma", false }
        };
        List<Dictionary<string, object>> WarnResults =
            await DatabaseService.SelectDataFromTable("warns", WarnQuery, warnWhereConditions);
        foreach (var result in WarnResults) warnlist.Add(result);
        foreach (var warn in warnlist)
        {
            Dictionary<string, object> data = new()
            {
                { "userid", (long)warn["userid"] },
                { "punisherid", (long)warn["punisherid"] },
                { "datum", warn["datum"] },
                { "description", "[AUTO] Warn Abgelaufen: " + warn["description"] },
                { "caseid", "EXPIRED-" + warn["caseid"] }
            };
            await DatabaseService.InsertDataIntoTable("flags", data);
        }

        Dictionary<string, (object value, string comparisonOperator)> whereConditions = new()
        {
            { "datum", (expireTime, "<") },
            { "perma", (false, "=") }
        };

        int rowsDeleted = await DatabaseService.DeleteDataFromTable("warns", whereConditions);

        discord.Logger.LogInformation($"{rowsDeleted} Abgelaufene Verwarnungen in Flags verschoben.");
    }
}