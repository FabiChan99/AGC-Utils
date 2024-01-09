#region

using System.Reflection;
using System.Security.Claims;
using AGC_Management;
using AGC_Management.Controller;
using AGC_Management.Providers;
using AGC_Management.Services;
using AGC_Management.Tasks;
using AGC_Management.Utils;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.EventArgs;
using DisCatSharp.ApplicationCommands.Exceptions;
using DisCatSharp.CommandsNext.Exceptions;
using DisCatSharp.Extensions.OAuth2Web;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Extensions;
using Discord.OAuth2;
using KawaiiAPI.NET;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Session;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using ILogger = Serilog.ILogger;

#endregion

namespace AGC_Management;

public class CurrentApplication
{
    public static string VersionString { get; set; } = "v2.1.0";
    public static DiscordClient DiscordClient { get; set; }
    public static DiscordGuild TargetGuild { get; set; }
    public static ILogger Logger { get; set; }
}

internal class Program : BaseCommandModule
{
    private static void Main(string[] args)
    {
        MainAsync().GetAwaiter().GetResult();
    }


    private static async Task MainAsync()
    {
        var builder = WebApplication.CreateBuilder();
        var logger = Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Discord.OAuth2", LogEventLevel.Warning)
            .WriteTo.Console()
            // errors to errorfile
            .WriteTo.File("logs/errors/error-.txt", rollingInterval: RollingInterval.Day,
                levelSwitch: new LoggingLevelSwitch(LogEventLevel.Error))
            .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, levelSwitch: new LoggingLevelSwitch())
            .CreateLogger();
        CurrentApplication.Logger = logger;

