#region

using System.Text;
using AGC_Management.Attributes;
using AGC_Management.Helpers;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Extensions;
using DisCatSharp.Lavalink;
using DisCatSharp.Lavalink.Entities;
using LavaSharp.LavaManager;

#endregion

namespace AGC_Management.Commands.Music;

[SlashCommandGroup("queue", "Queue commands.")]
public sealed class QueueCommand : ApplicationCommandsModule
{
    [RequireConnectedLavalink]
    [EnsureGuild]
    [EnsureMatchGuildId]
    [ApplicationRequireExecutorInVoice]
    [SlashCommand("current", "Zeigt die aktuelle Warteschlange an.")]
    public static async Task Loop(InteractionContext ctx)
    {
        var lava = ctx.Client.GetLavalink();
        var node = lava.ConnectedSessions.First().Value;
        var player = node.GetGuildPlayer(ctx.Guild);
        var channel = ctx.Member.VoiceState?.Channel;

        if (player?.CurrentTrack == null)
        {
            var errorEmbed = EmbedGenerator.GetErrorEmbed("Es wird kein Song abgespielt.");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
            return;
        }

        if (player?.Channel.Id != channel?.Id)
        {
            var errorEmbed = EmbedGenerator.GetErrorEmbed("Du musst mit mir in einem Voice Channel sein.");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
            return;
        }


        var queue = LavaQueue.queue;

        if (queue.Count == 0)
        {
            var errorEmbed = EmbedGenerator.GetErrorEmbed("Es sind keine Songs in der Warteschlange.");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
            return;
        }

        // acknowledge the interaction

        // Paginate the queue
        var pages = new List<Page>();
        var queueList = queue.ToList();
        var queueString = new StringBuilder();
        var i = 1;

        List<LavalinkTrack> tracks = new();
        foreach (var item in queue)
        {
            tracks.Add(item.Item1);
        }

        // get the total length of the queue
        var totalTime = new TimeSpan();

        foreach (var item in tracks)
        {
            totalTime += item.Info.Length;
        }


        string formattedTime =
            $"{(int)totalTime.TotalDays} Tage, {totalTime.Hours} Stunden, {totalTime.Minutes} Minuten, {totalTime.Seconds} Sekunden";

        foreach (var item in queueList)
        {
            queueString.AppendLine(
                $"``{i}.`` {item.Item1.Info.Author} - {item.Item1.Info.Title} ({item.Item1.Info.Length:hh\\:mm\\:ss}) | Angefordert von {item.Item2.Mention}");
            i++;
            if (i % 25 == 0)
            {
                var eb = new DiscordEmbedBuilder();
                eb.WithTitle("Queue");
                eb.WithDescription(queueString.ToString());
                eb.WithColor(BotConfig.GetEmbedColor());
                eb.WithFooter(
                    $"Angefordert von {ctx.Member.DisplayName} | Seite {pages.Count + 1} von {Math.Ceiling((double)queue.Count / 25)}",
                    ctx.Member.AvatarUrl);
                eb.WithTimestamp(DateTime.Now);
                pages.Add(new Page(embed: eb,
                    content:
                    $"🎵 | Zeigt die Warteschlange an. Es sind {queue.Count} Songs in der Warteschlange mit einer Gesamtlänge von ``{formattedTime}``."));
                queueString.Clear();
            }
        }

        // Add the last page if there are remaining songs
        if (queueString.Length > 0)
        {
            var eb = new DiscordEmbedBuilder();
            eb.WithTitle("Queue");
            eb.WithDescription(queueString.ToString());
            eb.WithColor(BotConfig.GetEmbedColor());
            eb.WithFooter($"Angefordert von {ctx.Member.DisplayName} | Seite {pages.Count + 1}", ctx.Member.AvatarUrl);
            eb.WithTimestamp(DateTime.Now);
            pages.Add(new Page(embed: eb,
                content:
                $"🎵 | Zeigt die Warteschlange an. Es sind {queue.Count} Songs in der Warteschlange mit einer Gesamtlänge von ``{formattedTime}``."));
        }

        await ctx.Interaction.SendPaginatedResponseAsync(false, false, ctx.User, pages);
    }

