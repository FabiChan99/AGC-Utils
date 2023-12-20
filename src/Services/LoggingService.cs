#region

using DisCatSharp;
using DisCatSharp.Entities;

#endregion

namespace AGC_Management.Services
{
    public class LoggingService
    {
        public async Task<DiscordWebhook> RetrieveWebHook(DiscordClient client, string webhookName)
        {
            var webhookId = BotConfig.GetConfig()["Logging"][webhookName];
            return await client.GetWebhookAsync(ulong.Parse(webhookId));
        }

        public async Task SendLog(DiscordWebhook webhook, DiscordEmbed embed)
        {
            await webhook.ExecuteAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
    }
}