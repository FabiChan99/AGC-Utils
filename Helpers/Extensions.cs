using DisCatSharp.Entities;

namespace AGC_Management.Utils;

internal static class DiscordExtension
{
    internal static bool isTeamMember(this DiscordMember member)
    {
        ulong teamRole = ulong.Parse(BotConfig.GetConfig()["ServerConfig"]["StaffRoleId"]);
        return member.Roles.Any(x => x.Id == teamRole);
    }
}