using AGC_Management.Services;
using AGC_Management.Services.DatabaseHandler;
using AGC_Management.Tasks;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Exceptions;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using CatBox.NET;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace AGC_Management;

internal class Program : BaseCommandModule
{
    private static void Main(string[] args)
    {
        MainAsync().GetAwaiter().GetResult();
    }

    private static async Task MainAsync()
    {
        bool DebugMode;
        try
        {
            DebugMode = bool.Parse(BotConfig.GetConfig()["MainConfig"]["DebugMode"]);
        }
        catch
        {
            DebugMode = false;
        }

        string DcApiToken = "";
        try
        {
            DcApiToken = DebugMode
                ? BotConfig.GetConfig()["MainConfig"]["Discord_API_Token_DEB"]
                : BotConfig.GetConfig()["MainConfig"]["Discord_API_Token"];
        }
        catch
        {
            try
            {
                DcApiToken = BotConfig.GetConfig()["MainConfig"]["Discord_API_Token"];
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
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        var serviceProvider = new ServiceCollection()
            .AddCatBoxServices(f => f.CatBoxUrl = new Uri("https://catbox.moe/user/api.php"))
            .AddLogging(lb => lb.AddSerilog())
            .BuildServiceProvider();

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
            Locale = "de",
            ServiceProvider = serviceProvider,
            MessageCacheSize = 10000
        });
        discord.RegisterEventHandlers(Assembly.GetExecutingAssembly());
        var commands = discord.UseCommandsNext(new CommandsNextConfiguration
        {
            PrefixResolver = GetPrefix,
            EnableDms = false,
            EnableMentionPrefix = true,
            IgnoreExtraArguments = true,
            EnableDefaultHelp = bool.Parse(BotConfig.GetConfig()["MainConfig"]["EnableBuiltInHelp"])
        });
        discord.ClientErrored += Discord_ClientErrored;
        discord.UseInteractivity(new InteractivityConfiguration
        {
            Timeout = TimeSpan.FromMinutes(2)
        });
        commands.RegisterCommands(Assembly.GetExecutingAssembly());
        commands.CommandErrored += Commands_CommandErrored;
        await discord.ConnectAsync();

        await StartTasks(discord);
        await LavalinkHandler.InitLavalink(discord);
        await Task.Delay(-1);
    }

    private static Task StartTasks(DiscordClient discord)
    {
        //// start Warn Expire Task
        ModerationSystemTasks MST = new();
        MST.StartRemovingWarnsPeriodically(discord);

        //// start TempVC Check Task
        TempVoiceTasks TVT = new();
        TVT.StartRemoveEmptyTempVoices(discord);
        return Task.CompletedTask;
    }


    private static Task<int> GetPrefix(DiscordMessage message)
    {
        return Task.Run(() =>
        {
            string prefix;
            if (GlobalProperties.DebugMode)
                prefix = "!!!";
            else
                try
                {
                    prefix = BotConfig.GetConfig()["MainConfig"]["BotPrefix"];
                }
                catch
                {
                    prefix = "!!!"; //Fallback Config
                }

            int CommandStart = -1;
            CommandStart = message.GetStringPrefixLength(prefix);
            return CommandStart;
        });
    }


    private static Task Discord_ClientErrored(DiscordClient sender, ClientErrorEventArgs e)
    {
        sender.Logger.LogError($"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}");
        sender.Logger.LogError($"Stacktrace: {e.Exception.GetType()}: {e.Exception.StackTrace}");
        return Task.CompletedTask;
    }

    private static Task Commands_CommandErrored(CommandsNextExtension cn, CommandErrorEventArgs e)
    {
        // check if error is DisCatSharp.CommandsNext.Exceptions.CommandNotFoundException
        if (e.Exception is CommandNotFoundException)
            // ignore the error silently
            return Task.CompletedTask;
        cn.Client.Logger.LogError($"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}");
        cn.Client.Logger.LogError($"Exception occured: {e.Exception.GetType()}: {e.Exception.StackTrace}");

        return Task.CompletedTask;
    }
}

public static class GlobalProperties
{
    // Server Staffrole ID
    public static ulong StaffRoleId { get; } = ulong.Parse(BotConfig.GetConfig()["ServerConfig"]["StaffRoleId"]);

    // Debug Mode
    public static bool DebugMode { get; } = ParseBoolean(BotConfig.GetConfig()["MainConfig"]["DebugMode"]);

    // Bot Owner ID
    public static ulong BotOwnerId { get; } = ulong.Parse(BotConfig.GetConfig()["MainConfig"]["BotOwnerId"]);

    private static bool ParseBoolean(string boolString)
    {
        if (bool.TryParse(boolString, out bool parsedBool))
            return parsedBool;
        return false;
    }
}