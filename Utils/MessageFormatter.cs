namespace AGC_Management.Utils;

public class MessageFormatter
{
    public static async Task<string> FormatLevelUpMessage(string message, bool isWithReward, DiscordUser user,
        DiscordRole role = null)
    {
        var level = await LevelUtils.GetUserLevel(user.Id);
        var formattedMessage = message.Replace("{usermention}", user.Mention);
        formattedMessage = formattedMessage.Replace("{username}", user.Username);
        if (isWithReward)
            if (role != null)
            {
                formattedMessage = formattedMessage.Replace("{rolemention}", role.Mention);
                formattedMessage = formattedMessage.Replace("{rolename}", role.Name);
            }

        formattedMessage = formattedMessage.Replace("{level}", level.ToString());

        return formattedMessage;
    }

    public static string BoolToEmoji(bool value)
    {
        return value ? "✅" : "❌";
    }
}