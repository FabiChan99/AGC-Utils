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
            IniData iniData = parser.ReadFile("config.ini");
            string DcApiToken = iniData["MainConfig"]["Discord_API_Token"];
            DatabaseService.OpenConnection();
            var discord = new DiscordClient(new DiscordConfiguration
            {
                Token = DcApiToken,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Trace,
                Intents = DiscordIntents.AllUnprivileged,
                LogTimestampFormat = "MMM dd yyyy - HH:mm:ss tt"
            });
            discord.RegisterEventHandlers(Assembly.GetExecutingAssembly());
            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new List<string>() { "!!!" }
            });
            //commands.RegisterCommands(Assembly.GetExecutingAssembly());
            await discord.ConnectAsync();
            await Task.Delay(-1);
            
        }
    }
}