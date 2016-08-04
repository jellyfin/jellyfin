using Interfaces.IO;
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
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Naming.Audio;
using MediaBrowser.Naming.Common;
using MediaBrowser.Naming.TV;
using MediaBrowser.Naming.Video;
using MediaBrowser.Server.Implementations.Library.Validators;
using MediaBrowser.Server.Implementations.Logging;
using MediaBrowser.Server.Implementations.ScheduledTasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Net;
using MediaBrowser.Server.Implementations.Library.Resolvers;
using MoreLinq;
using SortOrder = MediaBrowser.Model.Entities.SortOrder;
using VideoResolver = MediaBrowser.Naming.Video.VideoResolver;

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
        private IMultiItemResolver[] MultiItemResolvers { get; set; }

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
        private readonly Func<IUserViewManager> _userviewManager;
        public bool IsScanRunning { get; private set; }

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
        public LibraryManager(ILogger logger, ITaskManager taskManager, IUserManager userManager, IServerConfigurationManager configurationManager, IUserDataManager userDataRepository, Func<ILibraryMonitor> libraryMonitorFactory, IFileSystem fileSystem, Func<IProviderManager> providerManagerFactory, Func<IUserViewManager> userviewManager)
        {
            _logger = logger;
            _taskManager = taskManager;
            _userManager = userManager;
            ConfigurationManager = configurationManager;
            _userDataRepository = userDataRepository;
            _libraryMonitorFactory = libraryMonitorFactory;
            _fileSystem = fileSystem;
            _providerManagerFactory = providerManagerFactory;
            _userviewManager = userviewManager;
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
            MultiItemResolvers = EntityResolvers.OfType<IMultiItemResolver>().ToArray();
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
        private volatile AggregateFolder _rootFolder;
        /// <summary>
        /// The _root folder sync lock
        /// </summary>
        private readonly object _rootFolderSyncLock = new object();
        /// <summary>
        /// Gets the root folder.
        /// </summary>
        /// <value>The root folder.</value>
        public AggregateFolder RootFolder
        {
            get
            {
                if (_rootFolder == null)
                {
                    lock (_rootFolderSyncLock)
                    {
                        if (_rootFolder == null)
                        {
                            _rootFolder = CreateRootFolder();
                        }
                    }
                }
                return _rootFolder;
            }
        }

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

            var newSeasonZeroName = ConfigurationManager.Configuration.SeasonZeroDisplayName;
            var seasonZeroNameChanged = !string.Equals(_seasonZeroDisplayName, newSeasonZeroName, StringComparison.Ordinal);
            var wizardChanged = config.IsStartupWizardCompleted != _wizardCompleted;

            RecordConfigurationValues(config);

            if (seasonZeroNameChanged || wizardChanged)
            {
                _taskManager.CancelIfRunningAndQueue<RefreshMediaLibraryTask>();
            }

            if (seasonZeroNameChanged)
            {
                Task.Run(async () =>
                {
                    await UpdateSeasonZeroNames(newSeasonZeroName, CancellationToken.None).ConfigureAwait(false);

                });
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
            var seasons = GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { typeof(Season).Name },
                Recursive = true,
                IndexNumber = 0

            }).Cast<Season>()
                .Where(i => !string.Equals(i.Name, newName, StringComparison.Ordinal))
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
            if (item is IItemByName)
            {
                if (!(item is MusicArtist))
                {
                    return;
                }
            }

            if (item.IsFolder)
            {
                if (!(item is ICollectionFolder) && !(item is UserView) && !(item is Channel))
                {
                    if (item.SourceType != SourceType.Library)
                    {
                        return;
                    }
                }
            }
            else
            {
                if (item is Photo)
                {
                    return;
                }
            }

            LibraryItemsCache.AddOrUpdate(id, item, delegate { return item; });
        }

        public async Task DeleteItem(BaseItem item, DeleteOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            _logger.Debug("Deleting item, Type: {0}, Name: {1}, Path: {2}, Id: {3}",
                item.GetType().Name,
                item.Name ?? "Unknown name",
                item.Path ?? string.Empty,
                item.Id);

            var parent = item.Parent;

            var locationType = item.LocationType;

            var children = item.IsFolder
                ? ((Folder)item).GetRecursiveChildren().ToList()
                : new List<BaseItem>();

            foreach (var metadataPath in GetMetadataPaths(item, children))
            {
                _logger.Debug("Deleting path {0}", metadataPath);

                try
                {
                    _fileSystem.DeleteDirectory(metadataPath, true);
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
                    if (_fileSystem.DirectoryExists(path))
                    {
                        _logger.Debug("Deleting path {0}", path);
                        _fileSystem.DeleteDirectory(path, true);
                    }
                    else if (_fileSystem.FileExists(path))
                    {
                        _logger.Debug("Deleting path {0}", path);
                        _fileSystem.DeleteFile(path);
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
                parent.RemoveChild(item);
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
        /// <param name="resolvers">The resolvers.</param>
        /// <returns>BaseItem.</returns>
        private BaseItem ResolveItem(ItemResolveArgs args, IItemResolver[] resolvers)
        {
            var item = (resolvers ?? EntityResolvers).Select(r => Resolve(args, r))
                .FirstOrDefault(i => i != null);

            if (item != null)
            {
                ResolverHelper.SetInitialItemValues(item, args, _fileSystem, this);
            }

            return item;
        }

        private BaseItem Resolve(ItemResolveArgs args, IItemResolver resolver)
        {
            try
            {
                return resolver.ResolvePath(args);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error in {0} resolving {1}", ex, resolver.GetType().Name, args.Path);
                return null;
            }
        }

        public Guid GetNewItemId(string key, Type type)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (ConfigurationManager.Configuration.EnableLocalizedGuids && key.StartsWith(ConfigurationManager.ApplicationPaths.ProgramDataPath))
            {
                // Try to normalize paths located underneath program-data in an attempt to make them more portable
                key = key.Substring(ConfigurationManager.ApplicationPaths.ProgramDataPath.Length)
                    .TrimStart(new[] { '/', '\\' })
                    .Replace("/", "\\");
            }

            if (!ConfigurationManager.Configuration.EnableCaseSensitiveItemIds)
            {
                key = key.ToLower();
            }

            key = type.FullName + key;

            return key.GetMD5();
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

        public BaseItem ResolvePath(FileSystemMetadata fileInfo,
            Folder parent = null)
        {
            return ResolvePath(fileInfo, new DirectoryService(_logger, _fileSystem), null, parent);
        }

        private BaseItem ResolvePath(FileSystemMetadata fileInfo, IDirectoryService directoryService, IItemResolver[] resolvers, Folder parent = null, string collectionType = null)
        {
            if (fileInfo == null)
            {
                throw new ArgumentNullException("fileInfo");
            }

            var fullPath = fileInfo.FullName;

            if (string.IsNullOrWhiteSpace(collectionType) && parent != null)
            {
                collectionType = GetContentTypeOverride(fullPath, true);
            }

            var args = new ItemResolveArgs(ConfigurationManager.ApplicationPaths, directoryService)
            {
                Parent = parent,
                Path = fullPath,
                FileInfo = fileInfo,
                CollectionType = collectionType
            };

            // Return null if ignore rules deem that we should do so
            if (IgnoreFile(args.FileInfo, args.Parent))
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
                    var paths = NormalizeRootPathList(fileSystemDictionary.Values);

                    fileSystemDictionary = paths.ToDictionary(i => i.FullName);
                }

                args.FileSystemDictionary = fileSystemDictionary;
            }

            // Check to see if we should resolve based on our contents
            if (args.IsDirectory && !ShouldResolvePathContents(args))
            {
                return null;
            }

            return ResolveItem(args, resolvers);
        }

        public bool IgnoreFile(FileSystemMetadata file, BaseItem parent)
        {
            return EntityResolutionIgnoreRules.Any(r => r.ShouldIgnore(file, parent));
        }

        public IEnumerable<FileSystemMetadata> NormalizeRootPathList(IEnumerable<FileSystemMetadata> paths)
        {
            var originalList = paths.ToList();

            var list = originalList.Where(i => i.IsDirectory)
                .Select(i => _fileSystem.NormalizePath(i.FullName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var dupes = list.Where(subPath => !subPath.EndsWith(":\\", StringComparison.OrdinalIgnoreCase) && list.Any(i => _fileSystem.ContainsSubPath(i, subPath)))
                .ToList();

            foreach (var dupe in dupes)
            {
                _logger.Info("Found duplicate path: {0}", dupe);
            }

            var newList = list.Except(dupes, StringComparer.OrdinalIgnoreCase).Select(_fileSystem.GetDirectoryInfo).ToList();
            newList.AddRange(originalList.Where(i => !i.IsDirectory));
            return newList;
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

        public IEnumerable<BaseItem> ResolvePaths(IEnumerable<FileSystemMetadata> files, IDirectoryService directoryService, Folder parent, string collectionType)
        {
            return ResolvePaths(files, directoryService, parent, collectionType, EntityResolvers);
        }

        public IEnumerable<BaseItem> ResolvePaths(IEnumerable<FileSystemMetadata> files, IDirectoryService directoryService, Folder parent, string collectionType, IItemResolver[] resolvers)
        {
            var fileList = files.Where(i => !IgnoreFile(i, parent)).ToList();

            if (parent != null)
            {
                var multiItemResolvers = resolvers == null ? MultiItemResolvers : resolvers.OfType<IMultiItemResolver>().ToArray();

                foreach (var resolver in multiItemResolvers)
                {
                    var result = resolver.ResolveMultiple(parent, fileList, collectionType, directoryService);

                    if (result != null && result.Items.Count > 0)
                    {
                        var items = new List<BaseItem>();
                        items.AddRange(result.Items);

                        foreach (var item in items)
                        {
                            ResolverHelper.SetInitialItemValues(item, parent, _fileSystem, this, directoryService);
                        }
                        items.AddRange(ResolveFileList(result.ExtraFiles, directoryService, parent, collectionType, resolvers));
                        return items;
                    }
                }
            }

            return ResolveFileList(fileList, directoryService, parent, collectionType, resolvers);
        }

        private IEnumerable<BaseItem> ResolveFileList(IEnumerable<FileSystemMetadata> fileList, IDirectoryService directoryService, Folder parent, string collectionType, IItemResolver[] resolvers)
        {
            return fileList.Select(f =>
            {
                try
                {
                    return ResolvePath(f, directoryService, resolvers, parent, collectionType);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error resolving path {0}", ex, f.FullName);
                    return null;
                }
            }).Where(i => i != null);
        }

        /// <summary>
        /// Creates the root media folder
        /// </summary>
        /// <returns>AggregateFolder.</returns>
        /// <exception cref="System.InvalidOperationException">Cannot create the root folder until plugins have loaded</exception>
        public AggregateFolder CreateRootFolder()
        {
            var rootFolderPath = ConfigurationManager.ApplicationPaths.RootFolderPath;

            _fileSystem.CreateDirectory(rootFolderPath);

            var rootFolder = GetItemById(GetNewItemId(rootFolderPath, typeof(AggregateFolder))) as AggregateFolder ?? (AggregateFolder)ResolvePath(_fileSystem.GetDirectoryInfo(rootFolderPath));

            // Add in the plug-in folders
            foreach (var child in PluginFolderCreators)
            {
                var folder = child.GetFolder();

                if (folder != null)
                {
                    if (folder.Id == Guid.Empty)
                    {
                        if (string.IsNullOrWhiteSpace(folder.Path))
                        {
                            folder.Id = GetNewItemId(folder.GetType().Name, folder.GetType());
                        }
                        else
                        {
                            folder.Id = GetNewItemId(folder.Path, folder.GetType());
                        }
                    }

                    var dbItem = GetItemById(folder.Id) as BasePluginFolder;

                    if (dbItem != null && string.Equals(dbItem.Path, folder.Path, StringComparison.OrdinalIgnoreCase))
                    {
                        folder = dbItem;
                    }

                    if (folder.ParentId != rootFolder.Id)
                    {
                        folder.ParentId = rootFolder.Id;
                        var task = folder.UpdateToRepository(ItemUpdateType.MetadataImport, CancellationToken.None);
                        Task.WaitAll(task);
                    }

                    rootFolder.AddVirtualChild(folder);

                    RegisterItem(folder);
                }
            }

            return rootFolder;
        }

        private volatile UserRootFolder _userRootFolder;
        private readonly object _syncLock = new object();
        public Folder GetUserRootFolder()
        {
            if (_userRootFolder == null)
            {
                lock (_syncLock)
                {
                    if (_userRootFolder == null)
                    {
                        var userRootPath = ConfigurationManager.ApplicationPaths.DefaultUserViewsPath;

                        _fileSystem.CreateDirectory(userRootPath);

                        var tmpItem = GetItemById(GetNewItemId(userRootPath, typeof(UserRootFolder))) as UserRootFolder;

                        if (tmpItem == null)
                        {
                            tmpItem = (UserRootFolder)ResolvePath(_fileSystem.GetDirectoryInfo(userRootPath));
                        }

                        _userRootFolder = tmpItem;
                    }
                }
            }

            return _userRootFolder;
        }

        public BaseItem FindByPath(string path, bool? isFolder)
        {
            // If this returns multiple items it could be tricky figuring out which one is correct. 
            // In most cases, the newest one will be and the others obsolete but not yet cleaned up

            var query = new InternalItemsQuery
            {
                Path = path,
                IsFolder = isFolder,
                SortBy = new[] { ItemSortBy.DateCreated },
                SortOrder = SortOrder.Descending,
                Limit = 1
            };

            return GetItemList(query)
                .FirstOrDefault();
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
        /// Gets the artists path.
        /// </summary>
        /// <value>The artists path.</value>
        public string ArtistsPath
        {
            get
            {
                return Path.Combine(ConfigurationManager.ApplicationPaths.ItemsByNamePath, "artists");
            }
        }

        /// <summary>
        /// Gets a Genre
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Genre}.</returns>
        public MusicArtist GetArtist(string name)
        {
            return GetItemByName<MusicArtist>(ArtistsPath, name);
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

            // Trim the period at the end because windows will have a hard time with that
            var validFilename = _fileSystem.GetValidFilename(name)
                .Trim()
                .TrimEnd('.');

            string subFolderPrefix = null;

            var type = typeof(T);

            if (type == typeof(Person))
            {
                var subFolderIndex = 0;

                while (!char.IsLetterOrDigit(validFilename[subFolderIndex]))
                {
                    subFolderIndex++;
                }

                subFolderPrefix = validFilename.Substring(subFolderIndex, 1);
            }

            var fullPath = string.IsNullOrEmpty(subFolderPrefix) ?
                Path.Combine(path, validFilename) :
                Path.Combine(path, subFolderPrefix, validFilename);

            return CreateItemByName<T>(fullPath, name);
        }

        private T CreateItemByName<T>(string path, string name)
            where T : BaseItem, new()
        {
            if (typeof(T) == typeof(MusicArtist))
            {
                var existing = GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { typeof(T).Name },
                    Name = name

                }).Cast<MusicArtist>()
                .OrderBy(i => i.IsAccessedByName ? 1 : 0)
                .Cast<T>()
                .FirstOrDefault();

                if (existing != null)
                {
                    return existing;
                }
            }

            var id = GetNewItemId(path, typeof(T));

            var item = GetItemById(id) as T;

            if (item == null)
            {
                item = new T
                {
                    Name = name,
                    Id = id,
                    DateCreated = DateTime.UtcNow,
                    DateModified = DateTime.UtcNow,
                    Path = path
                };

                var task = CreateItem(item, CancellationToken.None);
                Task.WaitAll(task);
            }

            return item;
        }

        public IEnumerable<MusicArtist> GetAlbumArtists(IEnumerable<IHasAlbumArtist> items)
        {
            var names = items
                .SelectMany(i => i.AlbumArtists)
                .DistinctNames()
                .Select(i =>
                {
                    try
                    {
                        var artist = GetArtist(i);

                        return artist;
                    }
                    catch
                    {
                        // Already logged at lower levels
                        return null;
                    }
                })
                .Where(i => i != null);

            return names;
        }

        public IEnumerable<MusicArtist> GetArtists(IEnumerable<IHasArtist> items)
        {
            var names = items
                .SelectMany(i => i.AllArtists)
                .DistinctNames()
                .Select(i =>
                {
                    try
                    {
                        var artist = GetArtist(i);

                        return artist;
                    }
                    catch
                    {
                        // Already logged at lower levels
                        return null;
                    }
                })
                .Where(i => i != null);

            return names;
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
            _fileSystem.CreateDirectory(ConfigurationManager.ApplicationPaths.PeoplePath);

            return new PeopleValidator(this, _logger, ConfigurationManager, _fileSystem).ValidatePeople(cancellationToken, progress);
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
            IsScanRunning = true;
            _libraryMonitorFactory().Stop();

            try
            {
                await PerformLibraryValidation(progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _libraryMonitorFactory().Start();
                IsScanRunning = false;
            }
        }

        private async Task PerformLibraryValidation(IProgress<double> progress, CancellationToken cancellationToken)
        {
            _logger.Info("Validating media library");

            await RootFolder.RefreshMetadata(cancellationToken).ConfigureAwait(false);

            progress.Report(.5);

            // Start by just validating the children of the root, but go no further
            await RootFolder.ValidateChildren(new Progress<double>(), cancellationToken, new MetadataRefreshOptions(_fileSystem), recursive: false);

            progress.Report(1);

            var userRoot = GetUserRootFolder();

            await userRoot.RefreshMetadata(cancellationToken).ConfigureAwait(false);

            await userRoot.ValidateChildren(new Progress<double>(), cancellationToken, new MetadataRefreshOptions(_fileSystem), recursive: false).ConfigureAwait(false);
            progress.Report(2);

            var innerProgress = new ActionableProgress<double>();

            innerProgress.RegisterAction(pct => progress.Report(2 + pct * .73));

            // Now validate the entire media library
            await RootFolder.ValidateChildren(innerProgress, cancellationToken, new MetadataRefreshOptions(_fileSystem), recursive: true).ConfigureAwait(false);

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
                    double innerPercent = currentNumComplete * 100 + pct;
                    innerPercent /= numTasks;
                    progress.Report(innerPercent);
                });

                _logger.Debug("Running post-scan task {0}", task.GetType().Name);

                try
                {
                    await task.Run(innerProgress, cancellationToken).ConfigureAwait(false);
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
        public IEnumerable<VirtualFolderInfo> GetVirtualFolders()
        {
            return GetView(ConfigurationManager.ApplicationPaths.DefaultUserViewsPath);
        }

        /// <summary>
        /// Gets the view.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>IEnumerable{VirtualFolderInfo}.</returns>
        private IEnumerable<VirtualFolderInfo> GetView(string path)
        {
            var topLibraryFolders = GetUserRootFolder().Children.ToList();

            return _fileSystem.GetDirectoryPaths(path)
                .Select(dir => GetVirtualFolderInfo(dir, topLibraryFolders));
        }

        private VirtualFolderInfo GetVirtualFolderInfo(string dir, List<BaseItem> collectionFolders)
        {
            var info = new VirtualFolderInfo
            {
                Name = Path.GetFileName(dir),

                Locations = Directory.EnumerateFiles(dir, "*.mblink", SearchOption.TopDirectoryOnly)
                    .Select(_fileSystem.ResolveShortcut)
                    .OrderBy(i => i)
                    .ToList(),

                CollectionType = GetCollectionType(dir)
            };

            var libraryFolder = collectionFolders.FirstOrDefault(i => string.Equals(i.Path, dir, StringComparison.OrdinalIgnoreCase));

            if (libraryFolder != null && libraryFolder.HasImage(ImageType.Primary))
            {
                info.PrimaryImageItemId = libraryFolder.Id.ToString("N");
            }

            if (libraryFolder != null)
            {
                info.ItemId = libraryFolder.Id.ToString("N");
            }

            return info;
        }

        private string GetCollectionType(string path)
        {
            return _fileSystem.GetFiles(path, false)
                .Where(i => string.Equals(i.Extension, ".collection", StringComparison.OrdinalIgnoreCase))
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

            //_logger.Debug("GetitemById {0}", id);

            if (item != null)
            {
                RegisterItem(item);
            }

            return item;
        }

        public IEnumerable<BaseItem> GetItemList(InternalItemsQuery query)
        {
            if (query.Recursive && query.ParentId.HasValue)
            {
                var parent = GetItemById(query.ParentId.Value);
                if (parent != null)
                {
                    SetTopParentIdsOrAncestors(query, new List<BaseItem> { parent });
                    query.ParentId = null;
                }
            }

            if (query.User != null)
            {
                AddUserToQuery(query, query.User);
            }

            return ItemRepository.GetItemList(query);
        }

        public IEnumerable<BaseItem> GetItemList(InternalItemsQuery query, IEnumerable<string> parentIds)
        {
            var parents = parentIds.Select(i => GetItemById(new Guid(i))).Where(i => i != null).ToList();

            SetTopParentIdsOrAncestors(query, parents);

            if (query.AncestorIds.Length == 0 && query.TopParentIds.Length == 0)
            {
                if (query.User != null)
                {
                    AddUserToQuery(query, query.User);
                }
            }

            return ItemRepository.GetItemList(query);
        }

        public QueryResult<BaseItem> QueryItems(InternalItemsQuery query)
        {
            if (query.User != null)
            {
                AddUserToQuery(query, query.User);
            }

            if (query.EnableTotalRecordCount)
            {
                return ItemRepository.GetItems(query);
            }

            return new QueryResult<BaseItem>
            {
                Items = ItemRepository.GetItemList(query).ToArray()
            };
        }

        public List<Guid> GetItemIds(InternalItemsQuery query)
        {
            if (query.User != null)
            {
                AddUserToQuery(query, query.User);
            }

            return ItemRepository.GetItemIdsList(query);
        }

        public QueryResult<Tuple<BaseItem, ItemCounts>> GetStudios(InternalItemsQuery query)
        {
            if (query.User != null)
            {
                AddUserToQuery(query, query.User);
            }

            SetTopParentOrAncestorIds(query);
            return ItemRepository.GetStudios(query);
        }

        public QueryResult<Tuple<BaseItem, ItemCounts>> GetGenres(InternalItemsQuery query)
        {
            if (query.User != null)
            {
                AddUserToQuery(query, query.User);
            }

            SetTopParentOrAncestorIds(query);
            return ItemRepository.GetGenres(query);
        }

        public QueryResult<Tuple<BaseItem, ItemCounts>> GetGameGenres(InternalItemsQuery query)
        {
            if (query.User != null)
            {
                AddUserToQuery(query, query.User);
            }

            SetTopParentOrAncestorIds(query);
            return ItemRepository.GetGameGenres(query);
        }

        public QueryResult<Tuple<BaseItem, ItemCounts>> GetMusicGenres(InternalItemsQuery query)
        {
            if (query.User != null)
            {
                AddUserToQuery(query, query.User);
            }

            SetTopParentOrAncestorIds(query);
            return ItemRepository.GetMusicGenres(query);
        }

        public QueryResult<Tuple<BaseItem, ItemCounts>> GetArtists(InternalItemsQuery query)
        {
            if (query.User != null)
            {
                AddUserToQuery(query, query.User);
            }

            SetTopParentOrAncestorIds(query);
            return ItemRepository.GetArtists(query);
        }

        private void SetTopParentOrAncestorIds(InternalItemsQuery query)
        {
            if (query.AncestorIds.Length == 0)
            {
                return;
            }

            var parents = query.AncestorIds.Select(i => GetItemById(new Guid(i))).ToList();

            if (parents.All(i =>
            {
                if (i is ICollectionFolder || i is UserView)
                {
                    return true;
                }

                //_logger.Debug("Query requires ancestor query due to type: " + i.GetType().Name);
                return false;

            }))
            {
                // Optimize by querying against top level views
                query.TopParentIds = parents.SelectMany(i => GetTopParentsForQuery(i, query.User)).Select(i => i.Id.ToString("N")).ToArray();
                query.AncestorIds = new string[] { };
            }
        }

        public QueryResult<Tuple<BaseItem, ItemCounts>> GetAlbumArtists(InternalItemsQuery query)
        {
            if (query.User != null)
            {
                AddUserToQuery(query, query.User);
            }

            SetTopParentOrAncestorIds(query);
            return ItemRepository.GetAlbumArtists(query);
        }

        public QueryResult<BaseItem> GetItemsResult(InternalItemsQuery query)
        {
            if (query.Recursive && query.ParentId.HasValue)
            {
                var parent = GetItemById(query.ParentId.Value);
                if (parent != null)
                {
                    SetTopParentIdsOrAncestors(query, new List<BaseItem> { parent });
                    query.ParentId = null;
                }
            }

            if (query.User != null)
            {
                AddUserToQuery(query, query.User);
            }

            if (query.EnableTotalRecordCount)
            {
                return ItemRepository.GetItems(query);
            }

            return new QueryResult<BaseItem>
            {
                Items = ItemRepository.GetItemList(query).ToArray()
            };
        }

        private void SetTopParentIdsOrAncestors(InternalItemsQuery query, List<BaseItem> parents)
        {
            if (parents.All(i =>
            {
                if (i is ICollectionFolder || i is UserView)
                {
                    return true;
                }

                //_logger.Debug("Query requires ancestor query due to type: " + i.GetType().Name);
                return false;

            }))
            {
                // Optimize by querying against top level views
                query.TopParentIds = parents.SelectMany(i => GetTopParentsForQuery(i, query.User)).Select(i => i.Id.ToString("N")).ToArray();
            }
            else
            {
                // We need to be able to query from any arbitrary ancestor up the tree
                query.AncestorIds = parents.SelectMany(i => i.GetIdsForAncestorQuery()).Select(i => i.ToString("N")).ToArray();
            }
        }

        private void AddUserToQuery(InternalItemsQuery query, User user)
        {
            if (query.AncestorIds.Length == 0 && !query.ParentId.HasValue && query.ChannelIds.Length == 0 && query.TopParentIds.Length == 0 && string.IsNullOrWhiteSpace(query.AncestorWithPresentationUniqueKey))
            {
                var userViews = _userviewManager().GetUserViews(new UserViewQuery
                {
                    UserId = user.Id.ToString("N"),
                    IncludeHidden = true

                }, CancellationToken.None).Result.ToList();

                query.TopParentIds = userViews.SelectMany(i => GetTopParentsForQuery(i, user)).Select(i => i.Id.ToString("N")).ToArray();
            }
        }

        private IEnumerable<BaseItem> GetTopParentsForQuery(BaseItem item, User user)
        {
            var view = item as UserView;

            if (view != null)
            {
                if (string.Equals(view.ViewType, CollectionType.LiveTv))
                {
                    return new[] { view };
                }
                if (string.Equals(view.ViewType, CollectionType.Channels))
                {
                    var channelResult = BaseItem.ChannelManager.GetChannelsInternal(new ChannelQuery
                    {
                        UserId = user.Id.ToString("N")

                    }, CancellationToken.None).Result;

                    return channelResult.Items;
                }

                // Translate view into folders
                if (view.DisplayParentId != Guid.Empty)
                {
                    var displayParent = GetItemById(view.DisplayParentId);
                    if (displayParent != null)
                    {
                        return GetTopParentsForQuery(displayParent, user);
                    }
                    return new BaseItem[] { };
                }
                if (view.ParentId != Guid.Empty)
                {
                    var displayParent = GetItemById(view.ParentId);
                    if (displayParent != null)
                    {
                        return GetTopParentsForQuery(displayParent, user);
                    }
                    return new BaseItem[] { };
                }

                // Handle grouping
                if (user != null && !string.IsNullOrWhiteSpace(view.ViewType) && UserView.IsEligibleForGrouping(view.ViewType))
                {
                    return user.RootFolder
                        .GetChildren(user, true)
                        .OfType<CollectionFolder>()
                        .Where(i => string.IsNullOrWhiteSpace(i.CollectionType) || string.Equals(i.CollectionType, view.ViewType, StringComparison.OrdinalIgnoreCase))
                        .Where(i => user.IsFolderGrouped(i.Id))
                        .SelectMany(i => GetTopParentsForQuery(i, user));
                }
                return new BaseItem[] { };
            }

            var collectionFolder = item as CollectionFolder;
            if (collectionFolder != null)
            {
                return collectionFolder.GetPhysicalParents();
            }

            var topParent = item.GetTopParent();
            if (topParent != null)
            {
                return new[] { topParent };
            }
            return new BaseItem[] { };
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
                .OrderBy(i => i.GetType().Name.IndexOf("Default", StringComparison.OrdinalIgnoreCase) == -1 ? 0 : 1)
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
                        else
                        {
                            return null;
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

        public IEnumerable<Folder> GetCollectionFolders(BaseItem item)
        {
            while (!(item.GetParent() is AggregateFolder) && item.GetParent() != null)
            {
                item = item.GetParent();
            }

            if (item == null)
            {
                return new List<Folder>();
            }

            return GetUserRootFolder().Children
                .OfType<Folder>()
                .Where(i => string.Equals(i.Path, item.Path, StringComparison.OrdinalIgnoreCase) || i.PhysicalLocations.Contains(item.Path, StringComparer.OrdinalIgnoreCase));
        }

        public string GetContentType(BaseItem item)
        {
            string configuredContentType = GetConfiguredContentType(item, false);
            if (!string.IsNullOrWhiteSpace(configuredContentType))
            {
                return configuredContentType;
            }
            configuredContentType = GetConfiguredContentType(item, true);
            if (!string.IsNullOrWhiteSpace(configuredContentType))
            {
                return configuredContentType;
            }
            return GetInheritedContentType(item);
        }

        public string GetInheritedContentType(BaseItem item)
        {
            var type = GetTopFolderContentType(item);

            if (!string.IsNullOrWhiteSpace(type))
            {
                return type;
            }

            return item.GetParents()
                .Select(GetConfiguredContentType)
                .LastOrDefault(i => !string.IsNullOrWhiteSpace(i));
        }

        public string GetConfiguredContentType(BaseItem item)
        {
            return GetConfiguredContentType(item, false);
        }

        public string GetConfiguredContentType(string path)
        {
            return GetContentTypeOverride(path, false);
        }

        public string GetConfiguredContentType(BaseItem item, bool inheritConfiguredPath)
        {
            ICollectionFolder collectionFolder = item as ICollectionFolder;
            if (collectionFolder != null)
            {
                return collectionFolder.CollectionType;
            }
            return GetContentTypeOverride(item.ContainingFolderPath, inheritConfiguredPath);
        }

        private string GetContentTypeOverride(string path, bool inherit)
        {
            var nameValuePair = ConfigurationManager.Configuration.ContentTypes.FirstOrDefault(i => string.Equals(i.Name, path, StringComparison.OrdinalIgnoreCase) || (inherit && !string.IsNullOrWhiteSpace(i.Name) && _fileSystem.ContainsSubPath(i.Name, path)));
            if (nameValuePair != null)
            {
                return nameValuePair.Value;
            }
            return null;
        }

        private string GetTopFolderContentType(BaseItem item)
        {
            if (item == null)
            {
                return null;
            }

            while (!(item.GetParent() is AggregateFolder) && item.GetParent() != null)
            {
                item = item.GetParent();
            }

            return GetUserRootFolder().Children
                .OfType<ICollectionFolder>()
                .Where(i => string.Equals(i.Path, item.Path, StringComparison.OrdinalIgnoreCase) || i.PhysicalLocations.Contains(item.Path))
                .Select(i => i.CollectionType)
                .FirstOrDefault(i => !string.IsNullOrWhiteSpace(i));
        }

        private readonly TimeSpan _viewRefreshInterval = TimeSpan.FromHours(24);
        //private readonly TimeSpan _viewRefreshInterval = TimeSpan.FromMinutes(1);

        public Task<UserView> GetNamedView(User user,
            string name,
            string viewType,
            string sortName,
            CancellationToken cancellationToken)
        {
            return GetNamedView(user, name, null, viewType, sortName, cancellationToken);
        }

        public async Task<UserView> GetNamedView(string name,
            string viewType,
            string sortName,
            CancellationToken cancellationToken)
        {
            var path = Path.Combine(ConfigurationManager.ApplicationPaths.ItemsByNamePath, "views");

            path = Path.Combine(path, _fileSystem.GetValidFilename(viewType));

            var id = GetNewItemId(path + "_namedview_" + name, typeof(UserView));

            var item = GetItemById(id) as UserView;

            var refresh = false;

            if (item == null || !string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase))
            {
                _fileSystem.CreateDirectory(path);

                item = new UserView
                {
                    Path = path,
                    Id = id,
                    DateCreated = DateTime.UtcNow,
                    Name = name,
                    ViewType = viewType,
                    ForcedSortName = sortName
                };

                await CreateItem(item, cancellationToken).ConfigureAwait(false);

                refresh = true;
            }

            if (!refresh)
            {
                refresh = DateTime.UtcNow - item.DateLastRefreshed >= _viewRefreshInterval;
            }

            if (!refresh && item.DisplayParentId != Guid.Empty)
            {
                var displayParent = GetItemById(item.DisplayParentId);
                refresh = displayParent != null && displayParent.DateLastSaved > item.DateLastRefreshed;
            }

            if (refresh)
            {
                await item.UpdateToRepository(ItemUpdateType.MetadataImport, CancellationToken.None).ConfigureAwait(false);
                _providerManagerFactory().QueueRefresh(item.Id, new MetadataRefreshOptions(_fileSystem)
                {
                    // Not sure why this is necessary but need to figure it out
                    // View images are not getting utilized without this
                    ForceSave = true
                });
            }

            return item;
        }

        public async Task<UserView> GetNamedView(User user,
            string name,
            string parentId,
            string viewType,
            string sortName,
            CancellationToken cancellationToken)
        {
            var idValues = "38_namedview_" + name + user.Id.ToString("N") + (parentId ?? string.Empty) + (viewType ?? string.Empty);

            var id = GetNewItemId(idValues, typeof(UserView));

            var path = Path.Combine(ConfigurationManager.ApplicationPaths.InternalMetadataPath, "views", id.ToString("N"));

            var item = GetItemById(id) as UserView;

            var isNew = false;

            if (item == null)
            {
                _fileSystem.CreateDirectory(path);

                item = new UserView
                {
                    Path = path,
                    Id = id,
                    DateCreated = DateTime.UtcNow,
                    Name = name,
                    ViewType = viewType,
                    ForcedSortName = sortName,
                    UserId = user.Id
                };

                if (!string.IsNullOrWhiteSpace(parentId))
                {
                    item.DisplayParentId = new Guid(parentId);
                }

                await CreateItem(item, cancellationToken).ConfigureAwait(false);

                isNew = true;
            }

            var refresh = isNew || DateTime.UtcNow - item.DateLastRefreshed >= _viewRefreshInterval;

            if (!refresh && item.DisplayParentId != Guid.Empty)
            {
                var displayParent = GetItemById(item.DisplayParentId);
                refresh = displayParent != null && displayParent.DateLastSaved > item.DateLastRefreshed;
            }

            if (refresh)
            {
                _providerManagerFactory().QueueRefresh(item.Id, new MetadataRefreshOptions(_fileSystem)
                {
                    // Need to force save to increment DateLastSaved
                    ForceSave = true
                });
            }

            return item;
        }

        public async Task<UserView> GetShadowView(BaseItem parent,
        string viewType,
        string sortName,
        CancellationToken cancellationToken)
        {
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            var name = parent.Name;
            var parentId = parent.Id;

            var idValues = "38_namedview_" + name + parentId + (viewType ?? string.Empty);

            var id = GetNewItemId(idValues, typeof(UserView));

            var path = parent.Path;

            var item = GetItemById(id) as UserView;

            var isNew = false;

            if (item == null)
            {
                _fileSystem.CreateDirectory(path);

                item = new UserView
                {
                    Path = path,
                    Id = id,
                    DateCreated = DateTime.UtcNow,
                    Name = name,
                    ViewType = viewType,
                    ForcedSortName = sortName
                };

                item.DisplayParentId = parentId;

                await CreateItem(item, cancellationToken).ConfigureAwait(false);

                isNew = true;
            }

            var refresh = isNew || DateTime.UtcNow - item.DateLastRefreshed >= _viewRefreshInterval;

            if (!refresh && item.DisplayParentId != Guid.Empty)
            {
                var displayParent = GetItemById(item.DisplayParentId);
                refresh = displayParent != null && displayParent.DateLastSaved > item.DateLastRefreshed;
            }

            if (refresh)
            {
                _providerManagerFactory().QueueRefresh(item.Id, new MetadataRefreshOptions(_fileSystem)
                {
                    // Need to force save to increment DateLastSaved
                    ForceSave = true
                });
            }

            return item;
        }

        public async Task<UserView> GetNamedView(string name,
            string parentId,
            string viewType,
            string sortName,
            string uniqueId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            var idValues = "37_namedview_" + name + (parentId ?? string.Empty) + (viewType ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(uniqueId))
            {
                idValues += uniqueId;
            }

            var id = GetNewItemId(idValues, typeof(UserView));

            var path = Path.Combine(ConfigurationManager.ApplicationPaths.InternalMetadataPath, "views", id.ToString("N"));

            var item = GetItemById(id) as UserView;

            var isNew = false;

            if (item == null)
            {
                _fileSystem.CreateDirectory(path);

                item = new UserView
                {
                    Path = path,
                    Id = id,
                    DateCreated = DateTime.UtcNow,
                    Name = name,
                    ViewType = viewType,
                    ForcedSortName = sortName
                };

                if (!string.IsNullOrWhiteSpace(parentId))
                {
                    item.DisplayParentId = new Guid(parentId);
                }

                await CreateItem(item, cancellationToken).ConfigureAwait(false);

                isNew = true;
            }

            if (!string.Equals(viewType, item.ViewType, StringComparison.OrdinalIgnoreCase))
            {
                item.ViewType = viewType;
                await item.UpdateToRepository(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
            }

            var refresh = isNew || DateTime.UtcNow - item.DateLastRefreshed >= _viewRefreshInterval;

            if (!refresh && item.DisplayParentId != Guid.Empty)
            {
                var displayParent = GetItemById(item.DisplayParentId);
                refresh = displayParent != null && displayParent.DateLastSaved > item.DateLastRefreshed;
            }

            if (refresh)
            {
                _providerManagerFactory().QueueRefresh(item.Id, new MetadataRefreshOptions(_fileSystem)
                {
                    // Need to force save to increment DateLastSaved
                    ForceSave = true
                });
            }

            return item;
        }

        public bool IsVideoFile(string path)
        {
            var resolver = new VideoResolver(GetNamingOptions(), new PatternsLogger());
            return resolver.IsVideoFile(path);
        }

        public bool IsAudioFile(string path)
        {
            var parser = new AudioFileParser(GetNamingOptions());
            return parser.IsAudioFile(path);
        }

        public int? GetSeasonNumberFromPath(string path)
        {
            return new SeasonPathParser(GetNamingOptions(), new RegexProvider()).Parse(path, true, true).SeasonNumber;
        }

        public bool FillMissingEpisodeNumbersFromPath(Episode episode)
        {
            var resolver = new EpisodeResolver(GetNamingOptions(),
                new PatternsLogger());

            var isFolder = episode.VideoType == VideoType.BluRay || episode.VideoType == VideoType.Dvd ||
                           episode.VideoType == VideoType.HdDvd;

            var locationType = episode.LocationType;

            var episodeInfo = locationType == LocationType.FileSystem || locationType == LocationType.Offline ?
                resolver.Resolve(episode.Path, isFolder) :
                new Naming.TV.EpisodeInfo();

            if (episodeInfo == null)
            {
                episodeInfo = new Naming.TV.EpisodeInfo();
            }

            var changed = false;

            if (episodeInfo.IsByDate)
            {
                if (episode.IndexNumber.HasValue)
                {
                    episode.IndexNumber = null;
                    changed = true;
                }

                if (episode.IndexNumberEnd.HasValue)
                {
                    episode.IndexNumberEnd = null;
                    changed = true;
                }

                if (!episode.PremiereDate.HasValue)
                {
                    if (episodeInfo.Year.HasValue && episodeInfo.Month.HasValue && episodeInfo.Day.HasValue)
                    {
                        episode.PremiereDate = new DateTime(episodeInfo.Year.Value, episodeInfo.Month.Value, episodeInfo.Day.Value).ToUniversalTime();
                    }

                    if (episode.PremiereDate.HasValue)
                    {
                        changed = true;
                    }
                }

                if (!episode.ProductionYear.HasValue)
                {
                    episode.ProductionYear = episodeInfo.Year;

                    if (episode.ProductionYear.HasValue)
                    {
                        changed = true;
                    }
                }

                if (!episode.ParentIndexNumber.HasValue)
                {
                    var season = episode.Season;

                    if (season != null)
                    {
                        episode.ParentIndexNumber = season.IndexNumber;
                    }

                    if (episode.ParentIndexNumber.HasValue)
                    {
                        changed = true;
                    }
                }
            }
            else
            {
                if (!episode.IndexNumber.HasValue)
                {
                    episode.IndexNumber = episodeInfo.EpisodeNumber;

                    if (episode.IndexNumber.HasValue)
                    {
                        changed = true;
                    }
                }

                if (!episode.IndexNumberEnd.HasValue)
                {
                    episode.IndexNumberEnd = episodeInfo.EndingEpsiodeNumber;

                    if (episode.IndexNumberEnd.HasValue)
                    {
                        changed = true;
                    }
                }

                if (!episode.ParentIndexNumber.HasValue)
                {
                    episode.ParentIndexNumber = episodeInfo.SeasonNumber;

                    if (!episode.ParentIndexNumber.HasValue)
                    {
                        var season = episode.Season;

                        if (season != null)
                        {
                            episode.ParentIndexNumber = season.IndexNumber;
                        }
                    }

                    if (episode.ParentIndexNumber.HasValue)
                    {
                        changed = true;
                    }
                }
            }

            return changed;
        }

        public NamingOptions GetNamingOptions()
        {
            var options = new ExtendedNamingOptions();

            // These cause apps to have problems
            options.AudioFileExtensions.Remove(".m3u");
            options.AudioFileExtensions.Remove(".wpl");

            if (!ConfigurationManager.Configuration.EnableAudioArchiveFiles)
            {
                options.AudioFileExtensions.Remove(".rar");
                options.AudioFileExtensions.Remove(".zip");
            }

            if (!ConfigurationManager.Configuration.EnableVideoArchiveFiles)
            {
                options.VideoFileExtensions.Remove(".rar");
                options.VideoFileExtensions.Remove(".zip");
            }

            return options;
        }

        public ItemLookupInfo ParseName(string name)
        {
            var resolver = new VideoResolver(GetNamingOptions(), new PatternsLogger());

            var result = resolver.CleanDateTime(name);
            var cleanName = resolver.CleanString(result.Name);

            return new ItemLookupInfo
            {
                Name = cleanName.Name,
                Year = result.Year
            };
        }

        public IEnumerable<Video> FindTrailers(BaseItem owner, List<FileSystemMetadata> fileSystemChildren, IDirectoryService directoryService)
        {
            var files = owner.IsInMixedFolder ? new List<FileSystemMetadata>() : fileSystemChildren.Where(i => i.IsDirectory)
                .Where(i => string.Equals(i.Name, BaseItem.TrailerFolderName, StringComparison.OrdinalIgnoreCase))
                .SelectMany(i => _fileSystem.GetFiles(i.FullName, false))
                .ToList();

            var videoListResolver = new VideoListResolver(GetNamingOptions(), new PatternsLogger());

            var videos = videoListResolver.Resolve(fileSystemChildren.Select(i => new FileMetadata
            {
                Id = i.FullName,
                IsFolder = (i.Attributes & FileAttributes.Directory) == FileAttributes.Directory

            }).ToList());

            var currentVideo = videos.FirstOrDefault(i => string.Equals(owner.Path, i.Files.First().Path, StringComparison.OrdinalIgnoreCase));

            if (currentVideo != null)
            {
                files.AddRange(currentVideo.Extras.Where(i => string.Equals(i.ExtraType, "trailer", StringComparison.OrdinalIgnoreCase)).Select(i => _fileSystem.GetFileInfo(i.Path)));
            }

            var resolvers = new IItemResolver[]
            {
                new GenericVideoResolver<Trailer>(this)
            };

            return ResolvePaths(files, directoryService, null, null, resolvers)
                .OfType<Trailer>()
                .Select(video =>
                {
                    // Try to retrieve it from the db. If we don't find it, use the resolved version
                    var dbItem = GetItemById(video.Id) as Trailer;

                    if (dbItem != null)
                    {
                        video = dbItem;
                    }

                    video.ExtraType = ExtraType.Trailer;
                    video.TrailerTypes = new List<TrailerType> { TrailerType.LocalTrailer };

                    return video;

                    // Sort them so that the list can be easily compared for changes
                }).OrderBy(i => i.Path).ToList();
        }

        public IEnumerable<Video> FindExtras(BaseItem owner, List<FileSystemMetadata> fileSystemChildren, IDirectoryService directoryService)
        {
            var files = fileSystemChildren.Where(i => i.IsDirectory)
                .Where(i => string.Equals(i.Name, "extras", StringComparison.OrdinalIgnoreCase) || string.Equals(i.Name, "specials", StringComparison.OrdinalIgnoreCase))
                .SelectMany(i => _fileSystem.GetFiles(i.FullName, false))
                .ToList();

            var videoListResolver = new VideoListResolver(GetNamingOptions(), new PatternsLogger());

            var videos = videoListResolver.Resolve(fileSystemChildren.Select(i => new FileMetadata
            {
                Id = i.FullName,
                IsFolder = (i.Attributes & FileAttributes.Directory) == FileAttributes.Directory

            }).ToList());

            var currentVideo = videos.FirstOrDefault(i => string.Equals(owner.Path, i.Files.First().Path, StringComparison.OrdinalIgnoreCase));

            if (currentVideo != null)
            {
                files.AddRange(currentVideo.Extras.Where(i => !string.Equals(i.ExtraType, "trailer", StringComparison.OrdinalIgnoreCase)).Select(i => _fileSystem.GetFileInfo(i.Path)));
            }

            return ResolvePaths(files, directoryService, null, null)
                .OfType<Video>()
                .Select(video =>
                {
                    // Try to retrieve it from the db. If we don't find it, use the resolved version
                    var dbItem = GetItemById(video.Id) as Video;

                    if (dbItem != null)
                    {
                        video = dbItem;
                    }

                    SetExtraTypeFromFilename(video);

                    return video;

                    // Sort them so that the list can be easily compared for changes
                }).OrderBy(i => i.Path).ToList();
        }

        public string SubstitutePath(string path, string from, string to)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException("path");
            }
            if (string.IsNullOrWhiteSpace(from))
            {
                throw new ArgumentNullException("from");
            }
            if (string.IsNullOrWhiteSpace(to))
            {
                throw new ArgumentNullException("to");
            }

            var newPath = path.Replace(from, to, StringComparison.OrdinalIgnoreCase);

            if (!string.Equals(newPath, path))
            {
                if (to.IndexOf('/') != -1)
                {
                    newPath = newPath.Replace('\\', '/');
                }
                else
                {
                    newPath = newPath.Replace('/', '\\');
                }
            }

            return newPath;
        }

        private void SetExtraTypeFromFilename(Video item)
        {
            var resolver = new ExtraResolver(GetNamingOptions(), new PatternsLogger(), new RegexProvider());

            var result = resolver.GetExtraInfo(item.Path);

            if (string.Equals(result.ExtraType, "deletedscene", StringComparison.OrdinalIgnoreCase))
            {
                item.ExtraType = ExtraType.DeletedScene;
            }
            else if (string.Equals(result.ExtraType, "behindthescenes", StringComparison.OrdinalIgnoreCase))
            {
                item.ExtraType = ExtraType.BehindTheScenes;
            }
            else if (string.Equals(result.ExtraType, "interview", StringComparison.OrdinalIgnoreCase))
            {
                item.ExtraType = ExtraType.Interview;
            }
            else if (string.Equals(result.ExtraType, "scene", StringComparison.OrdinalIgnoreCase))
            {
                item.ExtraType = ExtraType.Scene;
            }
            else if (string.Equals(result.ExtraType, "sample", StringComparison.OrdinalIgnoreCase))
            {
                item.ExtraType = ExtraType.Sample;
            }
            else
            {
                item.ExtraType = ExtraType.Clip;
            }
        }

        public List<PersonInfo> GetPeople(InternalPeopleQuery query)
        {
            return ItemRepository.GetPeople(query);
        }

        public List<PersonInfo> GetPeople(BaseItem item)
        {
            if (item.SupportsPeople)
            {
                var people = GetPeople(new InternalPeopleQuery
                {
                    ItemId = item.Id
                });

                if (people.Count > 0)
                {
                    return people;
                }
            }

            return new List<PersonInfo>();
        }

        public List<Person> GetPeopleItems(InternalPeopleQuery query)
        {
            return ItemRepository.GetPeopleNames(query).Select(i =>
            {
                try
                {
                    return GetPerson(i);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error getting person", ex);
                    return null;
                }

            }).Where(i => i != null).ToList();
        }

        public List<string> GetPeopleNames(InternalPeopleQuery query)
        {
            return ItemRepository.GetPeopleNames(query);
        }

        public Task UpdatePeople(BaseItem item, List<PersonInfo> people)
        {
            if (!item.SupportsPeople)
            {
                return Task.FromResult(true);
            }

            return ItemRepository.UpdatePeople(item.Id, people);
        }

        private readonly SemaphoreSlim _dynamicImageResourcePool = new SemaphoreSlim(1, 1);
        public async Task<ItemImageInfo> ConvertImageToLocal(IHasImages item, ItemImageInfo image, int imageIndex)
        {
            foreach (var url in image.Path.Split('|'))
            {
                try
                {
                    _logger.Debug("ConvertImageToLocal item {0} - image url: {1}", item.Id, url);

                    await _providerManagerFactory().SaveImage(item, url, _dynamicImageResourcePool, image.Type, imageIndex, CancellationToken.None).ConfigureAwait(false);

                    var newImage = item.GetImageInfo(image.Type, imageIndex);

                    if (newImage != null)
                    {
                        newImage.IsPlaceholder = image.IsPlaceholder;
                    }

                    await item.UpdateToRepository(ItemUpdateType.ImageUpdate, CancellationToken.None).ConfigureAwait(false);

                    return item.GetImageInfo(image.Type, imageIndex);
                }
                catch (HttpException ex)
                {
                    if (ex.StatusCode.HasValue && ex.StatusCode.Value == HttpStatusCode.NotFound)
                    {
                        continue;
                    }
                    throw;
                }
            }

            // Remove this image to prevent it from retrying over and over
            item.RemoveImage(image);
            await item.UpdateToRepository(ItemUpdateType.ImageUpdate, CancellationToken.None).ConfigureAwait(false);

            throw new InvalidOperationException();
        }

        public void AddVirtualFolder(string name, string collectionType, string[] mediaPaths, bool refreshLibrary)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            name = _fileSystem.GetValidFilename(name);

            var rootFolderPath = ConfigurationManager.ApplicationPaths.DefaultUserViewsPath;

            var virtualFolderPath = Path.Combine(rootFolderPath, name);
            while (_fileSystem.DirectoryExists(virtualFolderPath))
            {
                name += "1";
                virtualFolderPath = Path.Combine(rootFolderPath, name);
            }

            if (mediaPaths != null)
            {
                var invalidpath = mediaPaths.FirstOrDefault(i => !_fileSystem.DirectoryExists(i));
                if (invalidpath != null)
                {
                    throw new ArgumentException("The specified path does not exist: " + invalidpath + ".");
                }
            }

            _libraryMonitorFactory().Stop();

            try
            {
                _fileSystem.CreateDirectory(virtualFolderPath);

                if (!string.IsNullOrEmpty(collectionType))
                {
                    var path = Path.Combine(virtualFolderPath, collectionType + ".collection");

                    using (File.Create(path))
                    {

                    }
                }

                if (mediaPaths != null)
                {
                    foreach (var path in mediaPaths)
                    {
                        AddMediaPath(name, path);
                    }
                }
            }
            finally
            {
                Task.Run(() =>
                {
                    // No need to start if scanning the library because it will handle it
                    if (refreshLibrary)
                    {
                        ValidateMediaLibrary(new Progress<double>(), CancellationToken.None);
                    }
                    else
                    {
                        // Need to add a delay here or directory watchers may still pick up the changes
                        var task = Task.Delay(1000);
                        // Have to block here to allow exceptions to bubble
                        Task.WaitAll(task);

                        _libraryMonitorFactory().Start();
                    }
                });
            }
        }

        public void RemoveVirtualFolder(string name, bool refreshLibrary)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            var rootFolderPath = ConfigurationManager.ApplicationPaths.DefaultUserViewsPath;

            var path = Path.Combine(rootFolderPath, name);

            if (!_fileSystem.DirectoryExists(path))
            {
                throw new DirectoryNotFoundException("The media folder does not exist");
            }

            _libraryMonitorFactory().Stop();

            try
            {
                _fileSystem.DeleteDirectory(path, true);
            }
            finally
            {
                Task.Run(() =>
                {
                    // No need to start if scanning the library because it will handle it
                    if (refreshLibrary)
                    {
                        ValidateMediaLibrary(new Progress<double>(), CancellationToken.None);
                    }
                    else
                    {
                        // Need to add a delay here or directory watchers may still pick up the changes
                        var task = Task.Delay(1000);
                        // Have to block here to allow exceptions to bubble
                        Task.WaitAll(task);

                        _libraryMonitorFactory().Start();
                    }
                });
            }
        }

        private const string ShortcutFileExtension = ".mblink";
        private const string ShortcutFileSearch = "*" + ShortcutFileExtension;
        public void AddMediaPath(string virtualFolderName, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException("path");
            }

            if (!_fileSystem.DirectoryExists(path))
            {
                throw new DirectoryNotFoundException("The path does not exist.");
            }

            var rootFolderPath = ConfigurationManager.ApplicationPaths.DefaultUserViewsPath;
            var virtualFolderPath = Path.Combine(rootFolderPath, virtualFolderName);

            var shortcutFilename = _fileSystem.GetFileNameWithoutExtension(path);

            var lnk = Path.Combine(virtualFolderPath, shortcutFilename + ShortcutFileExtension);

            while (_fileSystem.FileExists(lnk))
            {
                shortcutFilename += "1";
                lnk = Path.Combine(virtualFolderPath, shortcutFilename + ShortcutFileExtension);
            }

            _fileSystem.CreateShortcut(lnk, path);

            RemoveContentTypeOverrides(path);
        }

        private void RemoveContentTypeOverrides(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException("path");
            }

            var removeList = new List<NameValuePair>();

            foreach (var contentType in ConfigurationManager.Configuration.ContentTypes)
            {
                if (string.Equals(path, contentType.Name, StringComparison.OrdinalIgnoreCase)
                    || _fileSystem.ContainsSubPath(path, contentType.Name))
                {
                    removeList.Add(contentType);
                }
            }

            if (removeList.Count > 0)
            {
                ConfigurationManager.Configuration.ContentTypes = ConfigurationManager.Configuration.ContentTypes
                    .Except(removeList)
                        .ToArray();

                ConfigurationManager.SaveConfiguration();
            }
        }

        public void RemoveMediaPath(string virtualFolderName, string mediaPath)
        {
            if (string.IsNullOrWhiteSpace(mediaPath))
            {
                throw new ArgumentNullException("mediaPath");
            }

            var rootFolderPath = ConfigurationManager.ApplicationPaths.DefaultUserViewsPath;
            var path = Path.Combine(rootFolderPath, virtualFolderName);

            if (!_fileSystem.DirectoryExists(path))
            {
                throw new DirectoryNotFoundException(string.Format("The media collection {0} does not exist", virtualFolderName));
            }

            var shortcut = Directory.EnumerateFiles(path, ShortcutFileSearch, SearchOption.AllDirectories).FirstOrDefault(f => _fileSystem.ResolveShortcut(f).Equals(mediaPath, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(shortcut))
            {
                _fileSystem.DeleteFile(shortcut);
            }
        }
    }
}