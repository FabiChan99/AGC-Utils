namespace AGC_Management.Entities;

public class BannSystemWarn
{
    public string? warnId { get; set; }
    public ulong authorId { get; set; }
    public string? reason { get; set; }
    public long timestamp { get; set; }
}