#region

using System.Diagnostics;
using AGC_Management.Helpers;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

#endregion

namespace AGC_Management.Commands;

public class UpdateBot : ApplicationCommandsModule
{
    [ApplicationCommandRequireTeamOwner]
    [ApplicationCommandRequireUserPermissions(Permissions.Administrator)]
    [SlashCommand("installupdate", "Installiert ein Softwareupdate auf den Bot.")]
    public static async Task InstallUpdate(InteractionContext ctx,
        [Option("Updater-binary",
            "The Software update in a payload format (Bot Onwer only)")]
        DiscordAttachment payload)
    {
        // check filename 
        if (payload.FileName != "update.pkg")
        {
            var errorEmbed =
                EmbedGenerator.GetErrorEmbed(
                    "Die Datei muss update.pkg heißen und mit dem Packtool erstellt worden sein.");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed));
            return;
        }

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent("📥 | Update wird vorbereitet..."));


        // download and save it to disk
        var url = payload.Url;
        using var client = new HttpClient();
        var response = await client.GetAsync(url);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        await File.WriteAllBytesAsync("update.pkg", bytes);
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("📥 | Update wird installiert..."));
        var originalresponse = await ctx.Interaction.GetOriginalResponseAsync();
        var m = originalresponse;

        // install it
        string DcApiToken = "";
        try
        {
            DcApiToken = GlobalProperties.DebugMode
                ? BotConfig.GetConfig()["MainConfig"]["Discord_API_Token_DEB"]
                : BotConfig.GetConfig()["MainConfig"]["Discord_API_Token"];
        }
        // ReSharper disable once EmptyGeneralCatchClause
        catch
        {
        }

        string currentDirectory = Directory.GetCurrentDirectory();
        string agcManagementPath = Path.Combine(currentDirectory, "AGC Management.exe");
        // install it
        var process = new Process
        {
            StartInfo =
            {
                FileName = "cmd.exe",
                Arguments =
                    $"/c BotUpdater.exe normal {DcApiToken} \"{agcManagementPath}\" {ctx.Guild.Id} {ctx.Channel.Id} {m.Id}",
                UseShellExecute = true,
                CreateNoWindow = false
            }
        };
        process.Start();
        Environment.Exit(1);
    }
}