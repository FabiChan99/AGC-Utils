namespace AGC_Management.Entities.Web;

public class DiscordUserWebClaim
{
    public ulong UserId { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Discriminator { get; set; }
    public string Avatar { get; set; }
    public string WebRole { get; set; }
    
    public string FullQualifiedDiscordName { get; set; }

    /// <summary>
    /// Will be null if the email scope is not provided
    /// </summary>
    public string Email { get; set; } = null;

    /// <summary>
    /// Whether the email on this account has been verified, can be null
    /// </summary>
    public bool? Verified { get; set; } = null;
}