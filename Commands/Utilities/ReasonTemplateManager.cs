#region

using AGC_Management.Attributes;
using AGC_Management.Utils;

#endregion

namespace AGC_Management.Commands;

[Group("template")]
[Aliases("templates")]
public class ReasonTemplateManager : BaseCommandModule
{
    [GroupCommand]
    [RequireStaffRole]
    public async Task ReasonTemplateManager_Help(CommandContext ctx)
    {
        Dictionary<string, string> templates = await ReasonTemplateResolver.GetReplacements();
        DiscordEmbedBuilder eb = new DiscordEmbedBuilder()
            .WithTitle("Template Manager")
            .WithColor(DiscordColor.Red)
            .WithFooter("AGC Support-System", ctx.Guild.IconUrl);
        int i = 0;
        foreach (var (key, value) in templates)
        {
            eb.AddField(new DiscordEmbedField("-" + key, value, true));
            i++;
        }

        if (i == 0)
        {
            eb.WithDescription("__Keine Templates vorhanden.__");
        }

        await ctx.RespondAsync(eb);
    }

    [Command("add")]
    [RequireStaffRole]
    public async Task AddTemplate(CommandContext ctx, string key, [RemainingText] string text)
    {
        bool success = await ReasonTemplateResolver.AddReplacement(key, text);
        if (success)
        {
            await ctx.RespondAsync($"Template `{key}` hinzugefügt.");
        }
        else
        {
            await ctx.RespondAsync($"Template `{key}` existiert bereits.");
        }
    }

    [Command("remove")]
    [RequireStaffRole]
    public async Task RemoveTemplate(CommandContext ctx, string key)
    {
        bool success = await ReasonTemplateResolver.RemoveReplacement(key);
        if (success)
        {
            await ctx.RespondAsync($"Template `{key}` entfernt.");
        }
        else
        {
            await ctx.RespondAsync($"Template `{key}` existiert nicht.");
        }
    }
}