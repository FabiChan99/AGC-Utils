using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGC_Management.Helpers
{
    public class ModerationHelper
    {
        protected static readonly int FallbackWarnsToKick = 2;
        protected static readonly int FallbackWarnsToBan = 3;
        protected static readonly bool FallbackWarnsToBanEnabled = false;
        protected static readonly bool FallbackWarnsToKickEnabled = false;


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

            if (WarnsToKick <= 0)
            {
                throw new Exception("WarnsToKick must be greater than 0!");
            }

            if (WarnsToKick >= WarnsToBan)
            {
                throw new Exception("WarnsToKick must be lower than WarnsToBan!");
            }

            return (WarnsToKick, WarnsToBan);
        }


        public static async Task<DiscordEmbed> GenerateWarnEmbed(CommandContext ctx, DiscordUser user, DiscordUser mod, int warnCount, string caseid, bool isManual, string reason)
        {
            var (warnsToKick, warnsToBan) = await GetWarnKickValues();
            DiscordEmbed embed;
            if (warnCount >= warnsToBan)
            {
                return new DiscordEmbedBuilder()
                    .WithDescription(
                        $"Du hast eine Verwarnung vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. Verwarnung**. Du wirst nun aus dem Server gebannt. Du kannst einen [Entbannungsantrag einrichen](https://unban.animegamingcafe.de). Der Grund für die Verwarnung ist: ```{reason}```")
                    .WithColor(DiscordColor.Red).WithFooter($"").WithFooter(isManual ? $"Manuelle Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben." : $"Automatische Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben.")
                    .WithTitle("Du wurdest verwarnt!")
                    .Build();
            }

            if (warnCount == (warnsToBan-1))
            {
                return new DiscordEmbedBuilder()
                    .WithDescription(
                        $"Du hast eine Verwarnung vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. Verwarnung**, " +
                        $"beachte bitte, dass Du bei der nächsten Verwarnung aus **{ctx.Guild.Name}** gebannt wirst. Der Grund für die Verwarnung ist: ```{reason}```")
                    .WithColor(DiscordColor.Red)
                    .WithTitle("Du wurdest verwarnt!")
                    .Build();
            }

            if (warnCount >= warnsToKick)
            {
                if ((warnsToKick + 1) == warnsToBan)
                {
                    return new DiscordEmbedBuilder()
                        .WithDescription(
                            $"Du hast eine Verwarnung vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. Verwarnung**, " +
                            $"beachte bitte, dass Du bei der nächsten Verwarnung aus **{ctx.Guild.Name}** gebannt wirst. Du wirst nun aus dem Server gekickt. Der Grund für die Verwarnung ist: ```{reason}```")
                        .WithColor(DiscordColor.Red).WithFooter(isManual ? $"Manuelle Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben." : $"Automatische Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben.")
                        .WithTitle("Du wurdest verwarnt!")
                        .Build();
                }
                return new DiscordEmbedBuilder()
                    .WithDescription(
                        $"Du hast eine Verwarnung vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. Du wirst nun aus dem Server gekickt. Der Grund für die Verwarnung ist: ```{reason}```")
                    .WithColor(DiscordColor.Red).WithFooter(isManual ? $"Manuelle Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben." : $"Automatische Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben.")
                    .WithTitle("Du wurdest verwarnt!")
                    .Build();
            }

            if (warnCount == (warnsToKick - 1))
            {
                return new DiscordEmbedBuilder()
                    .WithDescription(
                        $"Du hast eine Verwarnung vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. Verwarnung**, beachte bitte, dass Du bei der nächsten Verwarnung aus **{ctx.Guild.Name}** gekickt wirst. Der Grund für die Verwarnung ist: ```{reason}```")
                    .WithColor(DiscordColor.Red).WithFooter(isManual ? $"Manuelle Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben." : $"Automatische Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben.")
                    .WithTitle("Du wurdest verwarnt!")
                    .Build();
            }

            return new DiscordEmbedBuilder()
                .WithDescription(
                    $"Du hast eine Verwarnung vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. Verwarnung**. Der Grund für die Verwarnung ist: ```{reason}```*")
                .WithColor(DiscordColor.Red).WithFooter(isManual ? $"Manuelle Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben." : $"Automatische Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben.")
                .WithTitle("Du wurdest verwarnt!")
                .Build();

        }
        public static async Task<DiscordEmbed> GeneratePermaWarnEmbed(CommandContext ctx, DiscordUser user, DiscordUser mod, int warnCount, string caseid,
            bool isManual, string reason)
        {
            var (warnsToKick, warnsToBan) = await GetWarnKickValues();
            DiscordEmbed embed;
            if (warnCount >= warnsToBan)
            {
                return new DiscordEmbedBuilder()
                    .WithDescription(
                        $"Du hast eine permanente Verwarnung (sie läuft nicht ab) vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. Verwarnung**. Du wirst nun aus dem Server gebannt. Du kannst einen [Entbannungsantrag einrichen](https://unban.animegamingcafe.de). Der Grund für die Verwarnung ist: ```{reason}```")
                    .WithColor(DiscordColor.Red).WithFooter($"").WithFooter(isManual ? $"Manuelle Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben." : $"Automatische Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben.")
                    .WithTitle("Du wurdest permanent verwarnt!")
                    .Build();
            }

            if (warnCount == (warnsToBan - 1))
            {
                return new DiscordEmbedBuilder()
                    .WithDescription(
                        $"Du hast eine permanente Verwarnung (sie läuft nicht ab) vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. Verwarnung**, " +
                        $"beachte bitte, dass Du bei der nächsten Verwarnung aus **{ctx.Guild.Name}** gebannt wirst. Der Grund für die Verwarnung ist: ```{reason}```")
                    .WithColor(DiscordColor.Red)
                    .WithTitle("Du wurdest permanent verwarnt!")
                    .Build();
            }

            if (warnCount >= warnsToKick)
            {
                if ((warnsToKick + 1) == warnsToBan)
                {
                    return new DiscordEmbedBuilder()
                        .WithDescription(
                            $"Du hast eine permanente Verwarnung (sie läuft nicht ab) vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. Verwarnung**, " +
                            $"beachte bitte, dass Du bei der nächsten Verwarnung aus **{ctx.Guild.Name}** gebannt wirst. Du wirst nun aus dem Server gekickt. Der Grund für die Verwarnung ist: ```{reason}```")
                        .WithColor(DiscordColor.Red).WithFooter(isManual ? $"Manuelle Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben." : $"Automatische Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben.")
                        .WithTitle("Du wurdest permanent verwarnt!")
                        .Build();
                }
                return new DiscordEmbedBuilder()
                    .WithDescription(
                        $"Du hast eine permanente Verwarnung (sie läuft nicht ab) vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. Verwarnung**, Du wirst nun aus dem Server gekickt. Der Grund für die Verwarnung ist: ```{reason}```")
                    .WithColor(DiscordColor.Red).WithFooter(isManual ? $"Manuelle Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben." : $"Automatische Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben.")
                    .WithTitle("Du wurdest permanent verwarnt!")
                    .Build();
            }

            if (warnCount == (warnsToKick - 1))
            {
                return new DiscordEmbedBuilder()
                    .WithDescription(
                        $"Du hast eine permanente Verwarnung (sie läuft nicht ab) vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. Verwarnung**, beachte bitte, dass Du bei der nächsten Verwarnung aus **{ctx.Guild.Name}** gekickt wirst. Der Grund für die Verwarnung ist: ```{reason}```*")
                    .WithColor(DiscordColor.Red).WithFooter(isManual ? $"Manuelle Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben." : $"Automatische Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben.")
                    .WithTitle("Du wurdest permanent verwarnt!")
                    .Build();
            }

            return new DiscordEmbedBuilder()
                .WithDescription(
                    $"Du hast eine permanente Verwarnung (sie läuft nicht ab) vom Serverteam erhalten, bitte beachte, dass Verwarnungen immer Folgen mit sich ziehen. Dies ist deine **{warnCount}. Verwarnung**. Der Grund für die Verwarnung ist: ```{reason}```*")
                .WithColor(DiscordColor.Red).WithFooter(isManual ? $"Manuelle Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben." : $"Automatische Verwarnung | Warnungs-ID: {caseid} <- Bei Fragen bitte ein Ticket eröffnen und diese ID angeben.")
                .WithTitle("Du wurdest permanent verwarnt!")
                .Build();

        }

    }
}
