#region

using AGC_Management.Enums;
using AGC_Management.Services;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;

#endregion

namespace AGC_Management.Commands;

[ApplicationCommandRequireTeamOwner]
[SlashCommandGroup("cache", "Cache Control", (long)Permissions.Administrator)]
public sealed class CacheControlCommands : ApplicationCommandsModule
{
    [SlashCommand("getcachevalue", "Gets the value of a cache key.", (long)Permissions.Administrator)]
    [Description("Gets the value of a cache key.")]
    public static async Task GetCacheValue(InteractionContext ctx,
        [Option("cachefile", "The cache file to get the value from.")] CustomDatabaseCacheType cachefile,
        [Option("key", "The key to get the value for.")] string key)
    {
        var value = await CachingService.GetCacheValue(cachefile, key);
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent("Value is: " + value ?? "Value not found."));
    }

    [SlashCommand("setcachevalue", "Sets the value of a cache key.", (long)Permissions.Administrator)]
    [Description("Sets the value of a cache key.")]
    public static async Task SetCacheValue(InteractionContext ctx,
        [Option("cachefile", "The cache file to set the value for.")] CustomDatabaseCacheType cachefile,
        [Option("key", "The key to set the value for.")] string key,
        [Option("value", "The value to set.")] string value)
    {
        await CachingService.SetCacheValue(cachefile, key, value);
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent("Value set."));
    }

    [SlashCommand("clearobjectfromcache", "Deletes a cache object.", (long)Permissions.Administrator)]
    [Description("Deletes a cache object.")]
    public static async Task ClearObjectFromCache(InteractionContext ctx,
        [Option("cachefile", "The cache file to delete the object from.")] CustomDatabaseCacheType cachefile,
        [Option("key", "The key to delete.")] string key)
    {
        CachingService.DeleteCacheObject(cachefile, key);
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent("Object deleted."));
    }
}