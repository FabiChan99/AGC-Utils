#region

using AGC_Management.Attributes;
using AGC_Management.Services;
using AGC_Management.Utils;
using AGC_Management.Utils.TempVoice;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Interactivity.Extensions;

#endregion

namespace AGC_Management.Commands.TempVC;

public sealed class ChannelInfoCommand : TempVoiceHelper
{
    [Command("vcinfo")]
    [Aliases("voiceinfo", "voice-info", "vc-info")]
    [RequireDatabase]
    public async Task VoiceInfo(CommandContext ctx, DiscordChannel channel = null)
    {
        if (channel == null)
        {
            channel = ctx.Member?.VoiceState?.Channel;
        }

        var userchannel = (long?)channel?.Id;
        var db_channels = await GetAllChannelIDsFromDB();
        if (userchannel == null)
        {
            await ctx.RespondAsync(
                "<:attention:1085333468688433232> Du musst in einem Channel sein, um diesen Befehl auszuführen!");
            return;
        }

        if (!db_channels.Contains((long)userchannel) && userchannel != null)
        {
            await ctx.RespondAsync(
                "<:attention:1085333468688433232> Der Aktuelle Voice Channel ist kein Custom Channel");
            return;
        }

        if (db_channels.Contains((long)userchannel) && userchannel != null)
        {
            long? channelownerid = await GetChannelOwnerID(channel);
            var channellimit = channel.UserLimit;
            DiscordMember channelowner = await ctx.Guild.GetMemberAsync((ulong)channelownerid);
            string channelname = channel.Name;
            var channel_timestamp = channel.CreationTimestamp;
            var channel_created = channel_timestamp.UtcDateTime;
            var rendered_channel_timestamp = channel_created.Timestamp();
            DiscordRole default_role = ctx.Guild.EveryoneRole;
            var yesemote = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
            var noemote = DiscordEmoji.FromName(ctx.Client, ":x:");
            var overwrites = channel.PermissionOverwrites.Select(x => x.ConvertToBuilder()).ToList();
            bool locked = false;
            bool hidden = false;
            var overwrite =
                channel.PermissionOverwrites.FirstOrDefault(o => o.Id == default_role.Id);
            if (overwrite?.CheckPermission(Permissions.UseVoice) == PermissionLevel.Denied)
            {
                locked = true;
            }

            if (overwrite == null || overwrite?.CheckPermission(Permissions.UseVoice) == PermissionLevel.Unset)
            {
                locked = false;
            }

            if (overwrite?.CheckPermission(Permissions.AccessChannels) == PermissionLevel.Denied)
            {
                hidden = true;
            }

            if (overwrite == null ||
                overwrite?.CheckPermission(Permissions.AccessChannels) == PermissionLevel.Unset)
            {
                hidden = false;
            }

            var hiddenemote = hidden ? yesemote : noemote;
            var lockedemote = locked ? yesemote : noemote;


            string climit = (channellimit == 0) ? "∞" : channellimit.ToString();

            string lreach = "";

            string soundboardac = "";
            var sbstate = GetSoundboardState(channel);
            if (sbstate)
            {
                soundboardac = yesemote;
            }
            else if (!sbstate)
            {
                soundboardac = noemote;
            }


            if (channellimit == channel.Users.Count && channellimit != 0)
            {
                lreach = yesemote;
            }

            if (channellimit < channel.Users.Count && channellimit != 0)
            {
                lreach = yesemote;
            }

            if (channellimit > channel.Users.Count)
            {
                lreach = noemote;
            }

            if (channellimit == 0)
            {
                lreach = "Kein Limit gesetzt";
            }

            List<string> Query = new()
            {
                "userid"
            };
            Dictionary<string, object> WhereCondiditons = new()
            {
                { "userid", (long)channelownerid }
            };

            string sessionemote = noemote;
            var usersession = await DatabaseService.SelectDataFromTable("tempvoicesession", Query, WhereCondiditons);
            if (usersession.Count > 0)
            {
                sessionemote = yesemote;
            }

            var ebb = new DiscordEmbedBuilder()
                .WithDescription(
                    $"**• Name des Channels** = ``{channelname}``\n" +
                    $"**• ID des Channels** = ``{channel.Id}``\n" +
                    $"**• Eigentümer** = {channelowner.Mention} ``({channelowner.Id})``\n" +
                    $"**• Useranzahl im VC** = ``{channel.Users.Count}``\n" +
                    $"**• Userlimit des VC's** = ``{climit}``\n" +
                    $"**• Limit des Channels erreicht** = {lreach}\n" +
                    $"**• Soundboard aktiv** = {soundboardac}\n" +
                    $"**• Erstellzeit** = {rendered_channel_timestamp}\n" +
                    $"**• Aktuelle Bitrate** = ``{channel.Bitrate} kbps``\n" +
                    $"**• Channel Versteckt** = {hiddenemote}\n" +
                    $"**• Channel Gesperrt** = {lockedemote}\n" +
                    $"**• Channelowner hat Session** = {sessionemote}")
                .WithColor(BotConfig.GetEmbedColor()).WithTitle("Voice Channel Informationen")
                //.WithThumbnail("https://cdn3.emoji.gg/emojis/2378-discord-voice-channel.png")
                .WithFooter($"{ctx.User.UsernameWithDiscriminator}");
            var caseid = Helpers.GenerateCaseID();
            List<DiscordButtonComponent> buttons = new(2)
            {
                new DiscordButtonComponent(ButtonStyle.Secondary, $"get_vcinfo_{caseid}",
                    "Info über Zugelassene oder Blockierte User (Nur Channelowner)")
            };


            var mb = new DiscordMessageBuilder()
                .WithEmbed(ebb).AddComponents(buttons);
            var omb = new DiscordMessageBuilder()
                .WithEmbed(ebb);
            DiscordMessage msg = await ctx.RespondAsync(mb);
            var interactivity = ctx.Client.GetInteractivity();
            var results = await interactivity.WaitForButtonAsync(msg, channelowner, TimeSpan.FromMinutes(3));
            if (results.TimedOut)
            {
                await msg.ModifyAsync(omb);
                return;
            }

            if (results.Result.Id == $"get_vcinfo_{caseid}")
            {
                string blocklist = string.Empty;
                string permitlist = string.Empty;
                var buserow = channel.PermissionOverwrites
                    .Where(x => x.Type == OverwriteType.Member)
                    .Where(x => x.CheckPermission(Permissions.UseVoice) == PermissionLevel.Denied).Select(x => x.Id)
                    .ToList();

                var puserow = channel.PermissionOverwrites
                    .Where(x => x.CheckPermission(Permissions.UseVoice) == PermissionLevel.Allowed)
                    .Where(x => x.Type == OverwriteType.Member)
                    .Where(x => x.Id != ctx.User.Id)
                    .Select(x => x.Id)
                    .ToList();
                foreach (var user in buserow)
                {
                    var member = await ctx.Guild.GetMemberAsync(user);
                    blocklist += $"{member.UsernameWithDiscriminator} {member.Mention} ``({member.Id})``\n";
                }

                foreach (var user in puserow)
                {
                    var member = await ctx.Guild.GetMemberAsync(user);
                    permitlist += $"{member.UsernameWithDiscriminator} {member.Mention} ``({member.Id})``\n";
                }

                if (buserow.Count == 0)
                {
                    blocklist = "Keine User blockiert";
                }

                if (puserow.Count == 0)
                {
                    permitlist = "Keine User zugelassen";
                }

                var emb = new DiscordEmbedBuilder().WithDescription("__Zugelassene User:__\n" +
                                                                    $"{permitlist}\n\n" +
                                                                    "__Blockierte User:__\n" +
                                                                    $"{blocklist}").WithColor(BotConfig.GetEmbedColor())
                    .WithTitle("Kanal Permit und Block Liste").WithFooter($"{ctx.User.UsernameWithDiscriminator}");
                var mbb = new DiscordInteractionResponseBuilder
                {
                    IsEphemeral = true
                }.AddEmbed(emb);

                await results.Result.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    mbb);
                await msg.ModifyAsync(omb);
            }
        }
    }
}