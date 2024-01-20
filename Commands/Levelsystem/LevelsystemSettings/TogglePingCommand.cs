#region

using AGC_Management.Utils;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;

#endregion

namespace AGC_Management.Commands.Levelsystem;

public class TogglePingCommand : ApplicationCommandsModule
{
    [SlashCommand("togglelevelping", "Schalte die Levelup-Pings ein oder aus")]
    public static async Task ToggleLevelPingCommand(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .WithContent("<a:loading_agc:1084157150747697203> Einstellungen werden gespeichert...").AsEphemeral());
        bool enabled = await LevelUtils.ToggleLevelUpPing(ctx.User.Id);
        if (enabled)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                "<:success:1085333481820790944> **Erfolgreich!** Levelup-Pings wurden aktiviert!"));
        }
        else
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                "<:success:1085333481820790944> **Erfolgreich!** Levelup-Pings wurden deaktiviert!"));
        }
    }
}