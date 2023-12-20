#region

using System.Text.RegularExpressions;
using AGC_Management.Utils;
using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Exceptions;

#endregion

namespace AGC_Management;

internal static class DiscordExtension
{
    private static readonly Regex InviteRegex =
        new(RegexPatterns.INVITE, RegexOptions.Compiled | RegexOptions.IgnoreCase);

    internal static List<DiscordOverwriteBuilder> ConvertToBuilderWithNewOverwrites(
        this IReadOnlyList<DiscordOverwrite> overwrites, DiscordMember member, Permissions allowed, Permissions denied)
    {
        return overwrites.Where(x => x.Id != member.Id)
            .Select(x => x.Type == OverwriteType.Role
                ? new DiscordOverwriteBuilder(x.GetRoleAsync().Result) { Allowed = x.Allowed, Denied = x.Denied }
                : new DiscordOverwriteBuilder(x.GetMemberAsync().Result) { Allowed = x.Allowed, Denied = x.Denied })
            .Append(new DiscordOverwriteBuilder(member)
            {
                Allowed = (overwrites.FirstOrDefault(x => x.Id == member.Id, null)?.Allowed ?? Permissions.None) |
                          allowed,
                Denied = (overwrites.FirstOrDefault(x => x.Id == member.Id, null)?.Denied ?? Permissions.None) | denied
            }).ToList();
    }

    internal static List<DiscordOverwriteBuilder> ConvertToBuilderWithNewOverwrites(
        this IReadOnlyList<DiscordOverwrite> overwrites, DiscordRole role, Permissions allowed, Permissions denied)
    {
        return overwrites.Where(x => x.Id != role.Id)
            .Select(x => x.Type == OverwriteType.Role
                ? new DiscordOverwriteBuilder(x.GetRoleAsync().Result) { Allowed = x.Allowed, Denied = x.Denied }
                : new DiscordOverwriteBuilder(x.GetMemberAsync().Result) { Allowed = x.Allowed, Denied = x.Denied })
            .Append(new DiscordOverwriteBuilder(role)
            {
                Allowed =
                    (overwrites.FirstOrDefault(x => x.Id == role.Id, null)?.Allowed ?? Permissions.None) | allowed,
                Denied = (overwrites.FirstOrDefault(x => x.Id == role.Id, null)?.Denied ?? Permissions.None) | denied
            }).ToList();
    }

    internal static List<DiscordOverwriteBuilder> ConvertToBuilder(this IReadOnlyList<DiscordOverwrite> overwrites)
    {
        return overwrites.Select(x =>
                x.Type == OverwriteType.Role
                    ? new DiscordOverwriteBuilder(x.GetRoleAsync().Result) { Allowed = x.Allowed, Denied = x.Denied }
                    : new DiscordOverwriteBuilder(x.GetMemberAsync().Result) { Allowed = x.Allowed, Denied = x.Denied })
            .ToList();
    }


    internal static async Task<DiscordUser?> TryGetUserAsync(this DiscordClient client, ulong userId, bool fetch = true)
    {
        try
        {
            return await client.GetUserAsync(userId, fetch).ConfigureAwait(false);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    ///     Truncates a string to a maximum length.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="maxLength"></param>
    /// <returns>string</returns>
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value; // Return original value if it's null or empty
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }

    internal static async Task<DiscordChannel?> TryGetChannelAsync(this DiscordClient client, ulong id,
        bool fetch = true)
    {
        try
        {
            return await client.GetChannelAsync(id, fetch).ConfigureAwait(false);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }


    public static async Task<List<string>> GetInvitesAsync(this DiscordMessage message, DiscordClient client)
    {
        var matches = InviteRegex.Matches(message.Content);
        var invites = new List<string>();

        foreach (Match match in matches)
        {
            if (match.Groups["code"].Success)
            {
                var code = match.Groups["code"].Value;
                var invite = await client.GetInviteByCodeAsync(code);

                if (invite != null)
                {
                    invites.Add(invite.ToString());
                }
            }
        }

        return invites;
    }

    public static List<string> GetInvites(this DiscordMessage message)
    {
        var matches = InviteRegex.Matches(message.Content);
        var invites = new List<string>();

        foreach (Match match in matches)
        {
            if (match.Groups["code"].Success)
            {
                var code = match.Groups["code"].Value;
                var inviteLink = $"https://discord.gg/{code}";
                invites.Add(inviteLink);
            }
        }

        return invites;
    }
}