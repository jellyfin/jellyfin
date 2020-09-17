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
using Emby.Server.Implementations.Plugins;
using Emby.Server.Implementations.QuickConnect;
using Emby.Server.Implementations.ScheduledTasks;
using Emby.Server.Implementations.Security;
using Emby.Server.Implementations.Serialization;
using Emby.Server.Implementations.Session;
using Emby.Server.Implementations.SyncPlay;
using Emby.Server.Implementations.TV;
using Emby.Server.Implementations.Updates;
using Jellyfin.Api.Helpers;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.Json;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller;
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
using MediaBrowser.Controller.QuickConnect;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Controller.SyncPlay;
using MediaBrowser.Controller.TV;
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
using MediaBrowser.Model.System;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Providers.Chapters;
using MediaBrowser.Providers.Manager;
using MediaBrowser.Providers.Plugins.TheTvdb;
using MediaBrowser.Providers.Subtitles;
using MediaBrowser.XbmcMetadata.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prometheus.DotNetRuntime;
using OperatingSystem = MediaBrowser.Common.System.OperatingSystem;
using WebSocketManager = Emby.Server.Implementations.HttpServer.WebSocketManager;

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
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IStartupOptions _startupOptions;

        private IMediaEncoder _mediaEncoder;
        private ISessionManager _sessionManager;
        private IHttpClientFactory _httpClientFactory;
        private IWebSocketManager _webSocketManager;

        private string[] _urlPrefixes;

        /// <summary>
        /// Gets a value indicating whether this instance can self restart.
        /// </summary>
        public bool CanSelfRestart => _startupOptions.RestartPath != null;

        public bool CoreStartupHasCompleted { get; private set; }

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
        protected ILogger<ApplicationHost> Logger { get; }

        protected IServiceCollection ServiceCollection { get; }

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
        protected IServerApplicationPaths ApplicationPaths { get; set; }

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
        /// Initializes a new instance of the <see cref="ApplicationHost"/> class.
        /// </summary>
        /// <param name="applicationPaths">Instance of the <see cref="IServerApplicationPaths"/> interface.</param>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        /// <param name="options">Instance of the <see cref="IStartupOptions"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
        /// <param name="serviceCollection">Instance of the <see cref="IServiceCollection"/> interface.</param>
        public ApplicationHost(
            IServerApplicationPaths applicationPaths,
            ILoggerFactory loggerFactory,
            IStartupOptions options,
            IFileSystem fileSystem,
            INetworkManager networkManager,
            IServiceCollection serviceCollection)
        {
            _xmlSerializer = new MyXmlSerializer();
            _jsonSerializer = new JsonSerializer();            
            
            ServiceCollection = serviceCollection;

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

            ApplicationVersion = typeof(ApplicationHost).Assembly.GetName().Version;
            ApplicationVersionString = ApplicationVersion.ToString(3);
            ApplicationUserAgent = Name.Replace(' ', '-') + "/" + ApplicationVersionString;
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
        public Version ApplicationVersion { get; }

        /// <inheritdoc />
        public string ApplicationVersionString { get; }

        /// <summary>
        /// Gets the current application user agent.
        /// </summary>
        /// <value>The application user agent.</value>
        public string ApplicationUserAgent { get; }

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
            CoreStartupHasCompleted = true;
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
        public void Init()
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
                    pluginBuilder.Append(plugin.Name)
                        .Append(' ')
                        .Append(plugin.Version)
                        .AppendLine();
                }

                Logger.LogInformation("Plugins: {Plugins}", pluginBuilder.ToString());
            }

            DiscoverTypes();

            RegisterServices();
        }

        /// <summary>
        /// Registers services/resources with the service collection that will be available via DI.
        /// </summary>
        protected virtual void RegisterServices()
        {
            ServiceCollection.AddSingleton(_startupOptions);

            ServiceCollection.AddMemoryCache();

            ServiceCollection.AddSingleton(ConfigurationManager);
            ServiceCollection.AddSingleton<IApplicationHost>(this);

            ServiceCollection.AddSingleton<IApplicationPaths>(ApplicationPaths);

            ServiceCollection.AddSingleton<IJsonSerializer, JsonSerializer>();

            ServiceCollection.AddSingleton(_fileSystemManager);
            ServiceCollection.AddSingleton<TvdbClientManager>();

            ServiceCollection.AddSingleton(_networkManager);

            ServiceCollection.AddSingleton<IIsoManager, IsoManager>();

            ServiceCollection.AddSingleton<ITaskManager, TaskManager>();

            ServiceCollection.AddSingleton(_xmlSerializer);

            ServiceCollection.AddSingleton<IStreamHelper, StreamHelper>();

            ServiceCollection.AddSingleton<ICryptoProvider, CryptographyProvider>();

            ServiceCollection.AddSingleton<ISocketFactory, SocketFactory>();

            ServiceCollection.AddSingleton<IInstallationManager, InstallationManager>();

            ServiceCollection.AddSingleton<IZipClient, ZipClient>();

            ServiceCollection.AddSingleton<IServerApplicationHost>(this);
            ServiceCollection.AddSingleton<IServerApplicationPaths>(ApplicationPaths);

            ServiceCollection.AddSingleton(ServerConfigurationManager);

            ServiceCollection.AddSingleton<ILocalizationManager, LocalizationManager>();

            ServiceCollection.AddSingleton<IBlurayExaminer, BdInfoExaminer>();

            ServiceCollection.AddSingleton<IUserDataRepository, SqliteUserDataRepository>();
            ServiceCollection.AddSingleton<IUserDataManager, UserDataManager>();

            ServiceCollection.AddSingleton<IItemRepository, SqliteItemRepository>();

            ServiceCollection.AddSingleton<IAuthenticationRepository, AuthenticationRepository>();

            // TODO: Refactor to eliminate the circular dependency here so that Lazy<T> isn't required
            ServiceCollection.AddTransient(provider => new Lazy<IDtoService>(provider.GetRequiredService<IDtoService>));

            // TODO: Refactor to eliminate the circular dependency here so that Lazy<T> isn't required
            ServiceCollection.AddTransient(provider => new Lazy<EncodingHelper>(provider.GetRequiredService<EncodingHelper>));
            ServiceCollection.AddSingleton<IMediaEncoder, MediaBrowser.MediaEncoding.Encoder.MediaEncoder>();

            // TODO: Refactor to eliminate the circular dependencies here so that Lazy<T> isn't required
            ServiceCollection.AddTransient(provider => new Lazy<ILibraryMonitor>(provider.GetRequiredService<ILibraryMonitor>));
            ServiceCollection.AddTransient(provider => new Lazy<IProviderManager>(provider.GetRequiredService<IProviderManager>));
            ServiceCollection.AddTransient(provider => new Lazy<IUserViewManager>(provider.GetRequiredService<IUserViewManager>));
            ServiceCollection.AddSingleton<ILibraryManager, LibraryManager>();

            ServiceCollection.AddSingleton<IMusicManager, MusicManager>();

            ServiceCollection.AddSingleton<ILibraryMonitor, LibraryMonitor>();

            ServiceCollection.AddSingleton<ISearchEngine, SearchEngine>();

            ServiceCollection.AddSingleton<IWebSocketManager, WebSocketManager>();

            ServiceCollection.AddSingleton<IImageProcessor, ImageProcessor>();

            ServiceCollection.AddSingleton<ITVSeriesManager, TVSeriesManager>();

            ServiceCollection.AddSingleton<IDeviceManager, DeviceManager>();

            ServiceCollection.AddSingleton<IMediaSourceManager, MediaSourceManager>();

            ServiceCollection.AddSingleton<ISubtitleManager, SubtitleManager>();

            ServiceCollection.AddSingleton<IProviderManager, ProviderManager>();

            // TODO: Refactor to eliminate the circular dependency here so that Lazy<T> isn't required
            ServiceCollection.AddTransient(provider => new Lazy<ILiveTvManager>(provider.GetRequiredService<ILiveTvManager>));
            ServiceCollection.AddSingleton<IDtoService, DtoService>();

            ServiceCollection.AddSingleton<IChannelManager, ChannelManager>();

            ServiceCollection.AddSingleton<ISessionManager, SessionManager>();

            ServiceCollection.AddSingleton<IDlnaManager, DlnaManager>();

            ServiceCollection.AddSingleton<ICollectionManager, CollectionManager>();

            ServiceCollection.AddSingleton<IPlaylistManager, PlaylistManager>();

            ServiceCollection.AddSingleton<ISyncPlayManager, SyncPlayManager>();

            ServiceCollection.AddSingleton<LiveTvDtoService>();
            ServiceCollection.AddSingleton<ILiveTvManager, LiveTvManager>();

            ServiceCollection.AddSingleton<IUserViewManager, UserViewManager>();

            ServiceCollection.AddSingleton<INotificationManager, NotificationManager>();

            ServiceCollection.AddSingleton<IDeviceDiscovery, DeviceDiscovery>();

            ServiceCollection.AddSingleton<IChapterManager, ChapterManager>();

            ServiceCollection.AddSingleton<IEncodingManager, MediaEncoder.EncodingManager>();

            ServiceCollection.AddSingleton<IAuthorizationContext, AuthorizationContext>();
            ServiceCollection.AddSingleton<ISessionContext, SessionContext>();

            ServiceCollection.AddSingleton<IAuthService, AuthService>();
            ServiceCollection.AddSingleton<IQuickConnect, QuickConnectManager>();

            ServiceCollection.AddSingleton<ISubtitleEncoder, MediaBrowser.MediaEncoding.Subtitles.SubtitleEncoder>();

            ServiceCollection.AddSingleton<IResourceFileManager, ResourceFileManager>();
            ServiceCollection.AddSingleton<EncodingHelper>();

            ServiceCollection.AddSingleton<IAttachmentExtractor, MediaBrowser.MediaEncoding.Attachments.AttachmentExtractor>();

            ServiceCollection.AddSingleton<TranscodingJobHelper>();
            ServiceCollection.AddScoped<MediaInfoHelper>();
            ServiceCollection.AddScoped<AudioHelper>();
            ServiceCollection.AddScoped<DynamicHlsHelper>();
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
            _httpClientFactory = Resolve<IHttpClientFactory>();
            _webSocketManager = Resolve<IWebSocketManager>();

            ((AuthenticationRepository)Resolve<IAuthenticationRepository>()).Initialize();

            SetStaticProperties();

            var userDataRepo = (SqliteUserDataRepository)Resolve<IUserDataRepository>();
            ((SqliteItemRepository)Resolve<IItemRepository>()).Initialize(userDataRepo, Resolve<IUserManager>());

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

            _urlPrefixes = GetUrlPrefixes().ToArray();
            _webSocketManager.Init(GetExports<IWebSocketListener>());

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

                plugin.RegisterServices(ServiceCollection);
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
                catch (TypeLoadException ex)
                {
                    Logger.LogError(ex, "Error loading types from {Assembly}.", ass.FullName);
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

            if (!_urlPrefixes.SequenceEqual(GetUrlPrefixes(), StringComparer.OrdinalIgnoreCase))
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
        /// Notifies that the kernel that a change has been made that requires a restart.
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
        /// Comparison function used in <see cref="GetPlugins" />.
        /// </summary>
        /// <param name="a">Item to compare.</param>
        /// <param name="b">Item to compare with.</param>
        /// <returns>Boolean result of the operation.</returns>
        private static int VersionCompare(
            (Version PluginVersion, string Name, string Path) a,
            (Version PluginVersion, string Name, string Path) b)
        {
            int compare = string.Compare(a.Name, b.Name, true, CultureInfo.InvariantCulture);

            if (compare == 0)
            {
                return a.PluginVersion.CompareTo(b.PluginVersion);
            }

            return compare;
        }

        /// <summary>
        /// Returns a list of plugins to install.
        /// </summary>
        /// <param name="path">Path to check.</param>
        /// <param name="cleanup">True if an attempt should be made to delete old plugs.</param>
        /// <returns>Enumerable list of dlls to load.</returns>
        private IEnumerable<string> GetPlugins(string path, bool cleanup = true)
        {
            var dllList = new List<string>();
            var versions = new List<(Version PluginVersion, string Name, string Path)>();
            var directories = Directory.EnumerateDirectories(path, "*.*", SearchOption.TopDirectoryOnly);
            string metafile;

            foreach (var dir in directories)
            {
                try
                {
                    metafile = Path.Combine(dir, "meta.json");
                    if (File.Exists(metafile))
                    {
                        var manifest = _jsonSerializer.DeserializeFromFile<PluginManifest>(metafile);

                        if (!Version.TryParse(manifest.TargetAbi, out var targetAbi))
                        {
                            targetAbi = new Version(0, 0, 0, 1);
                        }

                        if (!Version.TryParse(manifest.Version, out var version))
                        {
                            version = new Version(0, 0, 0, 1);
                        }

                        if (ApplicationVersion >= targetAbi)
                        {
                            // Only load Plugins if the plugin is built for this version or below.
                            versions.Add((version, manifest.Name, dir));
                        }
                    }
                    else
                    {
                        metafile = dir.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries)[^1];
                        // Add it under the path name and version 0.0.0.1.
                        versions.Add((new Version(0, 0, 0, 1), metafile, dir));
                    }
                }
                catch
                {
                    continue;
                }
            }

            string lastName = string.Empty;
            versions.Sort(VersionCompare);
            // Traverse backwards through the list.
            // The first item will be the latest version.
            for (int x = versions.Count - 1; x >= 0; x--)
            {
                if (!string.Equals(lastName, versions[x].Name, StringComparison.OrdinalIgnoreCase))
                {
                    dllList.AddRange(Directory.EnumerateFiles(versions[x].Path, "*.dll", SearchOption.AllDirectories));
                    lastName = versions[x].Name;
                    continue;
                }

                if (!string.IsNullOrEmpty(lastName) && cleanup)
                {
                    // Attempt a cleanup of old folders.
                    try
                    {
                        Logger.LogDebug("Deleting {Path}", versions[x].Path);
                        Directory.Delete(versions[x].Path, true);
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning(e, "Unable to delete {Path}", versions[x].Path);
                    }
                }
            }

            return dllList;
        }

        /// <summary>
        /// Gets the composable part assemblies.
        /// </summary>
        /// <returns>IEnumerable{Assembly}.</returns>
        protected IEnumerable<Assembly> GetComposablePartAssemblies()
        {
            if (Directory.Exists(ApplicationPaths.PluginsPath))
            {
                foreach (var file in GetPlugins(ApplicationPaths.PluginsPath))
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
                LocalAddress = localAddress,
                StartupWizardCompleted = ConfigurationManager.CommonConfiguration.IsStartupWizardCompleted
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

                return GetLocalApiUrl(addresses[0]);
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
                .Select(x => NormalizeConfiguredLocalAddress(x))
                .Where(i => i != null)
                .ToList();

            if (addresses.Count == 0)
            {
                addresses.AddRange(_networkManager.GetLocalIpAddresses());
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

                if (await IsLocalIpAddressValidAsync(address, cancellationToken).ConfigureAwait(false))
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

        public IPAddress NormalizeConfiguredLocalAddress(ReadOnlySpan<char> address)
        {
            var index = address.Trim('/').IndexOf('/');
            if (index != -1)
            {
                address = address.Slice(index + 1);
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
                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                using var response = await _httpClientFactory.CreateClient(NamedClient.Default)
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

                await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                var result = await System.Text.Json.JsonSerializer.DeserializeAsync<string>(stream, JsonDefaults.GetOptions(), cancellationToken).ConfigureAwait(false);
                var valid = string.Equals(Name, result, StringComparison.OrdinalIgnoreCase);

                _validAddressResults.AddOrUpdate(apiUrl, valid, (k, v) => valid);
                Logger.LogDebug("Ping test result to {0}. Success: {1}", apiUrl, valid);
                return valid;
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

        public IEnumerable<Assembly> GetApiPluginAssemblies()
        {
            var assemblies = _allConcreteTypes
                .Where(i => typeof(ControllerBase).IsAssignableFrom(i))
                .Select(i => i.Assembly)
                .Distinct();

            foreach (var assembly in assemblies)
            {
                Logger.LogDebug("Found API endpoints in plugin {Name}", assembly.FullName);
                yield return assembly;
            }
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
