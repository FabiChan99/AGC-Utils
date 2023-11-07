#region

using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.Exceptions;

#endregion

namespace AGC_Management.Helper;

internal static class Extensions
{
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
}