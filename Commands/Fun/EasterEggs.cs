#region

using AGC_Management.Attributes;

#endregion

namespace AGC_Management.Commands.Fun;

public class AGCEasterEggs : BaseCommandModule
{
    [AGCEasterEggsEnabled]
    [Command("koni")]
    public async Task Koni(CommandContext ctx)
    {
        await ctx.Channel.SendMessageAsync("POV <@244455692881100801>:");
        await ctx.Channel.SendMessageAsync(
            "https://cdn.discordapp.com/emojis/1116433698590556301.gif?size=256&quality=lossless");
    }
}