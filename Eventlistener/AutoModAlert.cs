namespace AGC_Management.Eventlistener
{
    [EventHandler]
    public class AutoModAlert : BaseCommandModule
    {
        private static readonly Queue<Func<Task>> sendQueue = new();
        private static Timer timer;

        String AutoModAlertChannelId = BotConfig.GetConfig()["AutoModNotify"]["AlertChannelId"];
        String AutoModChannelId = BotConfig.GetConfig()["AutoModNotify"]["AutoModChannelId"];
        Boolean AutoModAlertActive = bool.Parse(BotConfig.GetConfig()["AutoModNotify"]["AutoModAlertActive"]);

        public AutoModAlert()
        {
            timer = new Timer(SendAlertFromQueue, null, Timeout.Infinite, Timeout.Infinite);
        }

        private static void SendAlertFromQueue(object state)
        {
            if (sendQueue.Count > 0)
            {
                var taskFunc = sendQueue.Dequeue();
                var task = taskFunc();
                
                task.ContinueWith(_ =>
                {
                    if (sendQueue.Count > 0)
                    {
                        timer.Change(5000, Timeout.Infinite);
                    }
                    else
                    {
                        timer.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                });
            }
        }

        [Event]
        private Task MessageCreated(DiscordClient client, MessageCreateEventArgs args)
        {
            if (args.Channel.Id.ToString() != AutoModChannelId || !AutoModAlertActive)
            {
                return Task.CompletedTask;
            }

            sendQueue.Enqueue(async () =>
            {
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

            if (sendQueue.Count == 1)
            {
                timer.Change(0, Timeout.Infinite);
            }

            return Task.CompletedTask;
        }
    }
}
