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
}