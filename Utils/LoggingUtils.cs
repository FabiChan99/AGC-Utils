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
}