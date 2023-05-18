using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.Common.Utilities;
using DisCatSharp.Enums;
using IniParser;
using IniParser.Model;
using AGC_Management.Services.DatabaseHandler;
using AGC_Management.Commands.Moderation;
using DisCatSharp.Entities;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Extensions;

namespace AGC_Management
{
    class Program : BaseCommandModule
    {
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            var parser = new FileIniDataParser();
            IniData iniData;
            try
            {
                iniData = parser.ReadFile("config.ini");
            }
            catch (Exception ex)
            {
                // handle if no Ini present
                Console.WriteLine("Die Konfigurationsdatei konnte nicht gefunden werden, bitte lade das neueste Release herunter und verwende die .ini Datei.");
                Console.WriteLine("Fehlermeldung: " + ex.Message);
                Console.WriteLine("Drücke eine beliebige Taste um das Programm zu beenden.");
                Console.ReadKey();
                Environment.Exit(0);
            }
            iniData = parser.ReadFile("config.ini");
            bool DebugMode = bool.Parse(iniData["MainConfig"]["DebugMode"]);
            string DcApiToken;
            if (DebugMode)
            {
                DcApiToken = iniData["MainConfig"]["Discord_API_Token_DEB"];
            }
            else
            {
                DcApiToken = iniData["MainConfig"]["Discord_API_Token_REL"];
            }
            DatabaseService.OpenConnection();
            var discord = new DiscordClient(new DiscordConfiguration
            {
                Token = DcApiToken,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug,
                Intents = DiscordIntents.All,
                LogTimestampFormat = "MMM dd yyyy - HH:mm:ss tt"
            });
            discord.RegisterEventHandlers(Assembly.GetExecutingAssembly());
            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new List<string>() { "!!!" }
            });
            var interactivity = discord.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromMinutes(2)
            });
            commands.RegisterCommands(Assembly.GetExecutingAssembly());
            await discord.ConnectAsync();
            await Task.Delay(-1);
            
        }
    }




    public class GlobalProperties
    {
        // IniReader
        private static FileIniDataParser parser = new FileIniDataParser();
        public static IniData ConfigIni = parser.ReadFile("config.ini");

        // Default Embed Color
        public static DiscordColor EmbedColor { get; } = new DiscordColor(ConfigIni["EmbedConfig"]["DefaultEmbedColor"]);

        // Server Staffrole ID
        public static ulong StaffRoleId { get; } = ulong.Parse(ConfigIni["ServerConfig"]["StaffRoleId"]);

        // Server Staffrole Name
        public static string StaffRoleName { get; } = ConfigIni["ServerConfig"]["StaffRoleName"];

        // Servername Initals for embeds
        public static string ServerNameInitals { get; } = ConfigIni["ServerConfig"]["ServerNameInitials"];

        // Debug Mode
        public static bool DebugMode { get; } = bool.Parse(ConfigIni["MainConfig"]["DebugMode"]);
    }

}

