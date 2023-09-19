using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;

namespace AGC_Management.Commands.AutoQuoting;

public class AutoQuoteCommands : BaseCommandModule
{
    [Command("autoquote")]
    public async Task AutoQuoteCommand(CommandContext ctx, bool active)
    {
        if (active)
        {
            BotConfig.SetConfig("UtilsConfig", "AutoQuote", "True");
            await ctx.RespondAsync("AutoQuote aktiviert!");
        }
        else
        {
            BotConfig.SetConfig("UtilsConfig", "AutoQuote", "False");
            await ctx.RespondAsync("AutoQuote deaktiviert!");
        }
    }
}