#region

using AGC_Management.Helpers;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Lavalink;
using DisCatSharp.Lavalink.Entities;

#endregion

namespace AGC_Management.Helpers;

public sealed class ApplicationRequireExecutorInVoice : ApplicationCommandCheckBaseAttribute
{
    public override async Task<bool> ExecuteChecksAsync(BaseContext ctx)
    {
        if (ctx.Member.VoiceState?.Channel is null)
        {
            var embed = EmbedGenerator.GetErrorEmbed(
                "Du musst in einem VoiceChannel sein um diesen Command zu verwenden!");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed));
            return false;
        }

        return true;
    }
}

public sealed class RequireRunningPlayer : ApplicationCommandCheckBaseAttribute
{
    public override async Task<bool> ExecuteChecksAsync(BaseContext ctx)
    {
        var lava = ctx.Client.GetLavalink();
        var node = lava.ConnectedSessions.First().Value;
        var player = node.GetGuildPlayer(ctx.Guild);
        if (player is null)
        {
            var errorembed = EmbedGenerator.GetErrorEmbed("Ich bin in keinem VoiceChannel!");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorembed));
            return false;
        }

        return true;
    }
}

public sealed class EnsureGuild : ApplicationCommandCheckBaseAttribute
{
    public override async Task<bool> ExecuteChecksAsync(BaseContext ctx)
    {
        if (ctx.Guild is null)
        {
            var errorembed = EmbedGenerator.GetErrorEmbed("Dieser Command kann nur in einem Server verwendet werden.");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorembed));
            return false;
        }

        return true;
    }
}

public sealed class EnsureMatchGuildId : ApplicationCommandCheckBaseAttribute
{
    public override async Task<bool> ExecuteChecksAsync(BaseContext ctx)
    {
        var id = BotConfig.GetConfig()["ServerConfig"]["ServerId"];
        var configuredId = ulong.Parse(id);
        if (ctx.Guild.Id != configuredId)
        {
            var errorembed =
                EmbedGenerator.GetErrorEmbed(
                    "Dieser Command kann nur in dem Server verwendet werden, in dem ich konfiguriert bin.");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorembed));
            return false;
        }

        return true;
    }
}

public sealed class CheckDJ : ApplicationCommandCheckBaseAttribute
{
    public override async Task<bool> ExecuteChecksAsync(BaseContext ctx)
    {
        IReadOnlyCollection<DiscordMember> voicemembers = ctx.Member.VoiceState.Channel.Users;
        bool isPlaying = false;
        try
        {
            LavalinkTrack? ct = ctx.Client.GetLavalink().ConnectedSessions!.First().Value.GetGuildPlayer(ctx.Guild)
                .CurrentTrack;
            if (ct != null)
            {
                isPlaying = true;
            }
        }
        catch (Exception)
        {
            isPlaying = false;
        }

        if (!isPlaying)
        {
            if (voicemembers.Count == 1)
            {
                return true;
            }
        }
        else if (isPlaying)
        {
            if (voicemembers.Count == 2)
            {
                if (voicemembers.Any(x => x.Id == ctx.Client.CurrentUser.Id) &&
                    voicemembers.Any(x => x.Id == ctx.Member.Id))
                {
                    return true;
                }
            }
        }

        bool djconf = bool.Parse(BotConfig.GetConfig()["MusicConfig"]["RequireDJRole"]);
        if (!djconf)
        {
            return true;
        }

        bool isDJ = false;
        bool isCtxAdminorManager = ctx.Member.Permissions.HasPermission(Permissions.Administrator) ||
                                   ctx.Member.Permissions.HasPermission(Permissions.ManageGuild);
        if (isCtxAdminorManager)
        {
            isDJ = true;
        }

        bool checkRoleifuserhasdj = ctx.Member.Roles.Any(x => x.Name == "DJ");
        if (checkRoleifuserhasdj)
        {
            isDJ = true;
        }

        if (!isDJ)
        {
            DiscordEmbedBuilder errorembed =
                EmbedGenerator.GetErrorEmbed("Du musst die DJ Rolle haben um diesen Command zu verwenden!");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorembed));
            return false;
        }

        return isDJ;
    }
}