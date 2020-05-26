#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emby.Dlna;
using Emby.Dlna.Main;
using Emby.Dlna.Ssdp;
using Emby.Drawing;
using Emby.Notifications;
using Emby.Photos;
using Emby.Server.Implementations.Archiving;
using Emby.Server.Implementations.Channels;
using Emby.Server.Implementations.Collections;
using Emby.Server.Implementations.Configuration;
using Emby.Server.Implementations.Cryptography;
using Emby.Server.Implementations.Data;
using Emby.Server.Implementations.Devices;
using Emby.Server.Implementations.Dto;
using Emby.Server.Implementations.HttpServer;
using Emby.Server.Implementations.HttpServer.Security;
using Emby.Server.Implementations.IO;
using Emby.Server.Implementations.Library;
using Emby.Server.Implementations.LiveTv;
using Emby.Server.Implementations.Localization;
using Emby.Server.Implementations.Net;
using Emby.Server.Implementations.Playlists;
using Emby.Server.Implementations.ScheduledTasks;
using Emby.Server.Implementations.Security;
using Emby.Server.Implementations.Serialization;
using Emby.Server.Implementations.Services;
using Emby.Server.Implementations.Session;
using Emby.Server.Implementations.TV;
using Emby.Server.Implementations.Updates;
using Emby.Server.Implementations.SyncPlay;
using MediaBrowser.Api;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Controller.TV;
using MediaBrowser.Controller.SyncPlay;
using MediaBrowser.LocalMetadata.Savers;
using MediaBrowser.MediaEncoding.BdInfo;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Providers.Chapters;
using MediaBrowser.Providers.Manager;
using MediaBrowser.Providers.Plugins.TheTvdb;
using MediaBrowser.Providers.Subtitles;
using MediaBrowser.WebDashboard.Api;
using MediaBrowser.XbmcMetadata.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prometheus.DotNetRuntime;
using OperatingSystem = MediaBrowser.Common.System.OperatingSystem;

namespace Emby.Server.Implementations
{
    /// <summary>
    /// Class CompositionRoot.
    /// </summary>
    public abstract class ApplicationHost : IServerApplicationHost, IDisposable
    {
        /// <summary>
        /// The environment variable prefixes to log at server startup.
        /// </summary>
        private static readonly string[] _relevantEnvVarPrefixes = { "JELLYFIN_", "DOTNET_", "ASPNETCORE_" };

        private readonly IFileSystem _fileSystemManager;
        private readonly INetworkManager _networkManager;
        private readonly IXmlSerializer _xmlSerializer;
        private readonly IStartupOptions _startupOptions;

        private IMediaEncoder _mediaEncoder;
        private ISessionManager _sessionManager;
        private IHttpServer _httpServer;
        private IHttpClient _httpClient;

        /// <summary>
        /// Gets a value indicating whether this instance can self restart.
        /// </summary>
        public bool CanSelfRestart => _startupOptions.RestartPath != null;

