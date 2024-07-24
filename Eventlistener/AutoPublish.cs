namespace AGC_Management.Eventlistener;

[EventHandler]
public class AutoPublish : BaseCommandModule
{
    [Event]
    public static async Task MessageCreated(DiscordClient client, MessageCreateEventArgs args)
    {
        if (args.Channel.Id == 802947905241481227) await args.Channel.CrosspostMessageAsync(args.Message);
    }
}