using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Emby.Server.Implementations;
using Emby.Server.Implementations.Configuration;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.Extensions;
using Jellyfin.Server.Helpers;
using Jellyfin.Server.Implementations.DatabaseConfiguration;
using Jellyfin.Server.Implementations.Extensions;
using Jellyfin.Server.Implementations.StorageHelpers;
using Jellyfin.Server.Implementations.SystemBackupService;
using Jellyfin.Server.Migrations;
using Jellyfin.Server.Migrations.Stages;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using Microsoft.AspNetCore.Hosting;
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

        private static readonly SerilogLoggerFactory _loggerFactory = new SerilogLoggerFactory();
        private static SetupServer? _setupServer;
        private static CoreAppHost? _appHost;
        private static IHost? _jellyfinHost = null;
        private static long _startTimestamp;
        private static ILogger _logger = NullLogger.Instance;
        private static bool _restartOnShutdown;
        private static IStartupLogger<JellyfinMigrationService>? _migrationLogger;
        private static string? _restoreFromBackup;

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

        private static async Task StartApp(StartupOptions options)
        {
            _restoreFromBackup = options.RestoreArchive;
            _startTimestamp = Stopwatch.GetTimestamp();
            ServerApplicationPaths appPaths = StartupHelpers.CreateApplicationPaths(options);
            appPaths.MakeSanityCheckOrThrow();

            // $JELLYFIN_LOG_DIR needs to be set for the logger configuration manager
            Environment.SetEnvironmentVariable("JELLYFIN_LOG_DIR", appPaths.LogDirectoryPath);

            // Enable cl-va P010 interop for tonemapping on Intel VAAPI
            Environment.SetEnvironmentVariable("NEOReadDebugKeys", "1");
            Environment.SetEnvironmentVariable("EnableExtendedVaFormats", "1");

            await StartupHelpers.InitLoggingConfigFile(appPaths).ConfigureAwait(false);

            // Create an instance of the application configuration to use for application startup
            IConfiguration startupConfig = CreateAppConfiguration(options, appPaths);
            StartupHelpers.InitializeLoggingFramework(startupConfig, appPaths);
            _setupServer = new SetupServer(static () => _jellyfinHost?.Services?.GetService<INetworkManager>(), appPaths, static () => _appHost, _loggerFactory, startupConfig);
            await _setupServer.RunAsync().ConfigureAwait(false);
            _logger = _loggerFactory.CreateLogger("Main");
            StartupLogger.Logger = new StartupLogger(_logger);

            // Use the logging framework for uncaught exceptions instead of std error
            AppDomain.CurrentDomain.UnhandledException += (_, e)
                => _logger.LogCritical((Exception)e.ExceptionObject, "Unhandled Exception");

            _logger.LogInformation(
                "Jellyfin version: {Version}",
                Assembly.GetEntryAssembly()!.GetName().Version!.ToString(3));

            StartupHelpers.LogEnvironmentInfo(_logger, appPaths);

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

            StorageHelper.TestCommonPathsForStorageCapacity(appPaths, StartupLogger.Logger.With(_loggerFactory.CreateLogger<Startup>()).BeginGroup($"Storage Check"));

            StartupHelpers.PerformStaticInitialization();

            await ApplyStartupMigrationAsync(appPaths, startupConfig).ConfigureAwait(false);

            do
            {
                await StartServer(appPaths, options, startupConfig).ConfigureAwait(false);

                if (_restartOnShutdown)
                {
                    _startTimestamp = Stopwatch.GetTimestamp();
                    await _setupServer.StopAsync().ConfigureAwait(false);
                    await _setupServer.RunAsync().ConfigureAwait(false);
                }
            } while (_restartOnShutdown);

            _setupServer.Dispose();
        }

        private static async Task StartServer(IServerApplicationPaths appPaths, StartupOptions options, IConfiguration startupConfig)
        {
            using CoreAppHost appHost = new CoreAppHost(
                            appPaths,
                            _loggerFactory,
                            options,
                            startupConfig);
            _appHost = appHost;
            var configurationCompleted = false;
            try
            {
                _jellyfinHost = Host.CreateDefaultBuilder()
                    .UseConsoleLifetime()
                    .ConfigureServices(services => appHost.Init(services))
                    .ConfigureWebHostDefaults(webHostBuilder =>
                    {
                        webHostBuilder.ConfigureWebHostBuilder(appHost, startupConfig, appPaths, _logger);
                        if (bool.TryParse(Environment.GetEnvironmentVariable("JELLYFIN_ENABLE_IIS"), out var iisEnabled) && iisEnabled)
                        {
                            _logger.LogCritical("UNSUPPORTED HOSTING ENVIRONMENT Microsoft Internet Information Services. The option to run Jellyfin on IIS is an unsupported and untested feature. Only use at your own discretion.");
                            webHostBuilder.UseIIS();
                        }
                    })
                    .ConfigureAppConfiguration(config => config.ConfigureAppConfiguration(options, appPaths, startupConfig))
                    .UseSerilog()
                    .ConfigureServices(e => e
                        .RegisterStartupLogger()
                        .AddSingleton<IServiceCollection>(e))
                    .Build();

                /*
                 * Initialize the transcode path marker so we avoid starting Jellyfin in a broken state.
                 * This should really be a part of IApplicationPaths but this path is configured differently.
                 */
                _ = appHost.ConfigurationManager.GetTranscodePath();

                // Re-use the host service provider in the app host since ASP.NET doesn't allow a custom service collection.
                appHost.ServiceProvider = _jellyfinHost.Services;
                PrepareDatabaseProvider(appHost.ServiceProvider);

                if (!string.IsNullOrWhiteSpace(_restoreFromBackup))
                {
                    await appHost.ServiceProvider.GetService<IBackupService>()!.RestoreBackupAsync(_restoreFromBackup).ConfigureAwait(false);
                    _restoreFromBackup = null;
                    _restartOnShutdown = true;
                    return;
                }

                var jellyfinMigrationService = ActivatorUtilities.CreateInstance<JellyfinMigrationService>(appHost.ServiceProvider);
                await jellyfinMigrationService.PrepareSystemForMigration(_logger).ConfigureAwait(false);
                await jellyfinMigrationService.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, appHost.ServiceProvider).ConfigureAwait(false);

                await appHost.InitializeServices(startupConfig).ConfigureAwait(false);

                await jellyfinMigrationService.MigrateStepAsync(JellyfinMigrationStageTypes.AppInitialisation, appHost.ServiceProvider).ConfigureAwait(false);
                await jellyfinMigrationService.CleanupSystemAfterMigration(_logger).ConfigureAwait(false);
                try
                {
                    configurationCompleted = true;
                    await _setupServer!.StopAsync().ConfigureAwait(false);
                    await _jellyfinHost.StartAsync().ConfigureAwait(false);

                    if (!OperatingSystem.IsWindows() && startupConfig.UseUnixSocket())
                    {
                        var socketPath = StartupHelpers.GetUnixSocketPath(startupConfig, appPaths);

                        StartupHelpers.SetUnixSocketPermissions(startupConfig, socketPath, _logger);
                    }
                }
                catch (Exception)
                {
                    _logger.LogError("Kestrel failed to start! This is most likely due to an invalid address or port bind - correct your bind configuration in network.xml and try again");
                    throw;
                }

                await appHost.RunStartupTasksAsync().ConfigureAwait(false);

                _logger.LogInformation("Startup complete {Time:g}", Stopwatch.GetElapsedTime(_startTimestamp));

                await _jellyfinHost.WaitForShutdownAsync().ConfigureAwait(false);
                _restartOnShutdown = appHost.ShouldRestart;
                _restoreFromBackup = appHost.RestoreBackupPath;
            }
            catch (Exception ex)
            {
                _restartOnShutdown = false;
                _logger.LogCritical(ex, "Error while starting server");
                if (_setupServer!.IsAlive && !configurationCompleted)
                {
                    _setupServer!.SoftStop();
                    await Task.Delay(TimeSpan.FromMinutes(10)).ConfigureAwait(false);
                    await _setupServer!.StopAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                // Don't throw additional exception if startup failed.
                if (appHost.ServiceProvider is not null)
                {
                    _logger.LogInformation("Running query planner optimizations in the database... This might take a while");

                    var databaseProvider = appHost.ServiceProvider.GetRequiredService<IJellyfinDatabaseProvider>();
                    using var shutdownSource = new CancellationTokenSource();
                    shutdownSource.CancelAfter((int)TimeSpan.FromSeconds(60).TotalMicroseconds);
                    await databaseProvider.RunShutdownTask(shutdownSource.Token).ConfigureAwait(false);
                }

                _appHost = null;
                _jellyfinHost?.Dispose();
            }
        }

        /// <summary>
        /// [Internal]Runs the startup Migrations.
        /// </summary>
        /// <remarks>
        /// Not intended to be used other then by jellyfin and its tests.
        /// </remarks>
        /// <param name="appPaths">Application Paths.</param>
        /// <param name="startupConfig">Startup Config.</param>
        /// <returns>A task.</returns>
        public static async Task ApplyStartupMigrationAsync(ServerApplicationPaths appPaths, IConfiguration startupConfig)
        {
            _migrationLogger = StartupLogger.Logger.BeginGroup<JellyfinMigrationService>($"Migration Service");
            var startupConfigurationManager = new ServerConfigurationManager(appPaths, _loggerFactory, new MyXmlSerializer());
            startupConfigurationManager.AddParts([new DatabaseConfigurationFactory()]);
            var migrationStartupServiceProvider = new ServiceCollection()
                .AddLogging(d => d.AddSerilog())
                .AddJellyfinDbContext(startupConfigurationManager, startupConfig)
                .AddSingleton<IApplicationPaths>(appPaths)
                .AddSingleton<ServerApplicationPaths>(appPaths)
                .RegisterStartupLogger();

            migrationStartupServiceProvider.AddSingleton(migrationStartupServiceProvider);
            var startupService = migrationStartupServiceProvider.BuildServiceProvider();

            PrepareDatabaseProvider(startupService);

            var jellyfinMigrationService = ActivatorUtilities.CreateInstance<JellyfinMigrationService>(startupService);
            await jellyfinMigrationService.CheckFirstTimeRunOrMigration(appPaths).ConfigureAwait(false);
            await jellyfinMigrationService.MigrateStepAsync(Migrations.Stages.JellyfinMigrationStageTypes.PreInitialisation, startupService).ConfigureAwait(false);
        }

        /// <summary>
        /// [Internal]Runs the Jellyfin migrator service with the Core stage.
        /// </summary>
        /// <remarks>
        /// Not intended to be used other then by jellyfin and its tests.
        /// </remarks>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="jellyfinMigrationStage">The stage to run.</param>
        /// <returns>A task.</returns>
        public static async Task ApplyCoreMigrationsAsync(IServiceProvider serviceProvider, Migrations.Stages.JellyfinMigrationStageTypes jellyfinMigrationStage)
        {
            var jellyfinMigrationService = ActivatorUtilities.CreateInstance<JellyfinMigrationService>(serviceProvider, _migrationLogger!);
            await jellyfinMigrationService.MigrateStepAsync(jellyfinMigrationStage, serviceProvider).ConfigureAwait(false);
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

        private static void PrepareDatabaseProvider(IServiceProvider services)
        {
            var factory = services.GetRequiredService<IDbContextFactory<JellyfinDbContext>>();
            var provider = services.GetRequiredService<IJellyfinDatabaseProvider>();
            provider.DbContextFactory = factory;
        }
    }
}
