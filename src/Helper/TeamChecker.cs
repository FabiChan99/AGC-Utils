#region

using AGC_Management;
using DisCatSharp.Entities;

#endregion

namespace AGC_Ticket.Helpers;

public static class TeamChecker
{
    public static bool IsSupporter(DiscordMember member)
    {
        ulong SupporterRole = ulong.Parse(BotConfig.GetConfig()["TicketConfig"]["TeamRoleId"]);
        if (member.Roles.Any(x => x.Id == SupporterRole))
            return true;
        return false;
    }
}