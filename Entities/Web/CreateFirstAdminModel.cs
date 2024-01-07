namespace AGC_Management.Entities.Web;

public class CreateFirstAdminModel
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
    public string CreationToken { get; set; }
}