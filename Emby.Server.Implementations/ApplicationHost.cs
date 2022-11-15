#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Emby.Dlna;
using Emby.Dlna.Main;
using Emby.Dlna.Ssdp;
using Emby.Drawing;
using Emby.Naming.Common;
using Emby.Notifications;
using Emby.Photos;
using Emby.Server.Implementations.Channels;
using Emby.Server.Implementations.Collections;
using Emby.Server.Implementations.Configuration;
using Emby.Server.Implementations.Cryptography;
using Emby.Server.Implementations.Data;
using Emby.Server.Implementations.Devices;
using Emby.Server.Implementations.Dto;
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
using Emby.Server.Implementations.Serialization;
using Emby.Server.Implementations.Session;
using Emby.Server.Implementations.SyncPlay;
using Emby.Server.Implementations.TV;
using Emby.Server.Implementations.Updates;
using Jellyfin.Api.Helpers;
using Jellyfin.MediaEncoding.Hls.Playlist;
using Jellyfin.Networking.Configuration;
using Jellyfin.Networking.Manager;
using Jellyfin.Server.Implementations;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.ClientEvent;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.QuickConnect;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Controller.SyncPlay;
using MediaBrowser.Controller.TV;
using MediaBrowser.LocalMetadata.Savers;
using MediaBrowser.MediaEncoding.BdInfo;
using MediaBrowser.MediaEncoding.Subtitles;
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
using MediaBrowser.Providers.Lyric;
using MediaBrowser.Providers.Manager;
using MediaBrowser.Providers.Plugins.Tmdb;
using MediaBrowser.Providers.Subtitles;
using MediaBrowser.XbmcMetadata.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prometheus.DotNetRuntime;
using static MediaBrowser.Controller.Extensions.ConfigurationExtensions;
using WebSocketManager = Emby.Server.Implementations.HttpServer.WebSocketManager;

namespace Emby.Server.Implementations
{
    /// <summary>
    /// Class CompositionRoot.
    /// </summary>
    public abstract class ApplicationHost : IServerApplicationHost, IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// The environment variable prefixes to log at server startup.
        /// </summary>
        private static readonly string[] _relevantEnvVarPrefixes = { "JELLYFIN_", "DOTNET_", "ASPNETCORE_" };

        /// <summary>
        /// The disposable parts.
        /// </summary>
        private readonly ConcurrentDictionary<IDisposable, byte> _disposableParts = new();

        private readonly IFileSystem _fileSystemManager;
        private readonly IConfiguration _startupConfig;
        private readonly IXmlSerializer _xmlSerializer;
        private readonly IStartupOptions _startupOptions;
        private readonly IPluginManager _pluginManager;

        private List<Type> _creatingInstances;
        private IMediaEncoder _mediaEncoder;
        private ISessionManager _sessionManager;

        /// <summary>
        /// Gets or sets all concrete types.
        /// </summary>
        /// <value>All concrete types.</value>
        private Type[] _allConcreteTypes;

        private DeviceId _deviceId;

        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationHost"/> class.
        /// </summary>
        /// <param name="applicationPaths">Instance of the <see cref="IServerApplicationPaths"/> interface.</param>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        /// <param name="options">Instance of the <see cref="IStartupOptions"/> interface.</param>
        /// <param name="startupConfig">The <see cref="IConfiguration" /> interface.</param>
        protected ApplicationHost(
            IServerApplicationPaths applicationPaths,
            ILoggerFactory loggerFactory,
            IStartupOptions options,
            IConfiguration startupConfig)
        {
            ApplicationPaths = applicationPaths;
            LoggerFactory = loggerFactory;
            _startupOptions = options;
            _startupConfig = startupConfig;
            _fileSystemManager = new ManagedFileSystem(LoggerFactory.CreateLogger<ManagedFileSystem>(), applicationPaths);

            Logger = LoggerFactory.CreateLogger<ApplicationHost>();
            _fileSystemManager.AddShortcutHandler(new MbLinkShortcutHandler(_fileSystemManager));

            ApplicationVersion = typeof(ApplicationHost).Assembly.GetName().Version;
            ApplicationVersionString = ApplicationVersion.ToString(3);
            ApplicationUserAgent = Name.Replace(' ', '-') + "/" + ApplicationVersionString;

            _xmlSerializer = new MyXmlSerializer();
            ConfigurationManager = new ServerConfigurationManager(ApplicationPaths, LoggerFactory, _xmlSerializer, _fileSystemManager);
            _pluginManager = new PluginManager(
                LoggerFactory.CreateLogger<PluginManager>(),
                this,
                ConfigurationManager.Configuration,
                ApplicationPaths.PluginsPath,
                ApplicationVersion);
        }

