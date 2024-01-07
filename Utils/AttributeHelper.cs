#region

using AGC_Management.Managers;
using AGC_Management.Services;
using DisCatSharp.Lavalink;

#endregion

namespace AGC_Management.Attributes;

public class RequireStaffRole : CheckBaseAttribute
{
    private readonly ulong RoleId = ulong.Parse(BotConfig.GetConfig()["ServerConfig"]["StaffRoleId"]);

    public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
    {
        if (GlobalProperties.DebugMode)
        {
            return true;
        }

        // Check if user has staff role
        if (ctx.Member.Roles.Any(r => r.Id == RoleId))
            return true;
        return false;
    }
}

public class TicketRequireStaffRole : CheckBaseAttribute
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

public class RequireDatabase : CheckBaseAttribute
{
    public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
    {
        // Check if database is connected
        if (DatabaseService.IsConnected()) return true;

        Console.WriteLine("Database is not connected! Command disabled.");
        var embedBuilder = new DiscordEmbedBuilder().WithTitle("Fehler: Datenbank nicht verbunden!")
            .WithDescription(
                $"Command deaktiviert. Bitte informiere den Botentwickler ``{ctx.Client.GetUserAsync(GlobalProperties.BotOwnerId).Result.UsernameWithDiscriminator}``")
            .WithColor(DiscordColor.Red);
        var embed = embedBuilder.Build();
        var msg_e = new DiscordMessageBuilder().WithEmbed(embed).WithReply(ctx.Message.Id);
        await ctx.Channel.SendMessageAsync(msg_e);
        return false;
    }
}

public class ACRequireStaffRole : CheckBaseAttribute
{
    private readonly ulong RoleId = ulong.Parse(BotConfig.GetConfig()["ServerConfig"]["ACStaffRoleId"]);

    public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
    {
        if (GlobalProperties.DebugMode)
        {
            return true;
        }

        // Check if user has staff role
        if (ctx.Member.Roles.Any(r => r.Id == RoleId))
            return true;
        return false;
    }
}

public class RequireTeamCat : CheckBaseAttribute
{
    public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
    {
        if (GlobalProperties.DebugMode)
        {
            return true;
        }

        ulong botOwnerId = GlobalProperties.BotOwnerId;
        if (ctx.Member.Id == botOwnerId)
        {
            return true;
        }

        ulong teamAreaCategoryId = ulong.Parse(BotConfig.GetConfig()["ServerConfig"]["TeamAreaCategoryId"]);
        ulong logCategoryId = ulong.Parse(BotConfig.GetConfig()["ServerConfig"]["LogCategoryId"]);
        ulong modMailCategoryId = ulong.Parse(BotConfig.GetConfig()["ServerConfig"]["ModMailCategoryId"]);

        bool isChannelInValidCategory = ctx.Channel.ParentId == teamAreaCategoryId ||
                                        ctx.Channel.ParentId == logCategoryId ||
                                        ctx.Channel.ParentId == modMailCategoryId;

        return isChannelInValidCategory;
    }
}

public class RequireLavalink : CheckBaseAttribute
{
    public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
    {
        // Check if lavalink is connected
        if (ctx.Client.GetLavalink().ConnectedSessions.Any()) return true;

        Console.WriteLine("Lavalink is not connected! Command disabled.");
        var embedBuilder = new DiscordEmbedBuilder().WithTitle("Fehler: Lavalink nicht verbunden!")
            .WithDescription(
                $"Command deaktiviert. Bitte informiere den Botentwickler ``{ctx.Client.GetUserAsync(GlobalProperties.BotOwnerId).Result.UsernameWithDiscriminator}``")
            .WithColor(DiscordColor.Red);
        var embed = embedBuilder.Build();
        var msg_e = new DiscordMessageBuilder().WithEmbed(embed).WithReply(ctx.Message.Id);
        await ctx.Channel.SendMessageAsync(msg_e);
        return false;
    }
}

public class AGCEasterEggsEnabled : CheckBaseAttribute
{
    public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
    {
        // Check if AGC Setting is Enabled
        try
        {
            if (bool.TrueString == BotConfig.GetConfig()["ServerConfig"]["EasterEggsEnabled"] &&
                ctx.Guild.Id == 750365461945778209) return true;
        }
        catch (Exception)
        {
            // ignored
        }

        return false;
    }
}