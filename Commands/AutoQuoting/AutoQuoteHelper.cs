#region

using System.Text.RegularExpressions;

#endregion

namespace AGC_Management.Commands.AutoQuoting;

public class AutoQuoteHelper
{
    public static async Task<List<DiscordMessage>> GetMessagesWithMessageLinks(DiscordGuild guild, string content)
    {
        List<DiscordMessage> list = new();
        var guildId = guild.Id.ToString();
        var pattern =
            @"(?:https?://)?(?:\w+\.)?discord(?:app)?\.com/channels/(?<guild>\d+)/(?<channel>\d+)/(?<message>\d+)(?:\?\S*)?(?:#\S*)?";
        Regex MSG_URL_PATTERN = new(pattern, RegexOptions.IgnoreCase);
        var m = MSG_URL_PATTERN.Match(content);

        while (m.Success)
        {
            var groupstring = m.Groups["guild"].Value;
            if (groupstring != null && groupstring.Equals(guildId))
            {
                var channelId = ulong.Parse(m.Groups["channel"].Value);
                var channel = guild.GetChannel(channelId);
                if (channel != null)
                    try
                    {
                        var messageId = ulong.Parse(m.Groups["message"].Value);
                        var message = await channel.GetMessageAsync(messageId);
                        list.Add(message);
                    }
                    catch
                    {
                        // Ignore
                    }
            }

            m = m.NextMatch();
        }

        return list;
    }

    public static async Task ProcessMessageWithLinks(DiscordClient client, MessageCreateEventArgs args)
    {
        var messages = await GetMessagesWithMessageLinks(args.Guild, args.Message.Content);

        if (messages.Count > 0)
            try
            {
                for (var i = 0; i < Math.Min(3, messages.Count); i++)
                {
                    var message = messages[i];
                    try
                    {
                        var m = await PostQuote(args.Channel, message);
                        await args.Message.RespondAsync(m);
                    }
                    catch (Exception e)
                    {
                        client.Logger.LogError("Exception in Auto Quote: " + e);
                    }
                }
            }
            catch (Exception e)
            {
                client.Logger.LogError("Exception in Auto Quote: " + e);
            }
    }

    public static async Task<DiscordMessageBuilder> PostQuote(DiscordChannel channel, DiscordMessage quotedMessage)
    {
        DiscordEmbedBuilder eb;
        var ftitle = "AGC-Utils AutoQuote";
        if (quotedMessage.Embeds.Count == 0)
        {
            eb = new DiscordEmbedBuilder()
                .WithFooter(ftitle)
                .WithTimestamp(DateTime.Now);
            if (quotedMessage.Content.Length > 0) eb.WithDescription("\"" + quotedMessage.Content + "\"");

            if (quotedMessage.Attachments.Count > 0) eb.WithImageUrl(quotedMessage.Attachments[0].Url);
        }
        else
        {
            var embed = quotedMessage.Embeds[0];
            eb = new DiscordEmbedBuilder(embed);

            if (!string.IsNullOrEmpty(embed.Image?.Url?.ToString()))
                eb.WithImageUrl(embed.Image.Url.ToString());
            else if (quotedMessage.Attachments.Count > 0) eb.WithImageUrl(quotedMessage.Attachments[0].Url);

            if (!string.IsNullOrEmpty(embed.Footer?.Text))
                eb.WithFooter(embed.Footer.Text + " - " + ftitle);
            else
                eb.WithFooter(ftitle);
        }

        eb.WithAuthor($"Gesendet von {quotedMessage.Author.UsernameWithDiscriminator}",
            iconUrl: quotedMessage.Author.AvatarUrl);
        eb.WithColor(DiscordColor.White);

        return new DiscordMessageBuilder().WithEmbed(eb.Build());
    }
}