﻿#region

using System.Text.Json;
using AGC_Management.Enums.Web;
using AGC_Management.Services;
using DisCatSharp.Exceptions;
using Microsoft.AspNetCore.Components;

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
}