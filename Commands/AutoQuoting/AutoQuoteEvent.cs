﻿#region

#endregion

namespace AGC_Management.Commands.AutoQuoting;

[EventHandler]
internal class AutoQuoteEvent : BaseCommandModule
{
    [Event]
    public async Task MessageCreated(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        if (eventArgs.Author.IsBot) return;

        if (string.IsNullOrEmpty(eventArgs.Message.Guild?.Id.ToString())) return;

        _ = Task.Run(async () =>
        {
            var isAutoQuoteActive = false;
            try
            {
                isAutoQuoteActive = bool.Parse(BotConfig.GetConfig()["UtilsConfig"]["AutoQuote"]);
            }
            catch
            {
            }

            if (isAutoQuoteActive) await AutoQuoteHelper.ProcessMessageWithLinks(client, eventArgs);
        });
    }
}