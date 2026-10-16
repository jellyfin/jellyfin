using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using Emby.Server.Implementations;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Extensions;
using Jellyfin.Server.Helpers;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;

namespace Jellyfin.Server.Integration.Tests
{
    /// <summary>
    /// Factory for bootstrapping the Jellyfin application in memory for functional end to end tests.
    /// </summary>
    public class JellyfinApplicationFactory : WebApplicationFactory<Startup>
    {
        private static readonly string _testPathRoot = Path.Combine(Path.GetTempPath(), "jellyfin-test-data");
        private readonly ConcurrentBag<IDisposable> _disposableComponents = new ConcurrentBag<IDisposable>();
        private readonly string _webHostPathRoot;
        private TestDatabase? _database;
        private string? _createdRoot;
        private string? _provisionedConfigurationPath;

        /// <summary>
        /// Initializes static members of the <see cref="JellyfinApplicationFactory"/> class.
        /// </summary>
        static JellyfinApplicationFactory()
        {
            // Perform static initialization that only needs to happen once per test-run
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
                .CreateLogger();
            StartupHelpers.PerformStaticInitialization();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JellyfinApplicationFactory"/> class using a new temporary directory.
        /// </summary>
        public JellyfinApplicationFactory()
            : this(Path.Combine(_testPathRoot, "test-host-" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName())))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JellyfinApplicationFactory"/> class.
        /// </summary>
        /// <param name="webHostPathRoot">The directory the application paths are created in. Reusing it starts the same server again.</param>
        protected JellyfinApplicationFactory(string webHostPathRoot)
        {
            _webHostPathRoot = webHostPathRoot;
        }

        /// <inheritdoc/>
        protected override IHostBuilder CreateHostBuilder()
        {
            return new HostBuilder();
        }

        /// <inheritdoc/>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Skip ffmpeg check for testing
            Environment.SetEnvironmentVariable("JELLYFIN_FFMPEG__NOVALIDATION", "true");
            // Specify the startup command line options
            var commandLineOpts = new StartupOptions();

            // Use a temporary directory for the application paths
            var webHostPathRoot = _webHostPathRoot;
            if (!Directory.Exists(webHostPathRoot))
            {
                _createdRoot = webHostPathRoot;
            }

            Directory.CreateDirectory(Path.Combine(webHostPathRoot, "logs"));
            Directory.CreateDirectory(Path.Combine(webHostPathRoot, "config"));
            Directory.CreateDirectory(Path.Combine(webHostPathRoot, "cache"));
            Directory.CreateDirectory(Path.Combine(webHostPathRoot, "jellyfin-web"));
            ProvisionDatabase(Path.Combine(webHostPathRoot, "config"));
            var appPaths = new ServerApplicationPaths(
                webHostPathRoot,
                Path.Combine(webHostPathRoot, "logs"),
                Path.Combine(webHostPathRoot, "config"),
                Path.Combine(webHostPathRoot, "cache"),
                Path.Combine(webHostPathRoot, "jellyfin-web"));

            // Create the logging config file
            // TODO: We shouldn't need to do this since we are only logging to console
            StartupHelpers.InitLoggingConfigFile(appPaths).GetAwaiter().GetResult();

            // Create a copy of the application configuration to use for startup
            var startupConfig = Program.CreateAppConfiguration(commandLineOpts, appPaths);

            ILoggerFactory loggerFactory = new SerilogLoggerFactory();

            _disposableComponents.Add(loggerFactory);

            // Create the app host and initialize it
            var appHost = new TestAppHost(
                appPaths,
                loggerFactory,
                commandLineOpts,
                startupConfig);
            _disposableComponents.Add(appHost);

