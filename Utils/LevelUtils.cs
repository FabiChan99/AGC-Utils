using AGC_Management.Entities;
using AGC_Management.Services;

namespace AGC_Management.Utils;

public static class LevelUtils
{
    public static int XpForLevel(int lvl)
    {
        if (lvl <= 0)
        {
            return 0;
        }
        int alvl = lvl - 1;
        return (int)(5 / 6.0 * (151 * alvl + 33 * Math.Pow(alvl, 2) + 2 * Math.Pow(alvl, 3)) + 100);
    }
    
    public static Dictionary<int, int> MinAndMaxXpForThisLevel(int lvl)
    {
        int min = 0;
        int max = 0;
        if (lvl <= 0)
        {
            return new Dictionary<int, int> { { min, max } };
        }
        int alvl = lvl - 1;
        min = (int)(5 / 6.0 * (151 * alvl + 33 * Math.Pow(alvl, 2) + 2 * Math.Pow(alvl, 3)) + 100);
        max = (int)(5 / 6.0 * (151 * lvl + 33 * Math.Pow(lvl, 2) + 2 * Math.Pow(lvl, 3)) + 100);
        return new Dictionary<int, int> { { min, max } };
    }
    
    // example [#######-----] percentage: 50%
    // idk if this make sense, will see later
    public static string GenerateLevelProgressBarString(int xp, int currentLevel)
    {
        int xpForCurrentLevel = XpForLevel(currentLevel);
        int xpForNextLevel = XpForLevel(currentLevel + 1);
        int xpForThisLevel = xpForNextLevel - xpForCurrentLevel;
        int xpForThisLevelUntilNow = xp - xpForCurrentLevel;
        int percentage = (int)(xpForThisLevelUntilNow / (float)xpForThisLevel * 100);
        int percentageForProgressBar = (int)(percentage / 10.0);
        string progressBar = "";
        for (int i = 0; i < 10; i++)
        {
            if (i < percentageForProgressBar)
            {
                progressBar += "#";
            }
            else
            {
                progressBar += "-";
            }
        }
        return progressBar;
    }
    
    

    public static int LevelAtXp(int totalXp)
    {
        int level = 0;
        int xpForNextLevel = 100;
        
        while (totalXp >= xpForNextLevel)
        {
            totalXp -= xpForNextLevel;
            level++;
            xpForNextLevel = 5 * (level * level) + (50 * level) + 100;
        }

        return level;
    }

    public static int XpUntilNextLevel(int xp)
    {
        int currentLevel = LevelAtXp(xp);
        int xpForNextLevel = XpForLevel(currentLevel + 1);
        return xpForNextLevel - xp;
    }


    /// <summary>
    /// Retrieves the rank data for a specified user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A dictionary containing the rank data, where the key is the user ID and the value is the RankData object.</returns>
    public static async Task<Dictionary<ulong, RankData>> GetRank(ulong userId)
    {
        var rank = new Dictionary<ulong, RankData>();
        await using var db = new NpgsqlConnection(DatabaseService.GetConnectionString());
        await db.OpenAsync();

        var cmd = new NpgsqlCommand("SELECT current_xp FROM levelingdata WHERE userid = @id", db);
        cmd.Parameters.AddWithValue("@id", (long)userId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (reader.HasRows)
        {
            while (await reader.ReadAsync())
            {
                var xp = reader.GetInt32(0);
                var level = LevelAtXp(xp);
                rank[userId] = new RankData { Level = level, Xp = xp };
            }
        }
        else
        {
            rank[userId] = new RankData { Level = 0, Xp = 0 };
        }

        await reader.CloseAsync();
        await db.CloseAsync();
        return rank;
    }

    
    
    
}