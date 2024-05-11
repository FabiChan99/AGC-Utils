namespace AGC_Management.Eventlistener;


[EventHandler]
public class AutoModAlert : BaseCommandModule
{
    
    String AutoModAlertChannelId = BotConfig.GetConfig()["AutoModNotify"]["AlertChannelId"];
    String AutoModChannelId = BotConfig.GetConfig()["AutoModNotify"]["AutoModChannelId"];
    Boolean AutoModAlertActive = bool.Parse(BotConfig.GetConfig()["AutoModNotify"]["AutoModAlertActive"]);
    
    [Event]
    private Task MessageCreated(DiscordClient client, MessageCreateEventArgs args)
    {
        _ = Task.Run(async () =>
        {
            if (args.Channel.Id.ToString() != AutoModChannelId)
            {
                return;
            }
            if (!AutoModAlertActive)
            {
                return;
            }
            var embed = new DiscordEmbedBuilder();
            embed.AddField(new DiscordEmbedField("channel", args.Channel.Mention));

            foreach (var field in args.Message.Embeds[0].Fields)
            {
                if (field.Name is "rule_name" or "decision_id" or "channel_id")
                {
                    continue;
                }
                embed.AddField(field);
            }
            
            
            
            embed.WithTitle("AutoMod Alert");
            embed.WithFooter("Author: " + args.Author.Username + $" {args.Author.Id}");
            embed.WithColor(DiscordColor.Red);
            var channel = await client.GetChannelAsync(ulong.Parse(AutoModAlertChannelId));
            await channel.SendMessageAsync(embed: embed);
        });
        return Task.CompletedTask;
    }
}