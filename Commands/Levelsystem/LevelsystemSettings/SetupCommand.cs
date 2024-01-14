using DisCatSharp.ApplicationCommands.Attributes;

namespace AGC_Management.Commands.Levelsystem;

public partial class LevelSystemSettings
{
    [ApplicationCommandRequirePermissions(Permissions.Administrator)]
    [SlashCommand("setup", "Richtet das Levelsystem ein", defaultMemberPermissions:(long)Permissions.Administrator)]
    public static async Task SetupLevelcommand(CommandContext ctx)
    {
        // implement
    }
}