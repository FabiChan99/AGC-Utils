using DisCatSharp;
using DisCatSharp.Lavalink;
using DisCatSharp.Net;
using Microsoft.Extensions.Logging;

namespace AGC_Management.Services;

public static class LavalinkHandler
{
    private static ConnectionEndpoint LavalinkConfiguration()
    {
        var endpoint = new ConnectionEndpoint
        {
            Hostname = BotConfig.GetConfig()["Lavalink"]["LavalinkAddr"],
            Port = int.Parse(BotConfig.GetConfig()["Lavalink"]["LavalinkPort"]),
        };
        return endpoint;
    }

    private static LavalinkConfiguration lavalinkconfig()
    {
        var lavacfg = new LavalinkConfiguration
        {
            Password = BotConfig.GetConfig()["Lavalink"]["LavalinkPass"],
            RestEndpoint = LavalinkConfiguration(),
            SocketEndpoint = LavalinkConfiguration()
        };
        return lavacfg;
    }

    public static async Task<LavalinkExtension> InitLavalink(DiscordClient discord)
    {
        discord.Logger.LogInformation("Initializing Lavalink...");
        bool enableLavalink = false;
        try
        {
            enableLavalink = bool.Parse(BotConfig.GetConfig()["Lavalink"]["Active"]);
        }
        catch
        {
            discord.Logger.LogError("Lavalink Config could not be loaded. Disabling Lavalink...");
        }

        if (!enableLavalink)
        {
            discord.Logger.LogWarning("Lavalink is disabled. Skipping Lavalink initialization...");
            return null;
        }
        discord.Logger.LogInformation("Lavalink is enabled. Registering Lavalink to Bot...");
        var lavalink = discord.UseLavalink();
        return await ConnectToLavalinkServer(discord, lavalink);
    }

    private static async Task<LavalinkExtension> ConnectToLavalinkServer(DiscordClient discord, LavalinkExtension lavalink)
    {
        discord.Logger.LogInformation("Connecting to Lavalink Server...");

        try
        {
            await lavalink.ConnectAsync(LavalinkHandler.lavalinkconfig());
        }
        catch
        {
            discord.Logger.LogCritical(
                "Lavalink is enabled but could not connect to Lavalink Server. Disabling Lavalink...");
            return null;
        }

        await Task.Delay(5000);
        var lava = discord.GetLavalink();
        if (!lava.ConnectedNodes.Any())
        {
            discord.Logger.LogCritical("Lavalink is enabled but no Lavalink Node is connected. Disabling Lavalink...");
            return null;
        }

        return lavalink;
    }


}