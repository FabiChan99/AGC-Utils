using AGC_Management.Services.DatabaseHandler;
using DisCatSharp;
using Microsoft.Extensions.Logging;
using Npgsql;

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
                await Task.Delay(TimeSpan.FromMinutes(1));
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

        string selectQuery = $"SELECT * FROM warns WHERE datum < '{expireTime}' AND perma = 'False'";

        await using (NpgsqlDataReader warnReader = DatabaseService.ExecuteQuery(selectQuery))
        {
            while (warnReader.Read())
            {
                var warn = new
                {
                    UserId = warnReader.GetInt64(0),
                    PunisherId = warnReader.GetInt64(1),
                    Datum = warnReader.GetInt32(2),
                    Description = warnReader.GetString(3),
                    Perma = warnReader.GetBoolean(4),
                    CaseId = warnReader.GetString(5)
                };
                warnlist.Add(warn);
            }
        }

        foreach (var warn in warnlist)
        {
            Dictionary<string, object> data = new()
            {
                { "userid", (long)warn.UserId },
                { "punisherid", (long)warn.PunisherId },
                { "datum", warn.Datum },
                { "description", "[AUTO] Warn Abgelaufen: " + warn.Description },
                { "caseid", "EXPIRED-" + warn.CaseId }
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