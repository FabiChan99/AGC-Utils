using AGC_Management.Utils;
using DisCatSharp.ApplicationCommands;

namespace AGC_Management.Eventlistener.LoggingEvents;

[EventHandler]
public class onMemberBan : ApplicationCommandsModule
{
    [Event]
    private Task GuildAuditLogEntryCreated(DiscordClient client, GuildAuditLogEntryCreateEventArgs args)
    {
        if (args.Guild == null)
        {
            return Task.CompletedTask;
        }
        if (args.Guild != CurrentApplication.TargetGuild)
        {
            return Task.CompletedTask;
        }
        if (args.AuditLogEntry.ActionType != AuditLogActionType.Ban)
        {
            return Task.CompletedTask;
        }
        _ = Task.Run(async () =>
        {
            Console.WriteLine("Ban detected");
            
            var auditLogEntry = args.AuditLogEntry as DiscordAuditLogBanEntry;
            
            var targetuser = auditLogEntry.Target as DiscordUser;
            var moduser = auditLogEntry.UserResponsible;
            var reason = auditLogEntry.Reason;
            if (string.IsNullOrEmpty(auditLogEntry.Reason))
            {
                reason = "Kein Grund angegeben";
            }
            
            if (moduser == CurrentApplication.DiscordClient.CurrentUser && reason.Contains(" | Von Moderator: "))
            {
                return;
            }
            
            await LoggingUtils.LogGuildBan(targetuser.Id, moduser.Id, reason);
        });
        return Task.CompletedTask;
    }
}