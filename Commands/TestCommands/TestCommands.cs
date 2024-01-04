#region

#endregion

namespace AGC_Management.Commands.TestCommands;

public class TestCommands : BaseCommandModule
{
    [RequireOwner]
    [Command("test")]
    public async Task Test(CommandContext ctx)
    {
        await ctx.RespondAsync("Test");
    }
}