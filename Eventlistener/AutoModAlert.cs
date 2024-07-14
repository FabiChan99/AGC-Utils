namespace AGC_Management.Eventlistener
{
    [EventHandler]
    public class AutoModAlert : BaseCommandModule
    {
        private static readonly Queue<Task> sendQueue = new();
        private static Timer? timer;

        private string AutoModAlertChannelId = 0.ToString();
        private string AutoModChannelId = 0.ToString();
        private bool AutoModAlertActive;

        public AutoModAlert()
        {
            try
            {
                AutoModAlertActive = bool.Parse(BotConfig.GetConfig()["AutoModNotify"]["AutoModAlertActive"]);
                AutoModChannelId = BotConfig.GetConfig()["AutoModNotify"]["AutoModChannelId"];
                AutoModAlertChannelId = BotConfig.GetConfig()["AutoModNotify"]["AlertChannelId"];
            }
            catch (Exception e)
            {
                CurrentApplication.Logger.Error("Failed to load AutoModNotify config: " + e.Message);
                return;
            }

            timer = new Timer(SendAlertFromQueue, null, Timeout.Infinite, Timeout.Infinite);
        }

        public static void QueueSendAlert(Task alertTask)
        {
            lock (sendQueue)
            {
                sendQueue.Clear();
                sendQueue.Enqueue(alertTask);
                timer.Change(3000, Timeout.Infinite); 
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
                        timer.Change(10000, Timeout.Infinite); 
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
