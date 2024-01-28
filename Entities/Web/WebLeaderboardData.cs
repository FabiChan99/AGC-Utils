namespace AGC_Management.Entities.Web;

public class WebLeaderboardData
{
    public string Avatar { get; set; }
    public ulong UserId { get; set; }
    public string Username { get; set; }
    public int Level { get; set; }
    public string Experience { get; set; }
    public int Rank { get; set; }
    public int ProgressInPercent { get; set; }
}