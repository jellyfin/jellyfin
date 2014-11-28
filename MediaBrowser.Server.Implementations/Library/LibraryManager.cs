using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Progress;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Naming.Audio;
using MediaBrowser.Naming.Common;
using MediaBrowser.Naming.IO;
using MediaBrowser.Naming.Video;
using MediaBrowser.Server.Implementations.Library.Resolvers.TV;
using MediaBrowser.Server.Implementations.Library.Validators;
using MediaBrowser.Server.Implementations.ScheduledTasks;
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
        private ILibraryPostScanTask[] PostscanTasks { get; set; }

        /// <summary>
        /// Gets the intro providers.
        /// </summary>
        /// <value>The intro providers.</value>
        private IIntroProvider[] IntroProviders { get; set; }

        /// <summary>
        /// Gets the list of entity resolution ignore rules
        /// </summary>
        /// <value>The entity resolution ignore rules.</value>
        private IResolverIgnoreRule[] EntityResolutionIgnoreRules { get; set; }

        /// <summary>
        /// Gets the list of BasePluginFolders added by plugins
        /// </summary>
        /// <value>The plugin folders.</value>
        private IVirtualFolderCreator[] PluginFolderCreators { get; set; }

        /// <summary>
        /// Gets the list of currently registered entity resolvers
        /// </summary>
        /// <value>The entity resolvers enumerable.</value>
        private IItemResolver[] EntityResolvers { get; set; }

        /// <summary>
        /// Gets or sets the comparers.
        /// </summary>
        /// <value>The comparers.</value>
        private IBaseItemComparer[] Comparers { get; set; }

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
        private readonly IUserDataManager _userDataRepository;

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

        private readonly Func<ILibraryMonitor> _libraryMonitorFactory;
        private readonly Func<IProviderManager> _providerManagerFactory;

        /// <summary>
        /// The _library items cache
        /// </summary>
        private readonly ConcurrentDictionary<Guid, BaseItem> _libraryItemsCache;
        /// <summary>
        /// Gets the library items cache.
        /// </summary>
        /// <value>The library items cache.</value>
        private ConcurrentDictionary<Guid, BaseItem> LibraryItemsCache
        {
            get
            {
                return _libraryItemsCache;
            }
        }

        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryManager" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="taskManager">The task manager.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        public LibraryManager(ILogger logger, ITaskManager taskManager, IUserManager userManager, IServerConfigurationManager configurationManager, IUserDataManager userDataRepository, Func<ILibraryMonitor> libraryMonitorFactory, IFileSystem fileSystem, Func<IProviderManager> providerManagerFactory)
        {
            _logger = logger;
            _taskManager = taskManager;
            _userManager = userManager;
            ConfigurationManager = configurationManager;
            _userDataRepository = userDataRepository;
            _libraryMonitorFactory = libraryMonitorFactory;
            _fileSystem = fileSystem;
            _providerManagerFactory = providerManagerFactory;
            ByReferenceItems = new ConcurrentDictionary<Guid, BaseItem>();
            _libraryItemsCache = new ConcurrentDictionary<Guid, BaseItem>();

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
        /// <param name="postscanTasks">The postscan tasks.</param>
        public void AddParts(IEnumerable<IResolverIgnoreRule> rules,
            IEnumerable<IVirtualFolderCreator> pluginFolders,
            IEnumerable<IItemResolver> resolvers,
            IEnumerable<IIntroProvider> introProviders,
            IEnumerable<IBaseItemComparer> itemComparers,
            IEnumerable<ILibraryPostScanTask> postscanTasks)
        {
            EntityResolutionIgnoreRules = rules.ToArray();
            PluginFolderCreators = pluginFolders.ToArray();
            EntityResolvers = resolvers.OrderBy(i => i.Priority).ToArray();
            IntroProviders = introProviders.ToArray();
            Comparers = itemComparers.ToArray();

            PostscanTasks = postscanTasks.OrderBy(i =>
            {
                var hasOrder = i as IHasOrder;

                return hasOrder == null ? 0 : hasOrder.Order;

            }).ToArray();
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

        private bool _wizardCompleted;
        /// <summary>
        /// Records the configuration values.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        private void RecordConfigurationValues(ServerConfiguration configuration)
        {
            _seasonZeroDisplayName = configuration.SeasonZeroDisplayName;
            _itemsByNamePath = ConfigurationManager.ApplicationPaths.ItemsByNamePath;
            _wizardCompleted = configuration.IsStartupWizardCompleted;
        }

        /// <summary>
        /// Configurations the updated.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void ConfigurationUpdated(object sender, EventArgs e)
        {
            var config = ConfigurationManager.Configuration;

            var ibnPathChanged = !string.Equals(_itemsByNamePath, ConfigurationManager.ApplicationPaths.ItemsByNamePath, StringComparison.Ordinal);

            if (ibnPathChanged)
            {
                RemoveItemsByNameFromCache();
            }

            var newSeasonZeroName = ConfigurationManager.Configuration.SeasonZeroDisplayName;
            var seasonZeroNameChanged = !string.Equals(_seasonZeroDisplayName, newSeasonZeroName, StringComparison.Ordinal);
            var wizardChanged = config.IsStartupWizardCompleted != _wizardCompleted;

            RecordConfigurationValues(config);

            Task.Run(async () =>
            {
                if (seasonZeroNameChanged)
                {
                    await UpdateSeasonZeroNames(newSeasonZeroName, CancellationToken.None).ConfigureAwait(false);
                }

                if (seasonZeroNameChanged || ibnPathChanged || wizardChanged)
                {
                    _taskManager.CancelIfRunningAndQueue<RefreshMediaLibraryTask>();
                }
            });
        }

        private void RemoveItemsByNameFromCache()
        {
            RemoveItemsFromCache(i => i is Person);
            RemoveItemsFromCache(i => i is Year);
            RemoveItemsFromCache(i => i is Genre);
            RemoveItemsFromCache(i => i is MusicGenre);
            RemoveItemsFromCache(i => i is GameGenre);
            RemoveItemsFromCache(i => i is Studio);
            RemoveItemsFromCache(i =>
            {
                var artist = i as MusicArtist;
                return artist != null && artist.IsAccessedByName;
            });
        }

        private void RemoveItemsFromCache(Func<BaseItem, bool> remove)
        {
            var items = _libraryItemsCache.ToList().Where(i => remove(i.Value)).ToList();

            foreach (var item in items)
            {
                BaseItem value;
                _libraryItemsCache.TryRemove(item.Key, out value);
            }
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
                .Where(i => i.IndexNumber.HasValue && i.IndexNumber.Value == 0 && !string.Equals(i.Name, newName, StringComparison.Ordinal))
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
        /// Updates the item in library cache.
        /// </summary>
        /// <param name="item">The item.</param>
        private void UpdateItemInLibraryCache(BaseItem item)
        {
            RegisterItem(item);
        }

        public void RegisterItem(BaseItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            RegisterItem(item.Id, item);
        }

        private void RegisterItem(Guid id, BaseItem item)
        {
            LibraryItemsCache.AddOrUpdate(id, item, delegate { return item; });
        }

        public async Task DeleteItem(BaseItem item, DeleteOptions options)
        {
            _logger.Debug("Deleting item, Type: {0}, Name: {1}, Path: {2}, Id: {3}",
                item.GetType().Name,
                item.Name,
                item.Path ?? string.Empty,
                item.Id);

            var parent = item.Parent;

            var locationType = item.LocationType;

            var children = item.IsFolder
                ? ((Folder)item).RecursiveChildren.ToList()
                : new List<BaseItem>();

            foreach (var metadataPath in GetMetadataPaths(item, children))
            {
                _logger.Debug("Deleting path {0}", metadataPath);

                try
                {
                    Directory.Delete(metadataPath, true);
                }
                catch (DirectoryNotFoundException)
                {

                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error deleting {0}", ex, metadataPath);
                }
            }

            if (options.DeleteFileLocation && locationType != LocationType.Remote && locationType != LocationType.Virtual)
            {
                foreach (var path in item.GetDeletePaths().ToList())
                {
                    if (Directory.Exists(path))
                    {
                        _logger.Debug("Deleting path {0}", path);
                        Directory.Delete(path, true);
                    }
                    else if (File.Exists(path))
                    {
                        _logger.Debug("Deleting path {0}", path);
                        File.Delete(path);
                    }
                }

                if (parent != null)
                {
                    await parent.ValidateChildren(new Progress<double>(), CancellationToken.None)
                              .ConfigureAwait(false);
                }
            }
            else if (parent != null)
            {
                await parent.RemoveChild(item, CancellationToken.None).ConfigureAwait(false);
            }

            await ItemRepository.DeleteItem(item.Id, CancellationToken.None).ConfigureAwait(false);
            foreach (var child in children)
            {
                await ItemRepository.DeleteItem(child.Id, CancellationToken.None).ConfigureAwait(false);
            }

            BaseItem removed;
            _libraryItemsCache.TryRemove(item.Id, out removed);

            ReportItemRemoved(item);
        }

        private IEnumerable<string> GetMetadataPaths(BaseItem item, IEnumerable<BaseItem> children)
        {
            var list = new List<string>
            {
                item.GetInternalMetadataPath()
            };

            list.AddRange(children.Select(i => i.GetInternalMetadataPath()));

            return list;
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
                ResolverHelper.SetInitialItemValues(item, args, _fileSystem);
            }

            return item;
        }

        public IEnumerable<BaseItem> ReplaceVideosWithPrimaryVersions(IEnumerable<BaseItem> items)
        {
            var dict = new Dictionary<Guid, BaseItem>();

            foreach (var item in items)
            {
                var video = item as Video;

                if (video != null)
                {
                    if (video.PrimaryVersionId.HasValue)
                    {
                        var primary = GetItemById(video.PrimaryVersionId.Value) as Video;

                        if (primary != null)
                        {
                            dict[primary.Id] = primary;
                            continue;
                        }
                    }
                }
                dict[item.Id] = item;
            }

            return dict.Values;
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

        public BaseItem ResolvePath(FileSystemInfo fileInfo,
            Folder parent = null,
            string collectionType = null)
        {
            return ResolvePath(fileInfo, new DirectoryService(_logger), parent, collectionType);
        }

        public BaseItem ResolvePath(FileSystemInfo fileInfo, IDirectoryService directoryService, Folder parent = null, string collectionType = null)
        {
            if (fileInfo == null)
            {
                throw new ArgumentNullException("fileInfo");
            }

            var args = new ItemResolveArgs(ConfigurationManager.ApplicationPaths, this, directoryService)
            {
                Parent = parent,
                Path = fileInfo.FullName,
                FileInfo = fileInfo,
                CollectionType = collectionType
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

                var fileSystemDictionary = FileData.GetFilteredFileSystemEntries(directoryService, args.Path, _fileSystem, _logger, args, flattenFolderDepth: flattenFolderDepth, resolveShortcuts: isPhysicalRoot || args.IsVf);

                // Need to remove subpaths that may have been resolved from shortcuts
                // Example: if \\server\movies exists, then strip out \\server\movies\action
                if (isPhysicalRoot)
                {
                    var paths = NormalizeRootPathList(fileSystemDictionary.Keys);

                    fileSystemDictionary = paths.Select(i => (FileSystemInfo)new DirectoryInfo(i)).ToDictionary(i => i.FullName);
                }

                args.FileSystemDictionary = fileSystemDictionary;
            }

            // Check to see if we should resolve based on our contents
            if (args.IsDirectory && !ShouldResolvePathContents(args))
            {
                return null;
            }

            return ResolveItem(args);
        }

        public IEnumerable<string> NormalizeRootPathList(IEnumerable<string> paths)
        {
            var list = paths.Select(_fileSystem.NormalizePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var dupes = list.Where(subPath => !subPath.EndsWith(":\\", StringComparison.OrdinalIgnoreCase) && list.Any(i => _fileSystem.ContainsSubPath(i, subPath)))
                .ToList();

            foreach (var dupe in dupes)
            {
                _logger.Info("Found duplicate path: {0}", dupe);
            }

            return list.Except(dupes, StringComparer.OrdinalIgnoreCase);
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

        public List<T> ResolvePaths<T>(IEnumerable<FileSystemInfo> files, IDirectoryService directoryService, Folder parent, string collectionType = null)
            where T : BaseItem
        {
            return files.Select(f =>
            {
                try
                {
                    return ResolvePath(f, directoryService, parent, collectionType) as T;
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error resolving path {0}", ex, f.FullName);
                    return null;
                }

            }).Where(i => i != null)
            .ToList();
        }

        /// <summary>
        /// Creates the root media folder
        /// </summary>
        /// <returns>AggregateFolder.</returns>
        /// <exception cref="System.InvalidOperationException">Cannot create the root folder until plugins have loaded</exception>
        public AggregateFolder CreateRootFolder()
        {
            var rootFolderPath = ConfigurationManager.ApplicationPaths.RootFolderPath;

            Directory.CreateDirectory(rootFolderPath);

            var rootFolder = GetItemById(rootFolderPath.GetMBId(typeof(AggregateFolder))) as AggregateFolder ?? (AggregateFolder)ResolvePath(new DirectoryInfo(rootFolderPath));

            // Add in the plug-in folders
            foreach (var child in PluginFolderCreators)
            {
                var folder = child.GetFolder();

                if (folder != null)
                {
                    if (folder.Id == Guid.Empty)
                    {
                        folder.Id = (folder.Path ?? folder.GetType().Name).GetMBId(folder.GetType());
                    }

                    folder = GetItemById(folder.Id) as BasePluginFolder ?? folder;

                    rootFolder.AddVirtualChild(folder);

                    RegisterItem(folder);
                }
            }

            return rootFolder;
        }

        private UserRootFolder _userRootFolder;
        public Folder GetUserRootFolder()
        {
            if (_userRootFolder == null)
            {
                var userRootPath = ConfigurationManager.ApplicationPaths.DefaultUserViewsPath;

                Directory.CreateDirectory(userRootPath);

                _userRootFolder = GetItemById(userRootPath.GetMBId(typeof(UserRootFolder))) as UserRootFolder ??
                                  (UserRootFolder)ResolvePath(new DirectoryInfo(userRootPath));
            }

            return _userRootFolder;
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
                throw new ArgumentOutOfRangeException("Years less than or equal to 0 are invalid.");
            }

            return GetItemByName<Year>(ConfigurationManager.ApplicationPaths.YearPath, value.ToString(UsCulture));
        }

        /// <summary>
        /// Gets a Genre
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Genre}.</returns>
        public MusicArtist GetArtist(string name)
        {
            return GetItemByName<MusicArtist>(ConfigurationManager.ApplicationPaths.ArtistsPath, name);
        }

        private T GetItemByName<T>(string path, string name)
            where T : BaseItem, new()
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException("path");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            var validFilename = _fileSystem.GetValidFilename(name).Trim();

            string subFolderPrefix = null;

            var type = typeof(T);

            if (type == typeof(Person) && ConfigurationManager.Configuration.EnablePeoplePrefixSubFolders)
            {
                subFolderPrefix = validFilename.Substring(0, 1);
            }

            var fullPath = string.IsNullOrEmpty(subFolderPrefix) ?
                Path.Combine(path, validFilename) :
                Path.Combine(path, subFolderPrefix, validFilename);

            var id = fullPath.GetMBId(type);

            BaseItem obj;

            if (!_libraryItemsCache.TryGetValue(id, out obj))
            {
                obj = CreateItemByName<T>(fullPath, name, id);

                RegisterItem(id, obj);
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
        private T CreateItemByName<T>(string path, string name, Guid id)
            where T : BaseItem, new()
        {
            var isArtist = typeof(T) == typeof(MusicArtist);

            if (isArtist)
            {
                var validFilename = _fileSystem.GetValidFilename(name).Trim();

                var existing = RootFolder.RecursiveChildren
                    .OfType<T>()
                    .FirstOrDefault(i => string.Equals(_fileSystem.GetValidFilename(i.Name).Trim(), validFilename, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    return existing;
                }
            }

            var fileInfo = new DirectoryInfo(path);

            var isNew = false;

            if (!fileInfo.Exists)
            {
                fileInfo = Directory.CreateDirectory(path);

                isNew = true;
            }

            var item = isNew ? null : GetItemById(id) as T;

            if (item == null)
            {
                item = new T
                {
                    Name = name,
                    Id = id,
                    DateCreated = _fileSystem.GetCreationTimeUtc(fileInfo),
                    DateModified = _fileSystem.GetLastWriteTimeUtc(fileInfo),
                    Path = path
                };
            }

            if (isArtist)
            {
                (item as MusicArtist).IsAccessedByName = true;
            }

            return item;
        }

        /// <summary>
        /// Validate and refresh the People sub-set of the IBN.
        /// The items are stored in the db but not loaded into memory until actually requested by an operation.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public Task ValidatePeople(CancellationToken cancellationToken, IProgress<double> progress)
        {
            // Ensure the location is available.
            Directory.CreateDirectory(ConfigurationManager.ApplicationPaths.PeoplePath);

            return new PeopleValidator(this, _logger, ConfigurationManager).ValidatePeople(cancellationToken, progress);
        }

        /// <summary>
        /// Validates the artists.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public Task ValidateArtists(CancellationToken cancellationToken, IProgress<double> progress)
        {
            // Ensure the location is unavailable.
            Directory.CreateDirectory(ConfigurationManager.ApplicationPaths.ArtistsPath);

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
            // Ensure the location is unavailable.
            Directory.CreateDirectory(ConfigurationManager.ApplicationPaths.MusicGenrePath);

            return new MusicGenresValidator(this, _logger).Run(progress, cancellationToken);
        }

        /// <summary>
        /// Validates the game genres.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public Task ValidateGameGenres(CancellationToken cancellationToken, IProgress<double> progress)
        {
            // Ensure the location is unavailable.
            Directory.CreateDirectory(ConfigurationManager.ApplicationPaths.GameGenrePath);

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
            // Ensure the location is unavailable.
            Directory.CreateDirectory(ConfigurationManager.ApplicationPaths.StudioPath);

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
            // Ensure the location is unavailable.
            Directory.CreateDirectory(ConfigurationManager.ApplicationPaths.GenrePath);

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
        /// Queues the library scan.
        /// </summary>
        public void QueueLibraryScan()
        {
            // Just run the scheduled task so that the user can see it
            _taskManager.QueueScheduledTask<RefreshMediaLibraryTask>();
        }

        /// <summary>
        /// Validates the media library internal.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task ValidateMediaLibraryInternal(IProgress<double> progress, CancellationToken cancellationToken)
        {
            _libraryMonitorFactory().Stop();

            try
            {
                await PerformLibraryValidation(progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _libraryMonitorFactory().Start();
            }
        }

        private async Task PerformLibraryValidation(IProgress<double> progress, CancellationToken cancellationToken)
        {
            _logger.Info("Validating media library");

            await RootFolder.RefreshMetadata(cancellationToken).ConfigureAwait(false);

            progress.Report(.5);

            // Start by just validating the children of the root, but go no further
            await RootFolder.ValidateChildren(new Progress<double>(), cancellationToken, new MetadataRefreshOptions(), recursive: false);

            progress.Report(1);

            var userRoot = GetUserRootFolder();

            await userRoot.RefreshMetadata(cancellationToken).ConfigureAwait(false);

            await userRoot.ValidateChildren(new Progress<double>(), cancellationToken, new MetadataRefreshOptions(), recursive: false).ConfigureAwait(false);
            progress.Report(2);

            var innerProgress = new ActionableProgress<double>();

            innerProgress.RegisterAction(pct => progress.Report(2 + pct * .73));

            // Now validate the entire media library
            await RootFolder.ValidateChildren(innerProgress, cancellationToken, new MetadataRefreshOptions(), recursive: true).ConfigureAwait(false);

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
            return GetDefaultVirtualFolders();
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

                    Locations = Directory.EnumerateFiles(dir, "*.mblink", SearchOption.TopDirectoryOnly)
                                .Select(_fileSystem.ResolveShortcut)
                                .OrderBy(i => i)
                                .ToList(),

                    CollectionType = GetCollectionType(dir)
                });
        }

        private string GetCollectionType(string path)
        {
            return new DirectoryInfo(path).EnumerateFiles("*.collection", SearchOption.TopDirectoryOnly)
                .Select(i => _fileSystem.GetFileNameWithoutExtension(i))
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

            item = RetrieveItem(id);

            if (item != null)
            {
                RegisterItem(item);
            }

            return item;
        }

        public BaseItem GetMemoryItemById(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            BaseItem item;

            LibraryItemsCache.TryGetValue(id, out item);

            return item;
        }

        /// <summary>
        /// Gets the intros.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        public async Task<IEnumerable<Video>> GetIntros(BaseItem item, User user)
        {
            var tasks = IntroProviders
                .OrderBy(i => (i.GetType().Name.IndexOf("Default", StringComparison.OrdinalIgnoreCase) == -1 ? 0 : 1))
                .Take(1)
                .Select(i => GetIntros(i, item, user));

            var items = await Task.WhenAll(tasks).ConfigureAwait(false);

            return items
                .SelectMany(i => i.ToArray())
                .Select(ResolveIntro)
                .Where(i => i != null);
        }

        /// <summary>
        /// Gets the intros.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="item">The item.</param>
        /// <param name="user">The user.</param>
        /// <returns>Task&lt;IEnumerable&lt;IntroInfo&gt;&gt;.</returns>
        private async Task<IEnumerable<IntroInfo>> GetIntros(IIntroProvider provider, BaseItem item, User user)
        {
            try
            {
                return await provider.GetIntros(item, user).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error getting intros", ex);

                return new List<IntroInfo>();
            }
        }

        /// <summary>
        /// Gets all intro files.
        /// </summary>
        /// <returns>IEnumerable{System.String}.</returns>
        public IEnumerable<string> GetAllIntroFiles()
        {
            return IntroProviders.SelectMany(i =>
            {
                try
                {
                    return i.GetAllIntroFiles().ToList();
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error getting intro files", ex);

                    return new List<string>();
                }
            });
        }

        /// <summary>
        /// Resolves the intro.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>Video.</returns>
        private Video ResolveIntro(IntroInfo info)
        {
            Video video = null;

            if (info.ItemId.HasValue)
            {
                // Get an existing item by Id
                video = GetItemById(info.ItemId.Value) as Video;

                if (video == null)
                {
                    _logger.Error("Unable to locate item with Id {0}.", info.ItemId.Value);
                }
            }
            else if (!string.IsNullOrEmpty(info.Path))
            {
                try
                {
                    // Try to resolve the path into a video 
                    video = ResolvePath(_fileSystem.GetFileSystemInfo(info.Path)) as Video;

                    if (video == null)
                    {
                        _logger.Error("Intro resolver returned null for {0}.", info.Path);
                    }
                    else
                    {
                        // Pull the saved db item that will include metadata
                        var dbItem = GetItemById(video.Id) as Video;

                        if (dbItem != null)
                        {
                            video = dbItem;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error resolving path {0}.", ex, info.Path);
                }
            }
            else
            {
                _logger.Error("IntroProvider returned an IntroInfo with null Path and ItemId.");
            }

            return video;
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
            var locationType = item.LocationType;
            if (locationType != LocationType.Remote && locationType != LocationType.Virtual)
            {
                await _providerManagerFactory().SaveMetadata(item, updateReason).ConfigureAwait(false);
            }

            item.DateLastSaved = DateTime.UtcNow;

            var logName = item.LocationType == LocationType.Remote ? item.Name ?? item.Path : item.Path ?? item.Name;
            _logger.Debug("Saving {0} to database.", logName);

            await ItemRepository.SaveItem(item, cancellationToken).ConfigureAwait(false);

            UpdateItemInLibraryCache(item);

            if (ItemUpdated != null)
            {
                try
                {
                    ItemUpdated(this, new ItemChangeEventArgs
                    {
                        Item = item,
                        UpdateReason = updateReason
                    });
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
        /// Retrieves the item.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>BaseItem.</returns>
        public BaseItem RetrieveItem(Guid id)
        {
            return ItemRepository.RetrieveItem(id);
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

            var collectionTypes = GetUserRootFolder().Children
                .OfType<ICollectionFolder>()
                .Where(i => !string.IsNullOrEmpty(i.CollectionType) && (string.Equals(i.Path, item.Path, StringComparison.OrdinalIgnoreCase) || i.PhysicalLocations.Contains(item.Path)))
                .Select(i => i.CollectionType)
                .Distinct()
                .ToList();

            return collectionTypes.Count == 1 ? collectionTypes[0] : null;
        }

        public async Task<UserView> GetNamedView(string name,
            string type,
            string sortName,
            CancellationToken cancellationToken)
        {
            var path = Path.Combine(ConfigurationManager.ApplicationPaths.ItemsByNamePath,
                "views");

            path = Path.Combine(path, _fileSystem.GetValidFilename(type));

            var id = (path + "_namedview_" + name).GetMBId(typeof(UserView));

            var item = GetItemById(id) as UserView;

            var refresh = false;

            if (item == null ||
                !string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase))
            {
                Directory.CreateDirectory(path);

                item = new UserView
                {
                    Path = path,
                    Id = id,
                    DateCreated = DateTime.UtcNow,
                    Name = name,
                    ViewType = type,
                    ForcedSortName = sortName
                };

                await CreateItem(item, cancellationToken).ConfigureAwait(false);

                refresh = true;
            }

            if (!refresh && item != null)
            {
                refresh = (DateTime.UtcNow - item.DateLastSaved).TotalHours >= 24;
            }

            if (refresh)
            {
                await item.RefreshMetadata(new MetadataRefreshOptions
                {
                    ForceSave = true

                }, cancellationToken).ConfigureAwait(false);
            }

            return item;
        }

        public async Task<UserView> GetSpecialFolder(User user,
            string name,
            string parentId,
            string viewType,
            string sortName,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            if (string.IsNullOrWhiteSpace(parentId))
            {
                throw new ArgumentNullException("parentId");
            }

            if (string.IsNullOrWhiteSpace(viewType))
            {
                throw new ArgumentNullException("viewType");
            }

            var id = ("7_namedview_" + name + user.Id.ToString("N") + parentId).GetMBId(typeof(UserView));

            var path = BaseItem.GetInternalMetadataPathForId(id);

            var item = GetItemById(id) as UserView;

            var refresh = false;

            if (item == null)
            {
                Directory.CreateDirectory(path);

                item = new UserView
                {
                    Path = path,
                    Id = id,
                    DateCreated = DateTime.UtcNow,
                    Name = name,
                    ViewType = viewType,
                    ForcedSortName = sortName,
                    UserId = user.Id,
                    ParentId = new Guid(parentId)
                };

                await CreateItem(item, cancellationToken).ConfigureAwait(false);

                refresh = true;
            }

            if (!refresh && item != null)
            {
                refresh = (DateTime.UtcNow - item.DateLastSaved).TotalHours >= 24;
            }

            if (refresh)
            {
                await item.RefreshMetadata(new MetadataRefreshOptions
                {
                    ForceSave = true

                }, cancellationToken).ConfigureAwait(false);
            }

            return item;
        }

        public bool IsVideoFile(string path)
        {
            var resolver = new VideoResolver(new ExtendedNamingOptions(), new Naming.Logging.NullLogger());
            return resolver.IsVideoFile(path);
        }

        public bool IsAudioFile(string path)
        {
            var parser = new AudioFileParser(new ExtendedNamingOptions());
            return parser.IsAudioFile(path);
        }

        public int? GetSeasonNumberFromPath(string path)
        {
            return SeriesResolver.GetSeasonNumberFromPath(path);
        }

        public int? GetSeasonNumberFromEpisodeFile(string path)
        {
            return SeriesResolver.GetSeasonNumberFromEpisodeFile(path);
        }

        public int? GetEndingEpisodeNumberFromFile(string path)
        {
            return SeriesResolver.GetEndingEpisodeNumberFromFile(path);
        }

        public int? GetEpisodeNumberFromFile(string path, bool considerSeasonless)
        {
            return SeriesResolver.GetEpisodeNumberFromFile(path, considerSeasonless);
        }

        public ItemLookupInfo ParseName(string name)
        {
            var resolver = new VideoResolver(new ExtendedNamingOptions(), new Naming.Logging.NullLogger());

            var result = resolver.CleanDateTime(name);
            var cleanName = resolver.CleanString(result.Name);

            return new ItemLookupInfo
            {
                Name = cleanName.Name,
                Year = result.Year
            };
        }

        public IEnumerable<FileSystemInfo> GetAdditionalParts(string file,
            VideoType type,
            IEnumerable<FileSystemInfo> files)
        {
            var resolver = new StackResolver(new ExtendedNamingOptions(), new Naming.Logging.NullLogger());

            StackResult result;
            List<FileSystemInfo> filteredFiles;

            if (type == VideoType.BluRay || type == VideoType.Dvd)
            {
                filteredFiles = files.Where(i => (i.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                    .ToList();

                result = resolver.ResolveDirectories(filteredFiles.Select(i => i.FullName));
            }
            else
            {
                filteredFiles = files.Where(i => (i.Attributes & FileAttributes.Directory) != FileAttributes.Directory)
                    .ToList();

                result = resolver.ResolveFiles(filteredFiles.Select(i => i.FullName));
            }

            var stack = result.Stacks
                .FirstOrDefault(i => i.Files.Contains(file, StringComparer.OrdinalIgnoreCase));

            if (stack != null)
            {
                return stack.Files.Where(i => !string.Equals(i, file, StringComparison.OrdinalIgnoreCase))
                    .Select(i => filteredFiles.FirstOrDefault(f => string.Equals(i, f.FullName, StringComparison.OrdinalIgnoreCase)))
                    .Where(i => i != null);
            }

            return new List<FileSystemInfo>();
        }
    }
}