        public virtual bool CanLaunchWebBrowser
        {
            get
            {
                if (!Environment.UserInteractive)
                {
                    return false;
                }

                if (_startupOptions.IsService)
                {
                    return false;
                }

                if (OperatingSystem.Id == OperatingSystemId.Windows
                    || OperatingSystem.Id == OperatingSystemId.Darwin)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Occurs when [has pending restart changed].
        /// </summary>
        public event EventHandler HasPendingRestartChanged;

        /// <summary>
        /// Gets a value indicating whether this instance has changes that require the entire application to restart.
        /// </summary>
        /// <value><c>true</c> if this instance has pending application restart; otherwise, <c>false</c>.</value>
        public bool HasPendingRestart { get; private set; }

        /// <inheritdoc />
        public bool IsShuttingDown { get; private set; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        protected ILogger Logger { get; }

        private IPlugin[] _plugins;

        /// <summary>
        /// Gets the plugins.
        /// </summary>
        /// <value>The plugins.</value>
        public IReadOnlyList<IPlugin> Plugins => _plugins;

        /// <summary>
        /// Gets the logger factory.
        /// </summary>
        protected ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Gets or sets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        protected ServerApplicationPaths ApplicationPaths { get; set; }

        /// <summary>
        /// Gets or sets all concrete types.
        /// </summary>
        /// <value>All concrete types.</value>
        private Type[] _allConcreteTypes;

        /// <summary>
        /// The disposable parts.
        /// </summary>
        private readonly List<IDisposable> _disposableParts = new List<IDisposable>();

        /// <summary>
        /// Gets the configuration manager.
        /// </summary>
        /// <value>The configuration manager.</value>
        protected IConfigurationManager ConfigurationManager { get; set; }

        /// <summary>
        /// Gets or sets the service provider.
        /// </summary>
        public IServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// Gets the http port for the webhost.
        /// </summary>
        public int HttpPort { get; private set; }

        /// <summary>
        /// Gets the https port for the webhost.
        /// </summary>
        public int HttpsPort { get; private set; }

        /// <summary>
        /// Gets the server configuration manager.
        /// </summary>
        /// <value>The server configuration manager.</value>
        public IServerConfigurationManager ServerConfigurationManager => (IServerConfigurationManager)ConfigurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationHost" /> class.
        /// </summary>
        public ApplicationHost(
            ServerApplicationPaths applicationPaths,
            ILoggerFactory loggerFactory,
            IStartupOptions options,
            IFileSystem fileSystem,
            INetworkManager networkManager)
        {
            _xmlSerializer = new MyXmlSerializer();

            _networkManager = networkManager;
            networkManager.LocalSubnetsFn = GetConfiguredLocalSubnets;

            ApplicationPaths = applicationPaths;
            LoggerFactory = loggerFactory;
            _fileSystemManager = fileSystem;

            ConfigurationManager = new ServerConfigurationManager(ApplicationPaths, LoggerFactory, _xmlSerializer, _fileSystemManager);

            Logger = LoggerFactory.CreateLogger<ApplicationHost>();

            _startupOptions = options;

            // Initialize runtime stat collection
            if (ServerConfigurationManager.Configuration.EnableMetrics)
            {
                DotNetRuntimeStatsBuilder.Default().StartCollecting();
            }

            fileSystem.AddShortcutHandler(new MbLinkShortcutHandler(fileSystem));

            _networkManager.NetworkChanged += OnNetworkChanged;

            CertificateInfo = new CertificateInfo
            {
                Path = ServerConfigurationManager.Configuration.CertificatePath,
                Password = ServerConfigurationManager.Configuration.CertificatePassword
            };
            Certificate = GetCertificate(CertificateInfo);
        }

        public string ExpandVirtualPath(string path)
        {
            var appPaths = ApplicationPaths;

            return path.Replace(appPaths.VirtualDataPath, appPaths.DataPath, StringComparison.OrdinalIgnoreCase)
                .Replace(appPaths.VirtualInternalMetadataPath, appPaths.InternalMetadataPath, StringComparison.OrdinalIgnoreCase);
        }

        public string ReverseVirtualPath(string path)
        {
            var appPaths = ApplicationPaths;

            return path.Replace(appPaths.DataPath, appPaths.VirtualDataPath, StringComparison.OrdinalIgnoreCase)
                .Replace(appPaths.InternalMetadataPath, appPaths.VirtualInternalMetadataPath, StringComparison.OrdinalIgnoreCase);
        }

        private string[] GetConfiguredLocalSubnets()
        {
            return ServerConfigurationManager.Configuration.LocalNetworkSubnets;
        }

        private void OnNetworkChanged(object sender, EventArgs e)
        {
            _validAddressResults.Clear();
        }

        /// <inheritdoc />
        public Version ApplicationVersion { get; } = typeof(ApplicationHost).Assembly.GetName().Version;

        /// <inheritdoc />
        public string ApplicationVersionString { get; } = typeof(ApplicationHost).Assembly.GetName().Version.ToString(3);

        /// <summary>
        /// Gets the current application user agent.
        /// </summary>
        /// <value>The application user agent.</value>
        public string ApplicationUserAgent => Name.Replace(' ', '-') + "/" + ApplicationVersionString;

        /// <summary>
        /// Gets the email address for use within a comment section of a user agent field.
        /// Presently used to provide contact information to MusicBrainz service.
        /// </summary>
        public string ApplicationUserAgentAddress { get; } = "team@jellyfin.org";

        /// <summary>
        /// Gets the current application name.
        /// </summary>
        /// <value>The application name.</value>
        public string ApplicationProductName { get; } = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).ProductName;

        private DeviceId _deviceId;

        public string SystemId
        {
            get
            {
                if (_deviceId == null)
                {
                    _deviceId = new DeviceId(ApplicationPaths, LoggerFactory);
                }

                return _deviceId.Value;
            }
        }

        /// <inheritdoc/>
        public string Name => ApplicationProductName;

        /// <summary>
        /// Creates an instance of type and resolves all constructor dependencies.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.Object.</returns>
        public object CreateInstance(Type type)
            => ActivatorUtilities.CreateInstance(ServiceProvider, type);

        /// <summary>
        /// Creates an instance of type and resolves all constructor dependencies.
        /// </summary>
        /// /// <typeparam name="T">The type.</typeparam>
        /// <returns>T.</returns>
        public T CreateInstance<T>()
            => ActivatorUtilities.CreateInstance<T>(ServiceProvider);

        /// <summary>
        /// Creates the instance safe.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.Object.</returns>
        protected object CreateInstanceSafe(Type type)
        {
            try
            {
                Logger.LogDebug("Creating instance of {Type}", type);
                return ActivatorUtilities.CreateInstance(ServiceProvider, type);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating {Type}", type);
                return null;
            }
        }

        /// <summary>
        /// Resolves this instance.
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <returns>``0.</returns>
        public T Resolve<T>() => ServiceProvider.GetService<T>();

        /// <summary>
        /// Gets the export types.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>IEnumerable{Type}.</returns>
        public IEnumerable<Type> GetExportTypes<T>()
        {
            var currentType = typeof(T);

            return _allConcreteTypes.Where(i => currentType.IsAssignableFrom(i));
        }

        /// <inheritdoc />
        public IReadOnlyCollection<T> GetExports<T>(bool manageLifetime = true)
        {
            // Convert to list so this isn't executed for each iteration
            var parts = GetExportTypes<T>()
                .Select(CreateInstanceSafe)
                .Where(i => i != null)
                .Cast<T>()
                .ToList();

            if (manageLifetime)
            {
                lock (_disposableParts)
                {
                    _disposableParts.AddRange(parts.OfType<IDisposable>());
                }
            }

            return parts;
        }

        /// <summary>
        /// Runs the startup tasks.
        /// </summary>
        /// <returns><see cref="Task" />.</returns>
        public async Task RunStartupTasksAsync()
        {
            Logger.LogInformation("Running startup tasks");

            Resolve<ITaskManager>().AddTasks(GetExports<IScheduledTask>(false));

            ConfigurationManager.ConfigurationUpdated += OnConfigurationUpdated;

            _mediaEncoder.SetFFmpegPath();

            Logger.LogInformation("ServerId: {0}", SystemId);

            var entryPoints = GetExports<IServerEntryPoint>();

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            await Task.WhenAll(StartEntryPoints(entryPoints, true)).ConfigureAwait(false);
            Logger.LogInformation("Executed all pre-startup entry points in {Elapsed:g}", stopWatch.Elapsed);

            Logger.LogInformation("Core startup complete");
            _httpServer.GlobalResponse = null;

            stopWatch.Restart();
            await Task.WhenAll(StartEntryPoints(entryPoints, false)).ConfigureAwait(false);
            Logger.LogInformation("Executed all post-startup entry points in {Elapsed:g}", stopWatch.Elapsed);
            stopWatch.Stop();
        }

        private IEnumerable<Task> StartEntryPoints(IEnumerable<IServerEntryPoint> entryPoints, bool isBeforeStartup)
        {
            foreach (var entryPoint in entryPoints)
            {
                if (isBeforeStartup != (entryPoint is IRunBeforeStartup))
                {
                    continue;
                }

                Logger.LogDebug("Starting entry point {Type}", entryPoint.GetType());

                yield return entryPoint.RunAsync();
            }
        }

        /// <inheritdoc/>
        public void Init(IServiceCollection serviceCollection)
        {
            HttpPort = ServerConfigurationManager.Configuration.HttpServerPortNumber;
            HttpsPort = ServerConfigurationManager.Configuration.HttpsPortNumber;

            // Safeguard against invalid configuration
            if (HttpPort == HttpsPort)
            {
                HttpPort = ServerConfiguration.DefaultHttpPort;
                HttpsPort = ServerConfiguration.DefaultHttpsPort;
            }

            if (Plugins != null)
            {
                var pluginBuilder = new StringBuilder();

                foreach (var plugin in Plugins)
                {
                    pluginBuilder.AppendLine(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0} {1}",
                            plugin.Name,
                            plugin.Version));
                }

                Logger.LogInformation("Plugins: {Plugins}", pluginBuilder.ToString());
            }

            DiscoverTypes();

            RegisterServices(serviceCollection);
        }