            builder.ConfigureServices(services => appHost.Init(services))
                .ConfigureWebHostBuilder(appHost, startupConfig, appPaths, NullLogger.Instance)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder
                        .SetBasePath(appPaths.ConfigurationDirectoryPath)
                        .AddInMemoryCollection(ConfigurationOptions.DefaultConfiguration)
                        .AddEnvironmentVariables("JELLYFIN_")
                        .AddInMemoryCollection(commandLineOpts.ConvertToConfig());
                })
                .ConfigureServices(e => e
                    .AddSingleton<IStartupLogger, NullStartupLogger<object>>()
                    .AddTransient(typeof(IStartupLogger<>), typeof(NullStartupLogger<>))
                    .AddSingleton(e));
        }

        /// <inheritdoc/>
        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = builder.Build();
            var appHost = (TestAppHost)host.Services.GetRequiredService<IApplicationHost>();
            appHost.ServiceProvider = host.Services;
            var applicationPaths = appHost.ServiceProvider.GetRequiredService<IApplicationPaths>();
            Program.ApplyStartupMigrationAsync((ServerApplicationPaths)applicationPaths, appHost.ServiceProvider.GetRequiredService<IConfiguration>(), new()).GetAwaiter().GetResult();
            Program.ApplyCoreMigrationsAsync(appHost.ServiceProvider, Migrations.Stages.JellyfinMigrationStageTypes.CoreInitialisation).GetAwaiter().GetResult();
            appHost.InitializeServices(Mock.Of<IConfiguration>()).GetAwaiter().GetResult();
            Program.ApplyCoreMigrationsAsync(appHost.ServiceProvider, Migrations.Stages.JellyfinMigrationStageTypes.AppInitialisation).GetAwaiter().GetResult();
            host.Start();

            appHost.RunStartupTasksAsync().GetAwaiter().GetResult();

            return host;
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            try
            {
                foreach (var disposable in _disposableComponents)
                {
                    disposable.Dispose();
                }

                _disposableComponents.Clear();

                base.Dispose(disposing);
            }
            finally
            {
                try
                {
                    // Dropping forces closed any connection that a host which failed to dispose left open.
                    _database?.Dispose();
                    _database = null;
                }
                finally
                {
                    RemoveProvisionedConfiguration();
                }
            }
        }

        /// <summary>
        /// Writes the <c>database.xml</c> that makes the server use the server database of the test run.
        /// </summary>
        /// <param name="configurationDirectory">The configuration directory of the server.</param>
        /// <param name="database">The database to use.</param>
        internal static void WriteDatabaseConfiguration(string configurationDirectory, TestDatabase database)
        {
            var configuration = new DatabaseConfigurationOptions
            {
                DatabaseType = EngineTraits.Selected.ProviderKey,
                CustomProviderOptions = new CustomDatabaseOptions
                {
                    PluginName = string.Empty,
                    PluginAssembly = string.Empty,
                    ConnectionString = RelationalOptionsExtension.Extract(database.Options).ConnectionString!
                }
            };
            var path = GetDatabaseConfigurationPath(configurationDirectory);
            new FileStream(path, GetPrivateFileOptions()).Dispose();
            new MyXmlSerializer().SerializeToFile(configuration, path);
        }

        private static string GetDatabaseConfigurationPath(string configurationDirectory)
        {
            return Path.Combine(configurationDirectory, "database.xml");
        }

        private static FileStreamOptions GetPrivateFileOptions()
        {
            var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write, Share = FileShare.None };
            if (!OperatingSystem.IsWindows())
            {
                options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            }

            return options;
        }

        /// <summary>
        /// Points a new server at a database of its own. SQLite needs nothing, as the server creates its default database
        /// configuration itself. A server that is started again from a directory finds its <c>database.xml</c> and keeps its
        /// database, which then belongs to whoever created it. On an engine whose provider the server cannot select yet,
        /// there is no server to start, so the test is skipped.
        /// </summary>
        private void ProvisionDatabase(string configurationDirectory)
        {
            var traits = EngineTraits.Selected;
            var expectedKey = traits.ProviderKey;
            var path = GetDatabaseConfigurationPath(configurationDirectory);
            if (File.Exists(path))
            {
                var existing = (DatabaseConfigurationOptions)new MyXmlSerializer().DeserializeFromFile(typeof(DatabaseConfigurationOptions), path)!;
                if (!string.Equals(existing.DatabaseType, expectedKey, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"The database configuration in {configurationDirectory} is for {existing.DatabaseType}, but the tests run on {expectedKey}.");
                }

                return;
            }

            if (traits.IsEmbedded)
            {
                return;
            }

            Xunit.Assert.SkipUnless(traits.IsRegistered, $"The server cannot run on {traits.Provider} until its provider ({expectedKey}) is registered.");
            _database = TestDatabaseEnvironment.Backend.CreateEmpty();
            WriteDatabaseConfiguration(configurationDirectory, _database);
            _provisionedConfigurationPath = path;
        }

        private void RemoveProvisionedConfiguration()
        {
            var path = _provisionedConfigurationPath;
            if (path is null)
            {
                return;
            }

            _provisionedConfigurationPath = null;
            File.Delete(path);

            var root = _createdRoot;
            if (root is null)
            {
                return;
            }

            try
            {
                Directory.Delete(root, true);
            }
            catch (IOException)
            {
                // Best effort, the credentials are gone with the file.
            }
            catch (UnauthorizedAccessException)
            {
                // Best effort, the credentials are gone with the file.
            }
        }

        private sealed class NullStartupLogger<TCategory> : IStartupLogger<TCategory>
        {
            public StartupLogTopic? Topic => throw new NotImplementedException();

            public IStartupLogger BeginGroup(FormattableString logEntry)
            {
                return this;
            }

            public IStartupLogger<TCategory1> BeginGroup<TCategory1>(FormattableString logEntry)
            {
                return new NullStartupLogger<TCategory1>();
            }

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
            {
                return NullLogger.Instance.BeginScope(state);
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return NullLogger.Instance.IsEnabled(logLevel);
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                NullLogger.Instance.Log(logLevel, eventId, state, exception, formatter);
            }

            public Microsoft.Extensions.Logging.ILogger With(Microsoft.Extensions.Logging.ILogger logger)
            {
                return this;
            }

            public IStartupLogger<TCategory1> With<TCategory1>(Microsoft.Extensions.Logging.ILogger logger)
            {
                return new NullStartupLogger<TCategory1>();
            }

            IStartupLogger<TCategory> IStartupLogger<TCategory>.BeginGroup(FormattableString logEntry)
            {
                return new NullStartupLogger<TCategory>();
            }

            IStartupLogger IStartupLogger.With(Microsoft.Extensions.Logging.ILogger logger)
            {
                return this;
            }

            IStartupLogger<TCategory> IStartupLogger<TCategory>.With(Microsoft.Extensions.Logging.ILogger logger)
            {
                return this;
            }
        }
    }
}
