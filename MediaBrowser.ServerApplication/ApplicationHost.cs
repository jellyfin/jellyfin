using MediaBrowser.Api;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Constants;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Implementations;
using MediaBrowser.Common.Implementations.ScheduledTasks;
using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.MediaInfo;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Updates;
using MediaBrowser.Providers;
using MediaBrowser.Server.Implementations;
using MediaBrowser.Server.Implementations.BdInfo;
using MediaBrowser.Server.Implementations.Configuration;
using MediaBrowser.Server.Implementations.Drawing;
using MediaBrowser.Server.Implementations.Dto;
using MediaBrowser.Server.Implementations.EntryPoints;
using MediaBrowser.Server.Implementations.HttpServer;
using MediaBrowser.Server.Implementations.IO;
using MediaBrowser.Server.Implementations.Library;
using MediaBrowser.Server.Implementations.LiveTv;
using MediaBrowser.Server.Implementations.Localization;
using MediaBrowser.Server.Implementations.MediaEncoder;
using MediaBrowser.Server.Implementations.Persistence;
using MediaBrowser.Server.Implementations.Providers;
using MediaBrowser.Server.Implementations.ServerManager;
using MediaBrowser.Server.Implementations.Session;
using MediaBrowser.Server.Implementations.WebSocket;
using MediaBrowser.ServerApplication.FFMpeg;
using MediaBrowser.ServerApplication.Native;
using MediaBrowser.WebDashboard.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        /// Gets the name of the web application that can be used for url building.
        /// All api urls will be of the form {protocol}://{host}:{port}/{appname}/...
        /// </summary>
        /// <value>The name of the web application.</value>
        public string WebApplicationName
        {
            get { return "mediabrowser"; }
        }

        /// <summary>
        /// Gets the HTTP server URL prefix.
        /// </summary>
        /// <value>The HTTP server URL prefix.</value>
        public string HttpServerUrlPrefix
        {
            get
            {
                return "http://+:" + ServerConfigurationManager.Configuration.HttpServerPortNumber + "/" + WebApplicationName + "/";
            }
        }

        /// <summary>
        /// Gets the configuration manager.
        /// </summary>
        /// <returns>IConfigurationManager.</returns>
        protected override IConfigurationManager GetConfigurationManager()
        {
            return new ServerConfigurationManager(ApplicationPaths, LogManager, XmlSerializer);
        }

        /// <summary>
        /// Gets or sets the server manager.
        /// </summary>
        /// <value>The server manager.</value>
        private IServerManager ServerManager { get; set; }
        /// <summary>
        /// Gets or sets the user manager.
        /// </summary>
        /// <value>The user manager.</value>
        public IUserManager UserManager { get; set; }
        /// <summary>
        /// Gets or sets the library manager.
        /// </summary>
        /// <value>The library manager.</value>
        internal ILibraryManager LibraryManager { get; set; }
        /// <summary>
        /// Gets or sets the directory watchers.
        /// </summary>
        /// <value>The directory watchers.</value>
        private IDirectoryWatchers DirectoryWatchers { get; set; }
        /// <summary>
        /// Gets or sets the provider manager.
        /// </summary>
        /// <value>The provider manager.</value>
        private IProviderManager ProviderManager { get; set; }
        /// <summary>
        /// Gets or sets the HTTP server.
        /// </summary>
        /// <value>The HTTP server.</value>
        private IHttpServer HttpServer { get; set; }
        private IDtoService DtoService { get; set; }
        private IImageProcessor ImageProcessor { get; set; }

        /// <summary>
        /// Gets or sets the media encoder.
        /// </summary>
        /// <value>The media encoder.</value>
        private IMediaEncoder MediaEncoder { get; set; }

        private ISessionManager SessionManager { get; set; }

        private ILiveTvManager LiveTvManager { get; set; }

        private ILocalizationManager LocalizationManager { get; set; }

        /// <summary>
        /// Gets or sets the user data repository.
        /// </summary>
        /// <value>The user data repository.</value>
        private IUserDataRepository UserDataRepository { get; set; }
        private IUserRepository UserRepository { get; set; }
        internal IDisplayPreferencesRepository DisplayPreferencesRepository { get; set; }
        private IItemRepository ItemRepository { get; set; }
        private INotificationsRepository NotificationsRepository { get; set; }

        private Task<IHttpServer> _httpServerCreationTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationHost"/> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="logManager">The log manager.</param>
        public ApplicationHost(ServerApplicationPaths applicationPaths, ILogManager logManager)
            : base(applicationPaths, logManager)
        {

        }

        /// <summary>
        /// Runs the startup tasks.
        /// </summary>
        /// <returns>Task.</returns>
        public override async Task RunStartupTasks()
        {
            await base.RunStartupTasks().ConfigureAwait(false);

            DirectoryWatchers.Start();

            Logger.Info("Core startup complete");

            Parallel.ForEach(GetExports<IServerEntryPoint>(), entryPoint =>
            {
                try
                {
                    entryPoint.Run();
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error in {0}", ex, entryPoint.GetType().Name);
                }
            });
        }

        /// <summary>
        /// Called when [logger loaded].
        /// </summary>
        protected override void OnLoggerLoaded()
        {
            base.OnLoggerLoaded();

            _httpServerCreationTask = Task.Run(() => ServerFactory.CreateServer(this, LogManager, "Media Browser", "dashboard/index.html"));
        }

        /// <summary>
        /// Registers resources that classes will depend on
        /// </summary>
        /// <returns>Task.</returns>
        protected override async Task RegisterResources()
        {
            ServerKernel = new Kernel();

            await base.RegisterResources().ConfigureAwait(false);

            RegisterSingleInstance<IHttpResultFactory>(new HttpResultFactory(LogManager));

            RegisterSingleInstance<IServerApplicationHost>(this);
            RegisterSingleInstance<IServerApplicationPaths>(ApplicationPaths);

            RegisterSingleInstance(ServerKernel);
            RegisterSingleInstance(ServerConfigurationManager);

            RegisterSingleInstance<IWebSocketServer>(() => new AlchemyServer(Logger));

            RegisterSingleInstance<IBlurayExaminer>(() => new BdInfoExaminer());

            var mediaEncoderTask = RegisterMediaEncoder();

            UserDataRepository = new SqliteUserDataRepository(ApplicationPaths, JsonSerializer, LogManager);
            RegisterSingleInstance(UserDataRepository);

            UserRepository = await GetUserRepository().ConfigureAwait(false);
            RegisterSingleInstance(UserRepository);

            DisplayPreferencesRepository = new SqliteDisplayPreferencesRepository(ApplicationPaths, JsonSerializer, LogManager);
            RegisterSingleInstance(DisplayPreferencesRepository);

            ItemRepository = new SqliteItemRepository(ApplicationPaths, JsonSerializer, LogManager);
            RegisterSingleInstance(ItemRepository);

            UserManager = new UserManager(Logger, ServerConfigurationManager, UserRepository);
            RegisterSingleInstance(UserManager);

            LibraryManager = new LibraryManager(Logger, TaskManager, UserManager, ServerConfigurationManager, UserDataRepository, () => DirectoryWatchers);
            RegisterSingleInstance(LibraryManager);

            DirectoryWatchers = new DirectoryWatchers(LogManager, TaskManager, LibraryManager, ServerConfigurationManager);
            RegisterSingleInstance(DirectoryWatchers);

            ProviderManager = new ProviderManager(HttpClient, ServerConfigurationManager, DirectoryWatchers, LogManager, LibraryManager);
            RegisterSingleInstance(ProviderManager);

            RegisterSingleInstance<ILibrarySearchEngine>(() => new LuceneSearchEngine(ApplicationPaths, LogManager, LibraryManager));

            SessionManager = new SessionManager(UserDataRepository, ServerConfigurationManager, Logger, UserRepository);
            RegisterSingleInstance(SessionManager);

            HttpServer = await _httpServerCreationTask.ConfigureAwait(false);
            RegisterSingleInstance(HttpServer, false);

            ServerManager = new ServerManager(this, JsonSerializer, Logger, ServerConfigurationManager);
            RegisterSingleInstance(ServerManager);

            LocalizationManager = new LocalizationManager(ServerConfigurationManager);
            RegisterSingleInstance(LocalizationManager);

            ImageProcessor = new ImageProcessor(Logger, ServerConfigurationManager.ApplicationPaths);
            RegisterSingleInstance(ImageProcessor);

            DtoService = new DtoService(Logger, LibraryManager, UserManager, UserDataRepository, ItemRepository, ImageProcessor);
            RegisterSingleInstance(DtoService);

            LiveTvManager = new LiveTvManager();
            RegisterSingleInstance(LiveTvManager);

            var displayPreferencesTask = Task.Run(async () => await ConfigureDisplayPreferencesRepositories().ConfigureAwait(false));
            var itemsTask = Task.Run(async () => await ConfigureItemRepositories().ConfigureAwait(false));
            var userdataTask = Task.Run(async () => await ConfigureUserDataRepositories().ConfigureAwait(false));

            await ConfigureNotificationsRepository().ConfigureAwait(false);

            await Task.WhenAll(itemsTask, displayPreferencesTask, userdataTask, mediaEncoderTask).ConfigureAwait(false);

            SetKernelProperties();
        }

        /// <summary>
        /// Registers the media encoder.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task RegisterMediaEncoder()
        {
            var info = await new FFMpegDownloader(Logger, ApplicationPaths, HttpClient, ZipClient).GetFFMpegInfo().ConfigureAwait(false);

            MediaEncoder = new MediaEncoder(LogManager.GetLogger("MediaEncoder"), ApplicationPaths, JsonSerializer, info.Path, info.ProbePath, info.Version);
            RegisterSingleInstance(MediaEncoder);
        }

        /// <summary>
        /// Sets the kernel properties.
        /// </summary>
        private void SetKernelProperties()
        {
            Parallel.Invoke(
                 () => ServerKernel.FFMpegManager = new FFMpegManager(ApplicationPaths, MediaEncoder, Logger, ItemRepository),
                 () => LocalizedStrings.StringFiles = GetExports<LocalizedStringData>(),
                 SetStaticProperties
                 );
        }

        /// <summary>
        /// Gets the user repository.
        /// </summary>
        /// <returns>Task{IUserRepository}.</returns>
        private async Task<IUserRepository> GetUserRepository()
        {
            var repo = new SqliteUserRepository(JsonSerializer, LogManager, ApplicationPaths);

            await repo.Initialize().ConfigureAwait(false);

            return repo;
        }

        /// <summary>
        /// Configures the repositories.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task ConfigureNotificationsRepository()
        {
            var repo = new SqliteNotificationsRepository(LogManager, ApplicationPaths);

            await repo.Initialize().ConfigureAwait(false);

            NotificationsRepository = repo;

            RegisterSingleInstance(NotificationsRepository);
        }

        /// <summary>
        /// Configures the repositories.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task ConfigureDisplayPreferencesRepositories()
        {
            await DisplayPreferencesRepository.Initialize().ConfigureAwait(false);
        }

        /// <summary>
        /// Configures the item repositories.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task ConfigureItemRepositories()
        {
            await ItemRepository.Initialize().ConfigureAwait(false);

            ((LibraryManager)LibraryManager).ItemRepository = ItemRepository;
        }

        /// <summary>
        /// Configures the user data repositories.
        /// </summary>
        /// <returns>Task.</returns>
        private Task ConfigureUserDataRepositories()
        {
            return UserDataRepository.Initialize();
        }

        /// <summary>
        /// Dirty hacks
        /// </summary>
        private void SetStaticProperties()
        {
            // For now there's no real way to inject these properly
            BaseItem.Logger = LogManager.GetLogger("BaseItem");
            BaseItem.ConfigurationManager = ServerConfigurationManager;
            BaseItem.LibraryManager = LibraryManager;
            BaseItem.ProviderManager = ProviderManager;
            BaseItem.LocalizationManager = LocalizationManager;
            BaseItem.ItemRepository = ItemRepository;
            User.XmlSerializer = XmlSerializer;
            User.UserManager = UserManager;
            LocalizedStrings.ApplicationPaths = ApplicationPaths;
        }

        /// <summary>
        /// Finds the parts.
        /// </summary>
        protected override void FindParts()
        {
            if (IsFirstRun)
            {
                RegisterServerWithAdministratorAccess();
            }

            base.FindParts();

            HttpServer.Init(GetExports<IRestfulService>(false));

            ServerManager.AddWebSocketListeners(GetExports<IWebSocketListener>(false));

            StartServer(true);

            LibraryManager.AddParts(GetExports<IResolverIgnoreRule>(),
                                    GetExports<IVirtualFolderCreator>(),
                                    GetExports<IItemResolver>(),
                                    GetExports<IIntroProvider>(),
                                    GetExports<IBaseItemComparer>(),
                                    GetExports<ILibraryPrescanTask>(),
                                    GetExports<ILibraryPostScanTask>(),
                                    GetExports<IMetadataSaver>());

            ProviderManager.AddParts(GetExports<BaseMetadataProvider>());

            SessionManager.AddParts(GetExports<ISessionRemoteController>());

            ImageProcessor.AddParts(GetExports<IImageEnhancer>());

            LiveTvManager.AddParts(GetExports<ILiveTvService>());
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        /// <param name="retryOnFailure">if set to <c>true</c> [retry on failure].</param>
        private void StartServer(bool retryOnFailure)
        {
            try
            {
                ServerManager.Start(HttpServerUrlPrefix, ServerConfigurationManager.Configuration.EnableHttpLevelLogging);
            }
            catch
            {
                if (retryOnFailure)
                {
                    RegisterServerWithAdministratorAccess();

                    StartServer(false);
                }
                else
                {
                    throw;
                }
            }

            ServerManager.StartWebSocketServer();
        }

        /// <summary>
        /// Called when [configuration updated].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected override void OnConfigurationUpdated(object sender, EventArgs e)
        {
            base.OnConfigurationUpdated(sender, e);

            HttpServer.EnableHttpRequestLogging = ServerConfigurationManager.Configuration.EnableHttpLevelLogging;

            if (!string.Equals(HttpServer.UrlPrefix, HttpServerUrlPrefix, StringComparison.OrdinalIgnoreCase))
            {
                NotifyPendingRestart();
            }

            else if (!ServerManager.SupportsNativeWebSocket && ServerManager.WebSocketPortNumber != ServerConfigurationManager.Configuration.LegacyWebSocketPortNumber)
            {
                NotifyPendingRestart();
            }
        }

        /// <summary>
        /// Restarts this instance.
        /// </summary>
        public override async Task Restart()
        {
            try
            {
                await ServerManager.SendWebSocketMessageAsync("ServerRestarting", () => string.Empty, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error sending server restart web socket message", ex);
            }

            NativeApp.Restart();
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance can self update.
        /// </summary>
        /// <value><c>true</c> if this instance can self update; otherwise, <c>false</c>.</value>
        public override bool CanSelfUpdate
        {
            get
            {
#if DEBUG
                return false;
#endif
                return true;
            }
        }

        /// <summary>
        /// Gets the composable part assemblies.
        /// </summary>
        /// <returns>IEnumerable{Assembly}.</returns>
        protected override IEnumerable<Assembly> GetComposablePartAssemblies()
        {
            var list = Directory.EnumerateFiles(ApplicationPaths.PluginsPath, "*.dll", SearchOption.TopDirectoryOnly)
                .Select(LoadAssembly)
                .Where(a => a != null)
                .ToList();

            // Gets all plugin assemblies by first reading all bytes of the .dll and calling Assembly.Load against that
            // This will prevent the .dll file from getting locked, and allow us to replace it when needed

            // Include composable parts in the Api assembly 
            list.Add(typeof(ApiEntryPoint).Assembly);

            // Include composable parts in the Dashboard assembly 
            list.Add(typeof(DashboardInfo).Assembly);

            // Include composable parts in the Model assembly 
            list.Add(typeof(SystemInfo).Assembly);

            // Include composable parts in the Common assembly 
            list.Add(typeof(IApplicationHost).Assembly);

            // Include composable parts in the Controller assembly 
            list.Add(typeof(Kernel).Assembly);

            // Include composable parts in the Providers assembly 
            list.Add(typeof(ImagesByNameProvider).Assembly);

            // Common implementations
            list.Add(typeof(TaskManager).Assembly);

            // Server implementations
            list.Add(typeof(ServerApplicationPaths).Assembly);

            list.AddRange(Assemblies.GetAssembliesWithParts());

            // Include composable parts in the running assembly
            list.Add(GetType().Assembly);

            return list;
        }

        private readonly string _systemId = Environment.MachineName.GetMD5().ToString();

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
                FailedPluginAssemblies = FailedAssemblies.ToList(),
                InProgressInstallations = InstallationManager.CurrentInstallations.Select(i => i.Item1).ToList(),
                CompletedInstallations = InstallationManager.CompletedInstallations.ToList(),
                Id = _systemId,
                ProgramDataPath = ApplicationPaths.ProgramDataPath,
                MacAddress = GetMacAddress(),
                HttpServerPortNumber = ServerConfigurationManager.Configuration.HttpServerPortNumber,
                OperatingSystem = Environment.OSVersion.ToString()
            };
        }

        /// <summary>
        /// Gets the mac address.
        /// </summary>
        /// <returns>System.String.</returns>
        private string GetMacAddress()
        {
            try
            {
                return NetworkManager.GetMacAddress();
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error getting mac address", ex);
                return null;
            }
        }

        /// <summary>
        /// Shuts down.
        /// </summary>
        public override async Task Shutdown()
        {
            try
            {
                await ServerManager.SendWebSocketMessageAsync("ServerShuttingDown", () => string.Empty, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error sending server shutdown web socket message", ex);
            }

            NativeApp.Shutdown();
        }

        /// <summary>
        /// Registers the server with administrator access.
        /// </summary>
        private void RegisterServerWithAdministratorAccess()
        {
            Logger.Info("Requesting administrative access to authorize http server");

            try
            {
                ServerAuthorization.AuthorizeServer(ServerConfigurationManager.Configuration.HttpServerPortNumber,
                    HttpServerUrlPrefix, ServerConfigurationManager.Configuration.LegacyWebSocketPortNumber,
                    UdpServerEntryPoint.PortNumber,
                    ConfigurationManager.CommonApplicationPaths.TempDirectory);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error authorizing server", ex);
            }
        }

        /// <summary>
        /// Checks for update.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task{CheckForUpdateResult}.</returns>
        public override async Task<CheckForUpdateResult> CheckForApplicationUpdate(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var availablePackages = await InstallationManager.GetAvailablePackagesWithoutRegistrationInfo(cancellationToken).ConfigureAwait(false);

            var package = availablePackages.FirstOrDefault(p => p.name.Equals(Constants.MbServerPkgName, StringComparison.OrdinalIgnoreCase));

            if (package == null)
            {
                return null;
            }

            var version = package.versions
                .OrderByDescending(v => v.version)
                .FirstOrDefault(v => v.classification <= ConfigurationManager.CommonConfiguration.SystemUpdateLevel);

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
        public override async Task UpdateApplication(PackageVersionInfo package, CancellationToken cancellationToken, IProgress<double> progress)
        {
            await InstallationManager.InstallPackage(package, progress, cancellationToken).ConfigureAwait(false);

            OnApplicationUpdated(package.version);
        }

        /// <summary>
        /// Gets the HTTP message handler.
        /// </summary>
        /// <param name="enableHttpCompression">if set to <c>true</c> [enable HTTP compression].</param>
        /// <returns>HttpMessageHandler.</returns>
        protected override HttpMessageHandler GetHttpMessageHandler(bool enableHttpCompression)
        {
            return HttpMessageHandlerFactory.GetHttpMessageHandler(enableHttpCompression);
        }

        protected override void ConfigureAutoRunAtStartup(bool autorun)
        {
            Autorun.Configure(autorun);
        }
    }
}
