using System.Net;
using System.Text.RegularExpressions;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using Newtonsoft.Json.Linq;

namespace AGC_Management.Eventlistener.NSFWScanner;

[EventHandler]
public class NSFWCheck : BaseCommandModule
{
    [Event]
    public async Task MessageCreated(DiscordClient client, MessageCreateEventArgs args)
    {
        _ = Task.Run(async () =>
        {
            if (args.Channel.Type == ChannelType.Private || args.Message.Author.IsBot)
            {
                return;
            }
            if (args.Guild.Id != ulong.Parse(BotConfig.GetConfig()["ServerConfig"]["ServerId"]))
            {
                return;
            }

            bool isActivated = bool.Parse(BotConfig.GetConfig()["LinkLens"]["Active"]);
            if (!isActivated)
            {
                return;
            }

            using var _httpClient = new HttpClient();
            var apikey = BotConfig.GetConfig()["LinkLens"]["API-KEY"];
            _httpClient.DefaultRequestHeaders.Add("api-key", apikey);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.1000.0 Safari/537.36");

            var attachments = args.Message.Attachments;
            var urls = attachments.Select(att => att.Url).ToList();

            // Extract URLs from message if no attachments
            if (!attachments.Any())
            {
                var text = args.Message.Content;
                var matches = Regex.Matches(text, @"(https?://)?(www\.)?([^\s]+)\.([^\s]+)");
                urls.AddRange(matches.Select(match => match.Value));
            }

            foreach (var url in urls)
            {
                var response = await _httpClient.GetAsync($"https://api.linklens.xyz/?url={url}");
                var responseString = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(responseString);
                var isNSFW = bool.Parse(json["data"]["is_nsfw"].ToString());

                if (isNSFW)
                {
                    ulong AlertChannel = ulong.Parse(BotConfig.GetConfig()["LinkLens"]["AlertChannel"]);
                    var c = args.Guild.GetChannel(AlertChannel);
                    var e = GetReportMessage(args.Message, args.Author);
                    await c.SendMessageAsync(e);

                    break;
                }
            }
        });
    }


    private static DiscordMessageBuilder GetReportMessage(DiscordMessage message, DiscordUser user)
    {
        var embed = new DiscordEmbedBuilder()
            .WithAuthor("NSFW Inhalt erkannt!")
            .WithColor(DiscordColor.Red)
            .WithTimestamp(message.CreationTimestamp)
            .WithFooter("Reported at")
            .WithThumbnail(message.Author.AvatarUrl)
            .AddField(new DiscordEmbedField("Author", $"{message.Author.Mention} ``{message.Author.Id}``", false))
            .AddField(new DiscordEmbedField("Channel", message.Channel.Mention, false))
            .AddField(new DiscordEmbedField("Message Link",
                $"https://discord.com/channels/{message.Guild.Id}/{message.Channel.Id}/{message.Id}", false))
            .AddField(new DiscordEmbedField("Message Content", $"```{(string.IsNullOrWhiteSpace(message.Content) ? "none" : message.Content)}```", false));


        var button = new DiscordLinkButtonComponent($"https://discord.com/channels/{message.Guild.Id}/{message.Channel.Id}/{message.Id}", "Zur Nachricht");
        var mb = new DiscordMessageBuilder()
            .WithEmbed(embed)
            .WithReply(message.Id)
            .WithContent($"NSFW Inhalt von {user.Mention} wurde gemeldet!")
            .AddComponents(button);
        return mb;
    }
}