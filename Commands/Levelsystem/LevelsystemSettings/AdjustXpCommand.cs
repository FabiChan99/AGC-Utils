using System.Drawing;
using AGC_Management.Attributes;
using AGC_Management.Enums.LevelSystem;
using AGC_Management.Utils;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Interactivity.Extensions;

namespace AGC_Management.Commands.Levelsystem;

public partial class LevelSystemSettings
{
    [RequireTeamCat]
    [ApplicationCommandRequirePermissions(Permissions.Administrator)]
    [SlashCommand("adjust-xp", "Modifiziere den XP Stand eines Users", defaultMemberPermissions: (long)Permissions.Administrator)]
    public static async Task AdjustXp(InteractionContext ctx, [Option("action", "Die auszuführende Aktion")] ModifyAction aktion, [Option("user", "Der Benutzer der bearbeitet werden soll")] DiscordUser user , [Option("menge", "XP")] int xpmenge)
    {
        if (aktion == ModifyAction.Set && xpmenge < 0)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("<:attention:1085333468688433232> **Fehler!** Die Menge muss größer als 0 sein!").AsEphemeral());
            return;
        }
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("<a:loading_agc:1084157150747697203> Aktion wird ausgeführt...").AsEphemeral());
        if (aktion == ModifyAction.Add)
        {
            await LevelUtils.AddXp(user, xpmenge);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"<:success:1085333481820790944> **Erfolgreich!** {xpmenge} XP wurden zu {user.Username} hinzugefügt!" +
                                                                                 $" {user.Username} hat nun {await LevelUtils.GetXp(user.Id)} XP!"));
        }
        else if (aktion == ModifyAction.Remove)
        {
            var currentxp = await LevelUtils.GetXp(user.Id);

            if (currentxp < xpmenge)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"<:attention:1085333468688433232> **Fehler!** Die Menge darf nicht größer als der aktuelle XP Stand sein!"));
                return;
            }
            
            await LevelUtils.RemoveXp(user, xpmenge);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"<:success:1085333481820790944> **Erfolgreich!** {xpmenge} XP wurden von {user.Username} abgezogen. {user.Username} hat nun {currentxp - xpmenge} XP!"));
        }
        else if (aktion == ModifyAction.Set)
        {
            await LevelUtils.SetXp(user, xpmenge);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"<:success:1085333481820790944> **Erfolgreich!** {user.Username} hat nun {xpmenge} XP!"));
        }

        try
        {
            var member = await CurrentApplication.TargetGuild.GetMemberAsync(user.Id);
            await LevelUtils.UpdateLevelRoles(member);
        }
        catch (Exception)
        {
            //
        }


    }

}