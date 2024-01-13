using AGC_Management.Entities;
using AGC_Management.Services;

namespace AGC_Management.Utils;

public static class LevelUtils
{
    /// <summary>
    /// Calculates the experience points required to reach a given level.
    /// </summary>
    /// <param name="lvl">The level to calculate the experience points for.</param>
    /// <returns>The experience points required to reach the given level.</returns>
    public static int XpForLevel(int lvl)
    {
        if (lvl <= 0)
        {
            return 0;
        }
        int alvl = lvl - 1;
        return (int)(5 / 6.0 * (151 * alvl + 33 * Math.Pow(alvl, 2) + 2 * Math.Pow(alvl, 3)) + 100);
    }
    
    public static int XpForNextLevel(int lvl)
    {
        lvl += 1;
        int alvl = lvl;
        return (int)(5 / 6.0 * (151 * alvl + 33 * Math.Pow(alvl, 2) + 2 * Math.Pow(alvl, 3)) + 100);
    }
    
    public static int XpToFinishLevel(int xp)
    {
        int level = LevelAtXp(xp);
        int xpForNextLevel = XpForLevel(level + 1);
        return xpForNextLevel - xp;
    }
    
    
    
    /// <summary>
    /// Calculates the minimum and maximum experience points (XP) required for a given level.
    /// </summary>
    /// <param name="lvl">The level.</param>
    /// <returns>A dictionary containing the minimum and maximum XP values.</returns>
    public static Dictionary<int, int> MinAndMaxXpForThisLevel(int lvl)
    {
        int min = 0;
        int max = 0;
        if (lvl <= 0)
        {
            if (lvl == 0)
            {
                return new Dictionary<int, int> { { 0, 100 } };
            }
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


    /// <summary>
    /// Calculates the level based on the total experience points.
    /// </summary>
    /// <param name="totalXp">The total experience points.</param>
    /// <returns>The level corresponding to the total experience points.</returns>
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

    /// <summary>
    /// Calculates the amount of experience points needed to reach the next level.
    /// </summary>
    /// <param name="xp">The current amount of experience points.</param>
    /// <returns>The amount of experience points needed to reach the next level.</returns>
    public static int XpUntilNextLevel(int xp)
    {
        int currentLevel = LevelAtXp(xp);
        int xpForNextLevel = XpForLevel(currentLevel + 1);
        return xpForNextLevel - xp;
    }

    /// <summary>
    /// Method to retrieve the ranking of a user based on their XP in the leveling data table. </summary>
    /// <param name="userid">The unique identifier of the user.</param>
    /// <returns>The rank of the user. Returns 0 if the user is not found.</returns>
    public static async Task<int> GetUserRankAsync(ulong userid)
    {
        int rank = 0;
        await using var db = new NpgsqlConnection(DatabaseService.GetConnectionString());
        await db.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT userid, current_xp FROM levelingdata ORDER BY current_xp DESC", db);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (reader.HasRows)
        {
            while (await reader.ReadAsync())
            {
                rank++;
                if (reader.GetInt64(0) == (long)userid)
                {
                    break;
                }
            }
        }

        await db.CloseAsync();
        return rank;
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

        var cmd = new NpgsqlCommand("SELECT current_xp, current_level FROM levelingdata WHERE userid = @id", db);
        cmd.Parameters.AddWithValue("@id", (long)userId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (reader.HasRows)
        {
            while (await reader.ReadAsync())
            {
                var xp = reader.GetInt32(0);
                var level = reader.GetInt32(1);
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
    
    
    // leaderboard data for the leaderboard command
    public static async Task<List<LeaderboardData>> FetchLeaderboardData()
    {
        var leaderboardData = new List<LeaderboardData>();
        await using var db = new NpgsqlConnection(DatabaseService.GetConnectionString());
        await db.OpenAsync();
        
        var cmd = new NpgsqlCommand("SELECT userid, current_xp, current_level FROM levelingdata ORDER BY current_xp DESC", db);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (reader.HasRows)
        {
            while (await reader.ReadAsync())
            {
                var userId = reader.GetInt64(0);
                var xp = reader.GetInt32(1);
                var level = reader.GetInt32(2);
                leaderboardData.Add(new LeaderboardData { UserId = (ulong)userId, XP = xp, Level = level });
            }
        }
        else
        {
            leaderboardData.Add(new LeaderboardData { UserId = 0, XP = 0, Level = 0 });
        }
        
        await reader.CloseAsync();
        await db.CloseAsync();
        return leaderboardData;
    }

    public static int GetUserRank(ulong invokingUserId, List<LeaderboardData> leaderboardData)
    {
        for (int i = 0; i < leaderboardData.Count; i++)
        {
            if (leaderboardData[i].UserId == invokingUserId)
            {
                return i + 1;
            }
        }
        
        return -1;
    }
}