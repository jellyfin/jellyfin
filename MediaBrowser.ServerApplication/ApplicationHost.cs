using MediaBrowser.Api;
using MediaBrowser.Common.Implementations;
using MediaBrowser.Common.Implementations.HttpServer;
using MediaBrowser.Common.Implementations.Logging;
using MediaBrowser.Common.Implementations.NetworkManagement;
using MediaBrowser.Common.Implementations.ScheduledTasks;
using MediaBrowser.Common.Implementations.Serialization;
using MediaBrowser.Common.Implementations.ServerManager;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Updates;
using MediaBrowser.IsoMounter;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Updates;
using MediaBrowser.Server.Implementations;
using MediaBrowser.Server.Implementations.BdInfo;
using MediaBrowser.Server.Implementations.Library;
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
    public class ApplicationHost : BaseApplicationHost, IApplicationHost
    {
        /// <summary>
        /// Gets or sets the kernel.
        /// </summary>
        /// <value>The kernel.</value>
        internal Kernel Kernel { get; private set; }

        /// <summary>
        /// The json serializer
        /// </summary>
        private readonly IJsonSerializer _jsonSerializer = new JsonSerializer();

        /// <summary>
        /// The _XML serializer
        /// </summary>
        private readonly IXmlSerializer _xmlSerializer = new XmlSerializer();

        private WebSocketEvents _webSocketEvents;

        /// <summary>
        /// Gets the server application paths.
        /// </summary>
        /// <value>The server application paths.</value>
        protected IServerApplicationPaths ServerApplicationPaths
        {
            get { return (IServerApplicationPaths)ApplicationPaths; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationHost" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public ApplicationHost()
            : base()
        {
            Kernel = new Kernel(this, ServerApplicationPaths, _xmlSerializer, Logger);

            var networkManager = new NetworkManager();

            var serverManager = new ServerManager(this, Kernel, networkManager, _jsonSerializer, Logger);

            var taskManager = new TaskManager(ApplicationPaths, _jsonSerializer, Logger, serverManager);

            LogManager.ReloadLogger(Kernel.Configuration.EnableDebugLevelLogging ? LogSeverity.Debug : LogSeverity.Info);

            Logger.Info("Version {0} initializing", ApplicationVersion);

            RegisterResources(taskManager, networkManager, serverManager);

            FindParts();
        }

        /// <summary>
        /// Gets the application paths.
        /// </summary>
        /// <returns>IApplicationPaths.</returns>
        protected override IApplicationPaths GetApplicationPaths()
        {
            return new ServerApplicationPaths();
        }

        /// <summary>
        /// Gets the log manager.
        /// </summary>
        /// <returns>ILogManager.</returns>
        protected override ILogManager GetLogManager()
        {
            return new NlogManager(ApplicationPaths.LogDirectoryPath, "Server");
        }

        /// <summary>
        /// Registers resources that classes will depend on
        /// </summary>
        protected override void RegisterResources(ITaskManager taskManager, INetworkManager networkManager, IServerManager serverManager)
        {
            base.RegisterResources(taskManager, networkManager, serverManager);

            RegisterSingleInstance<IKernel>(Kernel);
            RegisterSingleInstance(Kernel);

            RegisterSingleInstance<IApplicationHost>(this);


            RegisterSingleInstance(ServerApplicationPaths);
            RegisterSingleInstance<IIsoManager>(new PismoIsoManager(Logger));
            RegisterSingleInstance<IBlurayExaminer>(new BdInfoExaminer());
            RegisterSingleInstance<IZipClient>(new DotNetZipClient());
            RegisterSingleInstance(_jsonSerializer);
            RegisterSingleInstance(_xmlSerializer);
            RegisterSingleInstance(ServerFactory.CreateServer(this, ProtobufSerializer, Logger, "Media Browser", "index.html"), false);

            var userManager = new UserManager(Kernel, Logger);
            RegisterSingleInstance<IUserManager>(userManager);

            RegisterSingleInstance<ILibraryManager>(new LibraryManager(Kernel, Logger, taskManager, userManager));
        }

        /// <summary>
        /// Finds the parts.
        /// </summary>
        protected override void FindParts()
        {
            base.FindParts();

            Resolve<ILibraryManager>().AddParts(GetExports<IResolutionIgnoreRule>(), GetExports<IVirtualFolderCreator>(), GetExports<IBaseItemResolver>());

            Kernel.InstallationManager = (InstallationManager)CreateInstance(typeof(InstallationManager));

            _webSocketEvents = new WebSocketEvents(Resolve<IServerManager>(), Resolve<IKernel>(), Resolve<ILogger>(), Resolve<IUserManager>(), Resolve<ILibraryManager>(), Kernel.InstallationManager);
        }

        /// <summary>
        /// Restarts this instance.
        /// </summary>
        public void Restart()
        {
            App.Instance.Restart();
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance can self update.
        /// </summary>
        /// <value><c>true</c> if this instance can self update; otherwise, <c>false</c>.</value>
        public bool CanSelfUpdate
        {
            get { return Kernel.Configuration.EnableAutoUpdate; }
        }

        /// <summary>
        /// Checks for update.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task{CheckForUpdateResult}.</returns>
        public async Task<CheckForUpdateResult> CheckForApplicationUpdate(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var pkgManager = Resolve<IPackageManager>();
            var availablePackages = await pkgManager.GetAvailablePackages(Resolve<IHttpClient>(), Resolve<INetworkManager>(), Kernel.SecurityManager, Kernel.ResourcePools, Resolve<IJsonSerializer>(), CancellationToken.None).ConfigureAwait(false);
            var version = Kernel.InstallationManager.GetLatestCompatibleVersion(availablePackages, "MBServer", Kernel.Configuration.SystemUpdateLevel);

            return version != null ? new CheckForUpdateResult { AvailableVersion = version.version, IsUpdateAvailable = version.version > ApplicationVersion, Package = version } :
                       new CheckForUpdateResult { AvailableVersion = ApplicationVersion, IsUpdateAvailable = false };
        }

        /// <summary>
        /// Updates the application.
        /// </summary>
        /// <param name="package">The package that contains the update</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public Task UpdateApplication(PackageVersionInfo package, CancellationToken cancellationToken, IProgress<double> progress)
        {
            var pkgManager = Resolve<IPackageManager>();
            return pkgManager.InstallPackage(Resolve<IHttpClient>(), Resolve<ILogger>(), Kernel.ResourcePools, progress, Resolve<IZipClient>(), Kernel.ApplicationPaths, package, cancellationToken);
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
            yield return typeof(IKernel).Assembly;

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
        /// Shuts down.
        /// </summary>
        public void Shutdown()
        {
            App.Instance.Dispatcher.Invoke(App.Instance.Shutdown);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool dispose)
        {
            if (dispose)
            {
                if (_webSocketEvents != null)
                {
                    _webSocketEvents.Dispose();
                }
            }

            base.Dispose(dispose);
        }
    }
}
