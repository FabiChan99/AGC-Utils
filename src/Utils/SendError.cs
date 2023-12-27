#region

using DisCatSharp;
using DisCatSharp.Entities;
using Microsoft.Extensions.Logging;

#endregion

namespace AGC_Management.Utils;

public static class ErrorReporting
{
    public static async Task SendErrorToDev(DiscordClient client, DiscordUser user,
        Exception exception)
    {
        var botOwner = await client.GetUserAsync(GlobalProperties.BotOwnerId);
        var embed2 = new DiscordEmbedBuilder();
        embed2.WithTitle("Fehler aufgetreten!");
        embed2.WithDescription($"Es ist ein Fehler aufgetreten. \n\n" +
                               $"__Fehlermeldung:__\n" +
                               $"```{exception.Message}```\n" +
                               $"__Stacktrace:__\n" +
                               $"```{exception.StackTrace}```\n" +
                               $"__User:__\n" +
                               $"``{user.UsernameWithDiscriminator}`` - ``{user.Id}``\n");
        embed2.WithColor(DiscordColor.Red);
        try
        {
            //await botOwner.SendMessageAsync(embed2);
        }
        catch (Exception)
        {
            // ignored
        }

        try
        {
            ulong errortrackingguildid = GlobalProperties.AGCGuild.Id;
            var errortrackingguild = await client.GetGuildAsync(errortrackingguildid);
            var errortrackingchannel = errortrackingguild.GetChannel(GlobalProperties.ErrorTrackingChannelId);
            await errortrackingchannel.SendMessageAsync(embed2);
        }
        catch (Exception)
        {
        }

        CurrentApplicationData.Client.Logger.LogError($"Exception occured: {exception.GetType()}: {exception.Message}");
        CurrentApplicationData.Client.Logger.LogError($"Stacktrace: {exception.StackTrace}");
    }
}