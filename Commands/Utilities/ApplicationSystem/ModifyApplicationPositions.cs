using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;

namespace AGC_Management.ApplicationSystem;

[SlashCommandGroup("modifyapplicationpositions", "Modify the application positions.", defaultMemberPermissions:(long)Permissions.Administrator)]
public sealed class ModifyApplicationPositions : ApplicationCommandsModule
{
    [SlashCommand("add", "Add a new application position.")]
    public async Task AddPosition(InteractionContext ctx, string positionName)
    {
        // Add the position to the database
    }

    [SlashCommand("remove", "Remove an application position.")]
    public async Task RemovePosition(InteractionContext ctx, string positionName)
    {
        // Remove the position from the database
    }

    [SlashCommand("modifyapplystatus", "Modify an application position to be applicable.")]
    public async Task ModifyApplyStatus(InteractionContext ctx, string positionName)
    {
        // Modify the position to be applicable
    }
}