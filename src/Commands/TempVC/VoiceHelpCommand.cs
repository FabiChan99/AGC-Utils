using AGC_Management.Utils.TempVoice;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;

namespace AGC_Management.Commands.TempVC;

public sealed class VoiceHelpCommand : TempVoiceHelper
{
    [Command("vchelp")]
    [Aliases("voicehelp", "voice-help", "vc-help")]
    public async Task VoiceHelp(CommandContext ctx)
    {
        var eb = new DiscordEmbedBuilder();
        string prefix = BotConfig.GetConfig()["MainConfig"]["BotPrefix"];
        string helpstring = "__**Grundbefehle:**__\n" +
                            $"> ``{prefix}hide`` - Macht den aktuellen Channel unsichtbar¹\n" +
                            $"> ``{prefix}unhide`` - Macht den aktuellen Channel sichtbar¹\n" +
                            $"> ``{prefix}lock`` - Sperrt den aktuellen Channel¹\n" +
                            $"> ``{prefix}vcstatus [set <name> | remove]`` - Setzt oder entfernt den Channelstatus¹\n" +
                            $"> ``{prefix}unlock`` - Entsperrt den aktuellen Channel¹\n" +
                            $"> ``{prefix}vckick @user/id`` - Kickt einen User aus dem Channel¹\n" +
                            $"> ``{prefix}block @user/id`` - Blockt einen User aus dem Channel¹\n" +
                            $"> ``{prefix}claim`` - Claimt den aktuellen Channel, wenn sich der Besitzer nicht mehr im Channel befindet\n" +
                            $"> ``{prefix}unblock @user/id`` - Macht die Blockierung eines Users rückgängig¹\n" +
                            $"> ``{prefix}transfer @user/id`` - Transferiert den Channeleigentümer\n" +
                            $"> ``{prefix}permit @user/id`` - Whitelistet einen User für einen Channel¹\n" +
                            $"> ``{prefix}unpermit @user/id`` - Macht das Whitelisting eines Users rückgängig¹\n" +
                            $"> ``{prefix}limit 0 - 99`` -  Setzt das Userlimit für den Channel (0 = Unlimited)¹\n" +
                            $"> ``{prefix}rename name`` - Verändert den Namen des Channels¹\n" +
                            $"> ``{prefix}togglesoundboard`` - Aktiviert oder Deaktiviert das VC Soundboard\n" +
                            $"> ``{prefix}vcinfo [optional <channelid>]`` - Zeigt ausführliche Infos über einen Channel an wie z.b. Eigentümer\n" +
                            $"> ``{prefix}joinrequest @user/id`` - Stellt eine Beitrittsanfrage an einen User\n" +
                            "\n" +
                            "**__Sitzungsverwaltung:__** (Persistente Kanäle)\n" +
                            $"> ``{prefix}session save`` - Speichert ein Abbild des Channels in der Datenbank\n" +
                            $"> ``{prefix}session read`` - Zeigt die aktuell gespeicherte Sitzung an\n" +
                            $"> ``{prefix}session delete`` -  Löscht die gespeicherte Sitzung\n" +
                            "\n" +
                            "**__Kanalmodverwaltung:__** (Mehrere Channelowner)\n" +
                            $"> ``{prefix}channelmod add @user/id`` - Ernennnt einen User zu einem Kanalmoderator\n" +
                            $"> ``{prefix}channelmod remove @user/id`` - Entfernt einen Kanalmoderator\n" +
                            $"> ``{prefix}channelmod reset`` - Entfernt alle Kanalmoderatoren\n" +
                            $"> ``{prefix}channelmod list`` - Listet alle Kanalmoderatoren auf\n\n"
                            + "¹ Funktion kann auch von einem Kanalmoderator ausgeführt werden.\n\n" +
                            "*Sollte etwas unklar sein, kannst du ein Ticket in <#826083443489636372> öffnen.*";

        eb.WithTitle("Temp-VC Commands");
        eb.WithDescription(helpstring);
        eb.WithColor(BotConfig.GetEmbedColor());
        await ctx.Channel.SendMessageAsync(eb);
    }
}