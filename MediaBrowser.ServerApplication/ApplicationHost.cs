using BDInfo;
using MediaBrowser.ClickOnce;
using MediaBrowser.Common.Implementations;
using MediaBrowser.Common.Implementations.ScheduledTasks;
using MediaBrowser.Common.Implementations.Serialization;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Implementations.Server;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller;
using MediaBrowser.IsoMounter;
using MediaBrowser.Logging.Nlog;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Updates;
using MediaBrowser.Networking.HttpManager;
using MediaBrowser.Networking.HttpServer;
using MediaBrowser.Networking.Management;
using MediaBrowser.Networking.Udp;
using MediaBrowser.Networking.WebSocket;
using MediaBrowser.Server.Implementations;
using MediaBrowser.ServerApplication.Implementations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// Gets or sets the log file path.
        /// </summary>
        /// <value>The log file path.</value>
        public string LogFilePath { get; private set; }

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

        /// <summary>
        /// The _application paths
        /// </summary>
        private readonly IServerApplicationPaths _applicationPaths = new ServerApplicationPaths();

        /// <summary>
        /// Gets a value indicating whether this instance is first run.
        /// </summary>
        /// <value><c>true</c> if this instance is first run; otherwise, <c>false</c>.</value>
        public bool IsFirstRun { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationHost" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public ApplicationHost()
            : base()
        {
            IsFirstRun = !File.Exists(_applicationPaths.SystemConfigurationFilePath);

            Logger = new NLogger("App");

            DiscoverTypes();

            Kernel = new Kernel(this, _applicationPaths, _xmlSerializer, Logger);
            
            var networkManager = new NetworkManager();

            var serverManager = new ServerManager(this, Kernel, networkManager, _jsonSerializer, Logger);

            var taskManager = new TaskManager(_applicationPaths, _jsonSerializer, Logger, serverManager);

            ReloadLogger();

            Logger.Info("Version {0} initializing", ApplicationVersion);

            var httpServer = ServerFactory.CreateServer(this, ProtobufSerializer, Logger, "Media Browser", "index.html");

            RegisterResources(taskManager, httpServer, networkManager, serverManager);

            FindParts(taskManager, httpServer);
        }

        /// <summary>
        /// Registers resources that classes will depend on
        /// </summary>
        private void RegisterResources(ITaskManager taskManager, IHttpServer httpServer, INetworkManager networkManager, IServerManager serverManager)
        {
            RegisterSingleInstance<IKernel>(Kernel);
            RegisterSingleInstance(Kernel);

            RegisterSingleInstance<IApplicationHost>(this);
            RegisterSingleInstance(Logger);

            RegisterSingleInstance(_applicationPaths);
            RegisterSingleInstance<IApplicationPaths>(_applicationPaths);
            RegisterSingleInstance(taskManager);
            RegisterSingleInstance<IIsoManager>(new PismoIsoManager(Logger));
            RegisterSingleInstance<IBlurayExaminer>(new BdInfoExaminer());
            RegisterSingleInstance<IHttpClient>(new HttpManager(_applicationPaths, Logger));
            RegisterSingleInstance<IZipClient>(new DotNetZipClient());
            RegisterSingleInstance<IWebSocketServer>(() => new AlchemyServer(Logger));
            RegisterSingleInstance(_jsonSerializer);
            RegisterSingleInstance(_xmlSerializer);
            RegisterSingleInstance(ProtobufSerializer);
            RegisterSingleInstance<IUdpServer>(new UdpServer(Logger), false);
            RegisterSingleInstance(httpServer, false);

            RegisterSingleInstance(networkManager);

            RegisterSingleInstance(serverManager);
        }

        /// <summary>
        /// Finds the parts.
        /// </summary>
        private void FindParts(ITaskManager taskManager, IHttpServer httpServer)
        {
            taskManager.AddTasks(GetExports<IScheduledTask>(false));

            httpServer.Init(GetExports<IRestfulService>(false));
        }

        /// <summary>
        /// Restarts this instance.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void Restart()
        {
            App.Instance.Restart();
        }

        /// <summary>
        /// Reloads the logger.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void ReloadLogger()
        {
            LogFilePath = Path.Combine(_applicationPaths.LogDirectoryPath, "Server-" + DateTime.Now.Ticks + ".log");

            NlogManager.AddFileTarget(LogFilePath, Kernel.Configuration.EnableDebugLevelLogging);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance can self update.
        /// </summary>
        /// <value><c>true</c> if this instance can self update; otherwise, <c>false</c>.</value>
        public bool CanSelfUpdate
        {
            get { return ClickOnceHelper.IsNetworkDeployed; }
        }

        /// <summary>
        /// Checks for update.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task{CheckForUpdateResult}.</returns>
        public Task<CheckForUpdateResult> CheckForApplicationUpdate(CancellationToken cancellationToken, IProgress<double> progress)
        {
            return new ApplicationUpdateCheck().CheckForApplicationUpdate(cancellationToken, progress);
        }

        /// <summary>
        /// Updates the application.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public Task UpdateApplication(CancellationToken cancellationToken, IProgress<double> progress)
        {
            return new ApplicationUpdater().UpdateApplication(cancellationToken, progress);
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
                .EnumerateFiles(_applicationPaths.PluginsPath, "*.dll", SearchOption.TopDirectoryOnly)
                .Select(LoadAssembly).Where(a => a != null))
            {
                yield return pluginAssembly;
            }

            var runningDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            var corePluginDirectory = Path.Combine(runningDirectory, "CorePlugins");

            // This will prevent the .dll file from getting locked, and allow us to replace it when needed
            foreach (var pluginAssembly in Directory
                .EnumerateFiles(corePluginDirectory, "*.dll", SearchOption.TopDirectoryOnly)
                .Select(LoadAssembly).Where(a => a != null))
            {
                yield return pluginAssembly;
            }

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
            App.Instance.Shutdown();
        }
    }
}
