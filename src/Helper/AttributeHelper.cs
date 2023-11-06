#region

using AGC_Management;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;

#endregion

public class RequireStaffRole : CheckBaseAttribute
{
    private readonly ulong RoleId = ulong.Parse(BotConfig.GetConfig()["TicketConfig"]["TeamRoleId"]);

    public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
    {
        if (ctx.Member.Roles.Any(r => r.Id == RoleId))
            return true;
        var m = await ctx.RespondAsync("⚠️ Du musst ein Teammitglied sein, um diese Aktion auszuführen!");
        await Task.Delay(500);
        await m.DeleteAsync();
        await ctx.Message.DeleteAsync();
        return false;
    }
}

public class RequireOpenTicket : CheckBaseAttribute
{
    public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
    {
        var openticket = await TicketManagerHelper.IsOpenTicket(ctx.Channel);
        if (!openticket)
        {
            ulong RoleId = ulong.Parse(BotConfig.GetConfig()["TicketConfig"]["TeamRoleId"]);
            if (ctx.Member.Roles.Any(r => r.Id == RoleId))
                await ctx.RespondAsync("Dies ist kein offenes Ticket.");
        }

        return openticket;
    }
}