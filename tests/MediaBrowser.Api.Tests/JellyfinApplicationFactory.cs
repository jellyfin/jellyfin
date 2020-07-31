using System;
using System.Collections.Concurrent;
using System.IO;
using Emby.Server.Implementations;
using Emby.Server.Implementations.IO;
using Emby.Server.Implementations.Networking;
using Jellyfin.Drawing.Skia;
using Jellyfin.Server;
using MediaBrowser.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace MediaBrowser.Api.Tests
{
    /// <summary>
    /// Factory for bootstrapping the Jellyfin application in memory for functional end to end tests.
    /// </summary>
    public class JellyfinApplicationFactory : WebApplicationFactory<Startup>
    {
        private static readonly string _testPathRoot = Path.Combine(Path.GetTempPath(), "jellyfin-test-data");
        private static readonly ConcurrentBag<IDisposable> _disposableComponents = new ConcurrentBag<IDisposable>();

        /// <summary>
        /// Initializes a new instance of the <see cref="JellyfinApplicationFactory"/> class.
        /// </summary>
        public JellyfinApplicationFactory()
        {
            // Perform static initialization that only needs to happen once per test-run
            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            Program.PerformStaticInitialization();
        }

        /// <inheritdoc/>
        protected override IWebHostBuilder CreateWebHostBuilder()
        {
            return new WebHostBuilder();
        }

        /// <inheritdoc/>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Specify the startup command line options
            var commandLineOpts = new StartupOptions
            {
                NoWebClient = true,
                NoAutoRunWebApp = true
            };

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
            Program.InitLoggingConfigFile(appPaths).GetAwaiter().GetResult();

            // Create a copy of the application configuration to use for startup
            var startupConfig = Program.CreateAppConfiguration(commandLineOpts, appPaths);

            ILoggerFactory loggerFactory = new SerilogLoggerFactory();
            _disposableComponents.Add(loggerFactory);

            // Create the app host and initialize it
            var appHost = new CoreAppHost(
                appPaths,
                loggerFactory,
                commandLineOpts,
                new ManagedFileSystem(loggerFactory.CreateLogger<ManagedFileSystem>(), appPaths),
                new NetworkManager(loggerFactory.CreateLogger<NetworkManager>()));
            _disposableComponents.Add(appHost);
            var serviceCollection = new ServiceCollection();
            appHost.Init(serviceCollection);

            // Configure the web host builder
            Program.ConfigureWebHostBuilder(builder, appHost, serviceCollection, commandLineOpts, startupConfig, appPaths);
        }

        /// <inheritdoc/>
        protected override TestServer CreateServer(IWebHostBuilder builder)
        {
            // Create the test server using the base implementation
            var testServer = base.CreateServer(builder);

            // Finish initializing the app host
            var appHost = (CoreAppHost)testServer.Services.GetRequiredService<IApplicationHost>();
            appHost.ServiceProvider = testServer.Services;
            appHost.InitializeServices().GetAwaiter().GetResult();
            appHost.RunStartupTasksAsync().GetAwaiter().GetResult();

            return testServer;
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
