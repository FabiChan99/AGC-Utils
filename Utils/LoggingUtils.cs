namespace AGC_Management.Utils;

public static class LoggingUtils
{
    public static async Task LogXpTransfer(ulong sourceuserid, ulong destinationuserid, int xpamount, ulong executorid)
    {
        var unixnow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var connection = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO xptransferlogs (sourceuserid, destinationuserid, executorid, amount, timestamp) VALUES (@sourceuserid, @destinationuserid, @exec, @amount, @timestamp)";
        command.Parameters.AddWithValue("sourceuserid", (long)sourceuserid);
        command.Parameters.AddWithValue("destinationuserid", (long)destinationuserid);
        command.Parameters.AddWithValue("exec", (long)executorid);
        command.Parameters.AddWithValue("amount", xpamount);
        command.Parameters.AddWithValue("timestamp", unixnow);
        await command.ExecuteNonQueryAsync();
    }

    public static async Task LogGuildBan(ulong userid, ulong moderatorid, string reason = "Kein Grund angegeben")
    {
        var unixnow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var connection = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO banlogs (userid, executorid, reason, timestamp) VALUES (@userid, @moderatorid, @reason, @timestamp)";
        command.Parameters.AddWithValue("userid", (long)userid);
        command.Parameters.AddWithValue("moderatorid", (long)moderatorid);
        command.Parameters.AddWithValue("reason", reason);
        command.Parameters.AddWithValue("timestamp", unixnow);
        await command.ExecuteNonQueryAsync();
    }
    
    public static async Task LogWebOAuthDiscordLogin(string useridentifier, string ip, string useragent)
    {
        var unixnow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var connection = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO dashboardlogins (userid, useragent, ip, timestamp) VALUES (@userid, @useragent, @ip, @timestamp)";
        command.Parameters.AddWithValue("userid", useridentifier);
        command.Parameters.AddWithValue("useragent", useragent);
        command.Parameters.AddWithValue("ip", ip);
        command.Parameters.AddWithValue("timestamp", unixnow);
        await command.ExecuteNonQueryAsync();
    }
    
}