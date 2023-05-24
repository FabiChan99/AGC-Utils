using System.Reflection;
using AGC_Management.Commands;
using AGC_Management.Services.DatabaseHandler;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Extensions;
using IniParser;
using IniParser.Model;
using Microsoft.Extensions.Logging;

namespace AGC_Management;

internal class Program : BaseCommandModule
{
    private static void Main(string[] args)
    {
        MainAsync().GetAwaiter().GetResult();
    }

    private static async Task MainAsync()
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
            Console.WriteLine(
                "Die Konfigurationsdatei konnte nicht geladen werden.");
            Console.WriteLine("Fehlermeldung: " + ex.Message);
            Console.WriteLine("Drücke eine beliebige Taste um das Programm zu beenden.");
            Console.ReadKey();
            Environment.Exit(0);
        }

        iniData = parser.ReadFile("config.ini");
        var DebugMode = bool.Parse(iniData["MainConfig"]["DebugMode"]);
        string DcApiToken = "";
        try
        {
            DcApiToken = DebugMode
                ? iniData["MainConfig"]["Discord_API_Token_DEB"]
                : iniData["MainConfig"]["Discord_API_Token_REL"];
        }
        catch
        {
            try
            {
                DcApiToken = iniData["MainConfig"]["Discord_API_Token_REL"];
            }
            catch
            {
                Console.WriteLine(
                    "Der Discord API Token konnte nicht geladen werden.");
                Console.WriteLine("Drücke eine beliebige Taste um das Programm zu beenden.");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }


        DatabaseService.OpenConnection();
        var discord = new DiscordClient(new DiscordConfiguration
        {
            Token = DcApiToken,
            TokenType = TokenType.Bot,
            AutoReconnect = true,
            MinimumLogLevel = LogLevel.Debug,
            Intents = DiscordIntents.All,
            LogTimestampFormat = "MMM dd yyyy - HH:mm:ss tt",
            DeveloperUserId = GlobalProperties.BotOwnerId,
            Locale = "de"
        });
        discord.RegisterEventHandlers(Assembly.GetExecutingAssembly());
        var commands = discord.UseCommandsNext(new CommandsNextConfiguration
        {
            StringPrefixes = new List<string>
            {
                "!!!"
            },
            EnableDms = false,
            EnableMentionPrefix = true
        });
        discord.ClientErrored += Discord_ClientErrored;
        discord.UseInteractivity(new InteractivityConfiguration
        {
            Timeout = TimeSpan.FromMinutes(2)
        });
        commands.RegisterCommands(Assembly.GetExecutingAssembly());
        commands.CommandErrored += Commands_CommandErrored;
        await discord.ConnectAsync();

        // Start the Task to remove warns older than 7 days
        ModerationSystemTasks instance = new();
        await instance.StartRemovingWarnsPeriodically(discord);


        await Task.Delay(-1);
    }


    private static Task Discord_ClientErrored(DiscordClient sender, ClientErrorEventArgs e)
    {
        sender.Logger.LogError($"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}");
        return Task.CompletedTask;
    }

    private static Task Commands_CommandErrored(CommandsNextExtension cn, CommandErrorEventArgs e)
    {
        cn.Client.Logger.LogError($"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}");
        return Task.CompletedTask;
    }
}

public class GlobalProperties
{
    // IniReader
    private static readonly FileIniDataParser parser = new();
    public static IniData ConfigIni = parser.ReadFile("config.ini");

    // Default Embed Color
    public static DiscordColor EmbedColor { get; } = new(ConfigIni["EmbedConfig"]["DefaultEmbedColor"]);

    // Server Staffrole ID
    public static ulong StaffRoleId { get; } = ulong.Parse(ConfigIni["ServerConfig"]["StaffRoleId"]);

    // Server Staffrole Name
    public static string StaffRoleName { get; } = ConfigIni["ServerConfig"]["StaffRoleName"];

    // Servername Initals for embeds
    public static string ServerNameInitals { get; } = ConfigIni["ServerConfig"]["ServerNameInitials"];

    // Debug Mode
    public static bool DebugMode { get; } = bool.Parse(ConfigIni["MainConfig"]["DebugMode"]);

    // Bot Owner ID
    public static ulong BotOwnerId { get; } = ulong.Parse(ConfigIni["MainConfig"]["BotOwnerId"]);
}