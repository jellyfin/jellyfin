using MediaBrowser.Common;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.MediaInfo;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Updates;
using MediaBrowser.Controller.Weather;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller
{
    /// <summary>
    /// Class Kernel
    /// </summary>
    public class Kernel : BaseKernel, IDisposable
    {
        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static Kernel Instance { get; private set; }

        /// <summary>
        /// Gets the image manager.
        /// </summary>
        /// <value>The image manager.</value>
        public ImageManager ImageManager { get; private set; }

        /// <summary>
        /// Gets the FFMPEG controller.
        /// </summary>
        /// <value>The FFMPEG controller.</value>
        public FFMpegManager FFMpegManager { get; private set; }

        /// <summary>
        /// Gets or sets the file system manager.
        /// </summary>
        /// <value>The file system manager.</value>
        public FileSystemManager FileSystemManager { get; private set; }

        /// <summary>
        /// Gets the provider manager.
        /// </summary>
        /// <value>The provider manager.</value>
        public ProviderManager ProviderManager { get; private set; }

        /// <summary>
        /// Gets the kernel context.
        /// </summary>
        /// <value>The kernel context.</value>
        public override KernelContext KernelContext
        {
            get { return KernelContext.Server; }
        }

        /// <summary>
        /// Gets the list of Localized string files
        /// </summary>
        /// <value>The string files.</value>
        public IEnumerable<LocalizedStringData> StringFiles { get; private set; }

        /// <summary>
        /// Gets the list of currently registered weather prvoiders
        /// </summary>
        /// <value>The weather providers.</value>
        public IEnumerable<IWeatherProvider> WeatherProviders { get; private set; }

        /// <summary>
        /// Gets the list of currently registered metadata prvoiders
        /// </summary>
        /// <value>The metadata providers enumerable.</value>
        public BaseMetadataProvider[] MetadataProviders { get; private set; }

        /// <summary>
        /// Gets the list of currently registered image processors
        /// Image processors are specialized metadata providers that run after the normal ones
        /// </summary>
        /// <value>The image enhancers.</value>
        public IEnumerable<IImageEnhancer> ImageEnhancers { get; private set; }

        /// <summary>
        /// Gets the list of available user repositories
        /// </summary>
        /// <value>The user repositories.</value>
        private IEnumerable<IUserRepository> UserRepositories { get; set; }

        /// <summary>
        /// Gets the active user repository
        /// </summary>
        /// <value>The user repository.</value>
        public IUserRepository UserRepository { get; private set; }

        /// <summary>
        /// Gets the active user repository
        /// </summary>
        /// <value>The display preferences repository.</value>
        public IDisplayPreferencesRepository DisplayPreferencesRepository { get; private set; }

        /// <summary>
        /// Gets the list of available item repositories
        /// </summary>
        /// <value>The item repositories.</value>
        private IEnumerable<IItemRepository> ItemRepositories { get; set; }

        /// <summary>
        /// Gets the active item repository
        /// </summary>
        /// <value>The item repository.</value>
        public IItemRepository ItemRepository { get; private set; }

        /// <summary>
        /// Gets the list of available DisplayPreferencesRepositories
        /// </summary>
        /// <value>The display preferences repositories.</value>
        private IEnumerable<IDisplayPreferencesRepository> DisplayPreferencesRepositories { get; set; }

        /// <summary>
        /// Gets the list of available item repositories
        /// </summary>
        /// <value>The user data repositories.</value>
        private IEnumerable<IUserDataRepository> UserDataRepositories { get; set; }

        /// <summary>
        /// Gets the active user data repository
        /// </summary>
        /// <value>The user data repository.</value>
        public IUserDataRepository UserDataRepository { get; private set; }

        /// <summary>
        /// Gets the UDP server port number.
        /// </summary>
        /// <value>The UDP server port number.</value>
        public override int UdpServerPortNumber
        {
            get { return 7359; }
        }

        private readonly IXmlSerializer _xmlSerializer;

        private readonly IServerConfigurationManager _configurationManager;
        private readonly ILogManager _logManager;

        /// <summary>
        /// Creates a kernel based on a Data path, which is akin to our current programdata path
        /// </summary>
        /// <param name="appHost">The app host.</param>
        /// <param name="xmlSerializer">The XML serializer.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <exception cref="System.ArgumentNullException">isoManager</exception>
        public Kernel(IApplicationHost appHost, IXmlSerializer xmlSerializer, ILogManager logManager, IServerConfigurationManager configurationManager)
            : base(appHost, logManager, configurationManager)
        {
            Instance = this;

            _configurationManager = configurationManager;
            _xmlSerializer = xmlSerializer;
            _logManager = logManager;
            
            // For now there's no real way to inject these properly
            BaseItem.Logger = logManager.GetLogger("BaseItem");
            User.XmlSerializer = _xmlSerializer;
            Ratings.ConfigurationManager = _configurationManager;
            LocalizedStrings.ApplicationPaths = _configurationManager.ApplicationPaths;
            BaseItem.ConfigurationManager = configurationManager;
        }

        /// <summary>
        /// Composes the parts with ioc container.
        /// </summary>
        protected void FindParts()
        {
            // For now there's no real way to inject these properly
            BaseItem.LibraryManager = ApplicationHost.Resolve<ILibraryManager>();
            User.UserManager = ApplicationHost.Resolve<IUserManager>();

            FFMpegManager = (FFMpegManager)ApplicationHost.CreateInstance(typeof(FFMpegManager));
            ImageManager = (ImageManager)ApplicationHost.CreateInstance(typeof(ImageManager));
            ProviderManager = (ProviderManager)ApplicationHost.CreateInstance(typeof(ProviderManager));
            
            UserDataRepositories = ApplicationHost.GetExports<IUserDataRepository>();
            UserRepositories = ApplicationHost.GetExports<IUserRepository>();
            DisplayPreferencesRepositories = ApplicationHost.GetExports<IDisplayPreferencesRepository>();
            ItemRepositories = ApplicationHost.GetExports<IItemRepository>();
            WeatherProviders = ApplicationHost.GetExports<IWeatherProvider>();
            ImageEnhancers = ApplicationHost.GetExports<IImageEnhancer>().OrderBy(e => e.Priority).ToArray();
            StringFiles = ApplicationHost.GetExports<LocalizedStringData>();
            MetadataProviders = ApplicationHost.GetExports<BaseMetadataProvider>().OrderBy(e => e.Priority).ToArray();
        }

        /// <summary>
        /// Performs initializations that can be reloaded at anytime
        /// </summary>
        /// <returns>Task.</returns>
        protected override async void ReloadInternal()
        {
            base.ReloadInternal();

            FindParts();

            await LoadRepositories().ConfigureAwait(false);

            await ApplicationHost.Resolve<IUserManager>().RefreshUsersMetadata(CancellationToken.None).ConfigureAwait(false);

            foreach (var entryPoint in ApplicationHost.GetExports<IServerEntryPoint>())
            {
                entryPoint.Run();
            }

            ReloadFileSystemManager();
        }

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
            if (dispose)
            {
                DisposeFileSystemManager();
            }
        }

        /// <summary>
        /// Called when [composable parts loaded].
        /// </summary>
        /// <returns>Task.</returns>
        protected Task LoadRepositories()
        {
            // Get the current item repository
            ItemRepository = GetRepository(ItemRepositories, _configurationManager.Configuration.ItemRepository);
            var itemRepoTask = ItemRepository.Initialize();

            // Get the current user repository
            UserRepository = GetRepository(UserRepositories, _configurationManager.Configuration.UserRepository);
            var userRepoTask = UserRepository.Initialize();

            // Get the current item repository
            UserDataRepository = GetRepository(UserDataRepositories, _configurationManager.Configuration.UserDataRepository);
            var userDataRepoTask = UserDataRepository.Initialize();

            // Get the current display preferences repository
            DisplayPreferencesRepository = GetRepository(DisplayPreferencesRepositories, _configurationManager.Configuration.DisplayPreferencesRepository);
            var displayPreferencesRepoTask = DisplayPreferencesRepository.Initialize();

            return Task.WhenAll(itemRepoTask, userRepoTask, userDataRepoTask, displayPreferencesRepoTask);
        }

        /// <summary>
        /// Gets a repository by name from a list, and returns the default if not found
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repositories">The repositories.</param>
        /// <param name="name">The name.</param>
        /// <returns>``0.</returns>
        private T GetRepository<T>(IEnumerable<T> repositories, string name)
            where T : class, IRepository
        {
            var enumerable = repositories as T[] ?? repositories.ToArray();

            return enumerable.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)) ??
                   enumerable.FirstOrDefault();
        }

        /// <summary>
        /// Disposes the file system manager.
        /// </summary>
        private void DisposeFileSystemManager()
        {
            if (FileSystemManager != null)
            {
                FileSystemManager.Dispose();
                FileSystemManager = null;
            }
        }

        /// <summary>
        /// Reloads the file system manager.
        /// </summary>
        private void ReloadFileSystemManager()
        {
            DisposeFileSystemManager();

            FileSystemManager = new FileSystemManager(this, _logManager, ApplicationHost.Resolve<ITaskManager>(), ApplicationHost.Resolve<ILibraryManager>(), _configurationManager);
            FileSystemManager.StartWatchers();
        }

        /// <summary>
        /// Gets the system info.
        /// </summary>
        /// <returns>SystemInfo.</returns>
        public override SystemInfo GetSystemInfo()
        {
            var info = base.GetSystemInfo();

            var installationManager = ApplicationHost.Resolve<IInstallationManager>();

            if (installationManager != null)
            {
                info.InProgressInstallations = installationManager.CurrentInstallations.Select(i => i.Item1).ToArray();
                info.CompletedInstallations = installationManager.CompletedInstallations.ToArray();
            }

            return info;
        }
    }
}
