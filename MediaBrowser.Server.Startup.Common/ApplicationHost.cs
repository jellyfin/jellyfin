using Emby.Drawing;
using Emby.Drawing.GDI;
using Emby.Drawing.ImageMagick;
using MediaBrowser.Api;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Implementations;
using MediaBrowser.Common.Implementations.ScheduledTasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Activity;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Connect;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.FileOrganization;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.News;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Social;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Controller.TV;
using MediaBrowser.Dlna;
using MediaBrowser.Dlna.ConnectionManager;
using MediaBrowser.Dlna.ContentDirectory;
using MediaBrowser.Dlna.Main;
using MediaBrowser.Dlna.MediaReceiverRegistrar;
using MediaBrowser.Dlna.Ssdp;
using MediaBrowser.LocalMetadata.Savers;
using MediaBrowser.MediaEncoding.BdInfo;
using MediaBrowser.MediaEncoding.Encoder;
using MediaBrowser.MediaEncoding.Subtitles;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Updates;
using MediaBrowser.Providers.Chapters;
using MediaBrowser.Providers.Manager;
using MediaBrowser.Providers.Subtitles;
using MediaBrowser.Server.Implementations;
using MediaBrowser.Server.Implementations.Activity;
using MediaBrowser.Server.Implementations.Channels;
using MediaBrowser.Server.Implementations.Collections;
using MediaBrowser.Server.Implementations.Configuration;
using MediaBrowser.Server.Implementations.Connect;
using MediaBrowser.Server.Implementations.Devices;
using MediaBrowser.Server.Implementations.Dto;
using MediaBrowser.Server.Implementations.EntryPoints;
using MediaBrowser.Server.Implementations.FileOrganization;
using MediaBrowser.Server.Implementations.HttpServer;
using MediaBrowser.Server.Implementations.HttpServer.Security;
using MediaBrowser.Server.Implementations.IO;
using MediaBrowser.Server.Implementations.Library;
using MediaBrowser.Server.Implementations.LiveTv;
using MediaBrowser.Server.Implementations.Localization;
using MediaBrowser.Server.Implementations.MediaEncoder;
using MediaBrowser.Server.Implementations.Notifications;
using MediaBrowser.Server.Implementations.Persistence;
using MediaBrowser.Server.Implementations.Playlists;
using MediaBrowser.Server.Implementations.Security;
using MediaBrowser.Server.Implementations.ServerManager;
using MediaBrowser.Server.Implementations.Session;
using MediaBrowser.Server.Implementations.Social;
using MediaBrowser.Server.Implementations.Sync;
using MediaBrowser.Server.Implementations.TV;
using MediaBrowser.Server.Startup.Common.FFMpeg;
using MediaBrowser.Server.Startup.Common.Migrations;
using MediaBrowser.WebDashboard.Api;
using MediaBrowser.XbmcMetadata.Providers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Api.Playback;
using MediaBrowser.Common.Implementations.Serialization;
using MediaBrowser.Common.Implementations.Updates;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Server.Startup.Common
{
    /// <summary>
    /// Class CompositionRoot
    /// </summary>
    public class ApplicationHost : BaseApplicationHost<ServerApplicationPaths>, IServerApplicationHost
    {
        /// <summary>
        /// Gets the server configuration manager.
        /// </summary>
        /// <value>The server configuration manager.</value>
        public IServerConfigurationManager ServerConfigurationManager
        {
            get { return (IServerConfigurationManager)ConfigurationManager; }
        }

        /// <summary>
        /// Gets the configuration manager.
        /// </summary>
        /// <returns>IConfigurationManager.</returns>
        protected override IConfigurationManager GetConfigurationManager()
        {
            return new ServerConfigurationManager(ApplicationPaths, LogManager, XmlSerializer, FileSystemManager);
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
        private ILibraryMonitor LibraryMonitor { get; set; }
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
        private ISubtitleEncoder SubtitleEncoder { get; set; }

        private IConnectManager ConnectManager { get; set; }
        private ISessionManager SessionManager { get; set; }

        private ILiveTvManager LiveTvManager { get; set; }

        public ILocalizationManager LocalizationManager { get; set; }

        private IEncodingManager EncodingManager { get; set; }
        private IChannelManager ChannelManager { get; set; }
        private ISyncManager SyncManager { get; set; }

        /// <summary>
        /// Gets or sets the user data repository.
        /// </summary>
        /// <value>The user data repository.</value>
        private IUserDataManager UserDataManager { get; set; }
        private IUserRepository UserRepository { get; set; }
        internal IDisplayPreferencesRepository DisplayPreferencesRepository { get; set; }
        internal IItemRepository ItemRepository { get; set; }
        private INotificationsRepository NotificationsRepository { get; set; }
        private IFileOrganizationRepository FileOrganizationRepository { get; set; }

        private INotificationManager NotificationManager { get; set; }
        private ISubtitleManager SubtitleManager { get; set; }
        private IChapterManager ChapterManager { get; set; }
        private IDeviceManager DeviceManager { get; set; }

        internal IUserViewManager UserViewManager { get; set; }

        private IAuthenticationRepository AuthenticationRepository { get; set; }
        private ISyncRepository SyncRepository { get; set; }
        private ITVSeriesManager TVSeriesManager { get; set; }
        private ICollectionManager CollectionManager { get; set; }
        private IMediaSourceManager MediaSourceManager { get; set; }
        private IPlaylistManager PlaylistManager { get; set; }

        private readonly StartupOptions _startupOptions;
        private readonly string _releaseAssetFilename;

        internal INativeApp NativeApp { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationHost" /> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="options">The options.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="releaseAssetFilename">The release asset filename.</param>
        /// <param name="nativeApp">The native application.</param>
        public ApplicationHost(ServerApplicationPaths applicationPaths,
            ILogManager logManager,
            StartupOptions options,
            IFileSystem fileSystem,
            string releaseAssetFilename,
            INativeApp nativeApp)
            : base(applicationPaths, logManager, fileSystem)
        {
            _startupOptions = options;
            _releaseAssetFilename = releaseAssetFilename;
            NativeApp = nativeApp;

            SetBaseExceptionMessage();
        }

        private Version _version;
        /// <summary>
        /// Gets the current application version
        /// </summary>
        /// <value>The application version.</value>
        public override Version ApplicationVersion
        {
            get
            {
                return _version ?? (_version = NativeApp.GetType().Assembly.GetName().Version);
            }
        }

        public override string OperatingSystemDisplayName
        {
            get { return NativeApp.Environment.OperatingSystemVersionString; }
        }

        public override bool IsRunningAsService
        {
            get { return NativeApp.IsRunningAsService; }
        }

        public bool SupportsRunningAsService
        {
            get { return NativeApp.SupportsRunningAsService; }
        }

        public bool SupportsLibraryMonitor
        {
            get { return NativeApp.SupportsLibraryMonitor; }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Emby Server";
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can self restart.
        /// </summary>
        /// <value><c>true</c> if this instance can self restart; otherwise, <c>false</c>.</value>
        public override bool CanSelfRestart
        {
            get { return NativeApp.CanSelfRestart; }
        }

        public bool SupportsAutoRunAtStartup
        {
            get { return NativeApp.SupportsAutoRunAtStartup; }
        }

        private void SetBaseExceptionMessage()
        {
            var builder = GetBaseExceptionMessage(ApplicationPaths);

            // Skip if plugins haven't been loaded yet
            //if (Plugins != null)
            //{
            //    var pluginString = string.Join("|", Plugins.Select(i => i.Name + "-" + i.Version.ToString()).ToArray());
            //    builder.Insert(0, string.Format("Plugins: {0}{1}", pluginString, Environment.NewLine));
            //}

            builder.Insert(0, string.Format("Version: {0}{1}", ApplicationVersion, Environment.NewLine));
            builder.Insert(0, "*** Error Report ***" + Environment.NewLine);

            LogManager.ExceptionMessagePrefix = builder.ToString();
        }

        /// <summary>
        /// Runs the startup tasks.
        /// </summary>
        /// <summary>
        /// Runs the startup tasks.
        /// </summary>
        public override async Task RunStartupTasks()
        {
            await PerformPreInitMigrations().ConfigureAwait(false);

            if (ServerConfigurationManager.Configuration.MigrationVersion < CleanDatabaseScheduledTask.MigrationVersion &&
                ServerConfigurationManager.Configuration.IsStartupWizardCompleted)
            {
                TaskManager.SuspendTriggers = true;
            }

            await base.RunStartupTasks().ConfigureAwait(false);

            await MediaEncoder.Init().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(MediaEncoder.EncoderPath))
            {
                if (ServerConfigurationManager.Configuration.IsStartupWizardCompleted)
                {
                    ServerConfigurationManager.Configuration.IsStartupWizardCompleted = false;
                    ServerConfigurationManager.SaveConfiguration();
                }
            }

            Logger.Info("ServerId: {0}", SystemId);
            Logger.Info("Core startup complete");
            HttpServer.GlobalResponse = null;

            PerformPostInitMigrations();
            Logger.Info("Post-init migrations complete");

            foreach (var entryPoint in GetExports<IServerEntryPoint>().ToList())
            {
                var name = entryPoint.GetType().FullName;
                Logger.Info("Starting entry point {0}", name);
                var now = DateTime.UtcNow;
                try
                {
                    entryPoint.Run();
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error in {0}", ex, name);
                }
                Logger.Info("Entry point completed: {0}. Duration: {1} seconds", name, (DateTime.UtcNow - now).TotalSeconds.ToString(CultureInfo.InvariantCulture));
            }
            Logger.Info("All entry points have started");

            LogManager.RemoveConsoleOutput();
        }

        protected override IJsonSerializer CreateJsonSerializer()
        {
            var result = base.CreateJsonSerializer();

            ServiceStack.Text.JsConfig<Movie>.ExcludePropertyNames = new[] { "ShortOverview" };
            ServiceStack.Text.JsConfig<Movie>.ExcludePropertyNames = new[] { "Taglines" };
            ServiceStack.Text.JsConfig<Movie>.ExcludePropertyNames = new[] { "Keywords" };
            ServiceStack.Text.JsConfig<Trailer>.ExcludePropertyNames = new[] { "ShortOverview" };
            ServiceStack.Text.JsConfig<Series>.ExcludePropertyNames = new[] { "ShortOverview" };
            ServiceStack.Text.JsConfig<Person>.ExcludePropertyNames = new[] { "PlaceOfBirth" };

            ServiceStack.Text.JsConfig<LiveTvProgram>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<LiveTvChannel>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<LiveTvVideoRecording>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<LiveTvAudioRecording>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<Series>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<Audio>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<MusicAlbum>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<MusicArtist>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<MusicGenre>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<MusicVideo>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<Movie>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<Playlist>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<AudioPodcast>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<Trailer>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<BoxSet>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<Episode>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<Season>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<Book>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<CollectionFolder>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<Folder>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<Game>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<GameGenre>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<GameSystem>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<Genre>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<Person>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<Photo>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<PhotoAlbum>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<Studio>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<UserRootFolder>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<UserView>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<Video>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<Year>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<Channel>.ExcludePropertyNames = new[] { "ProviderIds" };
            ServiceStack.Text.JsConfig<AggregateFolder>.ExcludePropertyNames = new[] { "ProviderIds" };

            ServiceStack.Text.JsConfig<LiveTvProgram>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<LiveTvChannel>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<LiveTvVideoRecording>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<LiveTvAudioRecording>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<Series>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<Audio>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<MusicAlbum>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<MusicArtist>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<MusicGenre>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<MusicVideo>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<Movie>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<Playlist>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<AudioPodcast>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<Trailer>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<BoxSet>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<Episode>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<Season>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<Book>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<CollectionFolder>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<Folder>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<Game>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<GameGenre>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<GameSystem>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<Genre>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<Person>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<Photo>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<PhotoAlbum>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<Studio>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<UserRootFolder>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<UserView>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<Video>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<Year>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<Channel>.ExcludePropertyNames = new[] { "ImageInfos" };
            ServiceStack.Text.JsConfig<AggregateFolder>.ExcludePropertyNames = new[] { "ImageInfos" };

            ServiceStack.Text.JsConfig<LiveTvProgram>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<LiveTvChannel>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<LiveTvVideoRecording>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<LiveTvAudioRecording>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<Series>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<Audio>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<MusicAlbum>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<MusicArtist>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<MusicGenre>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<MusicVideo>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<Movie>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<Playlist>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<AudioPodcast>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<Trailer>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<BoxSet>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<Episode>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<Season>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<Book>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<CollectionFolder>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<Folder>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<Game>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<GameGenre>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<GameSystem>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<Genre>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<Person>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<Photo>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<PhotoAlbum>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<Studio>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<UserRootFolder>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<UserView>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<Video>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<Year>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<Channel>.ExcludePropertyNames = new[] { "ProductionLocations" };
            ServiceStack.Text.JsConfig<AggregateFolder>.ExcludePropertyNames = new[] { "ProductionLocations" };

            ServiceStack.Text.JsConfig<LiveTvProgram>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<LiveTvChannel>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<LiveTvVideoRecording>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<LiveTvAudioRecording>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<Series>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<Audio>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<MusicAlbum>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<MusicArtist>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<MusicGenre>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<MusicVideo>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<Movie>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<Playlist>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<AudioPodcast>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<Trailer>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<BoxSet>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<Episode>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<Season>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<Book>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<CollectionFolder>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<Folder>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<Game>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<GameGenre>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<GameSystem>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<Genre>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<Person>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<Photo>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<PhotoAlbum>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<Studio>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<UserRootFolder>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<UserView>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<Video>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<Year>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<Channel>.ExcludePropertyNames = new[] { "ThemeSongIds" };
            ServiceStack.Text.JsConfig<AggregateFolder>.ExcludePropertyNames = new[] { "ThemeSongIds" };

            ServiceStack.Text.JsConfig<LiveTvProgram>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<LiveTvChannel>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<LiveTvVideoRecording>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<LiveTvAudioRecording>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<Series>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<Audio>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<MusicAlbum>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<MusicArtist>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<MusicGenre>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<MusicVideo>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<Movie>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<Playlist>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<AudioPodcast>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<Trailer>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<BoxSet>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<Episode>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<Season>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<Book>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<CollectionFolder>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<Folder>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<Game>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<GameGenre>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<GameSystem>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<Genre>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<Person>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<Photo>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<PhotoAlbum>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<Studio>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<UserRootFolder>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<UserView>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<Video>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<Year>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<Channel>.ExcludePropertyNames = new[] { "ThemeVideoIds" };
            ServiceStack.Text.JsConfig<AggregateFolder>.ExcludePropertyNames = new[] { "ThemeVideoIds" };

            return result;
        }

        public override Task Init(IProgress<double> progress)
        {
            HttpPort = ServerConfigurationManager.Configuration.HttpServerPortNumber;
            HttpsPort = ServerConfigurationManager.Configuration.HttpsPortNumber;

            return base.Init(progress);
        }

        private async Task PerformPreInitMigrations()
        {
            var migrations = new List<IVersionMigration>
            {
                new UpdateLevelMigration(ServerConfigurationManager, this, HttpClient, JsonSerializer, _releaseAssetFilename, Logger)
            };

            foreach (var task in migrations)
            {
                try
                {
                    await task.Run().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error running migration", ex);
                }
            }
        }

        private void PerformPostInitMigrations()
        {
            var migrations = new List<IVersionMigration>
            {
                new MovieDbEpisodeProviderMigration(ServerConfigurationManager),
                new DbMigration(ServerConfigurationManager, TaskManager)
            };

            foreach (var task in migrations)
            {
                try
                {
                    task.Run();
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error running migration", ex);
                }
            }
        }

        /// <summary>
        /// Registers resources that classes will depend on
        /// </summary>
        protected override async Task RegisterResources(IProgress<double> progress)
        {
            await base.RegisterResources(progress).ConfigureAwait(false);

            RegisterSingleInstance<IHttpResultFactory>(new HttpResultFactory(LogManager, FileSystemManager, JsonSerializer));

            RegisterSingleInstance<IServerApplicationHost>(this);
            RegisterSingleInstance<IServerApplicationPaths>(ApplicationPaths);

            RegisterSingleInstance(ServerConfigurationManager);

            LocalizationManager = new LocalizationManager(ServerConfigurationManager, FileSystemManager, JsonSerializer, LogManager.GetLogger("LocalizationManager"));
            RegisterSingleInstance(LocalizationManager);

            RegisterSingleInstance<IBlurayExaminer>(() => new BdInfoExaminer());

            UserDataManager = new UserDataManager(LogManager, ServerConfigurationManager);
            RegisterSingleInstance(UserDataManager);

            UserRepository = await GetUserRepository().ConfigureAwait(false);

            var displayPreferencesRepo = new SqliteDisplayPreferencesRepository(LogManager, JsonSerializer, ApplicationPaths, NativeApp.GetDbConnector(), MemoryStreamProvider);
            DisplayPreferencesRepository = displayPreferencesRepo;
            RegisterSingleInstance(DisplayPreferencesRepository);

            var itemRepo = new SqliteItemRepository(ServerConfigurationManager, JsonSerializer, LogManager, NativeApp.GetDbConnector(), MemoryStreamProvider);
            ItemRepository = itemRepo;
            RegisterSingleInstance(ItemRepository);

            FileOrganizationRepository = await GetFileOrganizationRepository().ConfigureAwait(false);
            RegisterSingleInstance(FileOrganizationRepository);

            AuthenticationRepository = await GetAuthenticationRepository().ConfigureAwait(false);
            RegisterSingleInstance(AuthenticationRepository);

            SyncRepository = await GetSyncRepository().ConfigureAwait(false);
            RegisterSingleInstance(SyncRepository);

            UserManager = new UserManager(LogManager.GetLogger("UserManager"), ServerConfigurationManager, UserRepository, XmlSerializer, NetworkManager, () => ImageProcessor, () => DtoService, () => ConnectManager, this, JsonSerializer, FileSystemManager);
            RegisterSingleInstance(UserManager);

            LibraryManager = new LibraryManager(Logger, TaskManager, UserManager, ServerConfigurationManager, UserDataManager, () => LibraryMonitor, FileSystemManager, () => ProviderManager, () => UserViewManager);
            RegisterSingleInstance(LibraryManager);

            var musicManager = new MusicManager(LibraryManager);
            RegisterSingleInstance<IMusicManager>(new MusicManager(LibraryManager));

            LibraryMonitor = new LibraryMonitor(LogManager, TaskManager, LibraryManager, ServerConfigurationManager, FileSystemManager, this);
            RegisterSingleInstance(LibraryMonitor);

            ProviderManager = new ProviderManager(HttpClient, ServerConfigurationManager, LibraryMonitor, LogManager, FileSystemManager, ApplicationPaths, () => LibraryManager, JsonSerializer, MemoryStreamProvider);
            RegisterSingleInstance(ProviderManager);

            RegisterSingleInstance<ISearchEngine>(() => new SearchEngine(LogManager, LibraryManager, UserManager));

            HttpServer = ServerFactory.CreateServer(this, LogManager, ServerConfigurationManager, NetworkManager, MemoryStreamProvider, "Emby", "web/index.html");
            HttpServer.GlobalResponse = LocalizationManager.GetLocalizedString("StartupEmbyServerIsLoading");
            RegisterSingleInstance(HttpServer, false);
            progress.Report(10);

            ServerManager = new ServerManager(this, JsonSerializer, LogManager.GetLogger("ServerManager"), ServerConfigurationManager, MemoryStreamProvider);
            RegisterSingleInstance(ServerManager);

            var innerProgress = new ActionableProgress<double>();
            innerProgress.RegisterAction(p => progress.Report((.75 * p) + 15));

            ImageProcessor = GetImageProcessor();
            RegisterSingleInstance(ImageProcessor);

            TVSeriesManager = new TVSeriesManager(UserManager, UserDataManager, LibraryManager, ServerConfigurationManager);
            RegisterSingleInstance(TVSeriesManager);

            SyncManager = new SyncManager(LibraryManager, SyncRepository, ImageProcessor, LogManager.GetLogger("SyncManager"), UserManager, () => DtoService, this, TVSeriesManager, () => MediaEncoder, FileSystemManager, () => SubtitleEncoder, ServerConfigurationManager, UserDataManager, () => MediaSourceManager, JsonSerializer, TaskManager, MemoryStreamProvider);
            RegisterSingleInstance(SyncManager);

            DtoService = new DtoService(LogManager.GetLogger("DtoService"), LibraryManager, UserDataManager, ItemRepository, ImageProcessor, ServerConfigurationManager, FileSystemManager, ProviderManager, () => ChannelManager, SyncManager, this, () => DeviceManager, () => MediaSourceManager, () => LiveTvManager);
            RegisterSingleInstance(DtoService);

            var encryptionManager = new EncryptionManager();
            RegisterSingleInstance<IEncryptionManager>(encryptionManager);

            ConnectManager = new ConnectManager(LogManager.GetLogger("ConnectManager"), ApplicationPaths, JsonSerializer, encryptionManager, HttpClient, this, ServerConfigurationManager, UserManager, ProviderManager, SecurityManager, FileSystemManager);
            RegisterSingleInstance(ConnectManager);

            DeviceManager = new DeviceManager(new DeviceRepository(ApplicationPaths, JsonSerializer, LogManager.GetLogger("DeviceManager"), FileSystemManager), UserManager, FileSystemManager, LibraryMonitor, ServerConfigurationManager, LogManager.GetLogger("DeviceManager"), NetworkManager);
            RegisterSingleInstance(DeviceManager);

            var newsService = new Implementations.News.NewsService(ApplicationPaths, JsonSerializer);
            RegisterSingleInstance<INewsService>(newsService);

            var fileOrganizationService = new FileOrganizationService(TaskManager, FileOrganizationRepository, LogManager.GetLogger("FileOrganizationService"), LibraryMonitor, LibraryManager, ServerConfigurationManager, FileSystemManager, ProviderManager);
            RegisterSingleInstance<IFileOrganizationService>(fileOrganizationService);

            progress.Report(15);

            ChannelManager = new ChannelManager(UserManager, DtoService, LibraryManager, LogManager.GetLogger("ChannelManager"), ServerConfigurationManager, FileSystemManager, UserDataManager, JsonSerializer, LocalizationManager, HttpClient, ProviderManager);
            RegisterSingleInstance(ChannelManager);

            MediaSourceManager = new MediaSourceManager(ItemRepository, UserManager, LibraryManager, LogManager.GetLogger("MediaSourceManager"), JsonSerializer, FileSystemManager, UserDataManager);
            RegisterSingleInstance(MediaSourceManager);

            SessionManager = new SessionManager(UserDataManager, LogManager.GetLogger("SessionManager"), LibraryManager, UserManager, musicManager, DtoService, ImageProcessor, JsonSerializer, this, HttpClient, AuthenticationRepository, DeviceManager, MediaSourceManager);
            RegisterSingleInstance(SessionManager);

            var dlnaManager = new DlnaManager(XmlSerializer, FileSystemManager, ApplicationPaths, LogManager.GetLogger("Dlna"), JsonSerializer, this);
            RegisterSingleInstance<IDlnaManager>(dlnaManager);

            var connectionManager = new ConnectionManager(dlnaManager, ServerConfigurationManager, LogManager.GetLogger("UpnpConnectionManager"), HttpClient);
            RegisterSingleInstance<IConnectionManager>(connectionManager);

            CollectionManager = new CollectionManager(LibraryManager, FileSystemManager, LibraryMonitor, LogManager.GetLogger("CollectionManager"), ProviderManager);
            RegisterSingleInstance(CollectionManager);

            PlaylistManager = new PlaylistManager(LibraryManager, FileSystemManager, LibraryMonitor, LogManager.GetLogger("PlaylistManager"), UserManager, ProviderManager);
            RegisterSingleInstance<IPlaylistManager>(PlaylistManager);

            LiveTvManager = new LiveTvManager(this, ServerConfigurationManager, Logger, ItemRepository, ImageProcessor, UserDataManager, DtoService, UserManager, LibraryManager, TaskManager, LocalizationManager, JsonSerializer, ProviderManager, FileSystemManager, SecurityManager);
            RegisterSingleInstance(LiveTvManager);

            UserViewManager = new UserViewManager(LibraryManager, LocalizationManager, UserManager, ChannelManager, LiveTvManager, ServerConfigurationManager);
            RegisterSingleInstance(UserViewManager);

            var contentDirectory = new ContentDirectory(dlnaManager, UserDataManager, ImageProcessor, LibraryManager, ServerConfigurationManager, UserManager, LogManager.GetLogger("UpnpContentDirectory"), HttpClient, LocalizationManager, ChannelManager, MediaSourceManager, UserViewManager, () => MediaEncoder);
            RegisterSingleInstance<IContentDirectory>(contentDirectory);

            var mediaRegistrar = new MediaReceiverRegistrar(LogManager.GetLogger("MediaReceiverRegistrar"), HttpClient, ServerConfigurationManager);
            RegisterSingleInstance<IMediaReceiverRegistrar>(mediaRegistrar);

            NotificationManager = new NotificationManager(LogManager, UserManager, ServerConfigurationManager);
            RegisterSingleInstance(NotificationManager);

            SubtitleManager = new SubtitleManager(LogManager.GetLogger("SubtitleManager"), FileSystemManager, LibraryMonitor, LibraryManager, MediaSourceManager);
            RegisterSingleInstance(SubtitleManager);

            RegisterSingleInstance<IDeviceDiscovery>(new DeviceDiscovery(LogManager.GetLogger("IDeviceDiscovery"), ServerConfigurationManager));

            ChapterManager = new ChapterManager(LibraryManager, LogManager.GetLogger("ChapterManager"), ServerConfigurationManager, ItemRepository);
            RegisterSingleInstance(ChapterManager);

            await RegisterMediaEncoder(innerProgress).ConfigureAwait(false);
            progress.Report(90);

            EncodingManager = new EncodingManager(FileSystemManager, Logger, MediaEncoder, ChapterManager, LibraryManager);
            RegisterSingleInstance(EncodingManager);

            var sharingRepo = new SharingRepository(LogManager, ApplicationPaths, NativeApp.GetDbConnector());
            await sharingRepo.Initialize().ConfigureAwait(false);
            RegisterSingleInstance<ISharingManager>(new SharingManager(sharingRepo, ServerConfigurationManager, LibraryManager, this));

            var activityLogRepo = await GetActivityLogRepository().ConfigureAwait(false);
            RegisterSingleInstance(activityLogRepo);
            RegisterSingleInstance<IActivityManager>(new ActivityManager(LogManager.GetLogger("ActivityManager"), activityLogRepo, UserManager));

            var authContext = new AuthorizationContext(AuthenticationRepository, ConnectManager);
            RegisterSingleInstance<IAuthorizationContext>(authContext);
            RegisterSingleInstance<ISessionContext>(new SessionContext(UserManager, authContext, SessionManager));
            RegisterSingleInstance<IAuthService>(new AuthService(UserManager, authContext, ServerConfigurationManager, ConnectManager, SessionManager, DeviceManager));

            SubtitleEncoder = new SubtitleEncoder(LibraryManager, LogManager.GetLogger("SubtitleEncoder"), ApplicationPaths, FileSystemManager, MediaEncoder, JsonSerializer, HttpClient, MediaSourceManager, MemoryStreamProvider);
            RegisterSingleInstance(SubtitleEncoder);

            await displayPreferencesRepo.Initialize().ConfigureAwait(false);

            var userDataRepo = new SqliteUserDataRepository(LogManager, ApplicationPaths, NativeApp.GetDbConnector());

            ((UserDataManager)UserDataManager).Repository = userDataRepo;
            await itemRepo.Initialize(userDataRepo).ConfigureAwait(false);
            ((LibraryManager)LibraryManager).ItemRepository = ItemRepository;
            await ConfigureNotificationsRepository().ConfigureAwait(false);
            progress.Report(100);

            SetStaticProperties();

            await ((UserManager)UserManager).Initialize().ConfigureAwait(false);
        }

        private IImageProcessor GetImageProcessor()
        {
            var maxConcurrentImageProcesses = Math.Max(Environment.ProcessorCount, 4);

            if (_startupOptions.ContainsOption("-imagethreads"))
            {
                int.TryParse(_startupOptions.GetOption("-imagethreads"), NumberStyles.Any, CultureInfo.InvariantCulture, out maxConcurrentImageProcesses);
            }

            return new ImageProcessor(LogManager.GetLogger("ImageProcessor"), ServerConfigurationManager.ApplicationPaths, FileSystemManager, JsonSerializer, GetImageEncoder(), maxConcurrentImageProcesses, () => LibraryManager);
        }

        private IImageEncoder GetImageEncoder()
        {
            if (!_startupOptions.ContainsOption("-enablegdi"))
            {
                try
                {
                    return new ImageMagickEncoder(LogManager.GetLogger("ImageMagick"), ApplicationPaths, HttpClient, FileSystemManager, ServerConfigurationManager);
                }
                catch
                {
                    Logger.Error("Error loading ImageMagick. Will revert to GDI.");
                }
            }

            try
            {
                return new GDIImageEncoder(FileSystemManager, LogManager.GetLogger("GDI"));
            }
            catch
            {
                Logger.Error("Error loading GDI. Will revert to NullImageEncoder.");
            }

            return new NullImageEncoder();
        }

        protected override INetworkManager CreateNetworkManager(ILogger logger)
        {
            return NativeApp.CreateNetworkManager(logger);
        }

        /// <summary>
        /// Registers the media encoder.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task RegisterMediaEncoder(IProgress<double> progress)
        {
            string encoderPath = null;
            string probePath = null;

            var info = await new FFMpegLoader(Logger, ApplicationPaths, HttpClient, ZipClient, FileSystemManager, NativeApp.Environment, NativeApp.GetFfmpegInstallInfo())
                .GetFFMpegInfo(NativeApp.Environment, _startupOptions, progress).ConfigureAwait(false);

            encoderPath = info.EncoderPath;
            probePath = info.ProbePath;
            var hasExternalEncoder = string.Equals(info.Version, "external", StringComparison.OrdinalIgnoreCase);

            var mediaEncoder = new MediaEncoder(LogManager.GetLogger("MediaEncoder"),
                JsonSerializer,
                encoderPath,
                probePath,
                hasExternalEncoder,
                ServerConfigurationManager,
                FileSystemManager,
                LiveTvManager,
                IsoManager,
                LibraryManager,
                ChannelManager,
                SessionManager,
                () => SubtitleEncoder,
                () => MediaSourceManager,
                HttpClient,
                ZipClient, MemoryStreamProvider);

            MediaEncoder = mediaEncoder;
            RegisterSingleInstance(MediaEncoder);
        }

        /// <summary>
        /// Gets the user repository.
        /// </summary>
        /// <returns>Task{IUserRepository}.</returns>
        private async Task<IUserRepository> GetUserRepository()
        {
            var repo = new SqliteUserRepository(LogManager, ApplicationPaths, JsonSerializer, NativeApp.GetDbConnector(), MemoryStreamProvider);

            await repo.Initialize().ConfigureAwait(false);

            return repo;
        }

        /// <summary>
        /// Gets the file organization repository.
        /// </summary>
        /// <returns>Task{IUserRepository}.</returns>
        private async Task<IFileOrganizationRepository> GetFileOrganizationRepository()
        {
            var repo = new SqliteFileOrganizationRepository(LogManager, ServerConfigurationManager.ApplicationPaths, NativeApp.GetDbConnector());

            await repo.Initialize().ConfigureAwait(false);

            return repo;
        }

        private async Task<IAuthenticationRepository> GetAuthenticationRepository()
        {
            var repo = new AuthenticationRepository(LogManager, ServerConfigurationManager.ApplicationPaths, NativeApp.GetDbConnector());

            await repo.Initialize().ConfigureAwait(false);

            return repo;
        }

        private async Task<IActivityRepository> GetActivityLogRepository()
        {
            var repo = new ActivityRepository(LogManager, ServerConfigurationManager.ApplicationPaths, NativeApp.GetDbConnector());

            await repo.Initialize().ConfigureAwait(false);

            return repo;
        }

        private async Task<ISyncRepository> GetSyncRepository()
        {
            var repo = new SyncRepository(LogManager, JsonSerializer, ServerConfigurationManager.ApplicationPaths, NativeApp.GetDbConnector());

            await repo.Initialize().ConfigureAwait(false);

            return repo;
        }

        /// <summary>
        /// Configures the repositories.
        /// </summary>
        private async Task ConfigureNotificationsRepository()
        {
            var repo = new SqliteNotificationsRepository(LogManager, ApplicationPaths, NativeApp.GetDbConnector());

            await repo.Initialize().ConfigureAwait(false);

            NotificationsRepository = repo;

            RegisterSingleInstance(NotificationsRepository);
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
            Folder.UserManager = UserManager;
            BaseItem.FileSystem = FileSystemManager;
            BaseItem.UserDataManager = UserDataManager;
            BaseItem.ChannelManager = ChannelManager;
            BaseItem.LiveTvManager = LiveTvManager;
            Folder.UserViewManager = UserViewManager;
            UserView.TVSeriesManager = TVSeriesManager;
            UserView.PlaylistManager = PlaylistManager;
            BaseItem.CollectionManager = CollectionManager;
            BaseItem.MediaSourceManager = MediaSourceManager;
            CollectionFolder.XmlSerializer = XmlSerializer;
            BaseStreamingService.AppHost = this;
            BaseStreamingService.HttpClient = HttpClient;
        }

        /// <summary>
        /// Finds the parts.
        /// </summary>
        protected override void FindParts()
        {
            if (!ServerConfigurationManager.Configuration.IsPortAuthorized)
            {
                RegisterServerWithAdministratorAccess();
                ServerConfigurationManager.Configuration.IsPortAuthorized = true;
                ConfigurationManager.SaveConfiguration();
            }

            base.FindParts();

            HttpServer.Init(GetExports<IRestfulService>(false));

            ServerManager.AddWebSocketListeners(GetExports<IWebSocketListener>(false));

            StartServer();

            LibraryManager.AddParts(GetExports<IResolverIgnoreRule>(),
                                    GetExports<IVirtualFolderCreator>(),
                                    GetExports<IItemResolver>(),
                                    GetExports<IIntroProvider>(),
                                    GetExports<IBaseItemComparer>(),
                                    GetExports<ILibraryPostScanTask>());

            ProviderManager.AddParts(GetExports<IImageProvider>(),
                                     GetExports<IMetadataService>(),
                                     GetExports<IMetadataProvider>(),
                                     GetExports<IMetadataSaver>(),
                                     GetExports<IExternalId>());

            ImageProcessor.AddParts(GetExports<IImageEnhancer>());

            LiveTvManager.AddParts(GetExports<ILiveTvService>(), GetExports<ITunerHost>(), GetExports<IListingsProvider>());

            SubtitleManager.AddParts(GetExports<ISubtitleProvider>());

            SessionManager.AddParts(GetExports<ISessionControllerFactory>());

            ChannelManager.AddParts(GetExports<IChannel>());

            MediaSourceManager.AddParts(GetExports<IMediaSourceProvider>());

            NotificationManager.AddParts(GetExports<INotificationService>(), GetExports<INotificationTypeFactory>());
            SyncManager.AddParts(GetExports<ISyncProvider>());
        }

        private string CertificatePath { get; set; }

        private IEnumerable<string> GetUrlPrefixes()
        {
            var hosts = new List<string>();

            hosts.Add("+");

            return hosts.SelectMany(i =>
            {
                var prefixes = new List<string>
                {
                    "http://"+i+":" + HttpPort + "/"
                };

                if (!string.IsNullOrWhiteSpace(CertificatePath))
                {
                    prefixes.Add("https://" + i + ":" + HttpsPort + "/");
                }

                return prefixes;
            });
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        private void StartServer()
        {
            CertificatePath = GetCertificatePath(true);

            try
            {
                ServerManager.Start(GetUrlPrefixes(), CertificatePath);
                return;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error starting http server", ex);

                if (HttpPort == 8096)
                {
                    throw;
                }
            }

            HttpPort = 8096;

            try
            {
                ServerManager.Start(GetUrlPrefixes(), CertificatePath);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error starting http server", ex);

                throw;
            }
        }

        private string GetCertificatePath(bool generateCertificate)
        {
            if (!string.IsNullOrWhiteSpace(ServerConfigurationManager.Configuration.CertificatePath))
            {
                // Custom cert
                return ServerConfigurationManager.Configuration.CertificatePath;
            }

            // Generate self-signed cert
            var certHost = GetHostnameFromExternalDns(ServerConfigurationManager.Configuration.WanDdns);
            var certPath = Path.Combine(ServerConfigurationManager.ApplicationPaths.ProgramDataPath, "ssl", "cert_" + certHost.GetMD5().ToString("N") + ".pfx");

            if (generateCertificate)
            {
                if (!FileSystemManager.FileExists(certPath))
                {
                    FileSystemManager.CreateDirectory(Path.GetDirectoryName(certPath));

                    try
                    {
                        NetworkManager.GenerateSelfSignedSslCertificate(certPath, certHost);
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorException("Error creating ssl cert", ex);
                        return null;
                    }
                }
            }

            return certPath;
        }

        /// <summary>
        /// Called when [configuration updated].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected override void OnConfigurationUpdated(object sender, EventArgs e)
        {
            base.OnConfigurationUpdated(sender, e);

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

            if (!HttpServer.UrlPrefixes.SequenceEqual(GetUrlPrefixes(), StringComparer.OrdinalIgnoreCase))
            {
                requiresRestart = true;
            }

            if (!string.Equals(CertificatePath, GetCertificatePath(false), StringComparison.OrdinalIgnoreCase))
            {
                requiresRestart = true;
            }

            if (requiresRestart)
            {
                NotifyPendingRestart();
            }
        }

        /// <summary>
        /// Restarts this instance.
        /// </summary>
        public override async Task Restart()
        {
            if (!CanSelfRestart)
            {
                throw new PlatformNotSupportedException("The server is unable to self-restart. Please restart manually.");
            }

            try
            {
                await SessionManager.SendServerRestartNotification(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error sending server restart notification", ex);
            }

            Logger.Info("Calling NativeApp.Restart");

            NativeApp.Restart(_startupOptions);
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
#pragma warning disable 162
                return NativeApp.CanSelfUpdate;
#pragma warning restore 162
            }
        }

        /// <summary>
        /// Gets the composable part assemblies.
        /// </summary>
        /// <returns>IEnumerable{Assembly}.</returns>
        protected override IEnumerable<Assembly> GetComposablePartAssemblies()
        {
            var list = GetPluginAssemblies()
                .ToList();

            // Gets all plugin assemblies by first reading all bytes of the .dll and calling Assembly.Load against that
            // This will prevent the .dll file from getting locked, and allow us to replace it when needed

            // Include composable parts in the Api assembly 
            list.Add(typeof(ApiEntryPoint).Assembly);

            // Include composable parts in the Dashboard assembly 
            list.Add(typeof(DashboardService).Assembly);

            // Include composable parts in the Model assembly 
            list.Add(typeof(SystemInfo).Assembly);

            // Include composable parts in the Common assembly 
            list.Add(typeof(IApplicationHost).Assembly);

            // Include composable parts in the Controller assembly 
            list.Add(typeof(IServerApplicationHost).Assembly);

            // Include composable parts in the Providers assembly 
            list.Add(typeof(ProviderUtils).Assembly);

            // Common implementations
            list.Add(typeof(TaskManager).Assembly);

            // Server implementations
            list.Add(typeof(ServerApplicationPaths).Assembly);

            // MediaEncoding
            list.Add(typeof(MediaEncoder).Assembly);

            // Dlna 
            list.Add(typeof(DlnaEntryPoint).Assembly);

            // Local metadata 
            list.Add(typeof(BoxSetXmlSaver).Assembly);

            // Xbmc 
            list.Add(typeof(ArtistNfoProvider).Assembly);

            list.AddRange(NativeApp.GetAssembliesWithParts());

            // Include composable parts in the running assembly
            list.Add(GetType().Assembly);

            return list;
        }

        /// <summary>
        /// Gets the plugin assemblies.
        /// </summary>
        /// <returns>IEnumerable{Assembly}.</returns>
        private IEnumerable<Assembly> GetPluginAssemblies()
        {
            try
            {
                return Directory.EnumerateFiles(ApplicationPaths.PluginsPath, "*.dll", SearchOption.TopDirectoryOnly)
                    .Select(LoadAssembly)
                    .Where(a => a != null)
                    .ToList();
            }
            catch (DirectoryNotFoundException)
            {
                return new List<Assembly>();
            }
        }

        /// <summary>
        /// Gets the system status.
        /// </summary>
        /// <returns>SystemInfo.</returns>
        public async Task<SystemInfo> GetSystemInfo()
        {
            var localAddress = await GetLocalApiUrl().ConfigureAwait(false);

            return new SystemInfo
            {
                HasPendingRestart = HasPendingRestart,
                Version = ApplicationVersion.ToString(),
                IsNetworkDeployed = CanSelfUpdate,
                WebSocketPortNumber = HttpPort,
                FailedPluginAssemblies = FailedAssemblies.ToList(),
                InProgressInstallations = InstallationManager.CurrentInstallations.Select(i => i.Item1).ToList(),
                CompletedInstallations = InstallationManager.CompletedInstallations.ToList(),
                Id = SystemId,
                ProgramDataPath = ApplicationPaths.ProgramDataPath,
                LogPath = ApplicationPaths.LogDirectoryPath,
                ItemsByNamePath = ApplicationPaths.ItemsByNamePath,
                InternalMetadataPath = ApplicationPaths.InternalMetadataPath,
                CachePath = ApplicationPaths.CachePath,
                MacAddress = GetMacAddress(),
                HttpServerPortNumber = HttpPort,
                SupportsHttps = SupportsHttps,
                HttpsPortNumber = HttpsPort,
                OperatingSystem = NativeApp.Environment.OperatingSystem.ToString(),
                OperatingSystemDisplayName = OperatingSystemDisplayName,
                CanSelfRestart = CanSelfRestart,
                CanSelfUpdate = CanSelfUpdate,
                WanAddress = ConnectManager.WanApiAddress,
                HasUpdateAvailable = HasUpdateAvailable,
                SupportsAutoRunAtStartup = SupportsAutoRunAtStartup,
                TranscodingTempPath = ApplicationPaths.TranscodingTempPath,
                IsRunningAsService = IsRunningAsService,
                SupportsRunningAsService = SupportsRunningAsService,
                ServerName = FriendlyName,
                LocalAddress = localAddress,
                SupportsLibraryMonitor = SupportsLibraryMonitor,
                EncoderLocationType = MediaEncoder.EncoderLocationType,
                SystemArchitecture = NativeApp.Environment.SystemArchitecture,
                SystemUpdateLevel = ConfigurationManager.CommonConfiguration.SystemUpdateLevel,
                PackageName = _startupOptions.GetOption("-package")
            };
        }

        public bool EnableHttps
        {
            get
            {
                return SupportsHttps && ServerConfigurationManager.Configuration.EnableHttps;
            }
        }

        public bool SupportsHttps
        {
            get { return !string.IsNullOrWhiteSpace(HttpServer.CertificatePath); }
        }

        public async Task<string> GetLocalApiUrl()
        {
            try
            {
                // Return the first matched address, if found, or the first known local address
                var address = (await GetLocalIpAddresses().ConfigureAwait(false)).FirstOrDefault(i => !IPAddress.IsLoopback(i));

                if (address != null)
                {
                    return GetLocalApiUrl(address);
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error getting local Ip address information", ex);
            }

            return null;
        }

        public string GetLocalApiUrl(IPAddress ipAddress)
        {
            if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return GetLocalApiUrl("[" + ipAddress + "]");
            }

            return GetLocalApiUrl(ipAddress.ToString());
        }

        public string GetLocalApiUrl(string host)
        {
            return string.Format("http://{0}:{1}",
                host,
                HttpPort.ToString(CultureInfo.InvariantCulture));
        }

        public async Task<List<IPAddress>> GetLocalIpAddresses()
        {
            var addresses = NetworkManager.GetLocalIpAddresses().ToList();
            var list = new List<IPAddress>();

            foreach (var address in addresses)
            {
                var valid = await IsIpAddressValidAsync(address).ConfigureAwait(false);
                if (valid)
                {
                    list.Add(address);
                }
            }

            return list;
        }

        private readonly ConcurrentDictionary<string, bool> _validAddressResults = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private DateTime _lastAddressCacheClear;
        private async Task<bool> IsIpAddressValidAsync(IPAddress address)
        {
            if (IPAddress.IsLoopback(address))
            {
                return true;
            }

            var apiUrl = GetLocalApiUrl(address);
            apiUrl += "/system/ping";

            if ((DateTime.UtcNow - _lastAddressCacheClear).TotalMinutes >= 10)
            {
                _lastAddressCacheClear = DateTime.UtcNow;
                _validAddressResults.Clear();
            }

            bool cachedResult;
            if (_validAddressResults.TryGetValue(apiUrl, out cachedResult))
            {
                return cachedResult;
            }

            try
            {
                using (var response = await HttpClient.SendAsync(new HttpRequestOptions
                {
                    Url = apiUrl,
                    LogErrorResponseBody = false,
                    LogErrors = false,
                    LogRequest = false,
                    TimeoutMs = 30000,
                    BufferContent = false

                }, "POST").ConfigureAwait(false))
                {
                    using (var reader = new StreamReader(response.Content))
                    {
                        var result = reader.ReadToEnd();
                        var valid = string.Equals(Name, result, StringComparison.OrdinalIgnoreCase);

                        _validAddressResults.AddOrUpdate(apiUrl, valid, (k, v) => valid);
                        //Logger.Debug("Ping test result to {0}. Success: {1}", apiUrl, valid);
                        return valid;
                    }
                }
            }
            catch
            {
                //Logger.Debug("Ping test result to {0}. Success: {1}", apiUrl, false);

                _validAddressResults.AddOrUpdate(apiUrl, false, (k, v) => false);
                return false;
            }
        }

        public string FriendlyName
        {
            get
            {
                return string.IsNullOrWhiteSpace(ServerConfigurationManager.Configuration.ServerName)
                    ? Environment.MachineName
                    : ServerConfigurationManager.Configuration.ServerName;
            }
        }

        public int HttpPort { get; private set; }

        public int HttpsPort { get; private set; }

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
                await SessionManager.SendServerShutdownNotification(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error sending server shutdown notification", ex);
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
                NativeApp.AuthorizeServer(
                    UdpServerEntryPoint.PortNumber,
                    ServerConfigurationManager.Configuration.HttpServerPortNumber,
                    ServerConfigurationManager.Configuration.HttpsPortNumber,
                    ConfigurationManager.CommonApplicationPaths.ApplicationPath,
                    ConfigurationManager.CommonApplicationPaths.TempDirectory);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error authorizing server", ex);
            }
        }

        public event EventHandler HasUpdateAvailableChanged;

        private bool _hasUpdateAvailable;
        public bool HasUpdateAvailable
        {
            get { return _hasUpdateAvailable; }
            set
            {
                var fireEvent = value && !_hasUpdateAvailable;

                _hasUpdateAvailable = value;

                if (fireEvent)
                {
                    EventHelper.FireEventIfNotNull(HasUpdateAvailableChanged, this, EventArgs.Empty, Logger);
                }
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
            var cacheLength = TimeSpan.FromHours(3);
            var updateLevel = ConfigurationManager.CommonConfiguration.SystemUpdateLevel;

            if (updateLevel == PackageVersionClass.Beta)
            {
                cacheLength = TimeSpan.FromHours(1);
            }
            else if (updateLevel == PackageVersionClass.Dev)
            {
                cacheLength = TimeSpan.FromMinutes(5);
            }

            var result = await new GithubUpdater(HttpClient, JsonSerializer).CheckForUpdateResult("MediaBrowser", "Emby", ApplicationVersion, updateLevel, _releaseAssetFilename,
                    "MBServer", "Mbserver.zip", cacheLength, cancellationToken).ConfigureAwait(false);

            HasUpdateAvailable = result.IsUpdateAvailable;

            return result;
        }

        /// <summary>
        /// Updates the application.
        /// </summary>
        /// <param name="package">The package that contains the update</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        public override async Task UpdateApplication(PackageVersionInfo package, CancellationToken cancellationToken, IProgress<double> progress)
        {
            await InstallationManager.InstallPackage(package, false, progress, cancellationToken).ConfigureAwait(false);

            HasUpdateAvailable = false;

            OnApplicationUpdated(package);
        }

        /// <summary>
        /// Configures the automatic run at startup.
        /// </summary>
        /// <param name="autorun">if set to <c>true</c> [autorun].</param>
        protected override void ConfigureAutoRunAtStartup(bool autorun)
        {
            if (SupportsAutoRunAtStartup)
            {
                NativeApp.ConfigureAutoRun(autorun);
            }
        }

        /// <summary>
        /// This returns localhost in the case of no external dns, and the hostname if the 
        /// dns is prefixed with a valid Uri prefix.
        /// </summary>
        /// <param name="externalDns">The external dns prefix to get the hostname of.</param>
        /// <returns>The hostname in <paramref name="externalDns"/></returns>
        private static string GetHostnameFromExternalDns(string externalDns)
        {
            if (string.IsNullOrWhiteSpace(externalDns))
            {
                return "localhost";
            }

            try
            {
                return new Uri(externalDns).Host;
            }
            catch
            {
                return externalDns;
            }
        }

        public void LaunchUrl(string url)
        {
            NativeApp.LaunchUrl(url);
        }

        public void EnableLoopback(string appName)
        {
            NativeApp.EnableLoopback(appName);
        }
    }
}
