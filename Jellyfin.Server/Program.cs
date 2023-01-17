using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Emby.Server.Implementations;
using Jellyfin.Server.Extensions;
using Jellyfin.Server.Helpers;
using Jellyfin.Server.Implementations;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog;
using Serilog.Extensions.Logging;
using static MediaBrowser.Controller.Extensions.ConfigurationExtensions;
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
        public const string LoggingConfigFileDefault = "logging.default.json";

        /// <summary>
        /// The name of the logging configuration file containing the system-specific override settings.
        /// </summary>
        public const string LoggingConfigFileSystem = "logging.json";

        private static readonly ILoggerFactory _loggerFactory = new SerilogLoggerFactory();
        private static CancellationTokenSource _tokenSource = new();
        private static long _startTimestamp;
        private static ILogger _logger = NullLogger.Instance;
        private static bool _restartOnShutdown;

        /// <summary>
        /// The entry point of the application.
        /// </summary>
        /// <param name="args">The command line arguments passed.</param>
        /// <returns><see cref="Task" />.</returns>
        public static Task Main(string[] args)
        {
            static Task ErrorParsingArguments(IEnumerable<Error> errors)
            {
                Environment.ExitCode = 1;
                return Task.CompletedTask;
            }

            // Parse the command line arguments and either start the app or exit indicating error
            return Parser.Default.ParseArguments<StartupOptions>(args)
                .MapResult(StartApp, ErrorParsingArguments);
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
            _startTimestamp = Stopwatch.GetTimestamp();

            // Log all uncaught exceptions to std error
            static void UnhandledExceptionToConsole(object sender, UnhandledExceptionEventArgs e) =>
                Console.Error.WriteLine("Unhandled Exception\n" + e.ExceptionObject);
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionToConsole;

            ServerApplicationPaths appPaths = StartupHelpers.CreateApplicationPaths(options);

            // $JELLYFIN_LOG_DIR needs to be set for the logger configuration manager
            Environment.SetEnvironmentVariable("JELLYFIN_LOG_DIR", appPaths.LogDirectoryPath);

            // Enable cl-va P010 interop for tonemapping on Intel VAAPI
            Environment.SetEnvironmentVariable("NEOReadDebugKeys", "1");
            Environment.SetEnvironmentVariable("EnableExtendedVaFormats", "1");

            await StartupHelpers.InitLoggingConfigFile(appPaths).ConfigureAwait(false);

            // Create an instance of the application configuration to use for application startup
            IConfiguration startupConfig = CreateAppConfiguration(options, appPaths);

            StartupHelpers.InitializeLoggingFramework(startupConfig, appPaths);
            _logger = _loggerFactory.CreateLogger("Main");

            // Log uncaught exceptions to the logging instead of std error
            AppDomain.CurrentDomain.UnhandledException -= UnhandledExceptionToConsole;
            AppDomain.CurrentDomain.UnhandledException += (_, e)
                => _logger.LogCritical((Exception)e.ExceptionObject, "Unhandled Exception");

            // Intercept Ctrl+C and Ctrl+Break
            Console.CancelKeyPress += (_, e) =>
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
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
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

            // If hosting the web client, validate the client content path
            if (startupConfig.HostWebClient())
            {
                var webContentPath = appPaths.WebPath;
                if (!Directory.Exists(webContentPath) || !Directory.EnumerateFiles(webContentPath).Any())
                {
                    _logger.LogError(
                        "The server is expected to host the web client, but the provided content directory is either " +
                        "invalid or empty: {WebContentPath}. If you do not want to host the web client with the " +
                        "server, you may set the '--nowebclient' command line flag, or set" +
                        "'{ConfigKey}=false' in your config settings",
                        webContentPath,
                        HostWebClientKey);
                    Environment.ExitCode = 1;
                    return;
                }
            }

            StartupHelpers.PerformStaticInitialization();
            Migrations.MigrationRunner.RunPreStartup(appPaths, _loggerFactory);

            do
            {
                _restartOnShutdown = false;
                await StartServer(appPaths, options, startupConfig).ConfigureAwait(false);

                if (_restartOnShutdown)
                {
                    _tokenSource = new CancellationTokenSource();
                    _startTimestamp = Stopwatch.GetTimestamp();
                }
            } while (_restartOnShutdown);
        }

        private static async Task StartServer(IServerApplicationPaths appPaths, StartupOptions options, IConfiguration startupConfig)
        {
            var appHost = new CoreAppHost(
                appPaths,
                _loggerFactory,
                options,
                startupConfig);

            IHost? host = null;
            try
            {
                host = Host.CreateDefaultBuilder()
                    .ConfigureServices(services => appHost.Init(services))
                    .ConfigureWebHostDefaults(webHostBuilder => webHostBuilder.ConfigureWebHostBuilder(appHost, startupConfig, appPaths, _logger))
                    .ConfigureAppConfiguration(config => config.ConfigureAppConfiguration(options, appPaths, startupConfig))
                    .UseSerilog()
                    .Build();

                // Re-use the host service provider in the app host since ASP.NET doesn't allow a custom service collection.
                appHost.ServiceProvider = host.Services;

                await appHost.InitializeServices().ConfigureAwait(false);
                Migrations.MigrationRunner.Run(appHost, _loggerFactory);

                try
                {
                    await host.StartAsync(_tokenSource.Token).ConfigureAwait(false);

                    if (!OperatingSystem.IsWindows() && startupConfig.UseUnixSocket())
                    {
                        var socketPath = StartupHelpers.GetUnixSocketPath(startupConfig, appPaths);

                        StartupHelpers.SetUnixSocketPermissions(startupConfig, socketPath, _logger);
                    }
                }
                catch (Exception ex) when (ex is not TaskCanceledException)
                {
                    _logger.LogError("Kestrel failed to start! This is most likely due to an invalid address or port bind - correct your bind configuration in network.xml and try again");
                    throw;
                }

                await appHost.RunStartupTasksAsync(_tokenSource.Token).ConfigureAwait(false);

                _logger.LogInformation("Startup complete {Time:g}", Stopwatch.GetElapsedTime(_startTimestamp));

                // Block main thread until shutdown
                await Task.Delay(-1, _tokenSource.Token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // Don't throw on cancellation
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error while starting server");
            }
            finally
            {
                // Don't throw additional exception if startup failed.
                if (appHost.ServiceProvider is not null)
                {
                    _logger.LogInformation("Running query planner optimizations in the database... This might take a while");
                    // Run before disposing the application
                    var context = await appHost.ServiceProvider.GetRequiredService<IDbContextFactory<JellyfinDbContext>>().CreateDbContextAsync().ConfigureAwait(false);
                    await using (context.ConfigureAwait(false))
                    {
                        if (context.Database.IsSqlite())
                        {
                            await context.Database.ExecuteSqlRawAsync("PRAGMA optimize").ConfigureAwait(false);
                        }
                    }
                }

                await appHost.DisposeAsync().ConfigureAwait(false);
                host?.Dispose();
            }
        }

        /// <summary>
        /// Create the application configuration.
        /// </summary>
        /// <param name="commandLineOpts">The command line options passed to the program.</param>
        /// <param name="appPaths">The application paths.</param>
        /// <returns>The application configuration.</returns>
        public static IConfiguration CreateAppConfiguration(StartupOptions commandLineOpts, IApplicationPaths appPaths)
        {
            return new ConfigurationBuilder()
                .ConfigureAppConfiguration(commandLineOpts, appPaths)
                .Build();
        }

        private static IConfigurationBuilder ConfigureAppConfiguration(
            this IConfigurationBuilder config,
            StartupOptions commandLineOpts,
            IApplicationPaths appPaths,
            IConfiguration? startupConfig = null)
        {
            // Use the swagger API page as the default redirect path if not hosting the web client
            var inMemoryDefaultConfig = ConfigurationOptions.DefaultConfiguration;
            if (startupConfig is not null && !startupConfig.HostWebClient())
            {
                inMemoryDefaultConfig[DefaultRedirectKey] = "api-docs/swagger";
            }

            return config
                .SetBasePath(appPaths.ConfigurationDirectoryPath)
                .AddInMemoryCollection(inMemoryDefaultConfig)
                .AddJsonFile(LoggingConfigFileDefault, optional: false, reloadOnChange: true)
                .AddJsonFile(LoggingConfigFileSystem, optional: true, reloadOnChange: true)
                .AddEnvironmentVariables("JELLYFIN_")
                .AddInMemoryCollection(commandLineOpts.ConvertToConfig());
        }
    }
}
