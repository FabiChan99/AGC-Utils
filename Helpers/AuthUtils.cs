#region

using System.Text.Json;
using AGC_Management.Enums;
using AGC_Management.Enums.Web;
using AGC_Management.Services;
using DisCatSharp.Exceptions;

#endregion

namespace AGC_Management.Utils;

public sealed class AuthUtils
{
    public static async Task<string> RetrieveRole(ulong userId)
    {
        var guild = CurrentApplication.TargetGuild;
        DiscordMember? user = null;
        var servercfg = BotConfig.GetConfig()["ServerConfig"];
        DiscordRole adminRole = guild.GetRole(ulong.Parse(servercfg["AdminRoleId"]));
        DiscordRole? overrideRole = null;
        try
        {
            overrideRole = guild.GetRole(ulong.Parse(servercfg["WebOverrideRoleId"]));
        }
        catch (Exception)
        {
            // ignored
        }

        DiscordRole supRole = guild.GetRole(ulong.Parse(servercfg["SupportRoleId"]));
        DiscordRole modRole = guild.GetRole(ulong.Parse(servercfg["ModRoleId"]));
        DiscordRole staffRole = guild.GetRole(ulong.Parse(servercfg["StaffRoleId"]));
        try
        {
            user = await guild.GetMemberAsync(userId);
        }
        catch (NotFoundException)
        {
            return AccessLevel.NichtImServer.ToString();
        }

        try
        {
            if (overrideRole != null && user.Roles.Contains(overrideRole))
            {
                return AccessLevel.Administrator.ToString();
            }
        }
        catch (Exception)
        {
            // ignored
        }

        // bot owner
        if (user.Id == GlobalProperties.BotOwnerId)
        {
            return AccessLevel.BotOwner.ToString();
        }

        // admin
        if (user.Roles.Contains(adminRole))
        {
            return AccessLevel.Administrator.ToString();
        }

        try
        {
            var headmodroleid = ulong.Parse(await CachingService.GetCacheValue(CustomDatabaseCacheType.VariableCache, "headmodroleid"));
            var headmodrole = guild.GetRole(headmodroleid);
            if (user.Roles.Contains(headmodrole))
            {
                return AccessLevel.HeadModerator.ToString();
            }
        }
        catch (Exception)
        {
            // ignored
        }

        // mod
        if (user.Roles.Contains(modRole))
        {
            return AccessLevel.Moderator.ToString();
        }

        // sup
        if (user.Roles.Contains(supRole))
        {
            return AccessLevel.Supporter.ToString();
        }

        // staff
        if (user.Roles.Contains(staffRole))
        {
            return AccessLevel.Team.ToString();
        }

        // user
        return AccessLevel.User.ToString();
    }


    public static async Task<string> RetrieveName(JsonElement userClaims)
    {
        string userId_ = userClaims.GetProperty("id").ToString();
        ulong userId = ulong.Parse(userId_);

        return (await CurrentApplication.DiscordClient.GetUserAsync(userId)).UsernameWithDiscriminator;
    }

    public static async Task<string> RetrieveDisplayName(ulong userId)
    {
        return (await CurrentApplication.DiscordClient.GetUserAsync(userId)).GlobalName;
    }

    public static async Task<string?> RetrieveDisplayName(JsonElement userClaims)
    {
        string userId_ = userClaims.GetProperty("id").ToString();
        ulong userId = ulong.Parse(userId_);

        return (await CurrentApplication.DiscordClient.GetUserAsync(userId)).GlobalName;
    }


