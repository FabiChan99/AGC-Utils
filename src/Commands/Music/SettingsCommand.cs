using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using AGC_Management.Helpers;

namespace AGC_Management.Commands;

[ApplicationCommandRequireUserPermissions(Permissions.ManageGuild)]
[SlashCommandGroup("settings", "Ändere die Einstellungen des Bots.")]
public class SettingsCommand : ApplicationCommandsModule
{
    [SlashCommand("RequireDJRole", "Aktiviere oder deaktiviere die DJ-Rolle.")]
    public async Task RequireDJRole(InteractionContext ctx,
        [Option("setActive", "Set the active state.")]
        bool setActive)
    {
        var cfg = BotConfig.GetConfig()["MusicConfig"]["RequireDJRole"];
        var isAlreadyActive = bool.Parse(cfg);
        if (isAlreadyActive == setActive)
        {
            var embed = EmbedGenerator.GetErrorEmbed("Das DJ-Rollen-System ist bereits " +
                                                     (setActive ? "aktiviert" : "deaktiviert") + ".");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral());
            return;
        }

        BotConfig.SetConfig("MusicConfig", "RequireDJRole", setActive.ToString());
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent("🛠️ | Das DJ-Rollen-System wurde " +
                                                                (setActive ? "aktiviert" : "deaktiviert") + ".")
                .AsEphemeral());
    }

    [SlashCommand("SkipAndStopButtons", "Aktiviere oder deaktiviere die Skip- und Stop-Buttons.")]
    public async Task SkipAndStopButtons(InteractionContext ctx,
        [Option("setActive", "Set the active state.")]
        bool setActive)
    {
        var cfg = BotConfig.GetConfig()["MusicConfig"]["SkipAndStopButtons"];
        var isAlreadyActive = bool.Parse(cfg);
        if (isAlreadyActive == setActive)
        {
            var embed = EmbedGenerator.GetErrorEmbed("Die Skip- und Stop-Buttons sind bereits " +
                                                     (setActive ? "aktiviert" : "deaktiviert") + ".");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral());
            return;
        }

        BotConfig.SetConfig("MainConfig", "SkipAndStopButtons", setActive.ToString());
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent("🛠️ | Die Skip- und Stop-Buttons wurden " +
                                                                (setActive ? "``aktiviert``" : "``deaktiviert``") + ".")
                .AsEphemeral());
    }

    
    [SlashCommand("AutoDisconnect", "Enable or disable the auto-disconnect when all users left the channel.")]
    public async Task AutoDisconnect(InteractionContext ctx,
        [Option("setActive", "Set the active state.")]
        bool setActive)
    {
        var cfg = BotConfig.GetConfig()["MusicConfig"]["AutoLeaveOnEmptyChannel"];
        var isAlreadyActive = bool.Parse(cfg);
        if (isAlreadyActive == setActive)
        {
            var embed = EmbedGenerator.GetErrorEmbed("Das auto-disconnect ist bereits " +
                                                     (setActive ? "aktiviert" : "deaktiviert") + ".");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral());
            return;
        }

        BotConfig.SetConfig("MusicConfig", "AutoLeaveOnEmptyChannel", setActive.ToString());
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent("🛠️ | Das auto-disconnect wurde " +
                                                                (setActive ? "``aktiviert``" : "``deaktiviert``") + ".")
                .AsEphemeral());
    }
    
    [SlashCommand("AutoDisconnectDelay", "Setzt die Zeit in Sekunden, nach der der Bot sich automatisch aus dem Voice Channel entfernt.")]
    public async Task AutoDisconnectDelay(InteractionContext ctx,
        [Option("delay", "Der delay in Sekunden.")]
        int delay)
    {
        if (delay < 1 || delay > 300)
        {
            var embed = EmbedGenerator.GetErrorEmbed("Der delay muss zwischen 1 und 300 Sekunden liegen.");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral());
            return;
        }
        var cfg = BotConfig.GetConfig()["MusicConfig"]["AutoLeaveOnEmptyChannelDelay"];
        var isAlreadyActive = int.Parse(cfg);
        if (isAlreadyActive == delay)
        {
            var embed = EmbedGenerator.GetErrorEmbed("Der auto-disconnect delay ist bereits auf " + delay + " Sekunden gesetzt.");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral());
            return;
        }

        BotConfig.SetConfig("MusicConfig", "AutoLeaveOnEmptyChannelDelay", delay.ToString());
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent("🛠️ | Der auto-disconnect delay wurde auf ``" + delay + "`` Sekunden gesetzt.")
                .AsEphemeral());
    }
    
    [SlashCommand("AutoDisconnectDelayActive", "Aktiviere oder deaktiviere den auto-disconnect delay.")]
    public async Task AutoDisconnectDelayActive(InteractionContext ctx,
        [Option("setActive", "Set the active state.")]
        bool setActive)
    {
        var cfg = BotConfig.GetConfig()["MusicConfig"]["AutoLeaveOnEmptyChannelDelayActive"];
        var isAlreadyActive = bool.Parse(cfg);
        if (isAlreadyActive == setActive)
        {
            var embed = EmbedGenerator.GetErrorEmbed("The auto-disconnect delay is already " +
                                                     (setActive ? "enabled" : "disabled") + ".");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral());
            return;
        }

        BotConfig.SetConfig("MainConfig", "AutoLeaveOnEmptyChannelDelayActive", setActive.ToString());
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent("🛠️ | The auto-disconnect delay has been " +
                                                                (setActive ? "``enabled``" : "``disabled``") + ".")
                .AsEphemeral());
    }
    
    
}