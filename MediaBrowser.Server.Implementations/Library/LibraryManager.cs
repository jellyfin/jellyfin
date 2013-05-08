using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Progress;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Implementations.ScheduledTasks;
using MoreLinq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SortOrder = MediaBrowser.Model.Entities.SortOrder;

namespace MediaBrowser.Server.Implementations.Library
{
    /// <summary>
    /// Class LibraryManager
    /// </summary>
    public class LibraryManager : ILibraryManager
    {
        /// <summary>
        /// Gets the intro providers.
        /// </summary>
        /// <value>The intro providers.</value>
        private IEnumerable<IIntroProvider> IntroProviders { get; set; }

        /// <summary>
        /// Gets the list of entity resolution ignore rules
        /// </summary>
        /// <value>The entity resolution ignore rules.</value>
        private IEnumerable<IResolverIgnoreRule> EntityResolutionIgnoreRules { get; set; }

        /// <summary>
        /// Gets the list of BasePluginFolders added by plugins
        /// </summary>
        /// <value>The plugin folders.</value>
        private IEnumerable<IVirtualFolderCreator> PluginFolderCreators { get; set; }

        /// <summary>
        /// Gets the list of currently registered entity resolvers
        /// </summary>
        /// <value>The entity resolvers enumerable.</value>
        private IEnumerable<IItemResolver> EntityResolvers { get; set; }

        /// <summary>
        /// Gets or sets the comparers.
        /// </summary>
        /// <value>The comparers.</value>
        private IEnumerable<IBaseItemComparer> Comparers { get; set; }

        /// <summary>
        /// Gets the active item repository
        /// </summary>
        /// <value>The item repository.</value>
        public IItemRepository ItemRepository { get; set; }

        /// <summary>
        /// Occurs when [item added].
        /// </summary>
        public event EventHandler<ItemChangeEventArgs> ItemAdded;

        /// <summary>
        /// Occurs when [item updated].
        /// </summary>
        public event EventHandler<ItemChangeEventArgs> ItemUpdated;

        /// <summary>
        /// Occurs when [item removed].
        /// </summary>
        public event EventHandler<ItemChangeEventArgs> ItemRemoved;

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The _task manager
        /// </summary>
        private readonly ITaskManager _taskManager;

        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;

        private readonly IUserDataRepository _userDataRepository;

        /// <summary>
        /// Gets or sets the configuration manager.
        /// </summary>
        /// <value>The configuration manager.</value>
        private IServerConfigurationManager ConfigurationManager { get; set; }

        /// <summary>
        /// A collection of items that may be referenced from multiple physical places in the library
        /// (typically, multiple user roots).  We store them here and be sure they all reference a
        /// single instance.
        /// </summary>
        private ConcurrentDictionary<Guid, BaseItem> ByReferenceItems { get; set; }

