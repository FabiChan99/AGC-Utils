using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;

namespace AGC_Management.Helpers;

public static class ModerationHelper
{
    private static readonly int FallbackWarnsToKick = 2;
    private static readonly int FallbackWarnsToBan = 3;
    private static readonly bool FallbackWarnsToBanEnabled = false;
    private static readonly bool FallbackWarnsToKickEnabled = false;


    public static async Task<(bool, bool)> UserActioningEnabled()
    {
        bool WarnsToKickEnabled;
        bool WarnsToBanEnabled;
        try
        {
            WarnsToBanEnabled = bool.Parse(BotConfig.GetConfig()["ModerationConfig"]["WarnsToBanEnabled"]);
        }
        catch
        {
            WarnsToBanEnabled = FallbackWarnsToBanEnabled;
        }

        try
        {
            WarnsToKickEnabled = bool.Parse(BotConfig.GetConfig()["ModerationConfig"]["WarnsToKickEnabled"]);
        }
        catch
        {
            WarnsToKickEnabled = FallbackWarnsToKickEnabled;
        }

        return (WarnsToKickEnabled, WarnsToBanEnabled);
    }

    public static async Task<(int, int)> GetWarnKickValues()
    {
        int WarnsToKick;
        int WarnsToBan;
        try
        {
            WarnsToKick = int.Parse(BotConfig.GetConfig()["ModerationConfig"]["WarnsToKick"]);
        }
        catch
        {
            WarnsToKick = FallbackWarnsToKick;
        }

        try
        {
            WarnsToBan = int.Parse(BotConfig.GetConfig()["ModerationConfig"]["WarnsToBan"]);
        }
        catch
        {
            WarnsToBan = FallbackWarnsToBan;
        }

        if (WarnsToKick <= 0) throw new Exception("WarnsToKick must be greater than 0!");

        if (WarnsToKick >= WarnsToBan) throw new Exception("WarnsToKick must be lower than WarnsToBan!");

        return (WarnsToKick, WarnsToBan);
    }


    public static async Task<DiscordEmbed> GenerateWarnEmbed(CommandContext ctx, DiscordUser user, DiscordUser mod,
        int warnCount, string caseid, bool isManual, string reason)
    {
        string unbanurl = GetUnbanURL();
        var (warnsToKick, warnsToBan) = await GetWarnKickValues();
        if (warnCount >= warnsToBan)
            return new DiscordEmbedBuilder()
                .WithDescription(
                    $"Du hast eine Verwarnung vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. Verwarnung**. __Du wirst nun aus dem Server gebannt.__ Du kannst einen [Entbannungsantrag einreichen]({unbanurl}). Der Grund für die Verwarnung ist: ```{reason}```")
                .WithColor(DiscordColor.Red).WithFooter("").WithFooter(isManual
                    ? $"Manuelle Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben."
                    : $"Automatische Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben.")
                .WithTitle("Du wurdest verwarnt!")
                .Build();

        if (warnCount == warnsToBan - 1)
            return new DiscordEmbedBuilder()
                .WithDescription(
                    $"Du hast eine Verwarnung vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. Verwarnung**, " +
                    $"__beachte bitte, dass Du bei der nächsten Verwarnung aus **{ctx.Guild.Name}** gebannt wirst.__ Der Grund für die Verwarnung ist: ```{reason}```")
                .WithColor(DiscordColor.Red)
                .WithTitle("Du wurdest verwarnt!")
                .Build();

        if (warnCount >= warnsToKick)
        {
            if (warnsToKick + 1 == warnsToBan)
                return new DiscordEmbedBuilder()
                    .WithDescription(
                        $"Du hast eine Verwarnung vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. Verwarnung**, " +
                        $"__beachte bitte, dass Du bei der nächsten Verwarnung aus **{ctx.Guild.Name}** gebannt wirst.__ __Du wirst nun aus dem Server gekickt.__ Der Grund für die Verwarnung ist: ```{reason}```")
                    .WithColor(DiscordColor.Red).WithFooter(isManual
                        ? $"Manuelle Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben."
                        : $"Automatische Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben.")
                    .WithTitle("Du wurdest verwarnt!")
                    .Build();
            return new DiscordEmbedBuilder()
                .WithDescription(
                    $"Du hast eine Verwarnung vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. __Du wirst nun aus dem Server gekickt.__ Der Grund für die Verwarnung ist: ```{reason}```")
                .WithColor(DiscordColor.Red).WithFooter(isManual
                    ? $"Manuelle Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben."
                    : $"Automatische Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben.")
                .WithTitle("Du wurdest verwarnt!")
                .Build();
        }

        if (warnCount == warnsToKick - 1)
            return new DiscordEmbedBuilder()
                .WithDescription(
                    $"Du hast eine Verwarnung vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. Verwarnung**, __beachte bitte, dass Du bei der nächsten Verwarnung aus **{ctx.Guild.Name}** gekickt wirst.__ Der Grund für die Verwarnung ist: ```{reason}```")
                .WithColor(DiscordColor.Red).WithFooter(isManual
                    ? $"Manuelle Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben."
                    : $"Automatische Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben.")
                .WithTitle("Du wurdest verwarnt!")
                .Build();

        return new DiscordEmbedBuilder()
            .WithDescription(
                $"Du hast eine Verwarnung vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. Verwarnung**. Der Grund für die Verwarnung ist: ```{reason}```*")
            .WithColor(DiscordColor.Red).WithFooter(isManual
                ? $"Manuelle Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben."
                : $"Automatische Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben.")
            .WithTitle("Du wurdest verwarnt!")
            .Build();
    }

