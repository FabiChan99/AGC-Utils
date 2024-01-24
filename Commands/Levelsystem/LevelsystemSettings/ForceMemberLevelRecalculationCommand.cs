#region

using AGC_Management.Attributes;
using AGC_Management.Utils;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;

#endregion

namespace AGC_Management.Commands.Levelsystem;

public class ForceMemberLevelRecalculationCommand : ApplicationCommandsModule
{
    [RequireTeamCat]
    [ACRequireStaffRole]
    [ApplicationCommandRequirePermissions(Permissions.Administrator)]
    [SlashCommand("forcememberrecalc", "Erzwinge eine Neuberechnung der Level eines Nutzers")]
    public static async Task ForceMemberLevelRecalculation(InteractionContext ctx,
        [Option("user", "Der Nutzer bei dem die Level neu berechnet werden sollen.")]
        DiscordUser user)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent(
                "<a:loading_agc:1084157150747697203> Level werden neu berechnet..."));
        if (user.IsBot)
        {
            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent(
                    "<:attention:1085333468688433232> **Fehler!** Dies ist ein Bot!"));
            return;
        }

        await LevelUtils.RecalculateUserLevel(user.Id);
        if (ctx.Guild is not null)
        {
            var member = await ctx.Guild.GetMemberAsync(user.Id);
            await LevelUtils.UpdateLevelRoles(member);
        }

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
            "<:success:1085333481820790944> **Erfolgreich!** Level von ``" + user.Username +
            "`` wurden neu berechnet!"));
    }
}