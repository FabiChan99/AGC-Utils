#region

using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using KawaiiAPI.NET;
using KawaiiAPI.NET.Enums;
using Microsoft.Extensions.DependencyInjection;

#endregion

namespace AGC_Management.Commands.Fun;

public class RoleplaySystem : ApplicationCommandsModule
{
    private readonly KawaiiClient _kawaiiclient;
    private readonly IServiceProvider _serviceProvider;

    public RoleplaySystem(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _kawaiiclient = _serviceProvider.GetRequiredService<KawaiiClient>();
    }

    [SlashCommand("hug", "Umarmt einen User")]
    public async Task HugCommand(InteractionContext ctx,
        [Option("user", "Den Nutzer den du umarmen willst")] DiscordUser user)
    {
        var imageUrl = await _kawaiiclient.GetRandomGifAsync(KawaiiGifType.Hug);

        var embed = new DiscordEmbedBuilder()
            .WithImageUrl(imageUrl)
            .WithTitle($"{ctx.Member.DisplayName} umarmt {user.Username}!")
            .WithColor(DiscordColor.Blurple);
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .AddEmbed(embed).WithContent(user.Mention));
    }


    [SlashCommand("kiss", "Küsst einen User")]
    public async Task KissCommand(InteractionContext ctx,
        [Option("user", "Den Nutzer den du küssen willst")] DiscordUser user)
    {
        var imageUrl = await _kawaiiclient.GetRandomGifAsync(KawaiiGifType.Kiss);

        var embed = new DiscordEmbedBuilder()
            .WithImageUrl(imageUrl)
            .WithTitle($"{ctx.Member.DisplayName} küsst {user.Username}!")
            .WithColor(DiscordColor.Blurple);
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .AddEmbed(embed).WithContent(user.Mention));
    }

    [SlashCommand("pat", "Streichelt einen User")]
    public async Task PatCommand(InteractionContext ctx,
        [Option("user", "Den Nutzer den du streicheln willst")] DiscordUser user)
    {
        var imageUrl = await _kawaiiclient.GetRandomGifAsync(KawaiiGifType.Pat);

        var embed = new DiscordEmbedBuilder()
            .WithImageUrl(imageUrl)
            .WithTitle($"{ctx.Member.DisplayName} streichelt {user.Username}!")
            .WithColor(DiscordColor.Blurple);
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .AddEmbed(embed).WithContent(user.Mention));
    }

    [SlashCommand("kill", "Tötet einen User")]
    public async Task KillCommand(InteractionContext ctx,
        [Option("user", "Den Nutzer den du töten willst")] DiscordUser user)
    {
        var imageUrl = await _kawaiiclient.GetRandomGifAsync(KawaiiGifType.Kill);

        var embed = new DiscordEmbedBuilder()
            .WithImageUrl(imageUrl)
            .WithTitle($"{ctx.Member.DisplayName} tötet {user.Username}!")
            .WithColor(DiscordColor.Blurple);
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .AddEmbed(embed).WithContent(user.Mention));
    }

    [SlashCommand("lick", "Leckt einen User")]
    public async Task LickCommand(InteractionContext ctx,
        [Option("user", "Den Nutzer den du ablecken willst")] DiscordUser user)
    {
        var imageUrl = await _kawaiiclient.GetRandomGifAsync(KawaiiGifType.Lick);

        var embed = new DiscordEmbedBuilder()
            .WithImageUrl(imageUrl)
            .WithTitle($"{ctx.Member.DisplayName} leckt {user.Username} ab!")
            .WithColor(DiscordColor.Blurple);
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .AddEmbed(embed).WithContent(user.Mention));
    }

    [SlashCommand("bite", "Beißt einen User")]
    public async Task BiteCommand(InteractionContext ctx,
        [Option("user", "Den Nutzer den du beißen willst")] DiscordUser user)
    {
        var imageUrl = await _kawaiiclient.GetRandomGifAsync(KawaiiGifType.Bite);

        var embed = new DiscordEmbedBuilder()
            .WithImageUrl(imageUrl)
            .WithTitle($"{ctx.Member.DisplayName} beißt {user.Username}!")
            .WithColor(DiscordColor.Blurple);
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .AddEmbed(embed).WithContent(user.Mention));
    }

    [SlashCommand("cry", "Weint mit einem User")]
    public async Task CryCommand(InteractionContext ctx,
        [Option("user", "Den Nutzer mit dem du weinen willst")] DiscordUser user)
    {
        var imageUrl = await _kawaiiclient.GetRandomGifAsync(KawaiiGifType.Cry);

        var embed = new DiscordEmbedBuilder()
            .WithImageUrl(imageUrl)
            .WithTitle($"{ctx.Member.DisplayName} weint mit {user.Username}!")
            .WithColor(DiscordColor.Blurple);
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .AddEmbed(embed).WithContent(user.Mention));
    }

    [SlashCommand("love", "Liebt einen User")]
    public async Task LoveCommand(InteractionContext ctx,
        [Option("user", "Den Nutzer den du lieben willst")] DiscordUser user)
    {
        var imageUrl = await _kawaiiclient.GetRandomGifAsync(KawaiiGifType.Love);

        var embed = new DiscordEmbedBuilder()
            .WithImageUrl(imageUrl)
            .WithTitle($"{ctx.Member.DisplayName} liebt {user.Username}!")
            .WithColor(DiscordColor.Blurple);
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .AddEmbed(embed).WithContent(user.Mention));
    }
}