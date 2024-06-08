
namespace AGC_Management.Eventlistener
{
    [EventHandler]
    public class AutoModAlert : BaseCommandModule
    {
        private static readonly Queue<Task> sendQueue = new();
        private static Timer timer;

        private readonly string AutoModAlertChannelId = BotConfig.GetConfig()["AutoModNotify"]["AlertChannelId"];
        private readonly string AutoModChannelId = BotConfig.GetConfig()["AutoModNotify"]["AutoModChannelId"];
        private readonly bool AutoModAlertActive = bool.Parse(BotConfig.GetConfig()["AutoModNotify"]["AutoModAlertActive"]);

        public AutoModAlert()
        {
            timer = new Timer(SendAlertFromQueue, null, Timeout.Infinite, Timeout.Infinite);
        }

        public static void QueueSendAlert(Task alertTask)
        {
            sendQueue.Enqueue(alertTask);
            if (sendQueue.Count == 1)
            {
                timer.Change(0, Timeout.Infinite);
            }
        }

        private static void SendAlertFromQueue(object state)
        {
            if (sendQueue.Count > 0)
            {
                var task = sendQueue.Dequeue();
                task.Start();

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

            var alertTask = new Task(async () =>
            {
                var embed = new DiscordEmbedBuilder();

                foreach (var field in args.Message.Embeds[0].Fields)
                {
                    if (field.Name is "rule_name" or "decision_id" or "channel_id")
                    {
                        continue;
                    }
                    embed.AddField(field);
                }
                
                if (args.Message.Embeds[0].Fields.Any(x => x.Name == "channel_id"))
                {
                    var channelId = args.Message.Embeds[0].Fields.First(x => x.Name == "channel_id").Value;
                    embed.AddField(new DiscordEmbedField("Channel", $"<#{channelId}>"));
                }

                embed.WithTitle("AutoMod Alert");
                embed.WithFooter($"Author: {args.Author.Username} {args.Author.Id}");
                embed.WithColor(DiscordColor.Red);

                var channel = await client.GetChannelAsync(ulong.Parse(AutoModAlertChannelId));
                await channel.SendMessageAsync(embed: embed);
            });

            QueueSendAlert(alertTask);

            return Task.CompletedTask;
        }
    }
}
