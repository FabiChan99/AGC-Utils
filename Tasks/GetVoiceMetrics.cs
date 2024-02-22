#region

using AGC_Management.Enums;
using AGC_Management.Services;
using AGC_Management.Utils;

#endregion

namespace AGC_Management.Tasks;

public static class GetVoiceMetrics
{
    public static async Task LaunchLoops()
    {
        await Run();
    }

    private static async Task Run()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        while (true)
        {
            long lastrecals = 0;
            try
            {
                var s = await CachingService.GetCacheValue(CustomDatabaseCacheType.ConfigCache, "lastvcmetricsgather");
                lastrecals = long.Parse(s);
            }
            catch
            {
                // ignored
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var difference = now - lastrecals;
            
            if (difference >= 60)
            {
                if (CurrentApplication.TargetGuild == null) // check init
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    continue;
                }
                

                var guild = CurrentApplication.TargetGuild;
                var db = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
                foreach (var channel in guild.Channels.Values)
                {
                    try
                    {
                        // only allow voice and stage channels
                        if (channel.Type == ChannelType.Voice || channel.Type == ChannelType.Stage)
                        {
                            var vcmembers = channel.Users;

                        foreach (var vcm in vcmembers)
                        {
                            if (vcm == null)
                            {
                                continue;
                            }
                            

                            

                            var voicestate = vcm?.VoiceState;
                            if (voicestate == null)
                            {
                                continue;
                            }

                            var state = Statemapper(voicestate);
                            var userid = vcm.Id;
                            var channelid = channel.Id;
                            var voicestateint = (int)state;
                            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            bool isbot = vcm.IsBot;

                            await using var cmd = db.CreateCommand(
                                "INSERT INTO metrics_voice (userid, channelid, timestamp, isbot, voicestate) VALUES (@userid, @channelid, @timestamp, @isbot ,@voicestateint)");
                            cmd.Parameters.AddWithValue("userid", (long)userid);
                            cmd.Parameters.AddWithValue("channelid", (long)channelid);
                            cmd.Parameters.AddWithValue("timestamp", timestamp);
                            cmd.Parameters.AddWithValue("isbot", isbot);
                            cmd.Parameters.AddWithValue("voicestateint", voicestateint);
                            await cmd.ExecuteNonQueryAsync();
                                
                            }   

                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }



                }
            }
            await CachingService.SetCacheValue(CustomDatabaseCacheType.ConfigCache, "lastvcmetricsgather", now.ToString());

            await Task.Delay(TimeSpan.FromSeconds(60));
        }
    }
    
    
    private static StatsVoiceStates Statemapper(DiscordVoiceState state)
    {
        if (state.IsSelfMuted)
        {
            return StatsVoiceStates.Muted;
        }
        
        if (state.IsSelfDeafened)
        {
            return StatsVoiceStates.Deafened;
        }
        
        if (state.IsServerMuted)
        {
            return StatsVoiceStates.Muted;
        }
        
        if (state.IsServerDeafened)
        {
            return StatsVoiceStates.Deafened;
        }
        
        return StatsVoiceStates.Unmuted;
    }
}