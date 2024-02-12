namespace AGC_Management.Utils;

public static class PollUtils
{
    
    private static async Task<string> GetPollIdByMessageId(ulong messageId)
    {
        var con = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
        await using var command = con.CreateCommand();
        command.CommandText = "SELECT id FROM polls WHERE messageid = @messageid";
        command.Parameters.AddWithValue("messageid", messageId);
        var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return reader.GetString(0);
        }
        return "NOT_FOUND";
    }
    
    public static int MapLetterToPollOptionIndex(string letter)
    {
        return letter.ToUpper() switch
        {
            "A" => 1,
            "B" => 2,
            "C" => 3,
            "D" => 4,
            "E" => 5,
            "F" => 6,
            "G" => 7,
            "H" => 8,
            "I" => 9,
            "J" => 10,
            "K" => 11,
            "L" => 12,
            "M" => 13,
            "N" => 14,
            "O" => 15,
            "P" => 16,
            "Q" => 17,
            "R" => 18,
            _ => -1
        };
    }
    
    public static string MapPollOptionIndexToLetter(int index)
    {
        var ib = index switch
        {
            1 => "A",
            2 => "B",
            3 => "C",
            4 => "D",
            5 => "E",
            6 => "F",
            7 => "G",
            8 => "H",
            9 => "I",
            10 => "J",
            11 => "K",
            12 => "L",
            13 => "M",
            14 => "N",
            15 => "O",
            16 => "P",
            17 => "Q",
            18 => "R",
            _ => "Z"
        };

        return ib.ToLower();
    }
    
    
    public static async Task AddVote(ulong messageId, ulong VoterId, int optionIndex)
    {
        var pollId = await GetPollIdByMessageId(messageId);
        if (pollId == "NOT_FOUND")
        {
            return;
        }
        var con = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
        await using var command = con.CreateCommand();
        command.CommandText = "INSERT INTO pollvotes (pollid, optionindex, userid) VALUES (@pollid, @optionindex, @userid)";
        command.Parameters.AddWithValue("pollid", pollId);
        command.Parameters.AddWithValue("optionindex", optionIndex);
        command.Parameters.AddWithValue("userid", (long)VoterId);
        await command.ExecuteNonQueryAsync();
    }
    
    public static async Task RemoveVote(ulong messageId, ulong VoterId, int optionIndex)
    {
        var pollId = await GetPollIdByMessageId(messageId);
        if (pollId == "NOT_FOUND")
        {
            return;
        }
        var con = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
        await using var command = con.CreateCommand();
        command.CommandText = "DELETE FROM pollvotes WHERE pollid = @pollid , optionindex = @optionindex, userid = @userid";
        command.Parameters.AddWithValue("pollid", pollId);
        command.Parameters.AddWithValue("optionindex", optionIndex);
        command.Parameters.AddWithValue("userid", (long)VoterId);
        await command.ExecuteNonQueryAsync();
    }
}