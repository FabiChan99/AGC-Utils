namespace AGC_Management.Entities;

public class UserInfoApiResponse
{
    public List<BSWarnDTO> warns { get; set; }
}

public class BSWarnDTO
{
    public string? warnId { get; set; }
    public ulong authorId { get; set; }
    public string? reason { get; set; }
    public long timestamp { get; set; }
}
