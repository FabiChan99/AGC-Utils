#region

using AGC_Management.TempVoice;

#endregion

namespace AGC_Management.Eventlistener.TempVoice;

[EventHandler]
public class PanelInterfaceInteraction : TempVoiceHelper
{
    [Event]
    private static Task ComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        _ = Task.Run(async () =>
        {
            var Interaction = e.Interaction;
            var PanelMsgId = ulong.Parse(BotConfig.GetConfig()["TempVC"]["VCPanelMessageID"]);
            var PanelMsgChannelId = ulong.Parse(BotConfig.GetConfig()["TempVC"]["VCPanelChannelID"]);
            if (PanelMsgChannelId == 0 && PanelMsgId == 0)
            {
                sender.Logger.LogWarning(
                    $"Panel is not Initialized! Consider initializing with {BotConfig.GetConfig()["MainConfig"]["BotPrefix"]}initpanel");
                return;
            }

            if (Interaction.Channel.Id != PanelMsgChannelId) return;

            if (Interaction.Channel.Id == PanelMsgChannelId)
            {
                var customid = Interaction.Data.CustomId;

                if (customid == "channel_lock")
                    await PanelLockChannel(Interaction);
                else if (customid == "unlock_lock")
                    await PanelUnlockChannel(Interaction);
                else if (customid == "channel_rename")
                    await PanelChannelRename(Interaction, sender);
                else if (customid == "channel_hide")
                    await PanelHideChannel(Interaction);
                else if (customid == "channel_show")
                    await PanelUnhideChannel(Interaction);
                else if (customid == "channel_invite")
                    await PanelChannelInvite(Interaction);
                else if (customid == "invite_selector")
                    await PanelChannelInviteCallback(Interaction, sender);
                else if (customid == "channel_limit")
                    await PanelChannelLimit(Interaction, sender);
                else if (customid == "channel_delete")
                    await PanelChannelDelete(Interaction, sender, e);
                else if (customid == "channel_permit")
                    await PanelPermitVoiceSelector(Interaction, sender, e);
                else if (customid == "channel_permit")
                    await PanelPermitVoiceSelector(Interaction, sender, e);
                else if (customid == "permit_selector")
                    await PanelPermitVoiceSelectorCallback(Interaction, sender, e);
                else if (customid == "role_permit_button")
                    await PanelPermitVoiceRole(Interaction, sender, e);
                else if (customid == "role_permit_selector")
                    await PanelPermitVoiceRoleCallback(Interaction, sender, e);
                else if (customid == "channel_unpermit")
                    await PanelChannelUnpermit(Interaction, sender, e);
                else if (customid == "unpermit_levelrole")
                    await PanelChannelUnpermitRoleCallback(Interaction, sender, e);
                else if (customid == "unpermit_selector")
                    await PanelChannelUnpermitUserCallback(Interaction, sender, e);
                else if (customid == "channel_claim")
                    await PanelChannelClaim(Interaction, sender);
                else if (customid == "channel_transfer")
                    await PanelChannelTransfer(Interaction);
                else if (customid == "transfer_selector")
                    await PanelChannelTransferCallback(Interaction, sender, e);
                else if (customid == "channel_kick")
                    await PanelChannelKick(Interaction);
                else if (customid == "kick_selector")
                    await PanelChannelKickCallback(Interaction, sender, e);
                else if (customid == "channel_ban")
                    await PanelChannelBlock(Interaction);
                else if (customid == "ban_selector")
                    await PanelChannelBlockCallback(Interaction, sender, e);
                else if (customid == "channel_unban")
                    await PanelChannelUnblock(Interaction);
                else if (customid == "unban_selector") await PanelChannelUnblockCallback(Interaction, sender, e);
            }
        });
        return Task.CompletedTask;
    }
}