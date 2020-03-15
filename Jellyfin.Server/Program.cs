using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Emby.Drawing;
using Emby.Server.Implementations;
using Emby.Server.Implementations.HttpServer;
using Emby.Server.Implementations.IO;
using Emby.Server.Implementations.Networking;
using Jellyfin.Drawing.Skia;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using SQLitePCL;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Jellyfin.Server
{
    /// <summary>
    /// Class containing the entry point of the application.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The name of logging configuration file containing application defaults.
        /// </summary>
        public static readonly string LoggingConfigFileDefault = "logging.default.json";

        /// <summary>
        /// The name of the logging configuration file containing the system-specific override settings.
        /// </summary>
        public static readonly string LoggingConfigFileSystem = "logging.json";

        private static readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private static readonly ILoggerFactory _loggerFactory = new SerilogLoggerFactory();
        private static ILogger _logger = NullLogger.Instance;
        private static bool _restartOnShutdown;

        /// <summary>
        /// The entry point of the application.
        /// </summary>
        /// <param name="args">The command line arguments passed.</param>
        /// <returns><see cref="Task" />.</returns>
        public static Task Main(string[] args)
        {
            // For backwards compatibility.
            // Modify any input arguments now which start with single-hyphen to POSIX standard
            // double-hyphen to allow parsing by CommandLineParser package.
            const string Pattern = @"^(-[^-\s]{2})"; // Match -xx, not -x, not --xx, not xx
            const string Substitution = @"-$1"; // Prepend with additional single-hyphen
            var regex = new Regex(Pattern);
            for (var i = 0; i < args.Length; i++)
            {
                args[i] = regex.Replace(args[i], Substitution);
            }

            // Parse the command line arguments and either start the app or exit indicating error
            return Parser.Default.ParseArguments<StartupOptions>(args)
                .MapResult(StartApp, _ => Task.CompletedTask);
        }

        /// <summary>
        /// Shuts down the application.
        /// </summary>
        internal static void Shutdown()
        {
            if (!_tokenSource.IsCancellationRequested)
            {
                _tokenSource.Cancel();
            }
        }

        /// <summary>
        /// Restarts the application.
        /// </summary>
        internal static void Restart()
        {
            _restartOnShutdown = true;

            Shutdown();
        }

        private static async Task StartApp(StartupOptions options)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            // Log all uncaught exceptions to std error
            static void UnhandledExceptionToConsole(object sender, UnhandledExceptionEventArgs e) =>
                Console.Error.WriteLine("Unhandled Exception\n" + e.ExceptionObject.ToString());
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionToConsole;

            ServerApplicationPaths appPaths = CreateApplicationPaths(options);

            // $JELLYFIN_LOG_DIR needs to be set for the logger configuration manager
            Environment.SetEnvironmentVariable("JELLYFIN_LOG_DIR", appPaths.LogDirectoryPath);

            // Create an instance of the application configuration to use for application startup
            await InitLoggingConfigFile(appPaths).ConfigureAwait(false);
            IConfiguration startupConfig = CreateAppConfiguration(appPaths);

            // Initialize logging framework
            InitializeLoggingFramework(startupConfig, appPaths);
            _logger = _loggerFactory.CreateLogger("Main");

            // Log uncaught exceptions to the logging instead of std error
            AppDomain.CurrentDomain.UnhandledException -= UnhandledExceptionToConsole;
            AppDomain.CurrentDomain.UnhandledException += (sender, e)
                => _logger.LogCritical((Exception)e.ExceptionObject, "Unhandled Exception");

            // Intercept Ctrl+C and Ctrl+Break
            Console.CancelKeyPress += (sender, e) =>
            {
                if (_tokenSource.IsCancellationRequested)
                {
                    return; // Already shutting down
                }

                e.Cancel = true;
                _logger.LogInformation("Ctrl+C, shutting down");
                Environment.ExitCode = 128 + 2;
                Shutdown();
            };

            // Register a SIGTERM handler
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                if (_tokenSource.IsCancellationRequested)
                {
                    return; // Already shutting down
                }

                _logger.LogInformation("Received a SIGTERM signal, shutting down");
                Environment.ExitCode = 128 + 15;
                Shutdown();
            };

            _logger.LogInformation(
                "Jellyfin version: {Version}",
                Assembly.GetEntryAssembly()!.GetName().Version!.ToString(3));

            ApplicationHost.LogEnvironmentInfo(_logger, appPaths);

            // Make sure we have all the code pages we can get
            // Ref: https://docs.microsoft.com/en-us/dotnet/api/system.text.codepagesencodingprovider.instance?view=netcore-3.0#remarks
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Increase the max http request limit
            // The default connection limit is 10 for ASP.NET hosted applications and 2 for all others.
            ServicePointManager.DefaultConnectionLimit = Math.Max(96, ServicePointManager.DefaultConnectionLimit);

            // Disable the "Expect: 100-Continue" header by default
            // http://stackoverflow.com/questions/566437/http-post-returns-the-error-417-expectation-failed-c
            ServicePointManager.Expect100Continue = false;

            Batteries_V2.Init();
            if (raw.sqlite3_enable_shared_cache(1) != raw.SQLITE_OK)
            {
                _logger.LogWarning("Failed to enable shared cache for SQLite");
            }

            var appHost = new CoreAppHost(
                appPaths,
                _loggerFactory,
                options,
                new ManagedFileSystem(_loggerFactory.CreateLogger<ManagedFileSystem>(), appPaths),
                GetImageEncoder(appPaths),
                new NetworkManager(_loggerFactory.CreateLogger<NetworkManager>()));
            try
            {
                ServiceCollection serviceCollection = new ServiceCollection();
                await appHost.InitAsync(serviceCollection, startupConfig).ConfigureAwait(false);

                var webHost = CreateWebHostBuilder(appHost, serviceCollection, startupConfig, appPaths).Build();

                // A bit hacky to re-use service provider since ASP.NET doesn't allow a custom service collection.
                appHost.ServiceProvider = webHost.Services;
                appHost.FindParts();
                Migrations.MigrationRunner.Run(appHost, _loggerFactory);

                try
                {
                    await webHost.StartAsync().ConfigureAwait(false);
                }
                catch
                {
                    _logger.LogError("Kestrel failed to start! This is most likely due to an invalid address or port bind - correct your bind configuration in system.xml and try again.");
                    throw;
                }

                await appHost.RunStartupTasksAsync().ConfigureAwait(false);

                stopWatch.Stop();

                _logger.LogInformation("Startup complete {Time:g}", stopWatch.Elapsed);

                // Block main thread until shutdown
                await Task.Delay(-1, _tokenSource.Token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // Don't throw on cancellation
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error while starting server.");
            }
            finally
            {
                appHost?.Dispose();
            }

            if (_restartOnShutdown)
            {
                StartNewInstance(options);
            }
        }

        private static IWebHostBuilder CreateWebHostBuilder(
            ApplicationHost appHost,
            IServiceCollection serviceCollection,
            IConfiguration startupConfig,
            IApplicationPaths appPaths)
        {
            var webhostBuilder = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    var addresses = appHost.ServerConfigurationManager
                        .Configuration
                        .LocalNetworkAddresses
                        .Select(appHost.NormalizeConfiguredLocalAddress)
                        .Where(i => i != null)
                        .ToList();
                    if (addresses.Any())
                    {
                        foreach (var address in addresses)
                        {
                            _logger.LogInformation("Kestrel listening on {IpAddress}", address);
                            options.Listen(address, appHost.HttpPort);

                            if (appHost.EnableHttps && appHost.Certificate != null)
                            {
                                options.Listen(
                                    address,
                                    appHost.HttpsPort,
                                    listenOptions => listenOptions.UseHttps(appHost.Certificate));
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Kestrel listening on all interfaces");
                        options.ListenAnyIP(appHost.HttpPort);

                        if (appHost.EnableHttps && appHost.Certificate != null)
                        {
                            options.ListenAnyIP(
                                appHost.HttpsPort,
                                listenOptions => listenOptions.UseHttps(appHost.Certificate));
                        }
                    }
                })
                .ConfigureAppConfiguration(config => config.ConfigureAppConfiguration(appPaths, startupConfig))
                .UseSerilog()
                .ConfigureServices(services =>
                {
                    // Merge the external ServiceCollection into ASP.NET DI
                    services.TryAdd(serviceCollection);
                })
                .UseStartup<Startup>();

            // Set up static content hosting unless it has been disabled via config
            if (!startupConfig.NoWebContent())
            {
                // Fail startup if the web content does not exist
                if (!Directory.Exists(appHost.ContentRoot) || !Directory.GetFiles(appHost.ContentRoot).Any())
                {
                    throw new InvalidOperationException(
                        "The server is expected to host web content, but the provided content directory is either " +
                        $"invalid or empty: {appHost.ContentRoot}. If you do not want to host web content with the " +
                        $"server, you may set the '{MediaBrowser.Controller.Extensions.ConfigurationExtensions.NoWebContentKey}' flag.");
                }

                // Configure the web host to host the static web content
                webhostBuilder.UseContentRoot(appHost.ContentRoot);
            }

            return webhostBuilder;
        }

        /// <summary>
        /// Create the data, config and log paths from the variety of inputs(command line args,
        /// environment variables) or decide on what default to use. For Windows it's %AppPath%
        /// for everything else the
        /// <a href="https://specifications.freedesktop.org/basedir-spec/basedir-spec-latest.html">XDG approach</a>
        /// is followed.
        /// </summary>
        /// <param name="options">The <see cref="StartupOptions" /> for this instance.</param>
        /// <returns><see cref="ServerApplicationPaths" />.</returns>
        private static ServerApplicationPaths CreateApplicationPaths(StartupOptions options)
        {
            // dataDir
            // IF      --datadir
            // ELSE IF $JELLYFIN_DATA_DIR
            // ELSE IF windows, use <%APPDATA%>/jellyfin
            // ELSE IF $XDG_DATA_HOME then use $XDG_DATA_HOME/jellyfin
            // ELSE    use $HOME/.local/share/jellyfin
            var dataDir = options.DataDir;
            if (string.IsNullOrEmpty(dataDir))
            {
                dataDir = Environment.GetEnvironmentVariable("JELLYFIN_DATA_DIR");

                if (string.IsNullOrEmpty(dataDir))
                {
                    // LocalApplicationData follows the XDG spec on unix machines
                    dataDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "jellyfin");
                }
            }

            // configDir
            // IF      --configdir
            // ELSE IF $JELLYFIN_CONFIG_DIR
            // ELSE IF --datadir, use <datadir>/config (assume portable run)
            // ELSE IF <datadir>/config exists, use that
            // ELSE IF windows, use <datadir>/config
            // ELSE IF $XDG_CONFIG_HOME use $XDG_CONFIG_HOME/jellyfin
            // ELSE    $HOME/.config/jellyfin
            var configDir = options.ConfigDir;
            if (string.IsNullOrEmpty(configDir))
            {
                configDir = Environment.GetEnvironmentVariable("JELLYFIN_CONFIG_DIR");

                if (string.IsNullOrEmpty(configDir))
                {
                    if (options.DataDir != null
                        || Directory.Exists(Path.Combine(dataDir, "config"))
                        || RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        // Hang config folder off already set dataDir
                        configDir = Path.Combine(dataDir, "config");
                    }
                    else
                    {
                        // $XDG_CONFIG_HOME defines the base directory relative to which
                        // user specific configuration files should be stored.
                        configDir = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");

                        // If $XDG_CONFIG_HOME is either not set or empty,
                        // a default equal to $HOME /.config should be used.
                        if (string.IsNullOrEmpty(configDir))
                        {
                            configDir = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                ".config");
                        }

                        configDir = Path.Combine(configDir, "jellyfin");
                    }
                }
            }

            // cacheDir
            // IF      --cachedir
            // ELSE IF $JELLYFIN_CACHE_DIR
            // ELSE IF windows, use <datadir>/cache
            // ELSE IF XDG_CACHE_HOME, use $XDG_CACHE_HOME/jellyfin
            // ELSE    HOME/.cache/jellyfin
            var cacheDir = options.CacheDir;
            if (string.IsNullOrEmpty(cacheDir))
            {
                cacheDir = Environment.GetEnvironmentVariable("JELLYFIN_CACHE_DIR");

                if (string.IsNullOrEmpty(cacheDir))
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        // Hang cache folder off already set dataDir
                        cacheDir = Path.Combine(dataDir, "cache");
                    }
                    else
                    {
                        // $XDG_CACHE_HOME defines the base directory relative to which
                        // user specific non-essential data files should be stored.
                        cacheDir = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");

                        // If $XDG_CACHE_HOME is either not set or empty,
                        // a default equal to $HOME/.cache should be used.
                        if (string.IsNullOrEmpty(cacheDir))
                        {
                            cacheDir = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                ".cache");
                        }

                        cacheDir = Path.Combine(cacheDir, "jellyfin");
                    }
                }
            }

            // webDir
            // IF      --webdir
            // ELSE IF $JELLYFIN_WEB_DIR
            // ELSE    <bindir>/jellyfin-web
            var webDir = options.WebDir;
            if (string.IsNullOrEmpty(webDir))
            {
                webDir = Environment.GetEnvironmentVariable("JELLYFIN_WEB_DIR");

                if (string.IsNullOrEmpty(webDir))
                {
                    // Use default location under ResourcesPath
                    webDir = Path.Combine(AppContext.BaseDirectory, "jellyfin-web");
                }
            }

            // logDir
            // IF      --logdir
            // ELSE IF $JELLYFIN_LOG_DIR
            // ELSE IF --datadir, use <datadir>/log (assume portable run)
            // ELSE    <datadir>/log
            var logDir = options.LogDir;
            if (string.IsNullOrEmpty(logDir))
            {
                logDir = Environment.GetEnvironmentVariable("JELLYFIN_LOG_DIR");

                if (string.IsNullOrEmpty(logDir))
                {
                    // Hang log folder off already set dataDir
                    logDir = Path.Combine(dataDir, "log");
                }
            }

            // Ensure the main folders exist before we continue
            try
            {
                Directory.CreateDirectory(dataDir);
                Directory.CreateDirectory(logDir);
                Directory.CreateDirectory(configDir);
                Directory.CreateDirectory(cacheDir);
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine("Error whilst attempting to create folder");
                Console.Error.WriteLine(ex.ToString());
                Environment.Exit(1);
            }

            return new ServerApplicationPaths(dataDir, logDir, configDir, cacheDir, webDir);
        }

        /// <summary>
        /// Initialize the logging configuration file using the bundled resource file as a default if it doesn't exist
        /// already.
        /// </summary>
        private static async Task InitLoggingConfigFile(IApplicationPaths appPaths)
        {
            // Do nothing if the config file already exists
            string configPath = Path.Combine(appPaths.ConfigurationDirectoryPath, LoggingConfigFileDefault);
            if (File.Exists(configPath))
            {
                return;
            }

            // Get a stream of the resource contents
            // NOTE: The .csproj name is used instead of the assembly name in the resource path
            const string ResourcePath = "Jellyfin.Server.Resources.Configuration.logging.json";
            await using Stream? resource = typeof(Program).Assembly.GetManifestResourceStream(ResourcePath)
                ?? throw new InvalidOperationException($"Invalid resource path: '{ResourcePath}'");

            // Copy the resource contents to the expected file path for the config file
            await using Stream dst = File.Open(configPath, FileMode.CreateNew);
            await resource.CopyToAsync(dst).ConfigureAwait(false);
        }

        private static IConfiguration CreateAppConfiguration(IApplicationPaths appPaths)
        {
            return new ConfigurationBuilder()
                .ConfigureAppConfiguration(appPaths)
                .Build();
        }

        private static IConfigurationBuilder ConfigureAppConfiguration(this IConfigurationBuilder config, IApplicationPaths appPaths, IConfiguration? startupConfig = null)
        {
            // Use the swagger API page as the default redirect path if not hosting the jellyfin-web content
            var inMemoryDefaultConfig = ConfigurationOptions.DefaultConfiguration;
            if (startupConfig != null && startupConfig.NoWebContent())
            {
                inMemoryDefaultConfig[HttpListenerHost.DefaultRedirectKey] = "swagger/index.html";
            }

            return config
                .SetBasePath(appPaths.ConfigurationDirectoryPath)
                .AddInMemoryCollection(inMemoryDefaultConfig)
                .AddJsonFile(LoggingConfigFileDefault, optional: false, reloadOnChange: true)
                .AddJsonFile(LoggingConfigFileSystem, optional: true, reloadOnChange: true)
                .AddEnvironmentVariables("JELLYFIN_");
        }

        /// <summary>
        /// Initialize Serilog using configuration and fall back to defaults on failure.
        /// </summary>
        private static void InitializeLoggingFramework(IConfiguration configuration, IApplicationPaths appPaths)
        {
            try
            {
                // Serilog.Log is used by SerilogLoggerFactory when no logger is specified
                Serilog.Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration)
                    .Enrich.FromLogContext()
                    .Enrich.WithThreadId()
                    .CreateLogger();
            }
            catch (Exception ex)
            {
                Serilog.Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] [{ThreadId}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                    .WriteTo.Async(x => x.File(
                        Path.Combine(appPaths.LogDirectoryPath, "log_.log"),
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{ThreadId}] {SourceContext}: {Message}{NewLine}{Exception}"))
                    .Enrich.FromLogContext()
                    .Enrich.WithThreadId()
                    .CreateLogger();

                Serilog.Log.Logger.Fatal(ex, "Failed to create/read logger configuration");
            }
        }

        private static IImageEncoder GetImageEncoder(IApplicationPaths appPaths)
        {
            try
            {
                // Test if the native lib is available
                SkiaEncoder.TestSkia();

                return new SkiaEncoder(
                    _loggerFactory.CreateLogger<SkiaEncoder>(),
                    appPaths);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skia not available. Will fallback to NullIMageEncoder.");
            }

            return new NullImageEncoder();
        }

        private static void StartNewInstance(StartupOptions options)
        {
            _logger.LogInformation("Starting new instance");

            var module = options.RestartPath;

            if (string.IsNullOrWhiteSpace(module))
            {
                module = Environment.GetCommandLineArgs()[0];
            }

            string commandLineArgsString;
            if (options.RestartArgs != null)
            {
                commandLineArgsString = options.RestartArgs ?? string.Empty;
            }
            else
            {
                commandLineArgsString = string.Join(
                    ' ',
                    Environment.GetCommandLineArgs().Skip(1).Select(NormalizeCommandLineArgument));
            }

            _logger.LogInformation("Executable: {0}", module);
            _logger.LogInformation("Arguments: {0}", commandLineArgsString);

            Process.Start(module, commandLineArgsString);
        }

        private static string NormalizeCommandLineArgument(string arg)
        {
            if (!arg.Contains(" ", StringComparison.OrdinalIgnoreCase))
            {
                return arg;
            }

            return "\"" + arg + "\"";
        }
    }
}
