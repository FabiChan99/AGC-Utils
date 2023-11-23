using AGC_Management.Attributes;
using AGC_Management.Services.DatabaseHandler;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;

namespace AGC_Management.Commands.Moderation;

public class FlagUserCommand : BaseCommandModule
{
        [Command("flag")]
        [Description("Flaggt einen Nutzer")]
        [RequireDatabase]
        [RequireStaffRole]
        [RequireTeamCat]
        public async Task FlagUser(CommandContext ctx, DiscordUser user, [RemainingText] string reason)
        {
            if (await Helpers.Helpers.CheckForReason(ctx, reason)) return;
            var caseid = Helpers.Helpers.GenerateCaseID();

            var imgExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
            var imgAttachments = ctx.Message.Attachments
                .Where(att => imgExtensions.Contains(Path.GetExtension(att.FileName).ToLower()))
                .ToList();
            string urls = "";
            if (imgAttachments.Count > 0)
            {
                urls = await Helpers.Helpers.UploadToCatBox(ctx, imgAttachments);
            }


            Dictionary<string, object> data = new()
            {
                { "userid", (long)user.Id },
                { "punisherid", (long)ctx.User.Id },
                { "datum", DateTimeOffset.Now.ToUnixTimeSeconds() },
                { "description", reason + urls },
                { "caseid", caseid }
            };
            await DatabaseService.InsertDataIntoTable("flags", data);
            var flaglist = new List<dynamic>();

            List<string> selectedFlags = new()
            {
                "*"
            };

            Dictionary<string, object> whereConditions = new()
            {
                { "userid", (long)user.Id }
            };
            List<Dictionary<string, object>> results =
                await DatabaseService.SelectDataFromTable("flags", selectedFlags, whereConditions);
            foreach (var result in results) flaglist.Add(result);


            var flagcount = flaglist.Count;

            var embed = new DiscordEmbedBuilder()
                .WithTitle("Nutzer geflaggt")
                .WithDescription(
                    $"Der Nutzer {user.UsernameWithDiscriminator} `{user.Id}` wurde geflaggt!\n Grund: ```{reason}{urls}```Der User hat nun __{flagcount} Flag(s)__. \nID des Flags: ``{caseid}``")
                .WithColor(BotConfig.GetEmbedColor())
                .WithFooter(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl).Build();
            await ctx.RespondAsync(embed);
        }
}