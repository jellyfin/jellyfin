using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.MediaInfo;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Weather;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Controller
{
    /// <summary>
    /// Class Kernel
    /// </summary>
    public class Kernel 
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
        public ImageManager ImageManager { get; set; }

        /// <summary>
        /// Gets the FFMPEG controller.
        /// </summary>
        /// <value>The FFMPEG controller.</value>
        public FFMpegManager FFMpegManager { get; set; }

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
        public virtual string HttpServerUrlPrefix
        {
            get
            {
                return "http://+:" + _configurationManager.Configuration.HttpServerPortNumber + "/" + WebApplicationName + "/";
            }
        }

        /// <summary>
        /// Gets the list of Localized string files
        /// </summary>
        /// <value>The string files.</value>
        public IEnumerable<LocalizedStringData> StringFiles { get; set; }

        /// <summary>
        /// Gets the list of currently registered weather prvoiders
        /// </summary>
        /// <value>The weather providers.</value>
        public IEnumerable<IWeatherProvider> WeatherProviders { get; set; }

        /// <summary>
        /// Gets the list of currently registered image processors
        /// Image processors are specialized metadata providers that run after the normal ones
        /// </summary>
        /// <value>The image enhancers.</value>
        public IEnumerable<IImageEnhancer> ImageEnhancers { get; set; }

        /// <summary>
        /// Gets the list of available user repositories
        /// </summary>
        /// <value>The user repositories.</value>
        public IEnumerable<IUserRepository> UserRepositories { get; set; }

        /// <summary>
        /// Gets the active user repository
        /// </summary>
        /// <value>The user repository.</value>
        public IUserRepository UserRepository { get; set; }

        /// <summary>
        /// Gets the active user repository
        /// </summary>
        /// <value>The display preferences repository.</value>
        public IDisplayPreferencesRepository DisplayPreferencesRepository { get; set; }

        /// <summary>
        /// Gets the list of available item repositories
        /// </summary>
        /// <value>The item repositories.</value>
        public IEnumerable<IItemRepository> ItemRepositories { get; set; }

        /// <summary>
        /// Gets the active item repository
        /// </summary>
        /// <value>The item repository.</value>
        public IItemRepository ItemRepository { get; set; }

        /// <summary>
        /// Gets the list of available DisplayPreferencesRepositories
        /// </summary>
        /// <value>The display preferences repositories.</value>
        public IEnumerable<IDisplayPreferencesRepository> DisplayPreferencesRepositories { get; set; }

        /// <summary>
        /// Gets the list of available item repositories
        /// </summary>
        /// <value>The user data repositories.</value>
        public IEnumerable<IUserDataRepository> UserDataRepositories { get; set; }

        /// <summary>
        /// Gets the active user data repository
        /// </summary>
        /// <value>The user data repository.</value>
        public IUserDataRepository UserDataRepository { get; set; }

        /// <summary>
        /// Gets the UDP server port number.
        /// </summary>
        /// <value>The UDP server port number.</value>
        public int UdpServerPortNumber
        {
            get { return 7359; }
        }

        private readonly IServerConfigurationManager _configurationManager;

        /// <summary>
        /// Creates a kernel based on a Data path, which is akin to our current programdata path
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        public Kernel(IServerConfigurationManager configurationManager)
        {
            Instance = this;

            _configurationManager = configurationManager;
        }

        /// <summary>
        /// Called when [composable parts loaded].
        /// </summary>
        /// <returns>Task.</returns>
        public Task LoadRepositories(IServerConfigurationManager configurationManager)
        {
            // Get the current item repository
            ItemRepository = GetRepository(ItemRepositories, configurationManager.Configuration.ItemRepository);
            var itemRepoTask = ItemRepository.Initialize();

            // Get the current user repository
            UserRepository = GetRepository(UserRepositories, configurationManager.Configuration.UserRepository);
            var userRepoTask = UserRepository.Initialize();

            // Get the current item repository
            UserDataRepository = GetRepository(UserDataRepositories, configurationManager.Configuration.UserDataRepository);
            var userDataRepoTask = UserDataRepository.Initialize();

            // Get the current display preferences repository
            DisplayPreferencesRepository = GetRepository(DisplayPreferencesRepositories, configurationManager.Configuration.DisplayPreferencesRepository);
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
    }
}
