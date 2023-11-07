using System.Diagnostics;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Lavalink;
using Microsoft.CodeAnalysis;

namespace AGC_Management.Commands;

public class StatsCommand : BaseCommandModule
{
    [Command("stats")]
    [Description("Shows the bot's stats.")]
    [Aliases("botinfo", "botstats", "bot")]
    public async Task Stats(CommandContext ctx)
    {
        var embed = new DiscordEmbedBuilder();
        embed.WithTitle("AGC Management Bot Stats")
             .WithColor(DiscordColor.Blurple)
             .WithThumbnail(ctx.Client.CurrentUser.AvatarUrl) // Adds the bot's avatar as a thumbnail
             .WithFooter($"Requested by {ctx.User.Username}", ctx.User.AvatarUrl) // Adds the requester's username and avatar in the footer
             .WithTimestamp(DateTimeOffset.Now); // Adds the current time to the embed

        // Calculate uptime
        var uptime = DateTime.Now - Process.GetCurrentProcess().StartTime;
        string istring = $"Uptime: **{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s**\n";

        // Users cached
        var cachedUsers = ctx.Client.UserCache.Count;
        istring += $"Cached Users: **{cachedUsers}**\n";

        // RAM usage
        var ramUsage = Process.GetCurrentProcess().WorkingSet64;
        istring += $"RAM Usage: **{ramUsage / 1024 / 1024} MB**\n";

        // Bot latency
        var latency = ctx.Client.Ping;
        istring += $"Ping: **{latency} ms**\n";

        // .NET runtime version
        var netruntime = Environment.Version;
        istring += $"Microsoft .NET Runtime: **{netruntime}**\n";

        // Library version
        var libraryversion = ctx.Client.VersionString;
        istring += $"Library Version: **{libraryversion}**\n";

        // Operating System
        var OS = Environment.OSVersion;
        istring += $"Operating System: **{OS}**\n";
        
        var writtenin = "C#";
        istring += $"Written in: **{writtenin}**\n";
        
        var workingdir = Environment.ProcessPath;
        istring += $"Working Directory: **{workingdir}**\n";
        
        // CPU Core count
        var cpu = Environment.ProcessorCount;
        istring += $"CPU Cores: **{cpu}**\n";

        // Bot owner details
        DiscordUser botowner = ctx.Client.CurrentApplication.Owners.First();
        istring += $"Bot Owner: **{botowner.UsernameWithDiscriminator}** ``{botowner.Id}``\n";

        embed.WithDescription(istring);

        await ctx.RespondAsync(embed);
    }
}