    public static async Task<DiscordEmbed> GeneratePermaWarnEmbed(CommandContext ctx, DiscordUser user, DiscordUser mod,
        int warnCount, string caseid,
        bool isManual, string reason)
    {
        var (warnsToKick, warnsToBan) = await GetWarnKickValues();
        string unbanurl = GetUnbanURL();
        if (warnCount >= warnsToBan)
            return new DiscordEmbedBuilder()
                .WithDescription(
                    $"Du hast eine permanente Verwarnung (sie läuft nicht ab) vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. Verwarnung**. __Du wirst nun aus dem Server gebannt.__ Du kannst einen [Entbannungsantrag einreichen]({unbanurl}). Der Grund für die Verwarnung ist: ```{reason}```")
                .WithColor(DiscordColor.Red).WithFooter("").WithFooter(isManual
                    ? $"Manuelle Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben."
                    : $"Automatische Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben.")
                .WithTitle("Du wurdest permanent verwarnt!")
                .Build();

        if (warnCount == warnsToBan - 1)
            return new DiscordEmbedBuilder()
                .WithDescription(
                    $"Du hast eine permanente Verwarnung (sie läuft nicht ab) vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. Verwarnung**, " +
                    $"__beachte bitte, dass Du bei der nächsten Verwarnung aus **{ctx.Guild.Name}** gebannt wirst.__ Der Grund für die Verwarnung ist: ```{reason}```")
                .WithColor(DiscordColor.Red)
                .WithTitle("Du wurdest permanent verwarnt!")
                .Build();

        if (warnCount >= warnsToKick)
        {
            if (warnsToKick + 1 == warnsToBan)
                return new DiscordEmbedBuilder()
                    .WithDescription(
                        $"Du hast eine permanente Verwarnung (sie läuft nicht ab) vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. Verwarnung**, " +
                        $"__beachte bitte, dass Du bei der nächsten Verwarnung aus **{ctx.Guild.Name}** gebannt wirst.__ Du wirst nun aus dem Server gekickt. Der Grund für die Verwarnung ist: ```{reason}```")
                    .WithColor(DiscordColor.Red).WithFooter(isManual
                        ? $"Manuelle Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben."
                        : $"Automatische Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben.")
                    .WithTitle("Du wurdest permanent verwarnt!")
                    .Build();
            return new DiscordEmbedBuilder()
                .WithDescription(
                    $"Du hast eine permanente Verwarnung (sie läuft nicht ab) vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. Verwarnung**, __Du wirst nun aus dem Server gekickt.__ Der Grund für die Verwarnung ist: ```{reason}```")
                .WithColor(DiscordColor.Red).WithFooter(isManual
                    ? $"Manuelle Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben."
                    : $"Automatische Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben.")
                .WithTitle("Du wurdest permanent verwarnt!")
                .Build();
        }

        if (warnCount == warnsToKick - 1)
            return new DiscordEmbedBuilder()
                .WithDescription(
                    $"Du hast eine permanente Verwarnung (sie läuft nicht ab) vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. Verwarnung**, __beachte bitte, dass Du bei der nächsten Verwarnung aus **{ctx.Guild.Name}** gekickt wirst.__ Der Grund für die Verwarnung ist: ```{reason}```*")
                .WithColor(DiscordColor.Red).WithFooter(isManual
                    ? $"Manuelle Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben."
                    : $"Automatische Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben.")
                .WithTitle("Du wurdest permanent verwarnt!")
                .Build();

        return new DiscordEmbedBuilder()
            .WithDescription(
                $"Du hast eine permanente Verwarnung (sie läuft nicht ab) vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. Verwarnung**. Der Grund für die Verwarnung ist: ```{reason}```*")
            .WithColor(DiscordColor.Red).WithFooter(isManual
                ? $"Manuelle Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben."
                : $"Automatische Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben.")
            .WithTitle("Du wurdest permanent verwarnt!")
            .Build();
    }


    public static string GetUnbanURL()
    {
        string unbanurl;
        try
        {
            unbanurl = BotConfig.GetConfig()["ServerConfig"]["BanAppealURL"];
        }
        catch
        {
            unbanurl = "";
        }

        return unbanurl;
    }
}