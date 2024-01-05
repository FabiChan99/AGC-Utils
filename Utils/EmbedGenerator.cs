namespace AGC_Management.Utils;

public static class EmbedGenerator
{
    public static DiscordEmbedBuilder GetErrorEmbed(string description)
    {
        return new DiscordEmbedBuilder()
            .WithDescription(description)
            .WithTitle("Error")
            .WithFooter("Ups. Irgendwas stimmt hier nicht!",
                "https://cdn.discordapp.com/emojis/755048875965939833.webp")
            .WithColor(new DiscordColor(255, 69, 58));
    }
}