        /// <summary>
        /// Occurs when [has pending restart changed].
        /// </summary>
        public event EventHandler HasPendingRestartChanged;

        /// <summary>
        /// Gets the value of the PublishedServerUrl setting.
        /// </summary>
        private string PublishedServerUrl => _startupConfig[AddressOverrideKey];

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

                return OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();
            }
        }

        /// <summary>
        /// Gets the <see cref="INetworkManager"/> singleton instance.
        /// </summary>
        public INetworkManager NetManager { get; private set; }

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

        /// <summary>
        /// Gets the logger factory.
        /// </summary>
        protected ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Gets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        protected IServerApplicationPaths ApplicationPaths { get; }

        /// <summary>
        /// Gets the configuration manager.
        /// </summary>
        /// <value>The configuration manager.</value>
        public ServerConfigurationManager ConfigurationManager { get; }

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
        public string ApplicationUserAgentAddress => "team@jellyfin.org";

        /// <summary>
        /// Gets the current application name.
        /// </summary>
        /// <value>The application name.</value>
        public string ApplicationProductName { get; } = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).ProductName;

        public string SystemId
        {
            get
            {
                _deviceId ??= new DeviceId(ApplicationPaths, LoggerFactory);

                return _deviceId.Value;
            }
        }

        /// <inheritdoc/>
        public string Name => ApplicationProductName;

        private string CertificatePath { get; set; }

        public X509Certificate2 Certificate { get; private set; }

        /// <inheritdoc/>
        public bool ListenWithHttps => Certificate != null && ConfigurationManager.GetNetworkConfiguration().EnableHttps;

        public string FriendlyName =>
            string.IsNullOrEmpty(ConfigurationManager.Configuration.ServerName)
                ? Environment.MachineName
                : ConfigurationManager.Configuration.ServerName;

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

        /// <summary>
        /// Creates the instance safe.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.Object.</returns>
        protected object CreateInstanceSafe(Type type)
        {
            _creatingInstances ??= new List<Type>();

            if (_creatingInstances.Contains(type))
            {
                Logger.LogError("DI Loop detected in the attempted creation of {Type}", type.FullName);
                foreach (var entry in _creatingInstances)
                {
                    Logger.LogError("Called from: {TypeName}", entry.FullName);
                }

                _pluginManager.FailPlugin(type.Assembly);

                throw new TypeLoadException("DI Loop detected");
            }

            try
            {
                _creatingInstances.Add(type);
                Logger.LogDebug("Creating instance of {Type}", type);
                return ActivatorUtilities.CreateInstance(ServiceProvider, type);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating {Type}", type);
                // If this is a plugin fail it.
                _pluginManager.FailPlugin(type.Assembly);
                return null;
            }
            finally
            {
                _creatingInstances.Remove(type);
            }
        }

        /// <summary>
        /// Resolves this instance.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>``0.</returns>
        public T Resolve<T>() => ServiceProvider.GetService<T>();

        /// <inheritdoc/>
        public IEnumerable<Type> GetExportTypes<T>()
        {
            var currentType = typeof(T);
            var numberOfConcreteTypes = _allConcreteTypes.Length;
            for (var i = 0; i < numberOfConcreteTypes; i++)
            {
                var type = _allConcreteTypes[i];
                if (currentType.IsAssignableFrom(type))
                {
                    yield return type;
                }
            }
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
                foreach (var part in parts.OfType<IDisposable>())
                {
                    _disposableParts.TryAdd(part, byte.MinValue);
                }
            }

            return parts;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<T> GetExports<T>(CreationDelegateFactory defaultFunc, bool manageLifetime = true)
        {
            // Convert to list so this isn't executed for each iteration
            var parts = GetExportTypes<T>()
                .Select(i => defaultFunc(i))
                .Where(i => i != null)
                .Cast<T>()
                .ToList();

            if (manageLifetime)
            {
                foreach (var part in parts.OfType<IDisposable>())
                {
                    _disposableParts.TryAdd(part, byte.MinValue);
                }
            }

            return parts;
        }

        /// <summary>
        /// Runs the startup tasks.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><see cref="Task" />.</returns>
        public async Task RunStartupTasksAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Logger.LogInformation("Running startup tasks");

            Resolve<ITaskManager>().AddTasks(GetExports<IScheduledTask>(false));

            ConfigurationManager.ConfigurationUpdated += OnConfigurationUpdated;
            ConfigurationManager.NamedConfigurationUpdated += OnConfigurationUpdated;

            _mediaEncoder.SetFFmpegPath();

            Logger.LogInformation("ServerId: {ServerId}", SystemId);

            var entryPoints = GetExports<IServerEntryPoint>();

            cancellationToken.ThrowIfCancellationRequested();

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            await Task.WhenAll(StartEntryPoints(entryPoints, true)).ConfigureAwait(false);
            Logger.LogInformation("Executed all pre-startup entry points in {Elapsed:g}", stopWatch.Elapsed);

            Logger.LogInformation("Core startup complete");
            CoreStartupHasCompleted = true;

            cancellationToken.ThrowIfCancellationRequested();

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
            DiscoverTypes();

            ConfigurationManager.AddParts(GetExports<IConfigurationFactory>());

            NetManager = new NetworkManager(ConfigurationManager, LoggerFactory.CreateLogger<NetworkManager>());

            // Initialize runtime stat collection
            if (ConfigurationManager.Configuration.EnableMetrics)
            {
                DotNetRuntimeStatsBuilder.Default().StartCollecting();
            }

            var networkConfiguration = ConfigurationManager.GetNetworkConfiguration();
            HttpPort = networkConfiguration.HttpServerPortNumber;
            HttpsPort = networkConfiguration.HttpsPortNumber;

            // Safeguard against invalid configuration
            if (HttpPort == HttpsPort)
            {
                HttpPort = NetworkConfiguration.DefaultHttpPort;
                HttpsPort = NetworkConfiguration.DefaultHttpsPort;
            }

            CertificatePath = networkConfiguration.CertificatePath;
            Certificate = GetCertificate(CertificatePath, networkConfiguration.CertificatePassword);

            RegisterServices(serviceCollection);

            _pluginManager.RegisterServices(serviceCollection);
        }

        /// <summary>
        /// Registers services/resources with the service collection that will be available via DI.
        /// </summary>
        /// <param name="serviceCollection">Instance of the <see cref="IServiceCollection"/> interface.</param>
        protected virtual void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(_startupOptions);

            serviceCollection.AddMemoryCache();

            serviceCollection.AddSingleton<IServerConfigurationManager>(ConfigurationManager);
            serviceCollection.AddSingleton<IConfigurationManager>(ConfigurationManager);
            serviceCollection.AddSingleton<IApplicationHost>(this);
            serviceCollection.AddSingleton(_pluginManager);
            serviceCollection.AddSingleton<IApplicationPaths>(ApplicationPaths);

            serviceCollection.AddSingleton(_fileSystemManager);
            serviceCollection.AddSingleton<TmdbClientManager>();

            serviceCollection.AddSingleton(NetManager);

            serviceCollection.AddSingleton<ITaskManager, TaskManager>();

            serviceCollection.AddSingleton(_xmlSerializer);

            serviceCollection.AddSingleton<IStreamHelper, StreamHelper>();

            serviceCollection.AddSingleton<ICryptoProvider, CryptographyProvider>();

            serviceCollection.AddSingleton<ISocketFactory, SocketFactory>();

            serviceCollection.AddSingleton<IInstallationManager, InstallationManager>();

            serviceCollection.AddSingleton<IServerApplicationHost>(this);
            serviceCollection.AddSingleton(ApplicationPaths);

            serviceCollection.AddSingleton<ILocalizationManager, LocalizationManager>();

            serviceCollection.AddSingleton<IBlurayExaminer, BdInfoExaminer>();

            serviceCollection.AddSingleton<IUserDataRepository, SqliteUserDataRepository>();
            serviceCollection.AddSingleton<IUserDataManager, UserDataManager>();

            serviceCollection.AddSingleton<IItemRepository, SqliteItemRepository>();

            serviceCollection.AddSingleton<IMediaEncoder, MediaBrowser.MediaEncoding.Encoder.MediaEncoder>();
            serviceCollection.AddSingleton<EncodingHelper>();

            // TODO: Refactor to eliminate the circular dependencies here so that Lazy<T> isn't required
            serviceCollection.AddTransient(provider => new Lazy<ILibraryMonitor>(provider.GetRequiredService<ILibraryMonitor>));
            serviceCollection.AddTransient(provider => new Lazy<IProviderManager>(provider.GetRequiredService<IProviderManager>));
            serviceCollection.AddTransient(provider => new Lazy<IUserViewManager>(provider.GetRequiredService<IUserViewManager>));
            serviceCollection.AddSingleton<ILibraryManager, LibraryManager>();
            serviceCollection.AddSingleton<NamingOptions>();

            serviceCollection.AddSingleton<IMusicManager, MusicManager>();

            serviceCollection.AddSingleton<ILibraryMonitor, LibraryMonitor>();

            serviceCollection.AddSingleton<ISearchEngine, SearchEngine>();

            serviceCollection.AddSingleton<IWebSocketManager, WebSocketManager>();

            serviceCollection.AddSingleton<IImageProcessor, ImageProcessor>();

            serviceCollection.AddSingleton<ITVSeriesManager, TVSeriesManager>();

            serviceCollection.AddSingleton<IMediaSourceManager, MediaSourceManager>();

            serviceCollection.AddSingleton<ISubtitleManager, SubtitleManager>();
            serviceCollection.AddSingleton<ILyricManager, LyricManager>();

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

            serviceCollection.AddSingleton<IAuthService, AuthService>();
            serviceCollection.AddSingleton<IQuickConnect, QuickConnectManager>();

            serviceCollection.AddSingleton<ISubtitleParser, SubtitleEditParser>();
            serviceCollection.AddSingleton<ISubtitleEncoder, SubtitleEncoder>();

            serviceCollection.AddSingleton<IAttachmentExtractor, MediaBrowser.MediaEncoding.Attachments.AttachmentExtractor>();

            serviceCollection.AddSingleton<TranscodingJobHelper>();
            serviceCollection.AddScoped<MediaInfoHelper>();
            serviceCollection.AddScoped<AudioHelper>();
            serviceCollection.AddScoped<DynamicHlsHelper>();
            serviceCollection.AddScoped<IClientEventLogger, ClientEventLogger>();
            serviceCollection.AddSingleton<IDirectoryService, DirectoryService>();
        }

        /// <summary>
        /// Create services registered with the service container that need to be initialized at application startup.
        /// </summary>
        /// <returns>A task representing the service initialization operation.</returns>
        public async Task InitializeServices()
        {
            var jellyfinDb = await Resolve<IDbContextFactory<JellyfinDb>>().CreateDbContextAsync().ConfigureAwait(false);
            await using (jellyfinDb.ConfigureAwait(false))
            {
                if ((await jellyfinDb.Database.GetPendingMigrationsAsync().ConfigureAwait(false)).Any())
                {
                    Logger.LogInformation("There are pending EFCore migrations in the database. Applying... (This may take a while, do not stop Jellyfin)");
                    await jellyfinDb.Database.MigrateAsync().ConfigureAwait(false);
                    Logger.LogInformation("EFCore migrations applied successfully");
                }
            }

            var localizationManager = (LocalizationManager)Resolve<ILocalizationManager>();
            await localizationManager.LoadAll().ConfigureAwait(false);

            _mediaEncoder = Resolve<IMediaEncoder>();
            _sessionManager = Resolve<ISessionManager>();

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
            logger.LogInformation("Operating system: {OS}", MediaBrowser.Common.System.OperatingSystem.Name);
            logger.LogInformation("Architecture: {Architecture}", RuntimeInformation.OSArchitecture);
            logger.LogInformation("64-Bit Process: {Is64Bit}", Environment.Is64BitProcess);
            logger.LogInformation("User Interactive: {IsUserInteractive}", Environment.UserInteractive);
            logger.LogInformation("Processor count: {ProcessorCount}", Environment.ProcessorCount);
            logger.LogInformation("Program data path: {ProgramDataPath}", appPaths.ProgramDataPath);
            logger.LogInformation("Web resources path: {WebPath}", appPaths.WebPath);
            logger.LogInformation("Application directory: {ApplicationPath}", appPaths.ProgramSystemPath);
        }

        private X509Certificate2 GetCertificate(string path, string password)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                // Don't use an empty string password
                password = string.IsNullOrWhiteSpace(password) ? null : password;

                var localCert = new X509Certificate2(path, password, X509KeyStorageFlags.UserKeySet);
                if (!localCert.HasPrivateKey)
                {
                    Logger.LogError("No private key included in SSL cert {CertificateLocation}.", path);
                    return null;
                }

                return localCert;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading cert from {CertificateLocation}", path);
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
            BaseItem.ConfigurationManager = ConfigurationManager;
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
            CollectionFolder.ApplicationHost = this;
        }

        /// <summary>
        /// Finds plugin components and register them with the appropriate services.
        /// </summary>
        private void FindParts()
        {
            if (!ConfigurationManager.Configuration.IsPortAuthorized)
            {
                ConfigurationManager.Configuration.IsPortAuthorized = true;
                ConfigurationManager.SaveConfiguration();
            }

            _pluginManager.CreatePlugins();

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
                    _pluginManager.FailPlugin(ass);
                    continue;
                }
                catch (TypeLoadException ex)
                {
                    Logger.LogError(ex, "Error loading types from {Assembly}.", ass.FullName);
                    _pluginManager.FailPlugin(ass);
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

        /// <summary>
        /// Called when [configuration updated].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnConfigurationUpdated(object sender, EventArgs e)
        {
            var requiresRestart = false;
            var networkConfiguration = ConfigurationManager.GetNetworkConfiguration();

            // Don't do anything if these haven't been set yet
            if (HttpPort != 0 && HttpsPort != 0)
            {
                // Need to restart if ports have changed
                if (networkConfiguration.HttpServerPortNumber != HttpPort
                    || networkConfiguration.HttpsPortNumber != HttpsPort)
                {
                    if (ConfigurationManager.Configuration.IsPortAuthorized)
                    {
                        ConfigurationManager.Configuration.IsPortAuthorized = false;
                        ConfigurationManager.SaveConfiguration();

                        requiresRestart = true;
                    }
                }
            }

            if (ValidateSslCertificate(networkConfiguration))
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
        /// Validates the SSL certificate.
        /// </summary>
        /// <param name="networkConfig">The new configuration.</param>
        /// <exception cref="FileNotFoundException">The certificate path doesn't exist.</exception>
        private bool ValidateSslCertificate(NetworkConfiguration networkConfig)
        {
            var newPath = networkConfig.CertificatePath;

            if (!string.IsNullOrWhiteSpace(newPath)
                && !string.Equals(CertificatePath, newPath, StringComparison.Ordinal))
            {
                if (File.Exists(newPath))
                {
                    return true;
                }

                throw new FileNotFoundException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Certificate file '{0}' does not exist.",
                        newPath));
            }

            return false;
        }

        /// <summary>
        /// Notifies the kernel that a change has been made that requires a restart.
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
            foreach (var p in _pluginManager.LoadAssemblies())
            {
                yield return p;
            }

            // Include composable parts in the Model assembly
            yield return typeof(SystemInfo).Assembly;

            // Include composable parts in the Common assembly
            yield return typeof(IApplicationHost).Assembly;

            // Include composable parts in the Controller assembly
            yield return typeof(IServerApplicationHost).Assembly;

            // Include composable parts in the Providers assembly
            yield return typeof(ProviderManager).Assembly;

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

            // Network
            yield return typeof(NetworkManager).Assembly;

            // Hls
            yield return typeof(DynamicHlsPlaylistGenerator).Assembly;

            foreach (var i in GetAssembliesWithPartsInternal())
            {
                yield return i;
            }
        }

        protected abstract IEnumerable<Assembly> GetAssembliesWithPartsInternal();

        /// <summary>
        /// Gets the system status.
        /// </summary>
        /// <param name="request">Where this request originated.</param>
        /// <returns>SystemInfo.</returns>
        public SystemInfo GetSystemInfo(HttpRequest request)
        {
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
                OperatingSystem = MediaBrowser.Common.System.OperatingSystem.Id.ToString(),
                OperatingSystemDisplayName = MediaBrowser.Common.System.OperatingSystem.Name,
                CanSelfRestart = CanSelfRestart,
                CanLaunchWebBrowser = CanLaunchWebBrowser,
                TranscodingTempPath = ConfigurationManager.GetTranscodePath(),
                ServerName = FriendlyName,
                LocalAddress = GetSmartApiUrl(request),
                SupportsLibraryMonitor = true,
                SystemArchitecture = RuntimeInformation.OSArchitecture,
                PackageName = _startupOptions.PackageName
            };
        }

        public PublicSystemInfo GetPublicSystemInfo(HttpRequest request)
        {
            return new PublicSystemInfo
            {
                Version = ApplicationVersionString,
                ProductName = ApplicationProductName,
                Id = SystemId,
                OperatingSystem = MediaBrowser.Common.System.OperatingSystem.Id.ToString(),
                ServerName = FriendlyName,
                LocalAddress = GetSmartApiUrl(request),
                StartupWizardCompleted = ConfigurationManager.CommonConfiguration.IsStartupWizardCompleted
            };
        }

        /// <inheritdoc/>
        public string GetSmartApiUrl(IPAddress remoteAddr)
        {
            // Published server ends with a /
            if (!string.IsNullOrEmpty(PublishedServerUrl))
            {
                // Published server ends with a '/', so we need to remove it.
                return PublishedServerUrl.Trim('/');
            }

            string smart = NetManager.GetBindInterface(remoteAddr, out var port);
            return GetLocalApiUrl(smart.Trim('/'), null, port);
        }

        /// <inheritdoc/>
        public string GetSmartApiUrl(HttpRequest request)
        {
            // Return the host in the HTTP request as the API url
            if (ConfigurationManager.GetNetworkConfiguration().EnablePublishedServerUriByRequest)
            {
                int? requestPort = request.Host.Port;
                if ((requestPort == 80 && string.Equals(request.Scheme, "http", StringComparison.OrdinalIgnoreCase)) || (requestPort == 443 && string.Equals(request.Scheme, "https", StringComparison.OrdinalIgnoreCase)))
                {
                    requestPort = -1;
                }

                return GetLocalApiUrl(request.Host.Host, request.Scheme, requestPort);
            }

            return GetSmartApiUrl(request.HttpContext.Connection.RemoteIpAddress ?? IPAddress.Loopback);
        }

        /// <inheritdoc/>
        public string GetSmartApiUrl(string hostname)
        {
            // Published server ends with a /
            if (!string.IsNullOrEmpty(PublishedServerUrl))
            {
                // Published server ends with a '/', so we need to remove it.
                return PublishedServerUrl.Trim('/');
            }

            string smart = NetManager.GetBindInterface(hostname, out var port);
            return GetLocalApiUrl(smart.Trim('/'), null, port);
        }

        /// <inheritdoc/>
        public string GetApiUrlForLocalAccess(IPObject hostname = null, bool allowHttps = true)
        {
            // With an empty source, the port will be null
            var smart = NetManager.GetBindInterface(hostname ?? IPHost.None, out _);
            var scheme = !allowHttps ? Uri.UriSchemeHttp : null;
            int? port = !allowHttps ? HttpPort : null;
            return GetLocalApiUrl(smart, scheme, port);
        }

        /// <inheritdoc/>
        public string GetLocalApiUrl(string hostname, string scheme = null, int? port = null)
        {
            // If the smartAPI doesn't start with http then treat it as a host or ip.
            if (hostname.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return hostname.TrimEnd('/');
            }

            // NOTE: If no BaseUrl is set then UriBuilder appends a trailing slash, but if there is no BaseUrl it does
            // not. For consistency, always trim the trailing slash.
            scheme ??= ListenWithHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
            var isHttps = string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
            return new UriBuilder
            {
                Scheme = scheme,
                Host = hostname,
                Port = port ?? (isHttps ? HttpsPort : HttpPort),
                Path = ConfigurationManager.GetNetworkConfiguration().BaseUrl
            }.ToString().TrimEnd('/');
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

                foreach (var (part, _) in _disposableParts)
                {
                    var partType = part.GetType();
                    if (partType == type)
                    {
                        continue;
                    }

                    Logger.LogInformation("Disposing {Type}", partType.Name);

                    try
                    {
                        part.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error disposing {Type}", partType.Name);
                    }
                }

                _disposableParts.Clear();
            }

            _disposed = true;
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Used to perform asynchronous cleanup of managed resources or for cascading calls to <see cref="DisposeAsync"/>.
        /// </summary>
        /// <returns>A ValueTask.</returns>
        protected virtual async ValueTask DisposeAsyncCore()
        {
            var type = GetType();

            Logger.LogInformation("Disposing {Type}", type.Name);

            foreach (var (part, _) in _disposableParts)
            {
                var partType = part.GetType();
                if (partType == type)
                {
                    continue;
                }

                Logger.LogInformation("Disposing {Type}", partType.Name);

                try
                {
                    part.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error disposing {Type}", partType.Name);
                }
            }

            // used for closing websockets
            foreach (var session in _sessionManager.Sessions)
            {
                await session.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