    public static async Task<string> RetrieveRole(JsonElement userClaims)
    {
        string userId_ = userClaims.GetProperty("id").ToString();
        ulong userId = ulong.Parse(userId_);


        var guild = CurrentApplication.TargetGuild;
        DiscordMember? user = null;
        var servercfg = BotConfig.GetConfig()["ServerConfig"];
        DiscordRole adminRole = guild.GetRole(ulong.Parse(servercfg["AdminRoleId"]));
        DiscordRole supRole = guild.GetRole(ulong.Parse(servercfg["SupportRoleId"]));
        DiscordRole? overrideRole = null;
        try
        {
            overrideRole = guild.GetRole(ulong.Parse(servercfg["WebOverrideRoleId"]));
        }
        catch (Exception)
        {
            // ignored
        }

        DiscordRole modRole = guild.GetRole(ulong.Parse(servercfg["ModRoleId"]));
        DiscordRole staffRole = guild.GetRole(ulong.Parse(servercfg["StaffRoleId"]));
        try
        {
            user = await guild.GetMemberAsync(userId);
        }
        catch (NotFoundException)
        {
            return AccessLevel.NichtImServer.ToString();
        }

        try
        {
            if (overrideRole != null && user.Roles.Contains(overrideRole))
            {
                return AccessLevel.Administrator.ToString();
            }
        }
        catch (Exception)
        {
            // ignored
        }

        // bot owner
        if (user.Id == GlobalProperties.BotOwnerId)
        {
            return AccessLevel.BotOwner.ToString();
        }

        // admin
        if (user.Roles.Contains(adminRole))
        {
            return AccessLevel.Administrator.ToString();
        }
        
        
        try
        {
            var headmodroleid = ulong.Parse(await CachingService.GetCacheValue(CustomDatabaseCacheType.VariableCache, "headmodroleid"));
            var headmodrole = guild.GetRole(headmodroleid);
            if (user.Roles.Contains(headmodrole))
            {
                return AccessLevel.HeadModerator.ToString();
            }
        }
        catch (Exception)
        {
            // ignored
        }

        // mod
        if (user.Roles.Contains(modRole))
        {
            return AccessLevel.Moderator.ToString();
        }

        // sup
        if (user.Roles.Contains(supRole))
        {
            return AccessLevel.Supporter.ToString();
        }

        // staff
        if (user.Roles.Contains(staffRole))
        {
            return AccessLevel.Team.ToString();
        }

        // user
        return AccessLevel.User.ToString();
    }


    public static async Task<List<string>> RetrieveRoles(ulong id)
    {
        ulong userId = ulong.Parse(id.ToString());

        var guild = CurrentApplication.TargetGuild;
        DiscordMember? user = null;
        var servercfg = BotConfig.GetConfig()["ServerConfig"];
        DiscordRole adminRole = guild.GetRole(ulong.Parse(servercfg["AdminRoleId"]));
        DiscordRole supRole = guild.GetRole(ulong.Parse(servercfg["SupportRoleId"]));
        DiscordRole? overrideRole = null;

        try
        {
            overrideRole = guild.GetRole(ulong.Parse(servercfg["WebOverrideRoleId"]));
        }
        catch (Exception)
        {
            // ignored
        }

        DiscordRole modRole = guild.GetRole(ulong.Parse(servercfg["ModRoleId"]));
        DiscordRole staffRole = guild.GetRole(ulong.Parse(servercfg["StaffRoleId"]));

        try
        {
            user = await guild.GetMemberAsync(userId);
        }
        catch (NotFoundException)
        {
            return new List<string> { AccessLevel.NichtImServer.ToString() };
        }

        List<string> userRoles = new List<string>();

        // bot owner
        if (user.Id == GlobalProperties.BotOwnerId)
        {
            userRoles.Add(AccessLevel.BotOwner.ToString());
        }

        // override
        if (overrideRole != null && user.Roles.Contains(overrideRole))
        {
            userRoles.Add(AccessLevel.Administrator.ToString());
        }

        // admin
        if (user.Roles.Contains(adminRole))
        {
            userRoles.Add(AccessLevel.Administrator.ToString());
        }

        // mod
        if (user.Roles.Contains(modRole))
        {
            userRoles.Add(AccessLevel.Moderator.ToString());
        }

        // sup
        if (user.Roles.Contains(supRole))
        {
            userRoles.Add(AccessLevel.Supporter.ToString());
        }

        // staff
        if (user.Roles.Contains(staffRole))
        {
            userRoles.Add(AccessLevel.Team.ToString());
        }

        // user
        if (userRoles.Count == 0)
        {
            userRoles.Add(AccessLevel.User.ToString());
        }

        return userRoles;
    }
}