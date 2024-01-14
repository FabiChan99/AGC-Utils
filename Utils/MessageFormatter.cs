namespace AGC_Management.Utils;

public class MessageFormatter
{
    public static async Task<string> FormatLevelUpMessage(string message, bool isWithReward, DiscordUser user,
        DiscordRole role = null)
    {
        var formattedMessage = message.Replace("{usermention}", user.Mention);
        formattedMessage = formattedMessage.Replace("{username}", user.Username);
        if (isWithReward)
        {
            if (role != null)
            {
                formattedMessage = formattedMessage.Replace("{rolemention}", role.Mention);
                formattedMessage = formattedMessage.Replace("{rolename}", role.Name);
            }
        }

        return formattedMessage;
    }
}