        public Task ExecuteHttpHandlerAsync(HttpContext context, Func<Task> next)
            => _httpServer.RequestHandler(context);

        /// <summary>
        /// Registers services/resources with the service collection that will be available via DI.
        /// </summary>
        protected virtual void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(_startupOptions);

            serviceCollection.AddMemoryCache();

            serviceCollection.AddSingleton(ConfigurationManager);
            serviceCollection.AddSingleton<IApplicationHost>(this);

            serviceCollection.AddSingleton<IApplicationPaths>(ApplicationPaths);

            serviceCollection.AddSingleton<IJsonSerializer, JsonSerializer>();

            serviceCollection.AddSingleton(_fileSystemManager);
            serviceCollection.AddSingleton<TvdbClientManager>();

            serviceCollection.AddSingleton<IHttpClient, HttpClientManager.HttpClientManager>();

            serviceCollection.AddSingleton(_networkManager);

            serviceCollection.AddSingleton<IIsoManager, IsoManager>();

            serviceCollection.AddSingleton<ITaskManager, TaskManager>();

            serviceCollection.AddSingleton(_xmlSerializer);

            serviceCollection.AddSingleton<IStreamHelper, StreamHelper>();

            serviceCollection.AddSingleton<ICryptoProvider, CryptographyProvider>();

            serviceCollection.AddSingleton<ISocketFactory, SocketFactory>();

            serviceCollection.AddSingleton<IInstallationManager, InstallationManager>();

            serviceCollection.AddSingleton<IZipClient, ZipClient>();

            serviceCollection.AddSingleton<IHttpResultFactory, HttpResultFactory>();

            serviceCollection.AddSingleton<IServerApplicationHost>(this);
            serviceCollection.AddSingleton<IServerApplicationPaths>(ApplicationPaths);

            serviceCollection.AddSingleton(ServerConfigurationManager);

            serviceCollection.AddSingleton<ILocalizationManager, LocalizationManager>();

            serviceCollection.AddSingleton<IBlurayExaminer, BdInfoExaminer>();

            serviceCollection.AddSingleton<IUserDataRepository, SqliteUserDataRepository>();
            serviceCollection.AddSingleton<IUserDataManager, UserDataManager>();

            serviceCollection.AddSingleton<IDisplayPreferencesRepository, SqliteDisplayPreferencesRepository>();

            serviceCollection.AddSingleton<IItemRepository, SqliteItemRepository>();

            serviceCollection.AddSingleton<IAuthenticationRepository, AuthenticationRepository>();

            serviceCollection.AddSingleton<IUserRepository, SqliteUserRepository>();

            // TODO: Refactor to eliminate the circular dependency here so that Lazy<T> isn't required
            serviceCollection.AddTransient(provider => new Lazy<IDtoService>(provider.GetRequiredService<IDtoService>));
            serviceCollection.AddSingleton<IUserManager, UserManager>();

            // TODO: Refactor to eliminate the circular dependency here so that Lazy<T> isn't required
            // TODO: Add StartupOptions.FFmpegPath to IConfiguration and remove this custom activation
            serviceCollection.AddTransient(provider => new Lazy<EncodingHelper>(provider.GetRequiredService<EncodingHelper>));
            serviceCollection.AddSingleton<IMediaEncoder>(provider =>
                ActivatorUtilities.CreateInstance<MediaBrowser.MediaEncoding.Encoder.MediaEncoder>(provider, _startupOptions.FFmpegPath ?? string.Empty));

