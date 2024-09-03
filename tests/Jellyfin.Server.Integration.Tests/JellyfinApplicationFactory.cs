using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using Emby.Server.Implementations;
using Jellyfin.Server.Extensions;
using Jellyfin.Server.Helpers;
using MediaBrowser.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog;
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
            var webHostPathRoot = Path.Combine(_testPathRoot, "test-host-" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
            Directory.CreateDirectory(Path.Combine(webHostPathRoot, "logs"));
            Directory.CreateDirectory(Path.Combine(webHostPathRoot, "config"));
            Directory.CreateDirectory(Path.Combine(webHostPathRoot, "cache"));
            Directory.CreateDirectory(Path.Combine(webHostPathRoot, "jellyfin-web"));
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
                });
        }

        /// <inheritdoc/>
        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = builder.Build();
            var appHost = (TestAppHost)host.Services.GetRequiredService<IApplicationHost>();
            appHost.ServiceProvider = host.Services;
            appHost.InitializeServices().GetAwaiter().GetResult();
            host.Start();

            appHost.RunStartupTasksAsync().GetAwaiter().GetResult();

            return host;
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            foreach (var disposable in _disposableComponents)
            {
                disposable.Dispose();
            }

            _disposableComponents.Clear();

            base.Dispose(disposing);
        }
    }
}