    [EnsureGuild]
    [EnsureMatchGuildId]
    [ApplicationRequireExecutorInVoice]
    [CheckDJ]
    [SlashCommand("clear", "Clears the queue.")]
    public static async Task ClearQueue(InteractionContext ctx)
    {
        var lava = ctx.Client.GetLavalink();
        var node = lava.ConnectedSessions.First().Value;
        var player = node.GetGuildPlayer(ctx.Guild);
        var channel = ctx.Member.VoiceState?.Channel;

        if (player?.CurrentTrack == null)
        {
            var errorEmbed = EmbedGenerator.GetErrorEmbed("Es wird kein Song abgespielt.");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
            return;
        }

        if (player?.Channel.Id != channel?.Id)
        {
            var errorEmbed = EmbedGenerator.GetErrorEmbed("Du musst mit mir in einem Voice Channel sein.");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
            return;
        }

        if (LavaQueue.queue.Count == 0)
        {
            var errorEmbed = EmbedGenerator.GetErrorEmbed("Es sind keine Songs in der Warteschlange.");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
            return;
        }

        LavaQueue.queue.Clear();
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent("🗑️ | Die Warteschlange wurde geleert."));
    }

    [EnsureGuild]
    [EnsureMatchGuildId]
    [RequireRunningPlayer]
    [ApplicationRequireExecutorInVoice]
    [CheckDJ]
    [SlashCommand("removesong",
        "Entfernt einen Song aus der Warteschlange. Benutze /queue current um die Songnummer zu finden.")]
    public static async Task RemoveSong(InteractionContext ctx,
        [Option("songnumber", "The song number to remove.")]
        int songnumber)
    {
        var lava = ctx.Client.GetLavalink();
        var node = lava.ConnectedSessions.First().Value;
        var player = node.GetGuildPlayer(ctx.Guild);
        var channel = ctx.Member.VoiceState?.Channel;

        if (player?.Channel.Id != channel?.Id)
        {
            var errorEmbed = EmbedGenerator.GetErrorEmbed("Du musst mit mir in einem Voice Channel sein.");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
            return;
        }

        if (songnumber > LavaQueue.queue.Count || songnumber < 1)
        {
            var errorEmbed =
                EmbedGenerator.GetErrorEmbed(
                    $"Songnummer muss zwischen 1 und der Warteschlangenlänge ({LavaQueue.queue.Count}) liegen.");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
            return;
        }

        var queueList = LavaQueue.queue.ToList();
        var tracktoRemove = queueList[songnumber - 1];

        var eb = new DiscordEmbedBuilder();
        eb.WithTitle("Songremover");
        eb.WithDescription($"Bist du sicher, dass du ``{tracktoRemove.Item1.Info.Title}`` entfernen möchtest?");
        eb.WithColor(BotConfig.GetEmbedColor());
        eb.WithFooter($"{ctx.Member.UsernameWithDiscriminator}", ctx.Member.AvatarUrl);
        eb.WithTimestamp(DateTime.Now);
        var buttons = new List<DiscordButtonComponent>();
        var yesButton = new DiscordButtonComponent(ButtonStyle.Success, "yes", "Ja");
        var noButton = new DiscordButtonComponent(ButtonStyle.Danger, "no", "Nein");
        buttons.Add(yesButton);
        buttons.Add(noButton);
        var irb = new DiscordInteractionResponseBuilder();
        irb.AddEmbed(eb);
        irb.AddComponents(buttons);
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, irb);
        var interactivity = ctx.Client.GetInteractivity();

        bool MatchAuthor(ComponentInteractionCreateEventArgs args)
        {
            return args.User.Id == ctx.User.Id;
        }

        var result = await interactivity.WaitForButtonAsync(await ctx.GetOriginalResponseAsync(), MatchAuthor,
            TimeSpan.FromSeconds(45));
        if (result.TimedOut)
        {
            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent("⏱️ | Timed Out. Entfernung des Songs abgebrochen."));
            return;
        }

        if (result.Result.Id == "no")
        {
            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent("🚫 | Entfernung des Songs abgebrochen."));
            return;
        }

        if (queueList[songnumber - 1].Item1 != tracktoRemove.Item1)
        {
            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent(
                    "🚫 | Die Warteschlange wurde in der Zwischenzeit geändert. Entfernung des Songs abgebrochen."));
            return;
        }

        queueList.RemoveAt(songnumber - 1);
        LavaQueue.queue = new Queue<(LavalinkTrack, DiscordUser)>(queueList);
        var volstr = $"🗑️ | ``{tracktoRemove.Item1.Info.Title}`` wurde aus der Warteschlange entfernt.";
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(volstr));
    }
}