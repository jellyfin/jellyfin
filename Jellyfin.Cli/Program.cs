using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using Emby.Server.Implementations;
using Emby.Server.Implementations.Configuration;
using Emby.Server.Implementations.Cryptography;
using Emby.Server.Implementations.IO;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Api.Controllers;
using Jellyfin.Database.Implementations;
using Jellyfin.Drawing;
using Jellyfin.Drawing.Skia;
using Jellyfin.Networking.Manager;
using Jellyfin.Server;
using Jellyfin.Server.Helpers;
using Jellyfin.Server.Implementations.Events;
using Jellyfin.Server.Implementations.Users;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;

namespace Jellyfin.Cli
{
    /// <summary>
    /// Class containing the entry point of the application.
    /// </summary>
    public static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            return await Parser.Default.ParseArguments<Options.Wizard, object>(args)
                .MapResult(
                    async (Options.Wizard options) => await RunWizard(options).ConfigureAwait(true),
                    errors => Task.FromResult(1)).ConfigureAwait(true);
        }

        // Web wizard follows this flow:
        //   1. POST /Startup/User
        //   2. GET  /Localization/cultures
        //   3. GET  /Localization/countries
        //   4. POST /Startup/Configuration
        //   5. POST /Startup/RemoteAccess
        //   6. POST /Startup/Complete
        private static async Task<int> RunWizard(Options.Wizard options)
        {
            var services = InitWizardServices(options);
            StartupController? startupController;
            try
            {
                startupController = services.GetRequiredService<StartupController>();
            }
            catch (Exception)
            {
                Console.WriteLine("""
                  Got an exception during startup. This could mean the Jellyfin server has not been started yet.
                  You must start the Jellyfin server at least once before running this tool.

                  The exception follows:
                """);
                throw;
            }

            var configurationManager = services.GetRequiredService<IServerConfigurationManager>();

            var firstUser = await startupController.GetFirstUser().ConfigureAwait(true);
            var startupConfiguration = startupController.GetStartupConfiguration().Value;
            var networkConfiguration = configurationManager.GetNetworkConfiguration();

            var config = WizardConfig.FromDtos(firstUser, startupConfiguration, networkConfiguration);

            Console.WriteLine($"""
            Current Configuration:

              {string.Join("\n  ", config.AsLines())}

            """);

            config.Merge(WizardConfig.FromOptions(options));

            Console.WriteLine($"""
            New Configuration:

              {string.Join("\n  ", config.AsLines())}

            """);

            if (!options.Write)
            {
                Console.WriteLine("Use --write to save config.");
                return 0;
            }

            if (options.Password is null)
            {
                Console.WriteLine("--password-file should not be empty, cannot save config, aborting.");
                return 1;
            }

            await startupController.UpdateStartupUser(config.GetStartupUserDto()).ConfigureAwait(true);
            startupController.UpdateInitialConfiguration(config.GetStartupConfigurationDto());
            startupController.SetRemoteAccess(config.GetStartupRemoteAccessDto());
            startupController.CompleteWizard();

            Console.WriteLine("The new configuration has been written. Jellyfin server must be restarted to take it into account.");

            return 0;
        }

        private static ServiceProvider InitWizardServices(Options.Wizard options)
        {
            var services = new ServiceCollection();

            services.AddSingleton<StartupOptions>(new StartupOptions
            {
                DataDir = options.DataDir,
                ConfigDir = options.ConfigDir,
                CacheDir = options.CacheDir,
                LogDir = options.LogDir
            });
            services.AddSingleton<IStartupOptions, StartupOptions>();
            services.AddSingleton<IApplicationPaths>(provider =>
            {
                return StartupHelpers.CreateApplicationPaths(provider.GetRequiredService<StartupOptions>());
            });
            services.AddSingleton<IServerApplicationPaths>(provider =>
            {
                return StartupHelpers.CreateApplicationPaths(provider.GetRequiredService<StartupOptions>());
            });

            services.AddLogging();
            services.AddSingleton<ILoggerFactory, SerilogLoggerFactory>();
            services.AddSingleton<IXmlSerializer, MyXmlSerializer>();
            services.AddSingleton<IServerConfigurationManager>(provider =>
            {
                var appHost = provider.GetRequiredService<CoreAppHost>();
                var appPaths = provider.GetRequiredService<IApplicationPaths>();
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var xmlSerializer = provider.GetRequiredService<IXmlSerializer>();
                var configurationManager = new ServerConfigurationManager(appPaths, loggerFactory, xmlSerializer);
                appHost.DiscoverTypes();
                configurationManager.AddParts(appHost.GetExports<IConfigurationFactory>());
                return configurationManager;
            });

            services.AddSingleton<CoreAppHost>();
            services.AddSingleton<IServerApplicationHost, CoreAppHost>();
            services.AddSingleton<IConfiguration>(provider =>
            {
                var startupOptions = provider.GetRequiredService<StartupOptions>();
                var appPaths = provider.GetRequiredService<IApplicationPaths>();
                return Jellyfin.Server.Program.CreateAppConfiguration(startupOptions, appPaths);
            });
            services.AddSingleton<IApplicationHost, CoreAppHost>();
            services.AddSingleton<INetworkManager, NetworkManager>();
            services.AddSingleton<MediaBrowser.Common.Configuration.IConfigurationManager>(provider =>
            {
                return provider.GetRequiredService<IServerConfigurationManager>();
            });
            services.AddSingleton<IFileSystem, ManagedFileSystem>();
            services.AddSingleton<IImageProcessor, ImageProcessor>();
            bool useSkiaEncoder = SkiaEncoder.IsNativeLibAvailable();
            Type imageEncoderType = useSkiaEncoder
                ? typeof(SkiaEncoder)
                : typeof(NullImageEncoder);
            services.AddSingleton(typeof(IImageEncoder), imageEncoderType);
            services.AddSingleton<IEventManager, EventManager>();

            services.AddSingleton<IAuthenticationProvider, DefaultAuthenticationProvider>();
            services.AddSingleton<IAuthenticationProvider, InvalidAuthProvider>();
            services.AddSingleton<IPasswordResetProvider, DefaultPasswordResetProvider>();
            services.AddSingleton<ICryptoProvider, CryptographyProvider>();
            services.AddSingleton<IUserManager, UserManager>();

            services.AddDbContextFactory<JellyfinDbContext>((provider, opt) =>
            {
                var applicationPaths = provider.GetRequiredService<IApplicationPaths>();
                opt.UseSqlite($"Filename={Path.Combine(applicationPaths.DataPath, "jellyfin.db")}");
            });

            services.AddSingleton<StartupController>();

            return services.BuildServiceProvider();
        }
    }
}