        private ConcurrentDictionary<Guid, BaseItem> _libraryItemsCache;
        private object _libraryItemsCacheSyncLock = new object();
        private bool _libraryItemsCacheInitialized;
        private ConcurrentDictionary<Guid, BaseItem> LibraryItemsCache
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _libraryItemsCache, ref _libraryItemsCacheInitialized, ref _libraryItemsCacheSyncLock, CreateLibraryItemsCache);
                return _libraryItemsCache;
            }
        }

        private readonly ConcurrentDictionary<string, UserRootFolder> _userRootFolders =
            new ConcurrentDictionary<string, UserRootFolder>();

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryManager" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="taskManager">The task manager.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        public LibraryManager(ILogger logger, ITaskManager taskManager, IUserManager userManager, IServerConfigurationManager configurationManager, IUserDataRepository userDataRepository)
        {
            _logger = logger;
            _taskManager = taskManager;
            _userManager = userManager;
            ConfigurationManager = configurationManager;
            _userDataRepository = userDataRepository;
            ByReferenceItems = new ConcurrentDictionary<Guid, BaseItem>();

            ConfigurationManager.ConfigurationUpdated += ConfigurationUpdated;

            RecordConfigurationValues(configurationManager.Configuration);
        }

        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="rules">The rules.</param>
        /// <param name="pluginFolders">The plugin folders.</param>
        /// <param name="resolvers">The resolvers.</param>
        /// <param name="introProviders">The intro providers.</param>
        /// <param name="itemComparers">The item comparers.</param>
        public void AddParts(IEnumerable<IResolverIgnoreRule> rules,
            IEnumerable<IVirtualFolderCreator> pluginFolders,
            IEnumerable<IItemResolver> resolvers,
            IEnumerable<IIntroProvider> introProviders,
            IEnumerable<IBaseItemComparer> itemComparers)
        {
            EntityResolutionIgnoreRules = rules;
            PluginFolderCreators = pluginFolders;
            EntityResolvers = resolvers.OrderBy(i => i.Priority).ToArray();
            IntroProviders = introProviders;
            Comparers = itemComparers;
        }

        /// <summary>
        /// The _root folder
        /// </summary>
        private AggregateFolder _rootFolder;
        /// <summary>
        /// The _root folder sync lock
        /// </summary>
        private object _rootFolderSyncLock = new object();
        /// <summary>
        /// The _root folder initialized
        /// </summary>
        private bool _rootFolderInitialized;
        /// <summary>
        /// Gets the root folder.
        /// </summary>
        /// <value>The root folder.</value>
        public AggregateFolder RootFolder
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _rootFolder, ref _rootFolderInitialized, ref _rootFolderSyncLock, CreateRootFolder);
                return _rootFolder;
            }
            private set
            {
                _rootFolder = value;

                if (value == null)
                {
                    _rootFolderInitialized = false;
                }
            }
        }

        private bool _internetProvidersEnabled;
        private bool _peopleImageFetchingEnabled;
        private string _itemsByNamePath;

        private void RecordConfigurationValues(ServerConfiguration configuration)
        {
            _itemsByNamePath = ConfigurationManager.ApplicationPaths.ItemsByNamePath;
            _internetProvidersEnabled = configuration.EnableInternetProviders;
            _peopleImageFetchingEnabled = configuration.InternetProviderExcludeTypes == null || !configuration.InternetProviderExcludeTypes.Contains(typeof(Person).Name, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Configurations the updated.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        void ConfigurationUpdated(object sender, EventArgs e)
        {
            var config = ConfigurationManager.Configuration;

            // Figure out whether or not we should refresh people after the update is finished
            var refreshPeopleAfterUpdate = !_internetProvidersEnabled && config.EnableInternetProviders;

            // This is true if internet providers has just been turned on, or if People have just been removed from InternetProviderExcludeTypes
            if (!refreshPeopleAfterUpdate)
            {
                var newConfigurationFetchesPeopleImages = config.InternetProviderExcludeTypes == null || !config.InternetProviderExcludeTypes.Contains(typeof(Person).Name, StringComparer.OrdinalIgnoreCase);

                refreshPeopleAfterUpdate = newConfigurationFetchesPeopleImages && !_peopleImageFetchingEnabled;
            }

            var ibnPathChanged = !string.Equals(_itemsByNamePath, ConfigurationManager.ApplicationPaths.ItemsByNamePath);

            if (ibnPathChanged)
            {
                _itemsByName.Clear();
            }

            RecordConfigurationValues(config);

            Task.Run(() =>
            {
                // Any number of configuration settings could change the way the library is refreshed, so do that now
                _taskManager.CancelIfRunningAndQueue<RefreshMediaLibraryTask>();

                if (refreshPeopleAfterUpdate)
                {
                    _taskManager.CancelIfRunningAndQueue<PeopleValidationTask>();
                }
            });
        }

        /// <summary>
        /// Creates the library items cache.
        /// </summary>
        /// <returns>ConcurrentDictionary{GuidBaseItem}.</returns>
        private ConcurrentDictionary<Guid, BaseItem> CreateLibraryItemsCache()
        {
            var items = RootFolder.RecursiveChildren.ToList();

            items.Add(RootFolder);

            // Need to use DistinctBy Id because there could be multiple instances with the same id
            // due to sharing the default library
            var userRootFolders = _userManager.Users.Select(i => i.RootFolder)
                .DistinctBy(i => i.Id)
                .ToList();

            items.AddRange(userRootFolders);

            // Get all user collection folders
            var userFolders =
                _userManager.Users.SelectMany(i => i.RootFolder.Children)
                            .Where(i => !(i is BasePluginFolder))
                            .DistinctBy(i => i.Id)
                            .ToList();

            items.AddRange(userFolders);

            return new ConcurrentDictionary<Guid, BaseItem>(items.ToDictionary(i => i.Id));
        }

        /// <summary>
        /// Updates the item in library cache.
        /// </summary>
        /// <param name="item">The item.</param>
        private void UpdateItemInLibraryCache(BaseItem item)
        {
            LibraryItemsCache.AddOrUpdate(item.Id, item, delegate { return item; });
        }

        /// <summary>
        /// Resolves the item.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>BaseItem.</returns>
        public BaseItem ResolveItem(ItemResolveArgs args)
        {
            var item = EntityResolvers.Select(r => r.ResolvePath(args)).FirstOrDefault(i => i != null);

            if (item != null)
            {
                ResolverHelper.SetInitialItemValues(item, args);

                // Now handle the issue with posibly having the same item referenced from multiple physical
                // places within the library.  Be sure we always end up with just one instance.
                if (item is IByReferenceItem)
                {
                    item = GetOrAddByReferenceItem(item);
                }
            }

            return item;
        }


        /// <summary>
        /// Ensure supplied item has only one instance throughout
        /// </summary>
        /// <param name="item"></param>
        /// <returns>The proper instance to the item</returns>
        public BaseItem GetOrAddByReferenceItem(BaseItem item)
        {
            // Add this item to our list if not there already
            if (!ByReferenceItems.TryAdd(item.Id, item))
            {
                // Already there - return the existing reference
                item = ByReferenceItems[item.Id];
            }
            return item;
        }

        /// <summary>
        /// Resolves a path into a BaseItem
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="parent">The parent.</param>
        /// <param name="fileInfo">The file info.</param>
        /// <returns>BaseItem.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public BaseItem ResolvePath(string path, Folder parent = null, FileSystemInfo fileInfo = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException();
            }

            fileInfo = fileInfo ?? FileSystem.GetFileSystemInfo(path);

            if (!fileInfo.Exists)
            {
                return null;
            }

            var args = new ItemResolveArgs(ConfigurationManager.ApplicationPaths)
            {
                Parent = parent,
                Path = path,
                FileInfo = fileInfo
            };

            // Return null if ignore rules deem that we should do so
            if (EntityResolutionIgnoreRules.Any(r => r.ShouldIgnore(args)))
            {
                return null;
            }

            // Gather child folder and files
            if (args.IsDirectory)
            {
                var isPhysicalRoot = args.IsPhysicalRoot;

                // When resolving the root, we need it's grandchildren (children of user views)
                var flattenFolderDepth = isPhysicalRoot ? 2 : 0;

                args.FileSystemDictionary = FileData.GetFilteredFileSystemEntries(args.Path, _logger, flattenFolderDepth: flattenFolderDepth, args: args, resolveShortcuts: isPhysicalRoot || args.IsVf);
            }

            // Check to see if we should resolve based on our contents
            if (args.IsDirectory && !ShouldResolvePathContents(args))
            {
                return null;
            }

            return ResolveItem(args);
        }

        /// <summary>
        /// Determines whether a path should be ignored based on its contents - called after the contents have been read
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private static bool ShouldResolvePathContents(ItemResolveArgs args)
        {
            // Ignore any folders containing a file called .ignore
            return !args.ContainsFileSystemEntryByName(".ignore");
        }

        /// <summary>
        /// Resolves a set of files into a list of BaseItem
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="files">The files.</param>
        /// <param name="parent">The parent.</param>
        /// <returns>List{``0}.</returns>
        public List<T> ResolvePaths<T>(IEnumerable<FileSystemInfo> files, Folder parent)
            where T : BaseItem
        {
            var list = new List<T>();

            Parallel.ForEach(files, f =>
            {
                try
                {
                    var item = ResolvePath(f.FullName, parent, f) as T;

                    if (item != null)
                    {
                        lock (list)
                        {
                            list.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error resolving path {0}", ex, f.FullName);
                }
            });

            return list;
        }

        /// <summary>
        /// Creates the root media folder
        /// </summary>
        /// <returns>AggregateFolder.</returns>
        /// <exception cref="System.InvalidOperationException">Cannot create the root folder until plugins have loaded</exception>
        public AggregateFolder CreateRootFolder()
        {
            var rootFolderPath = ConfigurationManager.ApplicationPaths.RootFolderPath;
            var rootFolder = RetrieveItem(rootFolderPath.GetMBId(typeof(AggregateFolder))) as AggregateFolder ?? (AggregateFolder)ResolvePath(rootFolderPath);

            // Add in the plug-in folders
            foreach (var child in PluginFolderCreators)
            {
                rootFolder.AddVirtualChild(child.GetFolder());
            }

            return rootFolder;
        }

        /// <summary>
        /// Gets the user root folder.
        /// </summary>
        /// <param name="userRootPath">The user root path.</param>
        /// <returns>UserRootFolder.</returns>
        public UserRootFolder GetUserRootFolder(string userRootPath)
        {
            return _userRootFolders.GetOrAdd(userRootPath, key => RetrieveItem(userRootPath.GetMBId(typeof(UserRootFolder))) as UserRootFolder ?? (UserRootFolder)ResolvePath(userRootPath));
        }

        /// <summary>
        /// Gets a Person
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <returns>Task{Person}.</returns>
        public Task<Person> GetPerson(string name, bool allowSlowProviders = false)
        {
            return GetPerson(name, CancellationToken.None, allowSlowProviders);
        }

        /// <summary>
        /// Gets a Person
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <param name="forceCreation">if set to <c>true</c> [force creation].</param>
        /// <returns>Task{Person}.</returns>
        private Task<Person> GetPerson(string name, CancellationToken cancellationToken, bool allowSlowProviders = false, bool forceCreation = false)
        {
            return GetItemByName<Person>(ConfigurationManager.ApplicationPaths.PeoplePath, name, cancellationToken, allowSlowProviders, forceCreation);
        }

        /// <summary>
        /// Gets a Studio
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <returns>Task{Studio}.</returns>
        public Task<Studio> GetStudio(string name, bool allowSlowProviders = false)
        {
            return GetItemByName<Studio>(ConfigurationManager.ApplicationPaths.StudioPath, name, CancellationToken.None, allowSlowProviders);
        }

        /// <summary>
        /// Gets a Genre
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <returns>Task{Genre}.</returns>
        public Task<Genre> GetGenre(string name, bool allowSlowProviders = false)
        {
            return GetItemByName<Genre>(ConfigurationManager.ApplicationPaths.GenrePath, name, CancellationToken.None, allowSlowProviders);
        }

        /// <summary>
        /// Gets a Genre
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <returns>Task{Genre}.</returns>
        public Task<Artist> GetArtist(string name, bool allowSlowProviders = false)
        {
            return GetArtist(name, CancellationToken.None, allowSlowProviders);
        }

        /// <summary>
        /// Gets the artist.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <param name="forceCreation">if set to <c>true</c> [force creation].</param>
        /// <returns>Task{Artist}.</returns>
        private Task<Artist> GetArtist(string name, CancellationToken cancellationToken, bool allowSlowProviders = false, bool forceCreation = false)
        {
            return GetItemByName<Artist>(ConfigurationManager.ApplicationPaths.ArtistsPath, name, cancellationToken, allowSlowProviders, forceCreation);
        }

        /// <summary>
        /// The us culture
        /// </summary>
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Gets a Year
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <returns>Task{Year}.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public Task<Year> GetYear(int value, bool allowSlowProviders = false)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            return GetItemByName<Year>(ConfigurationManager.ApplicationPaths.YearPath, value.ToString(UsCulture), CancellationToken.None, allowSlowProviders);
        }

        /// <summary>
        /// The images by name item cache
        /// </summary>
        private readonly ConcurrentDictionary<string, BaseItem> _itemsByName = new ConcurrentDictionary<string, BaseItem>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Generically retrieves an IBN item
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path">The path.</param>
        /// <param name="name">The name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <param name="forceCreation">if set to <c>true</c> [force creation].</param>
        /// <returns>Task{``0}.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        private async Task<T> GetItemByName<T>(string path, string name, CancellationToken cancellationToken, bool allowSlowProviders = true, bool forceCreation = false)
            where T : BaseItem, new()
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException();
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException();
            }

            var key = Path.Combine(path, FileSystem.GetValidFilename(name));

            BaseItem obj;

            if (forceCreation || !_itemsByName.TryGetValue(key, out obj))
            {
                obj = await CreateItemByName<T>(path, name, cancellationToken, allowSlowProviders).ConfigureAwait(false);

                _itemsByName.AddOrUpdate(key, obj, (keyName, oldValue) => obj);
            }

            return obj as T;
        }

        /// <summary>
        /// Creates an IBN item based on a given path
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path">The path.</param>
        /// <param name="name">The name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <returns>Task{``0}.</returns>
        /// <exception cref="System.IO.IOException">Path not created:  + path</exception>
        private async Task<T> CreateItemByName<T>(string path, string name, CancellationToken cancellationToken, bool allowSlowProviders = true)
            where T : BaseItem, new()
        {
            cancellationToken.ThrowIfCancellationRequested();

            path = Path.Combine(path, FileSystem.GetValidFilename(name));

            var fileInfo = new DirectoryInfo(path);

            var isNew = false;

            if (!fileInfo.Exists)
            {
                Directory.CreateDirectory(path);
                fileInfo = new DirectoryInfo(path);

                if (!fileInfo.Exists)
                {
                    throw new IOException("Path not created: " + path);
                }

                isNew = true;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var id = path.GetMBId(typeof(T));

            var item = RetrieveItem(id) as T;
            if (item == null)
            {
                item = new T
                {
                    Name = name,
                    Id = id,
                    DateCreated = fileInfo.CreationTimeUtc,
                    DateModified = fileInfo.LastWriteTimeUtc,
                    Path = path
                };
                isNew = true;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Set this now so we don't cause additional file system access during provider executions
            item.ResetResolveArgs(fileInfo);

            await item.RefreshMetadata(cancellationToken, isNew, allowSlowProviders: allowSlowProviders).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            return item;
        }

        /// <summary>
        /// Validate and refresh the People sub-set of the IBN.
        /// The items are stored in the db but not loaded into memory until actually requested by an operation.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public async Task ValidatePeople(CancellationToken cancellationToken, IProgress<double> progress)
        {
            const int maxTasks = 25;

            var tasks = new List<Task>();

            var includedPersonTypes = new[] { PersonType.Actor, PersonType.Director, PersonType.GuestStar, PersonType.Writer, PersonType.Director, PersonType.Producer };

            var people = RootFolder.RecursiveChildren
                .Where(c => c.People != null)
                .SelectMany(c => c.People.Where(p => includedPersonTypes.Contains(p.Type)))
                .DistinctBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var numComplete = 0;

            foreach (var person in people)
            {
                if (tasks.Count > maxTasks)
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                    tasks.Clear();

                    // Safe cancellation point, when there are no pending tasks
                    cancellationToken.ThrowIfCancellationRequested();
                }

                // Avoid accessing the foreach variable within the closure
                var currentPerson = person;

                tasks.Add(Task.Run(async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        await GetPerson(currentPerson.Name, cancellationToken, true, true).ConfigureAwait(false);
                    }
                    catch (IOException ex)
                    {
                        _logger.ErrorException("Error validating IBN entry {0}", ex, currentPerson.Name);
                    }

                    // Update progress
                    lock (progress)
                    {
                        numComplete++;
                        double percent = numComplete;
                        percent /= people.Count;

                        progress.Report(100 * percent);
                    }
                }));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            progress.Report(100);

            _logger.Info("People validation complete");
        }

        public async Task ValidateArtists(CancellationToken cancellationToken, IProgress<double> progress)
        {
            const int maxTasks = 25;

            var tasks = new List<Task>();

            var artists = RootFolder.RecursiveChildren
                .OfType<Audio>()
                .SelectMany(c =>
                {
                    var list = new List<string>();

                    if (!string.IsNullOrEmpty(c.AlbumArtist))
                    {
                        list.Add(c.AlbumArtist);
                    }
                    if (!string.IsNullOrEmpty(c.Artist))
                    {
                        list.Add(c.Artist);
                    }

                    return list;
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var numComplete = 0;

            foreach (var artist in artists)
            {
                if (tasks.Count > maxTasks)
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                    tasks.Clear();

                    // Safe cancellation point, when there are no pending tasks
                    cancellationToken.ThrowIfCancellationRequested();
                }

                // Avoid accessing the foreach variable within the closure
                var currentArtist = artist;

                tasks.Add(Task.Run(async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        await GetArtist(currentArtist, cancellationToken, true, true).ConfigureAwait(false);
                    }
                    catch (IOException ex)
                    {
                        _logger.ErrorException("Error validating Artist {0}", ex, currentArtist);
                    }

                    // Update progress
                    lock (progress)
                    {
                        numComplete++;
                        double percent = numComplete;
                        percent /= artists.Count;

                        progress.Report(100 * percent);
                    }
                }));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            progress.Report(100);

            _logger.Info("Artist validation complete");
        }

        /// <summary>
        /// Reloads the root media folder
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task ValidateMediaLibrary(IProgress<double> progress, CancellationToken cancellationToken)
        {
            // Just run the scheduled task so that the user can see it
            return Task.Run(() => _taskManager.CancelIfRunningAndQueue<RefreshMediaLibraryTask>());
        }

        /// <summary>
        /// Validates the media library internal.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task ValidateMediaLibraryInternal(IProgress<double> progress, CancellationToken cancellationToken)
        {
            _logger.Info("Validating media library");

            await RootFolder.RefreshMetadata(cancellationToken).ConfigureAwait(false);

            // Start by just validating the children of the root, but go no further
            await RootFolder.ValidateChildren(new Progress<double>(), cancellationToken, recursive: false);

            foreach (var folder in _userManager.Users.Select(u => u.RootFolder).Distinct())
            {
                await ValidateCollectionFolders(folder, cancellationToken).ConfigureAwait(false);
            }

            var innerProgress = new ActionableProgress<double>();

            innerProgress.RegisterAction(pct => progress.Report(pct * .8));

            // Now validate the entire media library
            await RootFolder.ValidateChildren(innerProgress, cancellationToken, recursive: true).ConfigureAwait(false);

            innerProgress = new ActionableProgress<double>();

            innerProgress.RegisterAction(pct => progress.Report(80 + pct * .2));

            await ValidateArtists(cancellationToken, innerProgress);

            progress.Report(100);
        }

        /// <summary>
        /// Validates only the collection folders for a User and goes no further
        /// </summary>
        /// <param name="userRootFolder">The user root folder.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task ValidateCollectionFolders(UserRootFolder userRootFolder, CancellationToken cancellationToken)
        {
            _logger.Info("Validating collection folders within {0}", userRootFolder.Path);
            await userRootFolder.RefreshMetadata(cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            await userRootFolder.ValidateChildren(new Progress<double>(), cancellationToken, recursive: false).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the default view.
        /// </summary>
        /// <returns>IEnumerable{VirtualFolderInfo}.</returns>
        public IEnumerable<VirtualFolderInfo> GetDefaultVirtualFolders()
        {
            return GetView(ConfigurationManager.ApplicationPaths.DefaultUserViewsPath);
        }

        /// <summary>
        /// Gets the view.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{VirtualFolderInfo}.</returns>
        public IEnumerable<VirtualFolderInfo> GetVirtualFolders(User user)
        {
            return GetView(user.RootFolderPath);
        }

        /// <summary>
        /// Gets the view.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>IEnumerable{VirtualFolderInfo}.</returns>
        private IEnumerable<VirtualFolderInfo> GetView(string path)
        {
            return Directory.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly)
                .Select(dir => new VirtualFolderInfo
                {
                    Name = Path.GetFileName(dir),
                    Locations = Directory.EnumerateFiles(dir, "*.lnk", SearchOption.TopDirectoryOnly).Select(FileSystem.ResolveShortcut).OrderBy(i => i).ToList()
                });
        }

        /// <summary>
        /// Gets the item by id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>BaseItem.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        public BaseItem GetItemById(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            BaseItem item;

            if (LibraryItemsCache.TryGetValue(id, out item))
            {
                return item;
            }

            return ItemRepository.GetItem(id);
        }

        /// <summary>
        /// Gets the intros.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        public IEnumerable<string> GetIntros(BaseItem item, User user)
        {
            return IntroProviders.SelectMany(i => i.GetIntros(item, user));
        }

        /// <summary>
        /// Sorts the specified sort by.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="user">The user.</param>
        /// <param name="sortBy">The sort by.</param>
        /// <param name="sortOrder">The sort order.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        public IEnumerable<BaseItem> Sort(IEnumerable<BaseItem> items, User user, IEnumerable<string> sortBy, SortOrder sortOrder)
        {
            var isFirst = true;

            IOrderedEnumerable<BaseItem> orderedItems = null;

            foreach (var orderBy in sortBy.Select(o => GetComparer(o, user)).Where(c => c != null))
            {
                if (isFirst)
                {
                    orderedItems = sortOrder == SortOrder.Descending ? items.OrderByDescending(i => i, orderBy) : items.OrderBy(i => i, orderBy);
                }
                else
                {
                    orderedItems = sortOrder == SortOrder.Descending ? orderedItems.ThenByDescending(i => i, orderBy) : orderedItems.ThenBy(i => i, orderBy);
                }

                isFirst = false;
            }

            return orderedItems ?? items;
        }

        /// <summary>
        /// Gets the comparer.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="user">The user.</param>
        /// <returns>IBaseItemComparer.</returns>
        private IBaseItemComparer GetComparer(string name, User user)
        {
            var comparer = Comparers.FirstOrDefault(c => string.Equals(name, c.Name, StringComparison.OrdinalIgnoreCase));

            if (comparer != null)
            {
                // If it requires a user, create a new one, and assign the user
                if (comparer is IUserBaseItemComparer)
                {
                    var userComparer = (IUserBaseItemComparer)Activator.CreateInstance(comparer.GetType());

                    userComparer.User = user;
                    userComparer.UserManager = _userManager;
                    userComparer.UserDataRepository = _userDataRepository;

                    return userComparer;
                }
            }

            return comparer;
        }

        /// <summary>
        /// Creates the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task CreateItem(BaseItem item, CancellationToken cancellationToken)
        {
            await SaveItem(item, cancellationToken).ConfigureAwait(false);

            UpdateItemInLibraryCache(item);
            
            if (ItemAdded != null)
            {
                try
                {
                    ItemAdded(this, new ItemChangeEventArgs { Item = item });
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in ItemUpdated event handler", ex);
                }
            }
        }

        /// <summary>
        /// Updates the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task UpdateItem(BaseItem item, CancellationToken cancellationToken)
        {
            await SaveItem(item, cancellationToken).ConfigureAwait(false);

            UpdateItemInLibraryCache(item);

            if (ItemUpdated != null)
            {
                try
                {
                    ItemUpdated(this, new ItemChangeEventArgs { Item = item });
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in ItemUpdated event handler", ex);
                }
            }
        }

        /// <summary>
        /// Reports the item removed.
        /// </summary>
        /// <param name="item">The item.</param>
        public void ReportItemRemoved(BaseItem item)
        {
            if (ItemRemoved != null)
            {
                try
                {
                    ItemRemoved(this, new ItemChangeEventArgs { Item = item });
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in ItemRemoved event handler", ex);
                }
            }
        }

        /// <summary>
        /// Saves the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private Task SaveItem(BaseItem item, CancellationToken cancellationToken)
        {
            return ItemRepository.SaveItem(item, cancellationToken);
        }

        /// <summary>
        /// Retrieves the item.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>Task{BaseItem}.</returns>
        public BaseItem RetrieveItem(Guid id)
        {
            return ItemRepository.GetItem(id);
        }

        /// <summary>
        /// Saves the children.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="children">The children.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SaveChildren(Guid id, IEnumerable<BaseItem> children, CancellationToken cancellationToken)
        {
            return ItemRepository.SaveChildren(id, children, cancellationToken);
        }

        /// <summary>
        /// Retrieves the children.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        public IEnumerable<BaseItem> RetrieveChildren(Folder parent)
        {
            var children = ItemRepository.RetrieveChildren(parent).ToList();

            foreach (var child in children)
            {
                child.Parent = parent;
            }

            return children;
        }
    }
}
