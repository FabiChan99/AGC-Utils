namespace AGC_Management.Eventlistener
{
    [EventHandler]
    public class AutoModAlert : BaseCommandModule
    {
        private static readonly List<Task> sendQueue = new();
        private static Timer? timer;
        private static readonly object queueLock = new();

        private readonly string AutoModAlertChannelId = BotConfig.GetConfig()["AutoModNotify"]["AlertChannelId"] ?? "0";
        private readonly string AutoModChannelId = BotConfig.GetConfig()["AutoModNotify"]["AutoModChannelId"] ?? "0";
        private readonly bool AutoModAlertActive = ParseOrFalse(BotConfig.GetConfig()["AutoModNotify"]["AutoModAlertActive"], out var result) && result;
        
        private static bool ParseOrFalse(string value, out bool result)
        {
            try
            {
                result = bool.Parse(value);
                return true;
            }
            catch
            {
                result = false;
                return false;
            }
        }
        
        public AutoModAlert()
        {
            timer = new Timer(SendAlertFromQueue, null, Timeout.Infinite, Timeout.Infinite);
        }

        public static void QueueSendAlert(Task alertTask)
        {
            lock (queueLock)
            {
                sendQueue.Add(alertTask);
                if (sendQueue.Count == 1)
                {
                    timer.Change(0, Timeout.Infinite);
                }
            }
        }

        private static async void SendAlertFromQueue(object state)
        {
            List<Task> tasksToExecute;
            lock (queueLock)
            {
                tasksToExecute = new List<Task>(sendQueue);
                sendQueue.Clear();
            }

            if (tasksToExecute.Count > 0)
            {
                foreach (var task in tasksToExecute)
                {
                    task.Start();
                }

                await Task.WhenAll(tasksToExecute);
            }

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
