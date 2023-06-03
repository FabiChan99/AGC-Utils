using DisCatSharp;
using DisCatSharp.Net;
using DisCatSharp.Lavalink;
using Sentry;

namespace AGC_Management.Services;

public class LavalinkHandler
{
    private static ConnectionEndpoint LavalinkConfiguration()
    {
        var endpoint = new ConnectionEndpoint
        {
            Hostname = "127.0.0.1",
            Port = 2333
        };
        return endpoint;
    }

    public static LavalinkConfiguration lavalinkconfig()
    {
        var lavacfg = new LavalinkConfiguration
        {
            Password = "test",
            RestEndpoint = LavalinkConfiguration(),
            SocketEndpoint = LavalinkConfiguration()
        };
        return lavacfg;
    }

}