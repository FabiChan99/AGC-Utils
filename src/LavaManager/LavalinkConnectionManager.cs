#region

using DisCatSharp;
using DisCatSharp.Lavalink;
using DisCatSharp.Net;
using Microsoft.Extensions.Logging;

#endregion

namespace AGC_Management.LavaManager;

public class LavalinkConnectionManager
{
    public static LavalinkExtension? LavalinkExtension;
    public static LavalinkSession? LavalinkSession;
    public static bool LavalinkConnected;

    public static LavalinkConfiguration LavaConfig()
    {
        string LHost = BotConfig.GetConfig()["MusicConfig"]["Host"];
        int LPort = int.Parse(BotConfig.GetConfig()["MusicConfig"]["Port"]);
        string LPass = BotConfig.GetConfig()["MusicConfig"]["Password"];
        var endpoint = new ConnectionEndpoint
        {
            Hostname = LHost,
            Port = LPort
        };

        var lavalinkConfig = new LavalinkConfiguration
        {
            Password = LPass,
            RestEndpoint = endpoint,
            SocketEndpoint = endpoint
        };
        return lavalinkConfig;
    }

    public static async Task ConnectAsync(DiscordClient client)
    {
        ConsoleSpinner spinner = new();
        try
        {
            if (!bool.Parse(BotConfig.GetConfig()["MusicConfig"]["Active"]))
            {
                client.Logger.LogInformation("Lavalink ist deaktiviert. Verbindung wird nicht hergestellt.");
                return;
            }
            client.Logger.LogInformation("Verbinde mit Lavalink...");
            spinner.Start();
            LavalinkExtension = client.UseLavalink();
            LavalinkSession = await LavalinkExtension.ConnectAsync(LavaConfig());
            client.Logger.LogInformation("Verbunden mit Lavalink.");
            LavalinkConnected = true;
        }
        catch (Exception ex)
        {
            spinner.Stop();
            client.Logger.LogCritical(ex,
                "Lavalink failed to connect. Please Check your Lavalink Config in config.ini. " +
                "Check if Lavalink v4 is running and the correct host/port/password is set.");
        }
        spinner.Stop();
    }
}