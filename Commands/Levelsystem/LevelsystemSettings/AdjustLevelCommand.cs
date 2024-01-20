#region

using AGC_Management.Attributes;
using AGC_Management.Enums.LevelSystem;
using AGC_Management.Utils;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;

#endregion

namespace AGC_Management.Commands.Levelsystem;

public partial class LevelSystemSettings
{
    [RequireTeamCat]
    [ApplicationCommandRequirePermissions(Permissions.Administrator)]
    [SlashCommand("adjust-level", "Modifiziere den XP Stand eines Users", (long)Permissions.Administrator)]
    public static async Task AdjustLevel(InteractionContext ctx,
        [Option("action", "Die auszuführende Aktion")]
        ModifyAction aktion,
        [Option("user", "Der Benutzer der bearbeitet werden soll")]
        DiscordUser user,
        [Option("menge", "Level")] int levelmenge)
    {
        if (aktion == ModifyAction.Set && levelmenge < 0)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("<:attention:1085333468688433232> **Fehler!** Die Menge muss größer als 0 sein!")
                    .AsEphemeral());
            return;
        }

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .WithContent("<a:loading_agc:1084157150747697203> Aktion wird ausgeführt...").AsEphemeral());
        if (aktion == ModifyAction.Add)
        {
            await LevelUtils.AddLevel(user, levelmenge);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                $"<:success:1085333481820790944> **Erfolgreich!** {levelmenge} Level wurden zu {user.Username} hinzugefügt!" +
                $" {user.Username} hat nun {await LevelUtils.GetLevel(user.Id)} Level!"));
        }
        else if (aktion == ModifyAction.Remove)
        {
            var currentlevel = await LevelUtils.GetLevel(user.Id);

            if (currentlevel < levelmenge)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                    "<:attention:1085333468688433232> **Fehler!** Die Menge darf nicht größer als der aktuelle Level Stand sein!"));
                return;
            }

            await LevelUtils.RemoveLevel(user, levelmenge);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                $"<:success:1085333481820790944> **Erfolgreich!** {levelmenge} Level wurden von {user.Username} abgezogen. {user.Username} hat nun {currentlevel - levelmenge} Level!"));
        }
        else if (aktion == ModifyAction.Set)
        {
            await LevelUtils.SetLevel(user, levelmenge);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                $"<:success:1085333481820790944> **Erfolgreich!** {user.Username} hat nun {levelmenge} Level!"));
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