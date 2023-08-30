using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace AGC_Management.Services.Logging
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
            await webhook.ExecuteAsync(new DiscordWebhookBuilder
            {

            }.AddEmbed(embed));
        }
    }
}
