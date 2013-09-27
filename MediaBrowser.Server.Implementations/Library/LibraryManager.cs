using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Progress;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Implementations.Library.Validators;
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
        /// Gets or sets the postscan tasks.
        /// </summary>
        /// <value>The postscan tasks.</value>
        private IEnumerable<ILibraryPostScanTask> PostscanTasks { get; set; }
        /// <summary>
        /// Gets or sets the prescan tasks.
        /// </summary>
        /// <value>The prescan tasks.</value>
        private IEnumerable<ILibraryPrescanTask> PrescanTasks { get; set; }

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

        /// <summary>
        /// The _user data repository
        /// </summary>
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
        /// <value>The by reference items.</value>
        private ConcurrentDictionary<Guid, BaseItem> ByReferenceItems { get; set; }

        private IEnumerable<IMetadataSaver> _savers;

        private readonly Func<IDirectoryWatchers> _directoryWatchersFactory;

        /// <summary>
        /// The _library items cache
        /// </summary>
        private ConcurrentDictionary<Guid, BaseItem> _libraryItemsCache;
        /// <summary>
        /// The _library items cache sync lock
        /// </summary>
        private object _libraryItemsCacheSyncLock = new object();
        /// <summary>
        /// The _library items cache initialized
        /// </summary>
        private bool _libraryItemsCacheInitialized;
        /// <summary>
        /// Gets the library items cache.
        /// </summary>
        /// <value>The library items cache.</value>
        private ConcurrentDictionary<Guid, BaseItem> LibraryItemsCache
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _libraryItemsCache, ref _libraryItemsCacheInitialized, ref _libraryItemsCacheSyncLock, CreateLibraryItemsCache);
                return _libraryItemsCache;
            }
        }

        /// <summary>
        /// The _user root folders
        /// </summary>
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
        public LibraryManager(ILogger logger, ITaskManager taskManager, IUserManager userManager, IServerConfigurationManager configurationManager, IUserDataRepository userDataRepository, Func<IDirectoryWatchers> directoryWatchersFactory)
        {
            _logger = logger;
            _taskManager = taskManager;
            _userManager = userManager;
            ConfigurationManager = configurationManager;
            _userDataRepository = userDataRepository;
            _directoryWatchersFactory = directoryWatchersFactory;
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
        /// <param name="prescanTasks">The prescan tasks.</param>
        /// <param name="postscanTasks">The postscan tasks.</param>
        /// <param name="savers">The savers.</param>
        public void AddParts(IEnumerable<IResolverIgnoreRule> rules,
            IEnumerable<IVirtualFolderCreator> pluginFolders,
            IEnumerable<IItemResolver> resolvers,
            IEnumerable<IIntroProvider> introProviders,
            IEnumerable<IBaseItemComparer> itemComparers,
            IEnumerable<ILibraryPrescanTask> prescanTasks,
            IEnumerable<ILibraryPostScanTask> postscanTasks,
            IEnumerable<IMetadataSaver> savers)
        {
            EntityResolutionIgnoreRules = rules;
            PluginFolderCreators = pluginFolders;
            EntityResolvers = resolvers.OrderBy(i => i.Priority).ToArray();
            IntroProviders = introProviders;
            Comparers = itemComparers;
            PrescanTasks = prescanTasks;
            PostscanTasks = postscanTasks;
            _savers = savers;
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

        /// <summary>
        /// The _items by name path
        /// </summary>
        private string _itemsByNamePath;
        /// <summary>
        /// The _season zero display name
        /// </summary>
        private string _seasonZeroDisplayName;

        /// <summary>
        /// Records the configuration values.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        private void RecordConfigurationValues(ServerConfiguration configuration)
        {
            _seasonZeroDisplayName = ConfigurationManager.Configuration.SeasonZeroDisplayName;
            _itemsByNamePath = ConfigurationManager.ApplicationPaths.ItemsByNamePath;
        }

        /// <summary>
        /// Configurations the updated.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void ConfigurationUpdated(object sender, EventArgs e)
        {
            var config = ConfigurationManager.Configuration;

            var ibnPathChanged = !string.Equals(_itemsByNamePath, ConfigurationManager.ApplicationPaths.ItemsByNamePath, StringComparison.CurrentCulture);

            if (ibnPathChanged)
            {
                _itemsByName.Clear();
            }

            var newSeasonZeroName = ConfigurationManager.Configuration.SeasonZeroDisplayName;
            var seasonZeroNameChanged = !string.Equals(_seasonZeroDisplayName, newSeasonZeroName, StringComparison.CurrentCulture);

            RecordConfigurationValues(config);

            Task.Run(async () =>
            {
                if (seasonZeroNameChanged)
                {
                    await UpdateSeasonZeroNames(newSeasonZeroName, CancellationToken.None).ConfigureAwait(false);
                }

                // Any number of configuration settings could change the way the library is refreshed, so do that now
                _taskManager.CancelIfRunningAndQueue<RefreshMediaLibraryTask>();
            });
        }

        /// <summary>
        /// Updates the season zero names.
        /// </summary>
        /// <param name="newName">The new name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task UpdateSeasonZeroNames(string newName, CancellationToken cancellationToken)
        {
            var seasons = RootFolder.RecursiveChildren
                .OfType<Season>()
                .Where(i => i.IndexNumber.HasValue && i.IndexNumber.Value == 0 && !string.Equals(i.Name, newName, StringComparison.CurrentCulture))
                .ToList();

            foreach (var season in seasons)
            {
                season.Name = newName;

                try
                {
                    await UpdateItem(season, ItemUpdateType.MetadataDownload, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error saving {0}", ex, season.Path);
                }
            }
        }

        /// <summary>
        /// Creates the library items cache.
        /// </summary>
        /// <returns>ConcurrentDictionary{GuidBaseItem}.</returns>
        private ConcurrentDictionary<Guid, BaseItem> CreateLibraryItemsCache()
        {
            var items = RootFolder.GetRecursiveChildren();

            items.Add(RootFolder);

            // Need to use Distinct because there could be multiple instances with the same id
            // due to sharing the default library
            var userRootFolders = _userManager.Users.Select(i => i.RootFolder)
                .Distinct()
                .ToList();

            foreach (var folder in userRootFolders)
            {
                items.Add(folder);
            }

            // Get all user collection folders
            // Skip BasePluginFolders because we already got them from RootFolder.RecursiveChildren
            var userFolders = userRootFolders.SelectMany(i => i.Children)
                            .Where(i => !(i is BasePluginFolder))
                            .ToList();

            foreach (var folder in userFolders)
            {
                items.Add(folder);
            }

            var dictionary = new ConcurrentDictionary<Guid, BaseItem>();

            foreach (var item in items)
            {
                dictionary[item.Id] = item;
            }

            return dictionary;
        }

        /// <summary>
        /// Updates the item in library cache.
        /// </summary>
        /// <param name="item">The item.</param>
        private void UpdateItemInLibraryCache(BaseItem item)
        {
            if (!(item is IItemByName))
            {
                LibraryItemsCache.AddOrUpdate(item.Id, item, delegate { return item; });
            }
        }

        /// <summary>
        /// Resolves the item.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>BaseItem.</returns>
        public BaseItem ResolveItem(ItemResolveArgs args)
        {
            var item = EntityResolvers.Select(r =>
            {
                try
                {
                    return r.ResolvePath(args);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in {0} resolving {1}", ex, r.GetType().Name, args.Path);

                    return null;
                }

            }).FirstOrDefault(i => i != null);

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
        /// <param name="item">The item.</param>
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
        /// <param name="fileInfo">The file info.</param>
        /// <param name="parent">The parent.</param>
        /// <returns>BaseItem.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public BaseItem ResolvePath(FileSystemInfo fileInfo, Folder parent = null)
        {
            if (fileInfo == null)
            {
                throw new ArgumentNullException("fileInfo");
            }

            var args = new ItemResolveArgs(ConfigurationManager.ApplicationPaths, this)
            {
                Parent = parent,
                Path = fileInfo.FullName,
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

                args.FileSystemDictionary = FileData.GetFilteredFileSystemEntries(args.Path, _logger, args, flattenFolderDepth: flattenFolderDepth, resolveShortcuts: isPhysicalRoot || args.IsVf);

                // Need to remove subpaths that may have been resolved from shortcuts
                // Example: if \\server\movies exists, then strip out \\server\movies\action
                if (isPhysicalRoot)
                {
                    var paths = args.FileSystemDictionary.Keys.ToList();

                    foreach (var subPath in paths
                        .Where(subPath => !subPath.EndsWith(":\\", StringComparison.OrdinalIgnoreCase) && paths.Any(i => subPath.StartsWith(i.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))))
                    {
                        _logger.Info("Ignoring duplicate path: {0}", subPath);
                        args.FileSystemDictionary.Remove(subPath);
                    }
                }
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
                    if (f.Exists)
                    {
                        var item = ResolvePath(f, parent) as T;

                        if (item != null)
                        {
                            lock (list)
                            {
                                list.Add(item);
                            }
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

            if (!Directory.Exists(rootFolderPath))
            {
                Directory.CreateDirectory(rootFolderPath);
            }

            var rootFolder = RetrieveItem(rootFolderPath.GetMBId(typeof(AggregateFolder))) as AggregateFolder ?? (AggregateFolder)ResolvePath(new DirectoryInfo(rootFolderPath));

            // Add in the plug-in folders
            foreach (var child in PluginFolderCreators)
            {
                var folder = child.GetFolder();

                if (folder.Id == Guid.Empty)
                {
                    folder.Id = (folder.Path ?? folder.GetType().Name).GetMBId(folder.GetType());
                }

                rootFolder.AddVirtualChild(folder);
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
            return _userRootFolders.GetOrAdd(userRootPath, key => RetrieveItem(userRootPath.GetMBId(typeof(UserRootFolder))) as UserRootFolder ??
                (UserRootFolder)ResolvePath(new DirectoryInfo(userRootPath)));
        }

        public Person GetPersonSync(string name)
        {
            return GetItemByName<Person>(ConfigurationManager.ApplicationPaths.PeoplePath, name);
        }

        /// <summary>
        /// Gets a Person
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Person}.</returns>
        public Person GetPerson(string name)
        {
            return GetItemByName<Person>(ConfigurationManager.ApplicationPaths.PeoplePath, name);
        }

        /// <summary>
        /// Gets a Studio
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Studio}.</returns>
        public Studio GetStudio(string name)
        {
            return GetItemByName<Studio>(ConfigurationManager.ApplicationPaths.StudioPath, name);
        }

        /// <summary>
        /// Gets a Genre
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Genre}.</returns>
        public Genre GetGenre(string name)
        {
            return GetItemByName<Genre>(ConfigurationManager.ApplicationPaths.GenrePath, name);
        }

        /// <summary>
        /// Gets the genre.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{MusicGenre}.</returns>
        public MusicGenre GetMusicGenre(string name)
        {
            return GetItemByName<MusicGenre>(ConfigurationManager.ApplicationPaths.MusicGenrePath, name);
        }

        /// <summary>
        /// Gets the game genre.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{GameGenre}.</returns>
        public GameGenre GetGameGenre(string name)
        {
            return GetItemByName<GameGenre>(ConfigurationManager.ApplicationPaths.GameGenrePath, name);
        }

        /// <summary>
        /// Gets a Genre
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Genre}.</returns>
        public Artist GetArtist(string name)
        {
            return GetItemByName<Artist>(ConfigurationManager.ApplicationPaths.ArtistsPath, name);
        }

        /// <summary>
        /// The us culture
        /// </summary>
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Gets a Year
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Task{Year}.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public Year GetYear(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            return GetItemByName<Year>(ConfigurationManager.ApplicationPaths.YearPath, value.ToString(UsCulture));
        }

        /// <summary>
        /// The images by name item cache
        /// </summary>
        private readonly ConcurrentDictionary<string, BaseItem> _itemsByName = new ConcurrentDictionary<string, BaseItem>(StringComparer.OrdinalIgnoreCase);

        private T GetItemByName<T>(string path, string name)
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

            var validFilename = FileSystem.GetValidFilename(name);

            var key = Path.Combine(path, validFilename);

            BaseItem obj;

            if (!_itemsByName.TryGetValue(key, out obj))
            {
                var tuple = CreateItemByName<T>(key, name);

                obj = tuple.Item2;

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
        /// <returns>Task{``0}.</returns>
        /// <exception cref="System.IO.IOException">Path not created:  + path</exception>
        private Tuple<bool, T> CreateItemByName<T>(string path, string name)
            where T : BaseItem, new()
        {
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

            var type = typeof(T);

            var id = path.GetMBId(type);

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

            // Set this now so we don't cause additional file system access during provider executions
            item.ResetResolveArgs(fileInfo);

            return new Tuple<bool, T>(isNew, item);
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
            var people = RootFolder.GetRecursiveChildren()
                .SelectMany(c => c.People)
                .DistinctBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var numComplete = 0;

            foreach (var person in people)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var item = GetPerson(person.Name);

                    await item.RefreshMetadata(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error validating IBN entry {0}", ex, person.Name);
                }

                // Update progress
                numComplete++;
                double percent = numComplete;
                percent /= people.Count;

                progress.Report(100 * percent);
            }

            progress.Report(100);

            _logger.Info("People validation complete");

            // Bad practice, i know. But we keep a lot in memory, unfortunately.
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.Collect(2, GCCollectionMode.Forced, true);
        }

        /// <summary>
        /// Validates the artists.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public Task ValidateArtists(CancellationToken cancellationToken, IProgress<double> progress)
        {
            return new ArtistsValidator(this, _userManager, _logger).Run(progress, cancellationToken);
        }

        /// <summary>
        /// Validates the music genres.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public Task ValidateMusicGenres(CancellationToken cancellationToken, IProgress<double> progress)
        {
            return new MusicGenresValidator(this, _userManager, _logger).Run(progress, cancellationToken);
        }

        /// <summary>
        /// Validates the game genres.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public Task ValidateGameGenres(CancellationToken cancellationToken, IProgress<double> progress)
        {
            return new GameGenresValidator(this, _userManager, _logger).Run(progress, cancellationToken);
        }

        /// <summary>
        /// Validates the studios.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public Task ValidateStudios(CancellationToken cancellationToken, IProgress<double> progress)
        {
            return new StudiosValidator(this, _userManager, _logger).Run(progress, cancellationToken);
        }

        /// <summary>
        /// Validates the genres.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public Task ValidateGenres(CancellationToken cancellationToken, IProgress<double> progress)
        {
            return new GenresValidator(this, _userManager, _logger).Run(progress, cancellationToken);
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
            _taskManager.CancelIfRunningAndQueue<RefreshMediaLibraryTask>();

            return Task.FromResult(true);
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

            progress.Report(.5);

            // Start by just validating the children of the root, but go no further
            await RootFolder.ValidateChildren(new Progress<double>(), cancellationToken, recursive: false);

            progress.Report(1);

            foreach (var folder in _userManager.Users.Select(u => u.RootFolder).Distinct())
            {
                await ValidateCollectionFolders(folder, cancellationToken).ConfigureAwait(false);
            }

            progress.Report(2);

            var innerProgress = new ActionableProgress<double>();

            innerProgress.RegisterAction(pct => progress.Report(2 + pct * .13));

            // Run prescan tasks
            await RunPrescanTasks(innerProgress, cancellationToken).ConfigureAwait(false);

            progress.Report(15);

            innerProgress = new ActionableProgress<double>();

            innerProgress.RegisterAction(pct => progress.Report(15 + pct * .6));

            // Now validate the entire media library
            await RootFolder.ValidateChildren(innerProgress, cancellationToken, recursive: true).ConfigureAwait(false);

            progress.Report(75);

            innerProgress = new ActionableProgress<double>();

            innerProgress.RegisterAction(pct => progress.Report(75 + pct * .25));

            // Run post-scan tasks
            await RunPostScanTasks(innerProgress, cancellationToken).ConfigureAwait(false);

            progress.Report(100);

            // Bad practice, i know. But we keep a lot in memory, unfortunately.
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.Collect(2, GCCollectionMode.Forced, true);
        }

        /// <summary>
        /// Runs the prescan tasks.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task RunPrescanTasks(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var tasks = PrescanTasks.ToList();

            var numComplete = 0;
            var numTasks = tasks.Count;

            foreach (var task in tasks)
            {
                var innerProgress = new ActionableProgress<double>();

                // Prevent access to modified closure
                var currentNumComplete = numComplete;

                innerProgress.RegisterAction(pct =>
                {
                    double innerPercent = (currentNumComplete * 100) + pct;
                    innerPercent /= numTasks;
                    progress.Report(innerPercent);
                });

                try
                {
                    await task.Run(innerProgress, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.Info("Pre-scan task cancelled: {0}", task.GetType().Name);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error running pre-scan task", ex);
                }

                numComplete++;
                double percent = numComplete;
                percent /= numTasks;
                progress.Report(percent * 100);
            }

            progress.Report(100);
        }

        /// <summary>
        /// Runs the post scan tasks.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task RunPostScanTasks(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var tasks = PostscanTasks.ToList();

            var numComplete = 0;
            var numTasks = tasks.Count;

            foreach (var task in tasks)
            {
                var innerProgress = new ActionableProgress<double>();

                // Prevent access to modified closure
                var currentNumComplete = numComplete;

                innerProgress.RegisterAction(pct =>
                {
                    double innerPercent = (currentNumComplete * 100) + pct;
                    innerPercent /= numTasks;
                    progress.Report(innerPercent);
                });

                try
                {
                    await task.Run(innerProgress, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.Info("Post-scan task cancelled: {0}", task.GetType().Name);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error running postscan task", ex);
                }

                numComplete++;
                double percent = numComplete;
                percent /= numTasks;
                progress.Report(percent * 100);
            }

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

                    Locations = Directory.EnumerateFiles(dir, "*.lnk", SearchOption.TopDirectoryOnly)
                                .Select(FileSystem.ResolveShortcut)
                                .OrderBy(i => i)
                                .ToList(),

                    CollectionType = GetCollectionType(dir)
                });
        }

        private string GetCollectionType(string path)
        {
            return new DirectoryInfo(path).EnumerateFiles("*.collection", SearchOption.TopDirectoryOnly)
                .Select(i => Path.GetFileNameWithoutExtension(i.FullName))
                .FirstOrDefault();
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

            return RetrieveItem(id);
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
        public Task CreateItem(BaseItem item, CancellationToken cancellationToken)
        {
            return CreateItems(new[] { item }, cancellationToken);
        }

        /// <summary>
        /// Creates the items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task CreateItems(IEnumerable<BaseItem> items, CancellationToken cancellationToken)
        {
            var list = items.ToList();

            await ItemRepository.SaveItems(list, cancellationToken).ConfigureAwait(false);

            foreach (var item in list)
            {
                UpdateItemInLibraryCache(item);
            }

            UpdateCollectionFolders();

            if (ItemAdded != null)
            {
                foreach (var item in list)
                {
                    try
                    {
                        ItemAdded(this, new ItemChangeEventArgs { Item = item });
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error in ItemAdded event handler", ex);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateReason">The update reason.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task UpdateItem(BaseItem item, ItemUpdateType updateReason, CancellationToken cancellationToken)
        {
            if (item.LocationType == LocationType.FileSystem)
            {
                await SaveMetadata(item, updateReason).ConfigureAwait(false);
            }

            await ItemRepository.SaveItem(item, cancellationToken).ConfigureAwait(false);

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
            UpdateCollectionFolders();

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

        private void UpdateCollectionFolders()
        {
            foreach (var folder in _userManager.Users.SelectMany(i => i.RootFolder.Children).OfType<CollectionFolder>().ToList())
            {
                folder.ResetDynamicChildren();
            }
        }

        /// <summary>
        /// Retrieves the item.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>BaseItem.</returns>
        public BaseItem RetrieveItem(Guid id)
        {
            var item = ItemRepository.RetrieveItem(id);

            var folder = item as Folder;

            if (folder != null)
            {
                folder.LoadSavedChildren();
            }

            return item;
        }

        private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        /// Saves the metadata.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <returns>Task.</returns>
        private async Task SaveMetadata(BaseItem item, ItemUpdateType updateType)
        {
            foreach (var saver in _savers.Where(i => i.IsEnabledFor(item, updateType)))
            {
                var path = saver.GetSavePath(item);

                var semaphore = _fileLocks.GetOrAdd(path, key => new SemaphoreSlim(1, 1));

                var directoryWatchers = _directoryWatchersFactory();

                await semaphore.WaitAsync().ConfigureAwait(false);

                try
                {
                    directoryWatchers.TemporarilyIgnore(path);
                    saver.Save(item, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in metadata saver", ex);
                }
                finally
                {
                    directoryWatchers.RemoveTempIgnore(path);
                    semaphore.Release();
                }
            }
        }

        /// <summary>
        /// Finds the type of the collection.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        public string FindCollectionType(BaseItem item)
        {
            while (!(item.Parent is AggregateFolder) && item.Parent != null)
            {
                item = item.Parent;
            }

            if (item == null)
            {
                return null;
            }

            var collectionTypes = _userManager.Users
                .Select(i => i.RootFolder)
                .Distinct()
                .SelectMany(i => i.Children)
                .OfType<CollectionFolder>()
                .Where(i =>
                {
                    try
                    {
                        return i.LocationType != LocationType.Remote && i.LocationType != LocationType.Virtual &&
                               i.ResolveArgs.PhysicalLocations.Contains(item.Path);
                    }
                    catch (IOException ex)
                    {
                        _logger.ErrorException("Error getting resolve args for {0}", ex, i.Path);
                        return false;
                    }
                })
                .Select(i => i.CollectionType)
                .Where(i => !string.IsNullOrEmpty(i))
                .Distinct()
                .ToList();

            return collectionTypes.Count == 1 ? collectionTypes[0] : null;
        }
    }
}
