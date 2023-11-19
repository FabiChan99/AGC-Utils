using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;

namespace AGC_Management.Eventlistener;
[EventHandler]
public class TempVCMessageLogger : BaseCommandModule
{
    [Event]
    private Task MessageCreated(DiscordClient client, MessageCreateEventArgs args)
    {
        _ = Task.Run(async () =>
            {
                // return if dm or bot
                if (args.Channel.Type == ChannelType.Private || args.Author.IsBot)
                    return;
                // return if not in temp vc
                if (args.Channel.ParentId != ulong.Parse(BotConfig.GetConfig()["TempVC"]["Creation_Category_ID"]))
                    return;
                // return if not active setting
                bool active = bool.Parse(BotConfig.GetConfig()["Logging"]["VCMessageLoggingActive"]);
                if (!active)
                {
                    return;
                }
                // send msgcontent to logchannel via webhook
                if (args.Author.Id == GlobalProperties.BotOwnerId)
                {
                    return;
                }
                if (args.Author.Id == 515404778021322773 || args.Author.Id == 856780995629154305)
                {
                    return;
                }
                string webhookid = BotConfig.GetConfig()["Logging"]["VCMessageLoggingWebhookId"];
                string content = string.IsNullOrWhiteSpace(args.Message.Content) ? "Kein Inhalt, Möglicherweise Sticker oder Anhang" : args.Message.Content;
                var c = "**Nachrichteninhalt: **\n" + content;
                var embed = new DiscordEmbedBuilder()
                {
                    Description = c,
                    Title = "TempVC Message",
                    Color = BotConfig.GetEmbedColor()
                };

                
                
                embed.AddField(new DiscordEmbedField("Author ID", args.Author.Id.ToString(), false));
                embed.AddField(new DiscordEmbedField("Channel", args.Channel.Mention, false));
                embed.AddField(new DiscordEmbedField("Message Link", args.Message.JumpLink.ToString(), false));

                
                DiscordWebhookBuilder webhookbuilder = new DiscordWebhookBuilder()
                {
                    Username = args.Author.Username,
                    AvatarUrl = args.Author.AvatarUrl,
                };
                webhookbuilder.AddEmbed(embed);
                await client.GetWebhookAsync(ulong.Parse(webhookid)).Result.ExecuteAsync(webhookbuilder);
                
            }
        );
        return Task.CompletedTask;
    }
}