            // TODO: Refactor to eliminate the circular dependencies here so that Lazy<T> isn't required
            serviceCollection.AddTransient(provider => new Lazy<ILibraryMonitor>(provider.GetRequiredService<ILibraryMonitor>));
            serviceCollection.AddTransient(provider => new Lazy<IProviderManager>(provider.GetRequiredService<IProviderManager>));
            serviceCollection.AddTransient(provider => new Lazy<IUserViewManager>(provider.GetRequiredService<IUserViewManager>));
            serviceCollection.AddSingleton<ILibraryManager, LibraryManager>();

            serviceCollection.AddSingleton<IMusicManager, MusicManager>();

            serviceCollection.AddSingleton<ILibraryMonitor, LibraryMonitor>();

            serviceCollection.AddSingleton<ISearchEngine, SearchEngine>();

            serviceCollection.AddSingleton<ServiceController>();
            serviceCollection.AddSingleton<IHttpServer, HttpListenerHost>();

            serviceCollection.AddSingleton<IImageProcessor, ImageProcessor>();

            serviceCollection.AddSingleton<ITVSeriesManager, TVSeriesManager>();

            serviceCollection.AddSingleton<IDeviceManager, DeviceManager>();

            serviceCollection.AddSingleton<IMediaSourceManager, MediaSourceManager>();

            serviceCollection.AddSingleton<ISubtitleManager, SubtitleManager>();

            serviceCollection.AddSingleton<IProviderManager, ProviderManager>();

            // TODO: Refactor to eliminate the circular dependency here so that Lazy<T> isn't required
            serviceCollection.AddTransient(provider => new Lazy<ILiveTvManager>(provider.GetRequiredService<ILiveTvManager>));
            serviceCollection.AddSingleton<IDtoService, DtoService>();

            serviceCollection.AddSingleton<IChannelManager, ChannelManager>();

            serviceCollection.AddSingleton<ISessionManager, SessionManager>();

            serviceCollection.AddSingleton<IDlnaManager, DlnaManager>();

            serviceCollection.AddSingleton<ICollectionManager, CollectionManager>();

            serviceCollection.AddSingleton<IPlaylistManager, PlaylistManager>();

            serviceCollection.AddSingleton<ISyncPlayManager, SyncPlayManager>();

            serviceCollection.AddSingleton<LiveTvDtoService>();
            serviceCollection.AddSingleton<ILiveTvManager, LiveTvManager>();

            serviceCollection.AddSingleton<IUserViewManager, UserViewManager>();

            serviceCollection.AddSingleton<INotificationManager, NotificationManager>();

            serviceCollection.AddSingleton<IDeviceDiscovery, DeviceDiscovery>();

            serviceCollection.AddSingleton<IChapterManager, ChapterManager>();

            serviceCollection.AddSingleton<IEncodingManager, MediaEncoder.EncodingManager>();

            serviceCollection.AddSingleton<IAuthorizationContext, AuthorizationContext>();
            serviceCollection.AddSingleton<ISessionContext, SessionContext>();

            serviceCollection.AddSingleton<IAuthService, AuthService>();

            serviceCollection.AddSingleton<ISubtitleEncoder, MediaBrowser.MediaEncoding.Subtitles.SubtitleEncoder>();

            serviceCollection.AddSingleton<IResourceFileManager, ResourceFileManager>();
            serviceCollection.AddSingleton<EncodingHelper>();

            serviceCollection.AddSingleton<IAttachmentExtractor, MediaBrowser.MediaEncoding.Attachments.AttachmentExtractor>();
        }

        /// <summary>
        /// Create services registered with the service container that need to be initialized at application startup.
        /// </summary>
        /// <returns>A task representing the service initialization operation.</returns>
        public async Task InitializeServices()
        {
            var localizationManager = (LocalizationManager)Resolve<ILocalizationManager>();
            await localizationManager.LoadAll().ConfigureAwait(false);

            _mediaEncoder = Resolve<IMediaEncoder>();
            _sessionManager = Resolve<ISessionManager>();
            _httpServer = Resolve<IHttpServer>();
            _httpClient = Resolve<IHttpClient>();

            ((SqliteDisplayPreferencesRepository)Resolve<IDisplayPreferencesRepository>()).Initialize();
            ((AuthenticationRepository)Resolve<IAuthenticationRepository>()).Initialize();
            ((SqliteUserRepository)Resolve<IUserRepository>()).Initialize();

            SetStaticProperties();

            var userManager = (UserManager)Resolve<IUserManager>();
            userManager.Initialize();

            var userDataRepo = (SqliteUserDataRepository)Resolve<IUserDataRepository>();
            ((SqliteItemRepository)Resolve<IItemRepository>()).Initialize(userDataRepo, userManager);

            FindParts();
        }

