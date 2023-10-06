using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;


namespace AGC_Management.Commands.Antiraid;

[EventHandler]
public class DateKickEventhandler : BaseCommandModule
{
    [Event]
    public static async Task GuildMemberAdded(DiscordClient client, GuildMemberAddEventArgs eventArgs)
    {
        _ = Task.Run(async () =>
        {
            if (eventArgs.Guild?.Id != ulong.Parse(BotConfig.GetConfig()["ServerConfig"]["ServerId"]))
            {
                return;
            }
            var member = eventArgs.Member;
            DateTime createdate = member.CreationTimestamp.Date;
            int age = (DateTime.Now - createdate).Days;
            bool isDateKickActive = false;
            isDateKickActive = bool.Parse(BotConfig.GetConfig()["AntiRaid"]["DateKickActive"]);
            if (!isDateKickActive)
            {
                return;
            }
            int minAge = int.Parse(BotConfig.GetConfig()["AntiRaid"]["DateKickDays"]);
            if (age < minAge)
            {
                await DateKickUtils.NotifyUser(member, minAge);
                await member.RemoveAsync($"DateKick - Konto jünger als {minAge}");
                await DateKickUtils.SendToSecurityLogChannel(member.Guild, member, minAge);
            }
            return;
        });
    }
}


public class DateKickCommands : BaseCommandModule
{
    [Command("datekick")]
    public async Task DateKickCommand(CommandContext ctx, int minAge)
    {
        var embed = new DiscordEmbedBuilder();
        embed.WithTitle("DateKick-System");
        embed.WithDescription($"Das DateKick-System wurde auf ``{minAge} Tage`` eingestellt!");
        embed.WithColor(DiscordColor.Green);
        await ctx.RespondAsync(embed: embed);
        BotConfig.SetConfig("AntiRaid", "DateKickDays", minAge.ToString());
    }

    [Command("datekick")]
    public async Task DateKickCommand(CommandContext ctx, bool active)
    {
        var embed = new DiscordEmbedBuilder();
        embed.WithTitle("DateKick-System");
        embed.WithDescription($"Das DateKick-System wurde auf ``{active}`` eingestellt!");
        embed.WithColor(DiscordColor.Green);
        await ctx.RespondAsync(embed: embed);
        BotConfig.SetConfig("AntiRaid", "DateKickActive", active.ToString());
    }

}


internal class DateKickUtils
{
    public static async Task SendToSecurityLogChannel(DiscordGuild guild, DiscordMember member, int MinDays)
    {
        var embed = new DiscordEmbedBuilder();
        embed.WithTitle("User durch Datekick gekickt!");
        embed.WithDescription($"``{member.UsernameWithDiscriminator}`` ({member.Id}) wurde durch das DateKick-System entfernt" +
                              $", da der Account jünger als ``{MinDays} Tage alt`` ist. \n" +
                              $"Account erstellt: {member.CreationTimestamp.Timestamp()}");
        embed.WithFooter($"DateKick-System | {guild.Name}");
        embed.WithTimestamp(DateTime.Now);
        embed.WithColor(DiscordColor.Red);
        var channel = guild.GetChannel(ulong.Parse(BotConfig.GetConfig()["AntiRaid"]["DateKickLog"]));
        await channel.SendMessageAsync(embed: embed);
    }

    public static async Task NotifyUser(DiscordMember member, int MinDays)
    {
        var embed = new DiscordEmbedBuilder();
        embed.WithTitle("Du wurdest durch das DateKick-System entfernt!");
        embed.WithDescription($"Dein Account ist jünger als ``{MinDays} Tage alt`` und wurde daher nicht für den Server zugelassen!" +
                              $" Dein Account wurde erstellt am {member.CreationTimestamp.Timestamp()}");
        embed.WithTimestamp(DateTime.Now);
        embed.WithColor(DiscordColor.Red);
        try
        {
            await member.SendMessageAsync(embed: embed);
        }
        catch
        {
            // ignored
        }
        
    }
}