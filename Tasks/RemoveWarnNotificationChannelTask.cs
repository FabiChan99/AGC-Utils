#region

#endregion

namespace AGC_Management.Tasks;

public sealed class ExtendedModerationSystemLoop
{
    public static async Task LaunchLoops()
    {
        await CheckRemainingWarnChannels();
    }

    private static async Task CheckRemainingWarnChannels()
    {
        await Task.Delay(TimeSpan.FromSeconds(10));
        while (true)
        {
            var agc_guild = GlobalProperties.AGCGuild;
            var support_category = agc_guild.Channels.FirstOrDefault(x =>
                x.Value.Id == ulong.Parse(BotConfig.GetConfig()["TicketConfig"]["SupportCategoryId"])).Value;
            var warn_channels = support_category.Children.Where(x => x.Name.StartsWith("warn-")).ToList();
            foreach (var channel in warn_channels)
            {
                var channel_age = DateTimeOffset.Now - channel.CreationTimestamp;
                if (channel_age.TotalHours > 24)
                {
                    if (!channel.Name.StartsWith("warn-")) continue;

                    if (channel.Name.StartsWith("warn-"))
                    {
                        CurrentApplication.DiscordClient.Logger.LogInformation(
                            $"Lösche Warn-Channel {channel.Name} da er älter als 24 Stunden ist.");
                        await channel.DeleteAsync(
                            "Warn-Channel ist älter als 24 Stunden. Behandle ihn als abgeschlossen.");
                    }
                }
            }

            await Task.Delay(TimeSpan.FromHours(1));
        }
    }
}