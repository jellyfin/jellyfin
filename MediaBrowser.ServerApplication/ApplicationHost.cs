using MediaBrowser.Api;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Constants;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.Implementations;
using MediaBrowser.Common.Implementations.ScheduledTasks;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Updates;
using MediaBrowser.IsoMounter;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Updates;
using MediaBrowser.Server.Implementations;
using MediaBrowser.Server.Implementations.BdInfo;
using MediaBrowser.Server.Implementations.Configuration;
using MediaBrowser.Server.Implementations.HttpServer;
using MediaBrowser.Server.Implementations.Library;
using MediaBrowser.Server.Implementations.ServerManager;
using MediaBrowser.Server.Implementations.Udp;
using MediaBrowser.Server.Implementations.Updates;
using MediaBrowser.Server.Implementations.WebSocket;
using MediaBrowser.ServerApplication.Implementations;
using MediaBrowser.WebDashboard.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.ServerApplication
{
    /// <summary>
    /// Class CompositionRoot
    /// </summary>
    public class ApplicationHost : BaseApplicationHost<ServerApplicationPaths>, IServerApplicationHost
    {
        /// <summary>
        /// Gets the server kernel.
        /// </summary>
        /// <value>The server kernel.</value>
        protected Kernel ServerKernel { get; set; }

        /// <summary>
        /// Gets the server configuration manager.
        /// </summary>
        /// <value>The server configuration manager.</value>
        public IServerConfigurationManager ServerConfigurationManager
        {
            get { return (IServerConfigurationManager)ConfigurationManager; }
        }

        /// <summary>
        /// Gets the name of the log file prefix.
        /// </summary>
        /// <value>The name of the log file prefix.</value>
        protected override string LogFilePrefixName
        {
            get { return "Server"; }
        }

        /// <summary>
        /// Gets the configuration manager.
        /// </summary>
        /// <returns>IConfigurationManager.</returns>
        protected override IConfigurationManager GetConfigurationManager()
        {
            return new ServerConfigurationManager(ApplicationPaths, LogManager, XmlSerializer);
        }

        private IInstallationManager InstallationManager { get; set; }
        private IServerManager ServerManager { get; set; }

        public override async Task Init()
        {
            await base.Init().ConfigureAwait(false);

            await ServerKernel.Init().ConfigureAwait(false);
        }

        /// <summary>
        /// Registers resources that classes will depend on
        /// </summary>
        protected override async Task RegisterResources()
        {
            ServerKernel = new Kernel(this, XmlSerializer, LogManager, ServerConfigurationManager);
            
            await base.RegisterResources().ConfigureAwait(false);

            RegisterSingleInstance<IServerApplicationHost>(this);
            RegisterSingleInstance<IServerApplicationPaths>(ApplicationPaths);

            RegisterSingleInstance(ServerKernel);
            RegisterSingleInstance(ServerConfigurationManager);

            RegisterSingleInstance<IWebSocketServer>(() => new AlchemyServer(Logger));
            RegisterSingleInstance<IUdpServer>(new UdpServer(Logger), false);

            RegisterSingleInstance<IIsoManager>(new PismoIsoManager(Logger));
            RegisterSingleInstance<IBlurayExaminer>(new BdInfoExaminer());
            RegisterSingleInstance<IZipClient>(new DotNetZipClient());
            RegisterSingleInstance(ServerFactory.CreateServer(this, ProtobufSerializer, Logger, "Media Browser", "index.html"), false);

            ServerManager = new ServerManager(this, NetworkManager, JsonSerializer, Logger, ServerConfigurationManager, ServerKernel);
            RegisterSingleInstance(ServerManager);

            var userManager = new UserManager(ServerKernel, Logger, ServerConfigurationManager);

            RegisterSingleInstance<IUserManager>(userManager);

            RegisterSingleInstance<ILibraryManager>(new LibraryManager(ServerKernel, Logger, TaskManager, userManager, ServerConfigurationManager));

            InstallationManager = new InstallationManager(HttpClient, PackageManager, JsonSerializer, Logger, this);
            RegisterSingleInstance(InstallationManager);
        }

        /// <summary>
        /// Finds the parts.
        /// </summary>
        protected override void FindParts()
        {
            base.FindParts();

            Resolve<IHttpServer>().Init(GetExports<IRestfulService>(false));
            Resolve<IServerManager>().AddWebSocketListeners(GetExports<IWebSocketListener>(false));

            Resolve<IServerManager>().Start();

            Resolve<ILibraryManager>().AddParts(GetExports<IResolverIgnoreRule>(), GetExports<IVirtualFolderCreator>(), GetExports<IItemResolver>(), GetExports<IIntroProvider>());
        }

        /// <summary>
        /// Restarts this instance.
        /// </summary>
        public override void Restart()
        {
            App.Instance.Restart();
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance can self update.
        /// </summary>
        /// <value><c>true</c> if this instance can self update; otherwise, <c>false</c>.</value>
        public override bool CanSelfUpdate
        {
            get { return ConfigurationManager.CommonConfiguration.EnableAutoUpdate; }
        }

        /// <summary>
        /// Checks for update.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task{CheckForUpdateResult}.</returns>
        public async override Task<CheckForUpdateResult> CheckForApplicationUpdate(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var pkgManager = Resolve<IPackageManager>();
            var availablePackages = await pkgManager.GetAvailablePackages(CancellationToken.None).ConfigureAwait(false);
            var version = Resolve<IInstallationManager>().GetLatestCompatibleVersion(availablePackages, Constants.MBServerPkgName, ConfigurationManager.CommonConfiguration.SystemUpdateLevel);

            return version != null ? new CheckForUpdateResult { AvailableVersion = version.version, IsUpdateAvailable = version.version > ApplicationVersion, Package = version } :
                       new CheckForUpdateResult { AvailableVersion = ApplicationVersion, IsUpdateAvailable = false };
        }

        /// <summary>
        /// Gets the composable part assemblies.
        /// </summary>
        /// <returns>IEnumerable{Assembly}.</returns>
        protected override IEnumerable<Assembly> GetComposablePartAssemblies()
        {
            // Gets all plugin assemblies by first reading all bytes of the .dll and calling Assembly.Load against that
            // This will prevent the .dll file from getting locked, and allow us to replace it when needed
            foreach (var pluginAssembly in Directory
                .EnumerateFiles(ApplicationPaths.PluginsPath, "*.dll", SearchOption.TopDirectoryOnly)
                .Select(LoadAssembly).Where(a => a != null))
            {
                yield return pluginAssembly;
            }

            // Include composable parts in the Api assembly 
            yield return typeof(ApiService).Assembly;

            // Include composable parts in the Dashboard assembly 
            yield return typeof(DashboardInfo).Assembly;

            // Include composable parts in the Model assembly 
            yield return typeof(SystemInfo).Assembly;

            // Include composable parts in the Common assembly 
            yield return typeof(IApplicationHost).Assembly;

            // Include composable parts in the Controller assembly 
            yield return typeof(Kernel).Assembly;

            // Common implementations
            yield return typeof(TaskManager).Assembly;

            // Server implementations
            yield return typeof(ServerApplicationPaths).Assembly;

            // Include composable parts in the running assembly
            yield return GetType().Assembly;
        }

        /// <summary>
        /// Gets the system status.
        /// </summary>
        /// <returns>SystemInfo.</returns>
        public virtual SystemInfo GetSystemInfo()
        {
            return new SystemInfo
            {
                HasPendingRestart = HasPendingRestart,
                Version = ApplicationVersion.ToString(),
                IsNetworkDeployed = CanSelfUpdate,
                WebSocketPortNumber = ServerManager.WebSocketPortNumber,
                SupportsNativeWebSocket = ServerManager.SupportsNativeWebSocket,
                FailedPluginAssemblies = FailedAssemblies.ToArray(),
                InProgressInstallations = InstallationManager.CurrentInstallations.Select(i => i.Item1).ToArray(),
                CompletedInstallations = InstallationManager.CompletedInstallations.ToArray()
            };
        }

        /// <summary>
        /// Shuts down.
        /// </summary>
        public override void Shutdown()
        {
            App.Instance.Dispatcher.Invoke(App.Instance.Shutdown);
        }
    }
}
