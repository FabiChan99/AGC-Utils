namespace AGC_Management.Eventlistener
{
    [EventHandler]
    public class AutoModAlert : BaseCommandModule
    {
        private static readonly Queue<Task> sendQueue = new();
        private static Timer timer;
        private static readonly object queueLock = new();

        private readonly string AutoModAlertChannelId = BotConfig.GetConfig()["AutoModNotify"]["AlertChannelId"];
        private readonly string AutoModChannelId = BotConfig.GetConfig()["AutoModNotify"]["AutoModChannelId"];
        private readonly bool AutoModAlertActive = bool.Parse(BotConfig.GetConfig()["AutoModNotify"]["AutoModAlertActive"]);

        public AutoModAlert()
        {
            timer = new Timer(SendAlertFromQueue, null, Timeout.Infinite, Timeout.Infinite);
        }

        public static void QueueSendAlert(Task alertTask)
        {
            lock (queueLock)
            {
                sendQueue.Enqueue(alertTask);
                if (sendQueue.Count == 1)
                {
                    timer.Change(0, Timeout.Infinite);
                }
            }
        }

        private static void SendAlertFromQueue(object state)
        {
            Task[] tasksToExecute;

            lock (queueLock)
            {
                tasksToExecute = sendQueue.ToArray();
                sendQueue.Clear();
            }

            if (tasksToExecute.Length > 0)
            {
                var combinedTask = new Task(async () =>
                {
                    var client = tasksToExecute.First().AsyncState as DiscordClient;
                    var embed = new DiscordEmbedBuilder().WithTitle("AutoMod Alert").WithColor(DiscordColor.Red);

                    foreach (var task in tasksToExecute)
                    {
                        var messageTask = task as Task<MessageCreateEventArgs>;
                        if (messageTask != null)
                        {
                            var args = await messageTask;
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

                            embed.WithFooter($"Author: {args.Author.Username} {args.Author.Id}");
                        }
                    }

                    var channel = await client.GetChannelAsync(ulong.Parse(AutoModAlertChannelId));
                    await channel.SendMessageAsync(embed: embed);
                });

                combinedTask.Start();
                combinedTask.ContinueWith(_ =>
                {
                    lock (queueLock)
                    {
                        if (sendQueue.Count > 0)
                        {
                            timer.Change(10000, Timeout.Infinite);
                        }
                        else
                        {
                            timer.Change(Timeout.Infinite, Timeout.Infinite);
                        }
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

            var alertTask = new Task<MessageCreateEventArgs>(() => args)
            {
                AsyncState = client
            };

            QueueSendAlert(alertTask);

            return Task.CompletedTask;
        }
    }
}
