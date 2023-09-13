﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using AGC_Management.Helpers;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Extensions;

namespace AGC_Management.Commands
{
    public class Utilities : ApplicationCommandsModule
    {
        [RequireStaffRole]
        [ContextMenu(ApplicationCommandType.Message, "Steal Emoji")]
        public static async Task StealEmojiMessageCommand(ContextMenuContext ctx)
        {
            Console.WriteLine(ctx.TargetMessage.Content);
            DiscordMessage message = ctx.TargetMessage;
            ulong RoleId = ulong.Parse(BotConfig.GetConfig()["ServerConfig"]["StaffRoleId"]);
            if (!ctx.Member.Roles.Any(r => r.Id == RoleId))
            {
                var ib = new DiscordInteractionResponseBuilder();
                ib.IsEphemeral = true;
                ib.WithContent("Du hast nicht die benötigten Rechte um diesen Command auszuführen!");
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, ib);
                return;
            }
            if (message.Content == "")
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                                       new DiscordInteractionResponseBuilder().WithContent("Diese Nachricht hat keinen Inhalt!").AsEphemeral());
                return;
            }
            {
                string emoji;
                try
                {
                    emoji = message.Content.Split(":")[2].Split(">")[0];
                }
                catch
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                                                              new DiscordInteractionResponseBuilder().WithContent("Diese Nachricht enthält kein Emoji!").AsEphemeral());

                    return;
                }
                var randomid = new Random();
                var cid = randomid.Next(100000, 999999).ToString();
                DiscordInteractionModalBuilder modal = new();
                modal.WithTitle("Emoji Stealer");
                modal.CustomId = cid;
                modal.AddTextComponent(new DiscordTextComponent(TextComponentStyle.Small, label: "Neuer Name für den Emoji:", minLength:2, maxLength:49));

                await ctx.CreateModalResponseAsync(modal);

                var interactivity = ctx.Client.GetInteractivity();
                var result = await interactivity.WaitForModalAsync(cid, TimeSpan.FromMinutes(2));


                
                var emojiUrl = $"https://cdn.discordapp.com/emojis/{emoji}.png?v=1";
                var emojiBytes = await new HttpClient().GetByteArrayAsync(emojiUrl);
                var emojiStream = new MemoryStream(emojiBytes);

                if (emoji == null)
                {
                    await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());
                    await result.Result.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"Diese Nachricht enthält kein Emoji"));
                    return;
                }

                if (result.TimedOut)
                {
                    return;
                }
                else
                {

                    var emojiName = result.Result.Interaction.Data.Components[0].Value;

                    await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());
                    await ctx.Guild.CreateEmojiAsync(emojiName, emojiStream);
                    await result.Result.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"Emoji {emojiName} wurde erfolgreich hinzugefügt!"));
                }


            }



        }

    }
}