        logger.Information("Starting AGC Management Bot...");
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
                logger.Fatal(
                    "Der Discord API Token konnte nicht geladen werden.");
                logger.Fatal("Drücke eine beliebige Taste um das Programm zu beenden.");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }

        var client = new KawaiiClient();


        builder.Services.AddRazorPages();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddServerSideBlazor();
        builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog());
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<UserService>();
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });
        builder.Services.AddAuthentication(opt =>
            {
                opt.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                opt.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = DiscordDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddDiscord(x =>
            {
                x.AppId = BotConfig.GetConfig()["WebUI"]["ClientID"];
                x.AppSecret = BotConfig.GetConfig()["WebUI"]["ClientSecret"];
                x.Scope.Add("guilds");
                x.SaveTokens = true;
                x.ClaimActions.MapCustomJson(ClaimTypes.Role, element =>
                {
                    return AuthUtils.RetrieveRole(element).Result;
                });
                x.ClaimActions.MapCustomJson("FullQualifiedDiscordName", element =>
                {
                    return AuthUtils.RetrieveName(element).Result;
                });
            });
        builder.Services.AddAuthorization();
        

        var serviceProvider = new ServiceCollection()
            .AddLogging(lb => lb.AddSerilog())
            .AddSingleton(client)
            .AddSingleton<LoggingService>()
            .BuildServiceProvider();
        logger.Information("Connecting to Database...");
        var spinner = new ConsoleSpinner();
        spinner.Start();
        DatabaseService.OpenConnection();
        TicketDatabaseService.OpenConnection();
        spinner.Stop();
        logger.Information("Database connected!");
        await DatabaseService.InitializeAndUpdateDatabaseTables();
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
            MessageCacheSize = 10000,
            ShowReleaseNotesInUpdateCheck = false
        });
        discord.RegisterEventHandlers(Assembly.GetExecutingAssembly());
        var commands = discord.UseCommandsNext(new CommandsNextConfiguration
        {
            PrefixResolver = GetPrefix,
            EnableDms = false,
            EnableMentionPrefix = true,
            IgnoreExtraArguments = true,
            EnableDefaultHelp = bool.Parse(BotConfig.GetConfig()["MainConfig"]["EnableBuiltInHelp"] ?? "false")
        });
        discord.ClientErrored += Discord_ClientErrored;
        
        discord.UseInteractivity(new InteractivityConfiguration
        {
            Timeout = TimeSpan.FromMinutes(2)
        });
        commands.RegisterCommands(Assembly.GetExecutingAssembly());
        var appCommands = discord.UseApplicationCommands(new ApplicationCommandsConfiguration
        {
            ServiceProvider = serviceProvider, DebugStartup = false, EnableDefaultHelp = false
        });
        appCommands.SlashCommandErrored += Discord_SlashCommandErrored;
        appCommands.RegisterGlobalCommands(Assembly.GetExecutingAssembly());

        commands.CommandErrored += Commands_CommandErrored;
        
        await discord.ConnectAsync();
        await Task.Delay(5000);

        CurrentApplication.DiscordClient = discord;

        await StartTasks(discord);

        CurrentApplication.TargetGuild =
            await discord.GetGuildAsync(ulong.Parse(BotConfig.GetConfig()["ServerConfig"]["ServerId"]));
        _ = RunAspAsync(builder.Build());
        await Task.Delay(-1);
    }

    private static Task StartTasks(DiscordClient discord)
    {
        //// start Warn Expire Task
        ModerationSystemTasks MST = new();
        _ = MST.StartRemovingWarnsPeriodically(discord);

        //// start TempVC Check Task
        TempVoiceTasks TVT = new();
        _ = TVT.StartRemoveEmptyTempVoices(discord);

        _ = StatusUpdateTask(discord);
        _ = UpdateGuild(discord);
        _ = ExtendedModerationSystemLoop.LaunchLoops();

        return Task.CompletedTask;
    }

    private static Task StatusUpdateTask(DiscordClient discord)
    {
        return Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    await discord.UpdateStatusAsync(new DiscordActivity(
                        $"Version: {CurrentApplication.VersionString}",
                        ActivityType.Custom));
                    await Task.Delay(TimeSpan.FromSeconds(30));

                    await discord.UpdateStatusAsync(new DiscordActivity(await TicketString(), ActivityType.Custom));
                    await Task.Delay(TimeSpan.FromSeconds(30));

                    // get tempvc count
                    int tempvcCount = 0;
                    var constring = DatabaseService.GetConnectionString();
                    await using var con = new NpgsqlConnection(constring);
                    await con.OpenAsync();
                    string query = "SELECT channelid FROM tempvoice";
                    await using var cmd = new NpgsqlCommand(query, con);
                    await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
                    // get channels and fetch if they exist
                    while (reader.Read())
                    {
                        ulong channelid = (ulong)reader.GetInt64(0);
                        var channel = await discord.TryGetChannelAsync(channelid);
                        if (channel != null)
                        {
                            tempvcCount++;
                        }
                    }

                    await discord.UpdateStatusAsync(new DiscordActivity($" Offene Temp-VCs: {tempvcCount}",
                        ActivityType.Custom));
                    await Task.Delay(TimeSpan.FromSeconds(30));

                    // get membercount of agc
                    var guild = await discord.GetGuildAsync(
                        ulong.Parse(BotConfig.GetConfig()["ServerConfig"]["ServerId"]));
                    await discord.UpdateStatusAsync(new DiscordActivity($"Servermitglieder: {guild.MemberCount}",
                        ActivityType.Custom));
                    await Task.Delay(TimeSpan.FromSeconds(30));

                    // get vc user
                    int vcUsers = 0;
                    // for each channel in agc
                    foreach (var channel in guild.Channels.Values)
                    {
                        // if channel is voicechannel
                        if (channel.Type == ChannelType.Voice)
                        {
                            vcUsers += channel.Users.Count;
                        }
                    }

                    await discord.UpdateStatusAsync(new DiscordActivity($"User in VC: {vcUsers}", ActivityType.Custom));
                    await Task.Delay(TimeSpan.FromSeconds(30));
                }
                catch (Exception e)
                {
                    CurrentApplication.Logger.Error(e, "Error while updating status");
                }
            }
        });
    }


    private static async Task<string> TicketString()
    {
        int openTickets = 0;
        int closedTickets = 0;
        var con = TicketDatabaseService.GetConnection();
        string query = "SELECT COUNT(*) FROM ticketstore where closed = False";
        await using NpgsqlCommand cmd = new(query, con);
        openTickets = Convert.ToInt32(cmd.ExecuteScalar());

        string query1 = "SELECT COUNT(*) FROM ticketstore where closed = True";
        await using NpgsqlCommand cmd1 = new(query1, con);
        closedTickets = Convert.ToInt32(cmd1.ExecuteScalar());
        return $"Tickets: Offen: {openTickets} | Gesamt: {openTickets + closedTickets}";
    }


    private static async Task Discord_SlashCommandErrored(ApplicationCommandsExtension sender,
        SlashCommandErrorEventArgs e)
    {
        if (e.Exception is SlashExecutionChecksFailedException)
        {
            var ex = (SlashExecutionChecksFailedException)e.Exception;
            if (ex.FailedChecks.Any(x => x is ApplicationCommandRequireUserPermissionsAttribute))
            {
                var embed = EmbedGenerator.GetErrorEmbed(
                    "You don't have the required permissions to execute this command.");
                await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral());
                e.Handled = true;
                return;
            }

            e.Handled = true;
        }
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

    private static async Task UpdateGuild(DiscordClient client)
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        while (true)
        {
            GlobalProperties.AGCGuild =
                await client.GetGuildAsync(ulong.Parse(BotConfig.GetConfig()["ServerConfig"]["ServerId"]));
            await Task.Delay(TimeSpan.FromMinutes(5));
        }
    }

    private static async Task Discord_ClientErrored(DiscordClient sender, ClientErrorEventArgs e)
    {
        sender.Logger.LogError($"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}");
        sender.Logger.LogError($"Stacktrace: {e.Exception.GetType()}: {e.Exception.StackTrace}");
        await ErrorReporting.SendErrorToDev(sender, sender.CurrentUser, e.Exception);
    }

    private static async Task Commands_CommandErrored(CommandsNextExtension cn, CommandErrorEventArgs e)
    {
        CurrentApplication.DiscordClient.Logger.LogError(e.Exception,
            $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}");
        if (e.Exception is ArgumentException)
        {
            if (e.Exception.Message.Contains("Description length cannot exceed 4096 characters."))
            {
                DiscordEmbedBuilder web;
                web = new DiscordEmbedBuilder
                {
                    Title = "Fehler | DescriptionTooLongException",

                    Color = new DiscordColor("#FF0000")
                };
                web.WithDescription($"Das Embed hat zu viele Zeichen.\n" +
                                    $"**Stelle sicher dass die Hauptsektion nicht mehr als 4096 Zeichen hat!**");
                web.WithFooter($"Fehler ausgelöst von {e.Context.User.UsernameWithDiscriminator}");
                await e.Context.RespondAsync(embed: web, content: e.Context.User.Mention);
                return;
            }

            DiscordEmbedBuilder eb;
            eb = new DiscordEmbedBuilder
            {
                Title = "Fehler | BadArgumentException",

                Color = new DiscordColor("#FF0000")
            };
            eb.WithDescription($"Fehlerhafte Argumente.\n" +
                               $"**Stelle sicher dass alle Argumente richtig angegeben sind!**");
            eb.WithFooter($"Fehler ausgelöst von {e.Context.User.UsernameWithDiscriminator}");
            await e.Context.RespondAsync(embed: eb, content: e.Context.User.Mention);
            return;
        }

        if (e.Exception is CommandNotFoundException)
        {
            e.Handled = true;
            return;
        }

        await ErrorReporting.SendErrorToDev(CurrentApplication.DiscordClient, e.Context.User, e.Exception);

        var embed = new DiscordEmbedBuilder
        {
            Title = "Fehler | CommandErrored",
            Color = new DiscordColor("#FF0000")
        };
        embed.WithDescription($"Es ist ein Fehler aufgetreten.\n" +
                              $"**Fehler: {e.Exception.Message}**");
        embed.WithFooter($"Fehler ausgelöst von {e.Context.User.UsernameWithDiscriminator}");
        await e.Context.RespondAsync(embed: embed, content: e.Context.User.Mention);
    }

    private static async Task RunAspAsync(WebApplication app)
    {
        bool enabled;
        int port;

        try
        {
            enabled = bool.Parse(BotConfig.GetConfig()["WebUI"]["Active"]);
        }
        catch
        {
            enabled = false;
        }

        if (!enabled)
        {
            CurrentApplication.Logger.Information("WebUI is disabled.");
            return;
        }

        try
        {
            port = int.Parse(BotConfig.GetConfig()["WebUI"]["Port"]);
        }
        catch
        {
            port = 5000; // fallback
        }


        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.Urls.Add(BotConfig.GetConfig()["WebUI"]["DashboardURL"]);
        
        // bind to localhost to use a reverse proxy like nginx, apache or iis
        app.Urls.Add($"http://localhost:{port}");

        app.UseStaticFiles();
        
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        
        app.UseCookiePolicy(new CookiePolicyOptions()
        {
            MinimumSameSitePolicy = SameSiteMode.Lax
        });
        app.MapBlazorHub();
        app.MapDefaultControllerRoute();
        app.MapFallbackToPage("/_Host");

        CurrentApplication.Logger.Information("Starting WebUI on port " + port + "...");
        TempVariables.WebUiApp = app;
        await app.StartAsync();
        TempVariables.IsWebUiRunning = true;
        CurrentApplication.Logger.Information("WebUI started!");
    }


    public static class TempVariables
    {
        public static bool IsWebUiRunning { get; set; }
        public static WebApplication WebUiApp { get; set; }
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

    public static DiscordGuild AGCGuild { get; set; }

    public static ulong ErrorTrackingChannelId { get; } =
        ulong.Parse(BotConfig.GetConfig()["MainConfig"]["ErrorTrackingChannelId"]);

    private static bool ParseBoolean(string boolString)
    {
        if (bool.TryParse(boolString, out bool parsedBool))
            return parsedBool;
        return false;
    }
}