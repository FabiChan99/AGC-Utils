using DisCatSharp.Entities;
using IniParser.Model;
using IniParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGC_Management;
public static class BotConfig
{
    public static IniData GetConfig()
    {
        IniData ConfigIni;
        FileIniDataParser parser = new();
        try
        {
            ConfigIni = parser.ReadFile("config.ini");
        }
        catch
        {
            Console.WriteLine("Die Konfigurationsdatei konnte nicht geladen werden.");
            Console.WriteLine("Drücke eine beliebige Taste um das Programm zu beenden.");
            Console.ReadKey();
            Environment.Exit(0);
            return null;
        }
        return ConfigIni;
    }

    public static DiscordColor GetEmbedColor()
    {
        string fallbackColor = "000000";
        string colorstring;

        try
        {
            string colorString = GetConfig()["EmbedConfig"]["DefaultEmbedColor"];
            if (colorString.StartsWith("#"))
            {
                colorString = colorString.Remove(0, 1);
            }

            colorstring = colorString;
        }
        catch
        {
            colorstring = fallbackColor;
        }

        return new DiscordColor(colorstring);
    }

}

