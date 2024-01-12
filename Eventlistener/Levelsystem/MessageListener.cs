using DisCatSharp.ApplicationCommands;

namespace AGC_Management.Eventlistener.Levelsystem;

[EventHandler]
public sealed class MessageListener : ApplicationCommandsModule
{
    private ulong targetserverid = ulong.Parse(BotConfig.GetConfig()["ServerConfig"]["ServerId"]);
    
    [Event]
    private Task MessageCreated(DiscordClient client, MessageCreateEventArgs args)
    {
        _ = Task.Run(async () =>
        {
            bool lvlactive = false;
            try
            {
                lvlactive = bool.Parse(BotConfig.GetConfig()["Leveling"]["Active"]);
            }catch{}
            if (!lvlactive)
            {
                return;
            }
            if (args.Channel.Type == ChannelType.Private || args.Author.IsBot)
                return;
            if (args.Guild.Id != targetserverid)
            {
                return;
            }
            
            // later implemention here
        });
        return Task.CompletedTask;
    }
    
}