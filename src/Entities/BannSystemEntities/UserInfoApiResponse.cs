namespace AGC_Management.Entities;

public class UserInfoApiResponse
{
    public List<BannSystemWarn> warns { get; set; }
    public List<BannSystemReport> reports { get; set; }
}