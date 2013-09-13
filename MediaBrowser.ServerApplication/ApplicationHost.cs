using MediaBrowser.Api;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Constants;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Implementations;
using MediaBrowser.Common.Implementations.IO;
using MediaBrowser.Common.Implementations.ScheduledTasks;
using MediaBrowser.Common.Implementations.Updates;
using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.MediaInfo;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.IsoMounter;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.System;
using MediaBrowser.Providers;
using MediaBrowser.Server.Implementations;
using MediaBrowser.Server.Implementations.BdInfo;
using MediaBrowser.Server.Implementations.Configuration;
using MediaBrowser.Server.Implementations.Dto;
using MediaBrowser.Server.Implementations.HttpServer;
using MediaBrowser.Server.Implementations.IO;
using MediaBrowser.Server.Implementations.Library;
using MediaBrowser.Server.Implementations.Localization;
using MediaBrowser.Server.Implementations.MediaEncoder;
using MediaBrowser.Server.Implementations.Persistence;
using MediaBrowser.Server.Implementations.Providers;
using MediaBrowser.Server.Implementations.ServerManager;
using MediaBrowser.Server.Implementations.Session;
using MediaBrowser.Server.Implementations.WebSocket;
using MediaBrowser.ServerApplication.Implementations;
using MediaBrowser.WebDashboard.Api;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
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
        internal const int UdpServerPort = 7359;

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
            get { return "server"; }
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
        /// Gets or sets the zip client.
        /// </summary>
        /// <value>The zip client.</value>
        private IZipClient ZipClient { get; set; }
        /// <summary>
        /// Gets or sets the HTTP server.
        /// </summary>
        /// <value>The HTTP server.</value>
        private IHttpServer HttpServer { get; set; }
        private IDtoService DtoService { get; set; }

        /// <summary>
        /// Gets or sets the media encoder.
        /// </summary>
        /// <value>The media encoder.</value>
        private IMediaEncoder MediaEncoder { get; set; }

        private IIsoManager IsoManager { get; set; }

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

        /// <summary>
        /// The full path to our startmenu shortcut
        /// </summary>
        protected override string ProductShortcutPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Media Browser 3", "Media Browser Server.lnk"); }
        }

        private Task<IHttpServer> _httpServerCreationTask;

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

            IsoManager = new IsoManager();
            RegisterSingleInstance(IsoManager);

            RegisterSingleInstance<IBlurayExaminer>(() => new BdInfoExaminer());

            ZipClient = new DotNetZipClient();
            RegisterSingleInstance(ZipClient);

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

            MediaEncoder = new MediaEncoder(LogManager.GetLogger("MediaEncoder"), ZipClient, ApplicationPaths, JsonSerializer, HttpClient);
            RegisterSingleInstance(MediaEncoder);

            var clientConnectionManager = new SessionManager(UserDataRepository, ServerConfigurationManager, Logger, UserRepository);
            RegisterSingleInstance<ISessionManager>(clientConnectionManager);

            HttpServer = await _httpServerCreationTask.ConfigureAwait(false);
            RegisterSingleInstance(HttpServer, false);

            ServerManager = new ServerManager(this, JsonSerializer, Logger, ServerConfigurationManager);
            RegisterSingleInstance(ServerManager);

            LocalizationManager = new LocalizationManager(ServerConfigurationManager);
            RegisterSingleInstance(LocalizationManager);

            DtoService = new DtoService(Logger, LibraryManager, UserManager, UserDataRepository, ItemRepository);
            RegisterSingleInstance(DtoService);

            var displayPreferencesTask = Task.Run(async () => await ConfigureDisplayPreferencesRepositories().ConfigureAwait(false));
            var itemsTask = Task.Run(async () => await ConfigureItemRepositories().ConfigureAwait(false));
            var userdataTask = Task.Run(async () => await ConfigureUserDataRepositories().ConfigureAwait(false));

            await ConfigureNotificationsRepository().ConfigureAwait(false);

            await Task.WhenAll(itemsTask, displayPreferencesTask, userdataTask).ConfigureAwait(false);

            SetKernelProperties();
        }

        /// <summary>
        /// Sets the kernel properties.
        /// </summary>
        private void SetKernelProperties()
        {
            ServerKernel.ImageManager = new ImageManager(LogManager.GetLogger("ImageManager"),
                                                         ApplicationPaths, ItemRepository);
            Parallel.Invoke(
                 () => ServerKernel.FFMpegManager = new FFMpegManager(ApplicationPaths, MediaEncoder, Logger, ItemRepository),
                 () => ServerKernel.ImageManager.ImageEnhancers = GetExports<IImageEnhancer>().OrderBy(e => e.Priority).ToArray(),
                 () => LocalizedStrings.StringFiles = GetExports<LocalizedStringData>(),
                 SetStaticProperties
                 );
        }

        private async Task<IUserRepository> GetUserRepository()
        {
            var dbFile = Path.Combine(ApplicationPaths.DataPath, "users.db");

            var connection = await ConnectToDb(dbFile).ConfigureAwait(false);

            var repo = new SqliteUserRepository(connection, JsonSerializer, LogManager);

            repo.Initialize();

            return repo;
        }

        /// <summary>
        /// Configures the repositories.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task ConfigureNotificationsRepository()
        {
            var dbFile = Path.Combine(ApplicationPaths.DataPath, "notifications.db");

            var connection = await ConnectToDb(dbFile).ConfigureAwait(false);

            var repo = new SqliteNotificationsRepository(connection, LogManager);

            repo.Initialize();

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
        /// Connects to db.
        /// </summary>
        /// <param name="dbPath">The db path.</param>
        /// <returns>Task{IDbConnection}.</returns>
        /// <exception cref="System.ArgumentNullException">dbPath</exception>
        private static async Task<SQLiteConnection> ConnectToDb(string dbPath)
        {
            if (string.IsNullOrEmpty(dbPath))
            {
                throw new ArgumentNullException("dbPath");
            }

            var connectionstr = new SQLiteConnectionStringBuilder
            {
                PageSize = 4096,
                CacheSize = 4096,
                SyncMode = SynchronizationModes.Normal,
                DataSource = dbPath,
                JournalMode = SQLiteJournalModeEnum.Wal
            };

            var connection = new SQLiteConnection(connectionstr.ConnectionString);

            await connection.OpenAsync().ConfigureAwait(false);

            return connection;
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

            ProviderManager.AddParts(GetExports<BaseMetadataProvider>().ToArray());

            IsoManager.AddParts(GetExports<IIsoMounter>().ToArray());
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        /// <param name="retryOnFailure">if set to <c>true</c> [retry on failure].</param>
        private void StartServer(bool retryOnFailure)
        {
            try
            {
                ServerManager.Start();
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
        }

        /// <summary>
        /// Called when [configuration updated].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected override void OnConfigurationUpdated(object sender, EventArgs e)
        {
            base.OnConfigurationUpdated(sender, e);

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
        public override void Restart()
        {
            try
            {
                var task = ServerManager.SendWebSocketMessageAsync("ServerRestarting", () => string.Empty, CancellationToken.None);
                task.Wait();
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error sending server restart web socket message", ex);
            }

            App.Instance.Restart();
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
                return ConfigurationManager.CommonConfiguration.EnableAutoUpdate;
            }
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
            yield return typeof(ApiEntryPoint).Assembly;

            // Include composable parts in the Dashboard assembly 
            yield return typeof(DashboardInfo).Assembly;

            // Include composable parts in the Model assembly 
            yield return typeof(SystemInfo).Assembly;

            // Include composable parts in the Common assembly 
            yield return typeof(IApplicationHost).Assembly;

            // Include composable parts in the Controller assembly 
            yield return typeof(Kernel).Assembly;

            // Include composable parts in the Providers assembly 
            yield return typeof(ImagesByNameProvider).Assembly;

            // Common implementations
            yield return typeof(TaskManager).Assembly;

            // Server implementations
            yield return typeof(ServerApplicationPaths).Assembly;

            // Pismo
            yield return typeof(PismoIsoManager).Assembly;

            // Include composable parts in the running assembly
            yield return GetType().Assembly;
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
                FailedPluginAssemblies = FailedAssemblies.ToArray(),
                InProgressInstallations = InstallationManager.CurrentInstallations.Select(i => i.Item1).ToArray(),
                CompletedInstallations = InstallationManager.CompletedInstallations.ToArray(),
                Id = _systemId,
                ProgramDataPath = ApplicationPaths.ProgramDataPath,
                MacAddress = GetMacAddress(),
                HttpServerPortNumber = ServerConfigurationManager.Configuration.HttpServerPortNumber
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
        public override void Shutdown()
        {
            try
            {
                var task = ServerManager.SendWebSocketMessageAsync("ServerShuttingDown", () => string.Empty, CancellationToken.None);
                task.Wait();
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error sending server shutdown web socket message", ex);
            }

            App.Instance.Dispatcher.Invoke(App.Instance.Shutdown);
        }

        /// <summary>
        /// Registers the server with administrator access.
        /// </summary>
        private void RegisterServerWithAdministratorAccess()
        {
            Logger.Info("Requesting administrative access to authorize http server");

            // Create a temp file path to extract the bat file to
            var tmpFile = Path.Combine(ConfigurationManager.CommonApplicationPaths.TempDirectory, Guid.NewGuid() + ".bat");

            // Extract the bat file
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MediaBrowser.ServerApplication.RegisterServer.bat"))
            {
                using (var fileStream = File.Create(tmpFile))
                {
                    stream.CopyTo(fileStream);
                }
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = tmpFile,

                Arguments = string.Format("{0} {1} {2} {3}", ServerConfigurationManager.Configuration.HttpServerPortNumber,
                HttpServerUrlPrefix,
                UdpServerPort,
                ServerConfigurationManager.Configuration.LegacyWebSocketPortNumber),

                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Verb = "runas",
                ErrorDialog = false
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
            }
        }

        protected override string ApplicationUpdatePackageName
        {
            get { return Constants.MbServerPkgName; }
        }

        protected override HttpMessageHandler GetHttpMessageHandler(bool enableHttpCompression)
        {
            return new WebRequestHandler
            {
                CachePolicy = new RequestCachePolicy(RequestCacheLevel.Revalidate),
                AutomaticDecompression = enableHttpCompression ? DecompressionMethods.Deflate : DecompressionMethods.None
            };
        }
    }
}
