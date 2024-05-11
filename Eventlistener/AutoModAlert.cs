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
            embed.WithTitle("AutoMod Alert");
            embed.WithDescription($"AutoMod Alert: {args.Message.Content}");
            embed.WithColor(DiscordColor.Red);
            var channel = await client.GetChannelAsync(ulong.Parse(AutoModAlertChannelId));
            await channel.SendMessageAsync(embed: embed);
        });
        return Task.CompletedTask;
    }
}