        public static void LogEnvironmentInfo(ILogger logger, IApplicationPaths appPaths)
        {
            // Distinct these to prevent users from reporting problems that aren't actually problems
            var commandLineArgs = Environment
                .GetCommandLineArgs()
                .Distinct();

            // Get all relevant environment variables
            var allEnvVars = Environment.GetEnvironmentVariables();
            var relevantEnvVars = new Dictionary<object, object>();
            foreach (var key in allEnvVars.Keys)
            {
                if (_relevantEnvVarPrefixes.Any(prefix => key.ToString().StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                {
                    relevantEnvVars.Add(key, allEnvVars[key]);
                }
            }

            logger.LogInformation("Environment Variables: {EnvVars}", relevantEnvVars);
            logger.LogInformation("Arguments: {Args}", commandLineArgs);
            logger.LogInformation("Operating system: {OS}", OperatingSystem.Name);
            logger.LogInformation("Architecture: {Architecture}", RuntimeInformation.OSArchitecture);
            logger.LogInformation("64-Bit Process: {Is64Bit}", Environment.Is64BitProcess);
            logger.LogInformation("User Interactive: {IsUserInteractive}", Environment.UserInteractive);
            logger.LogInformation("Processor count: {ProcessorCount}", Environment.ProcessorCount);
            logger.LogInformation("Program data path: {ProgramDataPath}", appPaths.ProgramDataPath);
            logger.LogInformation("Web resources path: {WebPath}", appPaths.WebPath);
            logger.LogInformation("Application directory: {ApplicationPath}", appPaths.ProgramSystemPath);
        }

        private X509Certificate2 GetCertificate(CertificateInfo info)
        {
            var certificateLocation = info?.Path;

            if (string.IsNullOrWhiteSpace(certificateLocation))
            {
                return null;
            }

            try
            {
                if (!File.Exists(certificateLocation))
                {
                    return null;
                }

                // Don't use an empty string password
                var password = string.IsNullOrWhiteSpace(info.Password) ? null : info.Password;

                var localCert = new X509Certificate2(certificateLocation, password);
                // localCert.PrivateKey = PrivateKey.CreateFromFile(pvk_file).RSA;
                if (!localCert.HasPrivateKey)
                {
                    Logger.LogError("No private key included in SSL cert {CertificateLocation}.", certificateLocation);
                    return null;
                }

                return localCert;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading cert from {CertificateLocation}", certificateLocation);
                return null;
            }
        }

        /// <summary>
        /// Dirty hacks.
        /// </summary>
        private void SetStaticProperties()
        {
            // For now there's no real way to inject these properly
            BaseItem.Logger = Resolve<ILogger<BaseItem>>();
            BaseItem.ConfigurationManager = ServerConfigurationManager;
            BaseItem.LibraryManager = Resolve<ILibraryManager>();
            BaseItem.ProviderManager = Resolve<IProviderManager>();
            BaseItem.LocalizationManager = Resolve<ILocalizationManager>();
            BaseItem.ItemRepository = Resolve<IItemRepository>();
            User.UserManager = Resolve<IUserManager>();
            BaseItem.FileSystem = _fileSystemManager;
            BaseItem.UserDataManager = Resolve<IUserDataManager>();
            BaseItem.ChannelManager = Resolve<IChannelManager>();
            Video.LiveTvManager = Resolve<ILiveTvManager>();
            Folder.UserViewManager = Resolve<IUserViewManager>();
            UserView.TVSeriesManager = Resolve<ITVSeriesManager>();
            UserView.CollectionManager = Resolve<ICollectionManager>();
            BaseItem.MediaSourceManager = Resolve<IMediaSourceManager>();
            CollectionFolder.XmlSerializer = _xmlSerializer;
            CollectionFolder.JsonSerializer = Resolve<IJsonSerializer>();
            CollectionFolder.ApplicationHost = this;
            AuthenticatedAttribute.AuthService = Resolve<IAuthService>();
        }

        /// <summary>
        /// Finds plugin components and register them with the appropriate services.
        /// </summary>
        private void FindParts()
        {
            if (!ServerConfigurationManager.Configuration.IsPortAuthorized)
            {
                ServerConfigurationManager.Configuration.IsPortAuthorized = true;
                ConfigurationManager.SaveConfiguration();
            }

            ConfigurationManager.AddParts(GetExports<IConfigurationFactory>());
            _plugins = GetExports<IPlugin>()
                        .Select(LoadPlugin)
                        .Where(i => i != null)
                        .ToArray();

            _httpServer.Init(GetExportTypes<IService>(), GetExports<IWebSocketListener>(), GetUrlPrefixes());

            Resolve<ILibraryManager>().AddParts(
                GetExports<IResolverIgnoreRule>(),
                GetExports<IItemResolver>(),
                GetExports<IIntroProvider>(),
                GetExports<IBaseItemComparer>(),
                GetExports<ILibraryPostScanTask>());

            Resolve<IProviderManager>().AddParts(
                GetExports<IImageProvider>(),
                GetExports<IMetadataService>(),
                GetExports<IMetadataProvider>(),
                GetExports<IMetadataSaver>(),
                GetExports<IExternalId>());

            Resolve<ILiveTvManager>().AddParts(GetExports<ILiveTvService>(), GetExports<ITunerHost>(), GetExports<IListingsProvider>());

            Resolve<ISubtitleManager>().AddParts(GetExports<ISubtitleProvider>());

            Resolve<IChannelManager>().AddParts(GetExports<IChannel>());

            Resolve<IMediaSourceManager>().AddParts(GetExports<IMediaSourceProvider>());

            Resolve<INotificationManager>().AddParts(GetExports<INotificationService>(), GetExports<INotificationTypeFactory>());
            Resolve<IUserManager>().AddParts(GetExports<IAuthenticationProvider>(), GetExports<IPasswordResetProvider>());

            Resolve<IIsoManager>().AddParts(GetExports<IIsoMounter>());
        }

        private IPlugin LoadPlugin(IPlugin plugin)
        {
            try
            {
                if (plugin is IPluginAssembly assemblyPlugin)
                {
                    var assembly = plugin.GetType().Assembly;
                    var assemblyName = assembly.GetName();
                    var assemblyFilePath = assembly.Location;

                    var dataFolderPath = Path.Combine(ApplicationPaths.PluginsPath, Path.GetFileNameWithoutExtension(assemblyFilePath));

                    assemblyPlugin.SetAttributes(assemblyFilePath, dataFolderPath, assemblyName.Version);

                    try
                    {
                        var idAttributes = assembly.GetCustomAttributes(typeof(GuidAttribute), true);
                        if (idAttributes.Length > 0)
                        {
                            var attribute = (GuidAttribute)idAttributes[0];
                            var assemblyId = new Guid(attribute.Value);

                            assemblyPlugin.SetId(assemblyId);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error getting plugin Id from {PluginName}.", plugin.GetType().FullName);
                    }
                }

                if (plugin is IHasPluginConfiguration hasPluginConfiguration)
                {
                    hasPluginConfiguration.SetStartupInfo(s => Directory.CreateDirectory(s));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading plugin {PluginName}", plugin.GetType().FullName);
                return null;
            }

            return plugin;
        }

        /// <summary>
        /// Discovers the types.
        /// </summary>
        protected void DiscoverTypes()
        {
            Logger.LogInformation("Loading assemblies");

            _allConcreteTypes = GetTypes(GetComposablePartAssemblies()).ToArray();
        }

        private IEnumerable<Type> GetTypes(IEnumerable<Assembly> assemblies)
        {
            foreach (var ass in assemblies)
            {
                Type[] exportedTypes;
                try
                {
                    exportedTypes = ass.GetExportedTypes();
                }
                catch (FileNotFoundException ex)
                {
                    Logger.LogError(ex, "Error getting exported types from {Assembly}", ass.FullName);
                    continue;
                }

                foreach (Type type in exportedTypes)
                {
                    if (type.IsClass && !type.IsAbstract && !type.IsInterface && !type.IsGenericType)
                    {
                        yield return type;
                    }
                }
            }
        }

        private CertificateInfo CertificateInfo { get; set; }

        public X509Certificate2 Certificate { get; private set; }

        private IEnumerable<string> GetUrlPrefixes()
        {
            var hosts = new[] { "+" };

            return hosts.SelectMany(i =>
            {
                var prefixes = new List<string>
                {
                    "http://" + i + ":" + HttpPort + "/"
                };

                if (CertificateInfo != null)
                {
                    prefixes.Add("https://" + i + ":" + HttpsPort + "/");
                }

                return prefixes;
            });
        }

        /// <summary>
        /// Called when [configuration updated].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void OnConfigurationUpdated(object sender, EventArgs e)
        {
            var requiresRestart = false;

            // Don't do anything if these haven't been set yet
            if (HttpPort != 0 && HttpsPort != 0)
            {
                // Need to restart if ports have changed
                if (ServerConfigurationManager.Configuration.HttpServerPortNumber != HttpPort ||
                    ServerConfigurationManager.Configuration.HttpsPortNumber != HttpsPort)
                {
                    if (ServerConfigurationManager.Configuration.IsPortAuthorized)
                    {
                        ServerConfigurationManager.Configuration.IsPortAuthorized = false;
                        ServerConfigurationManager.SaveConfiguration();

                        requiresRestart = true;
                    }
                }
            }

            if (!_httpServer.UrlPrefixes.SequenceEqual(GetUrlPrefixes(), StringComparer.OrdinalIgnoreCase))
            {
                requiresRestart = true;
            }

            var currentCertPath = CertificateInfo?.Path;
            var newCertPath = ServerConfigurationManager.Configuration.CertificatePath;

            if (!string.Equals(currentCertPath, newCertPath, StringComparison.OrdinalIgnoreCase))
            {
                requiresRestart = true;
            }

            if (requiresRestart)
            {
                Logger.LogInformation("App needs to be restarted due to configuration change.");

                NotifyPendingRestart();
            }
        }

        /// <summary>
        /// Notifies that the kernel that a change has been made that requires a restart
        /// </summary>
        public void NotifyPendingRestart()
        {
            Logger.LogInformation("App needs to be restarted.");

            var changed = !HasPendingRestart;

            HasPendingRestart = true;

            if (changed)
            {
                EventHelper.QueueEventIfNotNull(HasPendingRestartChanged, this, EventArgs.Empty, Logger);
            }
        }

        /// <summary>
        /// Restarts this instance.
        /// </summary>
        public void Restart()
        {
            if (!CanSelfRestart)
            {
                throw new PlatformNotSupportedException("The server is unable to self-restart. Please restart manually.");
            }

            if (IsShuttingDown)
            {
                return;
            }

            IsShuttingDown = true;

            Task.Run(async () =>
            {
                try
                {
                    await _sessionManager.SendServerRestartNotification(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error sending server restart notification");
                }

                Logger.LogInformation("Calling RestartInternal");

                RestartInternal();
            });
        }

        protected abstract void RestartInternal();

        /// <summary>
        /// Gets the composable part assemblies.
        /// </summary>
        /// <returns>IEnumerable{Assembly}.</returns>
        protected IEnumerable<Assembly> GetComposablePartAssemblies()
        {
            if (Directory.Exists(ApplicationPaths.PluginsPath))
            {
                foreach (var file in Directory.EnumerateFiles(ApplicationPaths.PluginsPath, "*.dll", SearchOption.AllDirectories))
                {
                    Assembly plugAss;
                    try
                    {
                        plugAss = Assembly.LoadFrom(file);
                    }
                    catch (FileLoadException ex)
                    {
                        Logger.LogError(ex, "Failed to load assembly {Path}", file);
                        continue;
                    }

                    Logger.LogInformation("Loaded assembly {Assembly} from {Path}", plugAss.FullName, file);
                    yield return plugAss;
                }
            }

            // Include composable parts in the Api assembly
            yield return typeof(ApiEntryPoint).Assembly;

            // Include composable parts in the Dashboard assembly
            yield return typeof(DashboardService).Assembly;

            // Include composable parts in the Model assembly
            yield return typeof(SystemInfo).Assembly;

            // Include composable parts in the Common assembly
            yield return typeof(IApplicationHost).Assembly;

            // Include composable parts in the Controller assembly
            yield return typeof(IServerApplicationHost).Assembly;

            // Include composable parts in the Providers assembly
            yield return typeof(ProviderUtils).Assembly;

            // Include composable parts in the Photos assembly
            yield return typeof(PhotoProvider).Assembly;

            // Emby.Server implementations
            yield return typeof(InstallationManager).Assembly;

            // MediaEncoding
            yield return typeof(MediaBrowser.MediaEncoding.Encoder.MediaEncoder).Assembly;

            // Dlna
            yield return typeof(DlnaEntryPoint).Assembly;

            // Local metadata
            yield return typeof(BoxSetXmlSaver).Assembly;

            // Notifications
            yield return typeof(NotificationManager).Assembly;

            // Xbmc
            yield return typeof(ArtistNfoProvider).Assembly;

            foreach (var i in GetAssembliesWithPartsInternal())
            {
                yield return i;
            }
        }

        protected abstract IEnumerable<Assembly> GetAssembliesWithPartsInternal();

        /// <summary>
        /// Gets the system status.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>SystemInfo.</returns>
        public async Task<SystemInfo> GetSystemInfo(CancellationToken cancellationToken)
        {
            var localAddress = await GetLocalApiUrl(cancellationToken).ConfigureAwait(false);
            var transcodingTempPath = ConfigurationManager.GetTranscodePath();

            return new SystemInfo
            {
                HasPendingRestart = HasPendingRestart,
                IsShuttingDown = IsShuttingDown,
                Version = ApplicationVersionString,
                WebSocketPortNumber = HttpPort,
                CompletedInstallations = Resolve<IInstallationManager>().CompletedInstallations.ToArray(),
                Id = SystemId,
                ProgramDataPath = ApplicationPaths.ProgramDataPath,
                WebPath = ApplicationPaths.WebPath,
                LogPath = ApplicationPaths.LogDirectoryPath,
                ItemsByNamePath = ApplicationPaths.InternalMetadataPath,
                InternalMetadataPath = ApplicationPaths.InternalMetadataPath,
                CachePath = ApplicationPaths.CachePath,
                OperatingSystem = OperatingSystem.Id.ToString(),
                OperatingSystemDisplayName = OperatingSystem.Name,
                CanSelfRestart = CanSelfRestart,
                CanLaunchWebBrowser = CanLaunchWebBrowser,
                HasUpdateAvailable = HasUpdateAvailable,
                TranscodingTempPath = transcodingTempPath,
                ServerName = FriendlyName,
                LocalAddress = localAddress,
                SupportsLibraryMonitor = true,
                EncoderLocation = _mediaEncoder.EncoderLocation,
                SystemArchitecture = RuntimeInformation.OSArchitecture,
                PackageName = _startupOptions.PackageName
            };
        }

        public IEnumerable<WakeOnLanInfo> GetWakeOnLanInfo()
            => _networkManager.GetMacAddresses()
                .Select(i => new WakeOnLanInfo(i))
                .ToList();

        public async Task<PublicSystemInfo> GetPublicSystemInfo(CancellationToken cancellationToken)
        {
            var localAddress = await GetLocalApiUrl(cancellationToken).ConfigureAwait(false);

            return new PublicSystemInfo
            {
                Version = ApplicationVersionString,
                ProductName = ApplicationProductName,
                Id = SystemId,
                OperatingSystem = OperatingSystem.Id.ToString(),
                ServerName = FriendlyName,
                LocalAddress = localAddress
            };
        }

        /// <inheritdoc/>
        public bool ListenWithHttps => Certificate != null && ServerConfigurationManager.Configuration.EnableHttps;

        /// <inheritdoc/>
        public async Task<string> GetLocalApiUrl(CancellationToken cancellationToken)
        {
            try
            {
                // Return the first matched address, if found, or the first known local address
                var addresses = await GetLocalIpAddressesInternal(false, 1, cancellationToken).ConfigureAwait(false);
                if (addresses.Count == 0)
                {
                    return null;
                }

                return GetLocalApiUrl(addresses.First());
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error getting local Ip address information");
            }

            return null;
        }

        /// <summary>
        /// Removes the scope id from IPv6 addresses.
        /// </summary>
        /// <param name="address">The IPv6 address.</param>
        /// <returns>The IPv6 address without the scope id.</returns>
        private ReadOnlySpan<char> RemoveScopeId(ReadOnlySpan<char> address)
        {
            var index = address.IndexOf('%');
            if (index == -1)
            {
                return address;
            }

            return address.Slice(0, index);
        }

        /// <inheritdoc />
        public string GetLocalApiUrl(IPAddress ipAddress)
        {
            if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                var str = RemoveScopeId(ipAddress.ToString());
                Span<char> span = new char[str.Length + 2];
                span[0] = '[';
                str.CopyTo(span.Slice(1));
                span[^1] = ']';

                return GetLocalApiUrl(span);
            }

            return GetLocalApiUrl(ipAddress.ToString());
        }

        /// <inheritdoc/>
        public string GetLoopbackHttpApiUrl()
        {
            return GetLocalApiUrl("127.0.0.1", Uri.UriSchemeHttp, HttpPort);
        }

        /// <inheritdoc/>
        public string GetLocalApiUrl(ReadOnlySpan<char> host, string scheme = null, int? port = null)
        {
            // NOTE: If no BaseUrl is set then UriBuilder appends a trailing slash, but if there is no BaseUrl it does
            // not. For consistency, always trim the trailing slash.
            return new UriBuilder
            {
                Scheme = scheme ?? (ListenWithHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp),
                Host = host.ToString(),
                Port = port ?? (ListenWithHttps ? HttpsPort : HttpPort),
                Path = ServerConfigurationManager.Configuration.BaseUrl
            }.ToString().TrimEnd('/');
        }

        public Task<List<IPAddress>> GetLocalIpAddresses(CancellationToken cancellationToken)
        {
            return GetLocalIpAddressesInternal(true, 0, cancellationToken);
        }

        private async Task<List<IPAddress>> GetLocalIpAddressesInternal(bool allowLoopback, int limit, CancellationToken cancellationToken)
        {
            var addresses = ServerConfigurationManager
                .Configuration
                .LocalNetworkAddresses
                .Select(NormalizeConfiguredLocalAddress)
                .Where(i => i != null)
                .ToList();

            if (addresses.Count == 0)
            {
                addresses.AddRange(_networkManager.GetLocalIpAddresses(ServerConfigurationManager.Configuration.IgnoreVirtualInterfaces));
            }

            var resultList = new List<IPAddress>();

            foreach (var address in addresses)
            {
                if (!allowLoopback)
                {
                    if (address.Equals(IPAddress.Loopback) || address.Equals(IPAddress.IPv6Loopback))
                    {
                        continue;
                    }
                }

                var valid = await IsLocalIpAddressValidAsync(address, cancellationToken).ConfigureAwait(false);
                if (valid)
                {
                    resultList.Add(address);

                    if (limit > 0 && resultList.Count >= limit)
                    {
                        return resultList;
                    }
                }
            }

            return resultList;
        }

        public IPAddress NormalizeConfiguredLocalAddress(string address)
        {
            var index = address.Trim('/').IndexOf('/');

            if (index != -1)
            {
                address = address.Substring(index + 1);
            }

            if (IPAddress.TryParse(address.Trim('/'), out IPAddress result))
            {
                return result;
            }

            return null;
        }

        private readonly ConcurrentDictionary<string, bool> _validAddressResults = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        private async Task<bool> IsLocalIpAddressValidAsync(IPAddress address, CancellationToken cancellationToken)
        {
            if (address.Equals(IPAddress.Loopback)
                || address.Equals(IPAddress.IPv6Loopback))
            {
                return true;
            }

            var apiUrl = GetLocalApiUrl(address) + "/system/ping";

            if (_validAddressResults.TryGetValue(apiUrl, out var cachedResult))
            {
                return cachedResult;
            }

            try
            {
                using (var response = await _httpClient.SendAsync(
                    new HttpRequestOptions
                    {
                        Url = apiUrl,
                        LogErrorResponseBody = false,
                        BufferContent = false,
                        CancellationToken = cancellationToken
                    }, HttpMethod.Post).ConfigureAwait(false))
                {
                    using (var reader = new StreamReader(response.Content))
                    {
                        var result = await reader.ReadToEndAsync().ConfigureAwait(false);
                        var valid = string.Equals(Name, result, StringComparison.OrdinalIgnoreCase);

                        _validAddressResults.AddOrUpdate(apiUrl, valid, (k, v) => valid);
                        Logger.LogDebug("Ping test result to {0}. Success: {1}", apiUrl, valid);
                        return valid;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogDebug("Ping test result to {0}. Success: {1}", apiUrl, "Cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Ping test result to {0}. Success: {1}", apiUrl, false);

                _validAddressResults.AddOrUpdate(apiUrl, false, (k, v) => false);
                return false;
            }
        }

        public string FriendlyName =>
            string.IsNullOrEmpty(ServerConfigurationManager.Configuration.ServerName)
                ? Environment.MachineName
                : ServerConfigurationManager.Configuration.ServerName;

        /// <summary>
        /// Shuts down.
        /// </summary>
        public async Task Shutdown()
        {
            if (IsShuttingDown)
            {
                return;
            }

            IsShuttingDown = true;

            try
            {
                await _sessionManager.SendServerShutdownNotification(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error sending server shutdown notification");
            }

            ShutdownInternal();
        }

        protected abstract void ShutdownInternal();

        public event EventHandler HasUpdateAvailableChanged;

        private bool _hasUpdateAvailable;

        public bool HasUpdateAvailable
        {
            get => _hasUpdateAvailable;
            set
            {
                var fireEvent = value && !_hasUpdateAvailable;

                _hasUpdateAvailable = value;

                if (fireEvent)
                {
                    HasUpdateAvailableChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Removes the plugin.
        /// </summary>
        /// <param name="plugin">The plugin.</param>
        public void RemovePlugin(IPlugin plugin)
        {
            var list = _plugins.ToList();
            list.Remove(plugin);
            _plugins = list.ToArray();
        }

        public virtual void LaunchUrl(string url)
        {
            if (!CanLaunchWebBrowser)
            {
                throw new NotSupportedException();
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true,
                    ErrorDialog = false
                },
                EnableRaisingEvents = true
            };
            process.Exited += (sender, args) => ((Process)sender).Dispose();

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error launching url: {url}", url);
                throw;
            }
        }

        public virtual void EnableLoopback(string appName)
        {
        }

        private bool _disposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (_disposed)
            {
                return;
            }

            if (dispose)
            {
                var type = GetType();

                Logger.LogInformation("Disposing {Type}", type.Name);

                var parts = _disposableParts.Distinct().Where(i => i.GetType() != type).ToList();
                _disposableParts.Clear();

                foreach (var part in parts)
                {
                    Logger.LogInformation("Disposing {Type}", part.GetType().Name);

                    try
                    {
                        part.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error disposing {Type}", part.GetType().Name);
                    }
                }
            }

            _disposed = true;
        }
    }

    internal class CertificateInfo
    {
        public string Path { get; set; }

        public string Password { get; set; }
    }
}
