using System.Text;
using DisCatSharp.ApplicationCommands;

namespace AGC_Management.Eventlistener;

[EventHandler]
public class OnApplyComponentInteraction : BaseCommandModule
{
    [Event]
    public async Task ComponentInteractionCreated(DiscordClient client, ComponentInteractionCreateEventArgs args)
    {
        _ = Task.Run(async () =>
            {
                string cid = args.Interaction.Data.CustomId;
                var values = args.Interaction.Data.Values;
                if (cid != "applypanelselector") return;
                var randomid = new Random();
                var ncid = randomid.Next(100000, 999999).ToString();
                DiscordInteractionModalBuilder modal = new();
                var pos = $"{values[0].ToString().Substring(0, 1).ToUpper()}{values[0].ToString().Substring(1)}";
                var applicable = await isApplicable(pos.ToLower());
                Console.WriteLine(applicable);
                if (!applicable)
                {
                    var embed = new DiscordEmbedBuilder();
                    embed.WithTitle("Bewerbung");
                    embed.WithDescription($"Die Position ``{pos}`` ist aktuell nicht bewerbbar.");
                    embed.WithColor(DiscordColor.Red);
                    await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral());
                    return;
                }
                modal.WithTitle($"Bewerbung für ");
                modal.CustomId = cid;
                modal.AddTextComponent(new DiscordTextComponent(TextComponentStyle.Paragraph,
                    label: "Bewerbungstext", minLength: 200, maxLength: 4000, required: true, placeholder:"Geb hier deinen Bewerbungstext ein"));
                modal.CustomId = "bewerben_" + $"{pos}";
                

                
                await args.Interaction.CreateInteractionModalResponseAsync(modal);
                
            }
        );
        
        _ = Task.Run(async () =>
            {
                var cid = args.Interaction.Data.CustomId;
                if (!cid.StartsWith("bewerben_")) return;
                var why = args.Interaction.Data.Components[0].Value;
                var randomid = new Random();
                var ncid = randomid.Next(100000, 999999).ToString();
                var embed = new DiscordEmbedBuilder();
                var str = new StringBuilder();
                str.AppendLine(why);
                var str_as_base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(str.ToString()));
                var position = cid.Split("_")[1];   
                embed.WithTitle("Bewerbung");
                embed.WithDescription("Deine Bewerbung wurde erfolgreich abgeschickt!");
                embed.WithColor(DiscordColor.Gold);
                
                var unixnow = DateTimeOffset.Now.ToUnixTimeSeconds();
                
                //                 "CREATE TABLE IF NOT EXISTS bewerbungen (bewerbungsid TEXT, userid BIGINT, positionname TEXT, status INTEGER DEFAULT 0, timestamp BIGINT, bewerbungstext TEXT, seenby BIGINT[] DEFAULT '{}')"

                var db = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
                await using var command = db.CreateCommand("INSERT INTO bewerbungen (bewerbungsid, userid, positionname, status, timestamp, bewerbungstext) VALUES (@bewerbungsid, @userid, @positionname, @status, @timestamp, @bewerbungstext)");
                command.Parameters.AddWithValue("bewerbungsid", $"{position}_{ncid}");
                command.Parameters.AddWithValue("userid", (long)args.User.Id);
                command.Parameters.AddWithValue("positionname", position);
                command.Parameters.AddWithValue("status", 0);
                command.Parameters.AddWithValue("timestamp", unixnow);
                command.Parameters.AddWithValue("bewerbungstext", str_as_base64);
                await command.ExecuteNonQueryAsync();
                
                
                
            }
        );
        
    }
    
    
    
    
    private static async Task<bool> isApplicable(string Position)
    {
        var db = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
        await using var command = db.CreateCommand("SELECT applicable FROM applicationcategories WHERE positionid = @positionname");
        command.Parameters.AddWithValue("positionname", Position);
        await using var reader = await command.ExecuteReaderAsync();
        if (reader.HasRows)
        {
            await reader.ReadAsync();
            return reader.GetBoolean(0);
        }
        else
        {
            return false;
        }
    }
}