#region

using AGC_Management.Helpers;
using AGC_Ticket.Helpers;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;

#endregion

namespace AGC_Management.Components;

public class TicketComponents
{
    public static List<DiscordButtonComponent> GetTicketActionRow()
    {
        List<DiscordButtonComponent> buttons = new()
        {
            new DiscordButtonComponent(ButtonStyle.Danger, "ticket_close", "(Team) Ticket schließen ❌"),
            new DiscordButtonComponent(ButtonStyle.Primary, "ticket_claim", "(Team) Ticket Claimen 👋"),
            new DiscordButtonComponent(ButtonStyle.Secondary, "ticket_add_user", "(Team) User hinzufügen 👥"),
            new DiscordButtonComponent(ButtonStyle.Secondary, "ticket_remove_user", "(Team) User entfernen 👤"),
            new DiscordButtonComponent(ButtonStyle.Success, "ticket_more", "(Team) Mehr...")
        };
        return buttons;
    }

    public static List<DiscordButtonComponent> GetTicketClaimedActionRow()
    {
        List<DiscordButtonComponent> buttons = new()
        {
            new DiscordButtonComponent(ButtonStyle.Danger, "ticket_close", "(Team) Ticket schließen ❌"),
            new DiscordButtonComponent(ButtonStyle.Primary, "ticket_claim", "(Team) Ticket Claimen 👋", true),
            new DiscordButtonComponent(ButtonStyle.Secondary, "ticket_add_user", "(Team) User hinzufügen 👥"),
            new DiscordButtonComponent(ButtonStyle.Secondary, "ticket_remove_user", "(Team) User entfernen 👤"),
            new DiscordButtonComponent(ButtonStyle.Success, "ticket_more", "(Team) Mehr...")
        };
        return buttons;
    }

    public static List<DiscordButtonComponent> GetClosedTicketActionRow()
    {
        List<DiscordButtonComponent> buttons = new()
        {
            new DiscordButtonComponent(ButtonStyle.Danger, "ticket_close", "(Team) Ticket schließen ❌", true),
            new DiscordButtonComponent(ButtonStyle.Primary, "ticket_claim", "(Team) Ticket Claimen 👋", true),
            new DiscordButtonComponent(ButtonStyle.Secondary, "ticket_add_user", "(Team) User hinzufügen 👥", true),
            new DiscordButtonComponent(ButtonStyle.Secondary, "ticket_remove_user", "(Team) User entfernen 👤", true),
            new DiscordButtonComponent(ButtonStyle.Success, "ticket_more", "(Team) Mehr...")
        };
        return buttons;
    }

    public static List<DiscordButtonComponent> GetContactTicketActionRow()
    {
        List<DiscordButtonComponent> buttons = new()
        {
            new DiscordButtonComponent(ButtonStyle.Danger, "ticket_close", "(Team) Ticket schließen ❌"),
            new DiscordButtonComponent(ButtonStyle.Secondary, "ticket_add_user", "(Team) User hinzufügen 👥"),
            new DiscordButtonComponent(ButtonStyle.Secondary, "ticket_remove_user", "(Team) User entfernen 👤"),
            new DiscordButtonComponent(ButtonStyle.Success, "ticket_more", "(Team) Mehr...")
        };
        return buttons;
    }
    
    public static async Task RenderMore(InteractionCreateEventArgs interactionCreateEvent)
    {
        var user = await interactionCreateEvent.Interaction.User.ConvertToMember(interactionCreateEvent.Interaction.Guild);

        if (!TeamChecker.IsSupporter(user))
        {
            await interactionCreateEvent.Interaction.CreateResponseAsync(
                InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Du bist kein Teammitglied!").AsEphemeral());
            return;
        }

        var buttons = new List<DiscordButtonComponent>
        {
            new DiscordButtonComponent(ButtonStyle.Primary, "ticket_userinfo", "Userinfo"),
            new DiscordButtonComponent(ButtonStyle.Primary, "ticket_flagtranscript", "Transcript Flaggen"),
            new DiscordButtonComponent(ButtonStyle.Primary, "generatetranscript", "Transcript erzeugen"),
            new DiscordButtonComponent(ButtonStyle.Primary, "ticket_snippets", "Snippet senden"),
            new DiscordButtonComponent(ButtonStyle.Success, "manage_notification", "Benachr. verwalten")
        };

        var responseBuilder = new DiscordInteractionResponseBuilder().AddComponents(buttons).AsEphemeral();
        await interactionCreateEvent.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, responseBuilder);
    }

    public static List<DiscordActionRowComponent> GetNotificationManagerButtons()
    {
        var buttons = new List<DiscordButtonComponent>
        {
            new DiscordButtonComponent(ButtonStyle.Danger, "disable_notification", "Deaktivieren", true),
            new DiscordButtonComponent(ButtonStyle.Primary, "enable_noti_mode1", "Einmalig Ping", false),
            new DiscordButtonComponent(ButtonStyle.Primary, "enable_noti_mode2", "Einmalig DM", false),
            new DiscordButtonComponent(ButtonStyle.Primary, "enable_noti_mode3", "Einmalig Ping + DM", false),
            new DiscordButtonComponent(ButtonStyle.Primary, "enable_noti_mode4", "Bei jeder Nachricht Ping", false),
            new DiscordButtonComponent(ButtonStyle.Primary, "enable_noti_mode5", "Bei jeder Nachricht DM", false),
            new DiscordButtonComponent(ButtonStyle.Primary, "enable_noti_mode6", "Bei jeder Nachricht Ping + DM", false)
        };
        var actionRows = new List<DiscordActionRowComponent>
        {
        new DiscordActionRowComponent(new List<DiscordButtonComponent> { buttons[0] }),
        new DiscordActionRowComponent(buttons.GetRange(1, 3)),
        new DiscordActionRowComponent(buttons.GetRange(4, 3))

        };
        return actionRows;
    }
    
    public static List<DiscordActionRowComponent> GetNotificationManagerButtonsEnabledNotify()
    {
        var buttons = new List<DiscordButtonComponent>
        {
            new DiscordButtonComponent(ButtonStyle.Danger, "disable_notification", "Deaktivieren"),
            new DiscordButtonComponent(ButtonStyle.Primary, "enable_noti_mode1", "Einmalig Ping"),
            new DiscordButtonComponent(ButtonStyle.Primary, "enable_noti_mode2", "Einmalig DM"),
            new DiscordButtonComponent(ButtonStyle.Primary, "enable_noti_mode3", "Einmalig Ping + DM"),
            new DiscordButtonComponent(ButtonStyle.Primary, "enable_noti_mode4", "Bei jeder Nachricht Ping"),
            new DiscordButtonComponent(ButtonStyle.Primary, "enable_noti_mode5", "Bei jeder Nachricht DM"),
            new DiscordButtonComponent(ButtonStyle.Primary, "enable_noti_mode6", "Bei jeder Nachricht Ping + DM")
        };
        var actionRows = new List<DiscordActionRowComponent>
        {
        new DiscordActionRowComponent(new List<DiscordButtonComponent> { buttons[0] }),
        new DiscordActionRowComponent(buttons.GetRange(1, 3)),
        new DiscordActionRowComponent(buttons.GetRange(4, 3))

        };
        return actionRows;
    }
}