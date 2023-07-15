using DisCatSharp.CommandsNext;
using DisCatSharp.Lavalink;
using DisCatSharp.Lavalink.Entities;
using System.Web;

namespace AGC_Management.Helpers.Lavalink;

public class LavalinkHelper : BaseCommandModule
{
    protected static async Task<(LavalinkExtension, bool)> ConnectToVoice(CommandContext ctx)
    {
        var lava = ctx.Client.GetLavalink();
        if (!lava.ConnectedSessions.Any())
        {
            return (lava, false);
        }

        var node = lava.ConnectedSessions.Values.First();
        await node.ConnectAsync(ctx.Member.VoiceState?.Channel);
        return (lava, true);
    }



    protected string GetTrackInfo(LavalinkTrack track)
    {
        return $"{track.Info.Title} ({track.Info.Length:mm\\:ss})";
    }

    protected string GetTrackUrl(LavalinkTrack track)
    {
        return $"https://www.youtube.com/watch?v={track.Info.Identifier}";
    }



    protected string HandleYoutubeLink(string query)
    {
        if (Uri.TryCreate(query, UriKind.Absolute, out Uri uri))
        {
            var host = uri.Host;
            var path = uri.AbsolutePath;
            var queryParameters = HttpUtility.ParseQueryString(uri.Query);
            bool isYoutubeLink = (host == "www.youtube.com" && path == "/watch" && queryParameters["v"] != null) || (host == "youtu.be");

            if (isYoutubeLink)
            {
                return $"https://www.youtube.com/watch?v={queryParameters["v"]}";
            }
        }

        return $"{query}";
    }
}