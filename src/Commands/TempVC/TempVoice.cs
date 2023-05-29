using AGC_Management.Helpers.TempVoice;
using AGC_Management.Services.DatabaseHandler;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using Sentry;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AGC_Management.Commands.TempVC;


[EventHandler]
public class TempVCEventHandler : TempVoiceHelper
{
    [Event]
    private async Task VoiceStateUpdated(object sender, VoiceStateUpdateEventArgs e)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                //if (e.Guild.Id != ulong.Parse(BotConfig.GetConfig()["ServerConfig"]["ServerId"])) return;
                if (e.Guild.Id != 826878963354959933) return;

                var sessionresult = new List<Dictionary<string, object>>();
                var usersession = new List<dynamic>();
                List<string> Query = new()
                {
                    "userid", "channelname", "channelbitrate", "channellimit",
                    "blockedusers", "permitedusers", "locked", "hidden"
                };
                Dictionary<string, object> WhereCondiditons = new()
                {
                    { "userid", (long)e.User.Id }
                };
                sessionresult = await DatabaseService.SelectDataFromTable("tempvoicesession", Query, WhereCondiditons);
                if (sessionresult.Count == 0)
                {
                    List<long> all_channels = await GetAllTempChannels();
                    if ((e.Before?.Channel != null && e.After?.Channel == null) ||(e.Before?.Channel != null && e.After?.Channel != null))
                    {
                        if (all_channels.Contains((long)e.Before.Channel.Id))
                        {
                            if (e.Before.Channel.Users.Count == 0)
                            {
                                try
                                {
                                    Dictionary<string, (object value, string comparisonOperator)>
                                        DeletewhereConditions = new()
                                        {
                                            { "channelid", ((long)e.Before.Channel.Id, "=")}
                                        };
                                    
                                    await e.Before.Channel.DeleteAsync();
                                    await DatabaseService.DeleteDataFromTable("tempvoice", DeletewhereConditions);
                                }
                                catch (DisCatSharp.Exceptions.NotFoundException)
                                {
                                    Dictionary<string, (object value, string comparisonOperator)>
                                        DeletewhereConditions = new()
                                        {
                                            { "channelid", ((long)e.Before.Channel.Id, "=")}
                                        };

                                    await DatabaseService.DeleteDataFromTable("tempvoice", DeletewhereConditions);
                                }
                            }
                        }
                    }
                    if ((e.After?.Channel != null && e.Before?.Channel == null) || (e.Before?.Channel != null && e.After?.Channel != null))
                    {
                        ulong creationChannelId;
                        if (ulong.TryParse(GetVCConfig("Creation_Channel_ID"), out creationChannelId))
                        {
                            if (e.After.Channel.Id == creationChannelId)
                            {

                                DiscordChannel voice = await e.After?.Guild.CreateVoiceChannelAsync
                                    ($"{e.After?.User.UsernameWithDiscriminator}'s Tisch", e.After.Channel.Parent, default, null);
                                Dictionary<string, object> data = new()
                                {
                                    { "ownerid", (long)e.User.Id },
                                    { "channelid", (long)voice.Id },
                                    { "lastedited", (long)0 }
                                };
                                await DatabaseService.InsertDataIntoTable("tempvoice", data);
                                DiscordMember m = await e.Guild.GetMemberAsync(e.User.Id);
                                await voice.ModifyAsync(async x =>
                                {
                                    x.PermissionOverwrites = new List<DiscordOverwriteBuilder>
                                    {
                                        new DiscordOverwriteBuilder()
                                            .For(m)
                                            .Allow(Permissions.MoveMembers)
                                            .Allow(Permissions.ManageChannels)
                                            .Allow(Permissions.AccessChannels)
                                            .Allow(Permissions.UseVoice)
                                    };
                                });
                                await m.ModifyAsync(x => x.VoiceChannel = voice);

                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        });
    }
}


public class TempVoice : TempVoiceHelper
{
    private static List<ulong> LevelRoleIDs = new List<ulong>()
    {
        750402390691152005, 798562254408777739, 750450170189185024, 798555933089071154,
        750450342474416249, 750450621492101280, 798555135071617024, 751134108893184072,
        776055585912389673, 750458479793274950, 798554730988306483, 757683142894157904,
        810231454985486377, 810232899713630228, 810232892386705418
    };

    private static List<string> lookup = new List<string>()
    {
        "5+", "10+", "15+", "20+", "25+", "30+", "35+", "40+", "45+", "50+", "60+", "70+", "80+", "90+", "100+"
    };


}



/*  foreach (var item in sessionresult)
                    {
                        long userId = (long)item["userid"];
                        string channelName = (string)item["channelname"];
                        int channelBitrate = (int)item["channelbitrate"];
                        int channelLimit = (int)item["channellimit"];
                        string blockedusers = (string)item["blockedusers"];
                        string permitedusers = (string)item["permitedusers"];
                        bool locked = (bool)item["locked"];
                        bool hidden = (bool)item["hidden"];
                    }
*/