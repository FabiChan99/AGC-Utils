#region

using DisCatSharp.ApplicationCommands;

#endregion

namespace AGC_Management.Eventlistener.LoggingEvents;

[EventHandler]
public class onMetricsMessageCollect : ApplicationCommandsModule
{
    [Event]
    public async Task MessageCreated(DiscordClient client, MessageCreateEventArgs args)
    {
        if (args.Guild == null)
        {
            return;
        }

        if (args.Guild != CurrentApplication.TargetGuild)
        {
            return;
        }

        if (args.Message.Author.IsBot)
        {
            return;
        }


        var db = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
        using var command = db.CreateCommand();
        command.CommandText =
            "INSERT INTO metrics_messages (userid, messageid, channelid, timestamp) VALUES (@userid, @messageid, @channelid, @timestamp)";
        command.Parameters.AddWithValue("userid", (long)args.Message.Author.Id);
        command.Parameters.AddWithValue("messageid", (long)args.Message.Id);
        command.Parameters.AddWithValue("channelid", (long)args.Message.Channel.Id);
        command.Parameters.AddWithValue("timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        await command.ExecuteNonQueryAsync();
    }
}