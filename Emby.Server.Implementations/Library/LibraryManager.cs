using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Progress;
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
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Querying;
using Emby.Naming.Audio;
using Emby.Naming.Common;
using Emby.Naming.TV;
using Emby.Naming.Video;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Library.Resolvers;
using Emby.Server.Implementations.Library.Validators;
using Emby.Server.Implementations.ScheduledTasks;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Net;
using SortOrder = MediaBrowser.Model.Entities.SortOrder;
using VideoResolver = Emby.Naming.Video.VideoResolver;
using MediaBrowser.Common.Configuration;

using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Tasks;
using Emby.Server.Implementations.Playlists;
using MediaBrowser.Providers.MediaInfo;
using MediaBrowser.Controller;

namespace Emby.Server.Implementations.Library
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

        private readonly Func<ILibraryMonitor> _libraryMonitorFactory;
        private readonly Func<IProviderManager> _providerManagerFactory;
        private readonly Func<IUserViewManager> _userviewManager;
        public bool IsScanRunning { get; private set; }
        private IServerApplicationHost _appHost;

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
        public LibraryManager(IServerApplicationHost appHost, ILogger logger, ITaskManager taskManager, IUserManager userManager, IServerConfigurationManager configurationManager, IUserDataManager userDataRepository, Func<ILibraryMonitor> libraryMonitorFactory, IFileSystem fileSystem, Func<IProviderManager> providerManagerFactory, Func<IUserViewManager> userviewManager)
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
            _appHost = appHost;
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
            IEnumerable<IItemResolver> resolvers,
            IEnumerable<IIntroProvider> introProviders,
            IEnumerable<IBaseItemComparer> itemComparers,
            IEnumerable<ILibraryPostScanTask> postscanTasks)
        {
            EntityResolutionIgnoreRules = rules.ToArray();
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

        private bool _wizardCompleted;
        /// <summary>
        /// Records the configuration values.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        private void RecordConfigurationValues(ServerConfiguration configuration)
        {
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

            var wizardChanged = config.IsStartupWizardCompleted != _wizardCompleted;

            RecordConfigurationValues(config);

            if (wizardChanged)
            {
                _taskManager.CancelIfRunningAndQueue<RefreshMediaLibraryTask>();
            }
        }

        public void RegisterItem(BaseItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            if (item is IItemByName)
            {
                if (!(item is MusicArtist))
                {
                    return;
                }
            }

            else if (item.IsFolder)
            {
                //if (!(item is ICollectionFolder) && !(item is UserView) && !(item is Channel) && !(item is AggregateFolder))
                //{
                //    if (item.SourceType != SourceType.Library)
                //    {
                //        return;
                //    }
                //}
            }
            else
            {
                if (!(item is Video) && !(item is LiveTvChannel))
                {
                    return;
                }
            }

            LibraryItemsCache.AddOrUpdate(item.Id, item, delegate { return item; });
        }

        public void DeleteItem(BaseItem item, DeleteOptions options)
        {
            DeleteItem(item, options, false);
        }

        public void DeleteItem(BaseItem item, DeleteOptions options, bool notifyParentItem)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var parent = item.GetOwner() ?? item.GetParent();

            DeleteItem(item, options, parent, notifyParentItem);
        }

        public void DeleteItem(BaseItem item, DeleteOptions options, BaseItem parent, bool notifyParentItem)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (item.SourceType == SourceType.Channel)
            {
                if (options.DeleteFromExternalProvider)
                {
                    try
                    {
                        var task = BaseItem.ChannelManager.DeleteItem(item);
                        Task.WaitAll(task);
                    }
                    catch (ArgumentException)
                    {
                        // channel no longer installed
                    }
                }
                options.DeleteFileLocation = false;
            }

            if (item is LiveTvProgram)
            {
                _logger.LogDebug("Deleting item, Type: {0}, Name: {1}, Path: {2}, Id: {3}",
                    item.GetType().Name,
                    item.Name ?? "Unknown name",
                    item.Path ?? string.Empty,
                    item.Id);
            }
            else
            {
                _logger.LogInformation("Deleting item, Type: {0}, Name: {1}, Path: {2}, Id: {3}",
                    item.GetType().Name,
                    item.Name ?? "Unknown name",
                    item.Path ?? string.Empty,
                    item.Id);
            }

            var children = item.IsFolder
                ? ((Folder)item).GetRecursiveChildren(false).ToList()
                : new List<BaseItem>();

            foreach (var metadataPath in GetMetadataPaths(item, children))
            {
                _logger.LogDebug("Deleting path {0}", metadataPath);

                try
                {
                    _fileSystem.DeleteDirectory(metadataPath, true);
                }
                catch (IOException)
                {

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting {metadataPath}", metadataPath);
                }
            }

            if (options.DeleteFileLocation && item.IsFileProtocol)
            {
                // Assume only the first is required
                // Add this flag to GetDeletePaths if required in the future
                var isRequiredForDelete = true;

                foreach (var fileSystemInfo in item.GetDeletePaths().ToList())
                {
                    try
                    {
                         _logger.LogDebug("Deleting path {path}", fileSystemInfo.FullName);
                        if (fileSystemInfo.IsDirectory)
                        {
                            _fileSystem.DeleteDirectory(fileSystemInfo.FullName, true);
                        }
                        else
                        {
                            _fileSystem.DeleteFile(fileSystemInfo.FullName);
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        // may have already been deleted manually by user
                    }
                    catch (DirectoryNotFoundException)
                    {
                        // may have already been deleted manually by user
                    }
                    catch (IOException)
                    {
                        if (isRequiredForDelete)
                        {
                            throw;
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        if (isRequiredForDelete)
                        {
                            throw;
                        }
                    }

                    isRequiredForDelete = false;
                }
            }

            item.SetParent(null);

            ItemRepository.DeleteItem(item.Id, CancellationToken.None);
            foreach (var child in children)
            {
                ItemRepository.DeleteItem(child.Id, CancellationToken.None);
            }

            BaseItem removed;
            _libraryItemsCache.TryRemove(item.Id, out removed);

            ReportItemRemoved(item, parent);
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
                _logger.LogError(ex, "Error in {resolver} resolving {path}", resolver.GetType().Name, args.Path);
                return null;
            }
        }

        public Guid GetNewItemId(string key, Type type)
        {
            return GetNewItemIdInternal(key, type, false);
        }

        private Guid GetNewItemIdInternal(string key, Type type, bool forceCaseInsensitive)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (key.StartsWith(ConfigurationManager.ApplicationPaths.ProgramDataPath))
            {
                // Try to normalize paths located underneath program-data in an attempt to make them more portable
                key = key.Substring(ConfigurationManager.ApplicationPaths.ProgramDataPath.Length)
                    .TrimStart(new[] { '/', '\\' })
                    .Replace("/", "\\");
            }

            if (forceCaseInsensitive || !ConfigurationManager.Configuration.EnableCaseSensitiveItemIds)
            {
                key = key.ToLower();
            }

            key = type.FullName + key;

            return key.GetMD5();
        }

        public BaseItem ResolvePath(FileSystemMetadata fileInfo,
            Folder parent = null)
        {
            return ResolvePath(fileInfo, new DirectoryService(_logger, _fileSystem), null, parent);
        }

        private BaseItem ResolvePath(FileSystemMetadata fileInfo,
            IDirectoryService directoryService,
            IItemResolver[] resolvers,
            Folder parent = null,
            string collectionType = null,
            LibraryOptions libraryOptions = null)
        {
            if (fileInfo == null)
            {
                throw new ArgumentNullException("fileInfo");
            }

            var fullPath = fileInfo.FullName;

            if (string.IsNullOrEmpty(collectionType) && parent != null)
            {
                collectionType = GetContentTypeOverride(fullPath, true);
            }

            var args = new ItemResolveArgs(ConfigurationManager.ApplicationPaths, directoryService)
            {
                Parent = parent,
                Path = fullPath,
                FileInfo = fileInfo,
                CollectionType = collectionType,
                LibraryOptions = libraryOptions
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

                FileSystemMetadata[] files;
                var isVf = args.IsVf;

                try
                {
                    files = FileData.GetFilteredFileSystemEntries(directoryService, args.Path, _fileSystem, _appHost, _logger, args, flattenFolderDepth: flattenFolderDepth, resolveShortcuts: isPhysicalRoot || isVf);
                }
                catch (Exception ex)
                {
                    if (parent != null && parent.IsPhysicalRoot)
                    {
                        _logger.LogError(ex, "Error in GetFilteredFileSystemEntries isPhysicalRoot: {0} IsVf: {1}", isPhysicalRoot, isVf);

                        files = new FileSystemMetadata[] { };
                    }
                    else
                    {
                        throw;
                    }
                }

                // Need to remove subpaths that may have been resolved from shortcuts
                // Example: if \\server\movies exists, then strip out \\server\movies\action
                if (isPhysicalRoot)
                {
                    files = NormalizeRootPathList(files).ToArray();
                }

                args.FileSystemChildren = files;
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
            if (EntityResolutionIgnoreRules.Any(r => r.ShouldIgnore(file, parent)))
            {
                return true;
            }
            return false;
        }

        public List<FileSystemMetadata> NormalizeRootPathList(IEnumerable<FileSystemMetadata> paths)
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
                _logger.LogInformation("Found duplicate path: {0}", dupe);
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

        public IEnumerable<BaseItem> ResolvePaths(IEnumerable<FileSystemMetadata> files, IDirectoryService directoryService, Folder parent, LibraryOptions libraryOptions, string collectionType)
        {
            return ResolvePaths(files, directoryService, parent, libraryOptions, collectionType, EntityResolvers);
        }

        public IEnumerable<BaseItem> ResolvePaths(IEnumerable<FileSystemMetadata> files,
            IDirectoryService directoryService,
            Folder parent,
            LibraryOptions libraryOptions,
            string collectionType,
            IItemResolver[] resolvers)
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
                        items.AddRange(ResolveFileList(result.ExtraFiles, directoryService, parent, collectionType, resolvers, libraryOptions));
                        return items;
                    }
                }
            }

            return ResolveFileList(fileList, directoryService, parent, collectionType, resolvers, libraryOptions);
        }

        private IEnumerable<BaseItem> ResolveFileList(IEnumerable<FileSystemMetadata> fileList,
            IDirectoryService directoryService,
            Folder parent,
            string collectionType,
            IItemResolver[] resolvers,
            LibraryOptions libraryOptions)
        {
            return fileList.Select(f =>
            {
                try
                {
                    return ResolvePath(f, directoryService, resolvers, parent, collectionType, libraryOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error resolving path {path}", f.FullName);
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

            var rootFolder = GetItemById(GetNewItemId(rootFolderPath, typeof(AggregateFolder))) as AggregateFolder ?? ((Folder)ResolvePath(_fileSystem.GetDirectoryInfo(rootFolderPath))).DeepCopy<Folder,AggregateFolder>();

            // In case program data folder was moved
            if (!string.Equals(rootFolder.Path, rootFolderPath, StringComparison.Ordinal))
            {
                _logger.LogInformation("Resetting root folder path to {0}", rootFolderPath);
                rootFolder.Path = rootFolderPath;
            }

            // Add in the plug-in folders
            var path = Path.Combine(ConfigurationManager.ApplicationPaths.DataPath, "playlists");

            _fileSystem.CreateDirectory(path);

            Folder folder = new PlaylistsFolder
            {
                Path = path
            };

            if (folder.Id.Equals(Guid.Empty))
            {
                if (string.IsNullOrEmpty(folder.Path))
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
                folder.UpdateToRepository(ItemUpdateType.MetadataImport, CancellationToken.None);
            }

            rootFolder.AddVirtualChild(folder);

            RegisterItem(folder);

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
                            tmpItem = ((Folder)ResolvePath(_fileSystem.GetDirectoryInfo(userRootPath))).DeepCopy<Folder,UserRootFolder>();
                        }

                        // In case program data folder was moved
                        if (!string.Equals(tmpItem.Path, userRootPath, StringComparison.Ordinal))
                        {
                            _logger.LogInformation("Resetting user root folder path to {0}", userRootPath);
                            tmpItem.Path = userRootPath;
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

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            //_logger.LogInformation("FindByPath {0}", path);

            var query = new InternalItemsQuery
            {
                Path = path,
                IsFolder = isFolder,
                OrderBy = new[] { ItemSortBy.DateCreated }.Select(i => new ValueTuple<string, SortOrder>(i, SortOrder.Descending)).ToArray(),
                Limit = 1,
                DtoOptions = new DtoOptions(true)
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
            return CreateItemByName<Person>(Person.GetPath, name, new DtoOptions(true));
        }

        /// <summary>
        /// Gets a Studio
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Studio}.</returns>
        public Studio GetStudio(string name)
        {
            return CreateItemByName<Studio>(Studio.GetPath, name, new DtoOptions(true));
        }

        public Guid GetStudioId(string name)
        {
            return GetItemByNameId<Studio>(Studio.GetPath, name);
        }

        public Guid GetGenreId(string name)
        {
            return GetItemByNameId<Genre>(Genre.GetPath, name);
        }

        public Guid GetMusicGenreId(string name)
        {
            return GetItemByNameId<MusicGenre>(MusicGenre.GetPath, name);
        }

        public Guid GetGameGenreId(string name)
        {
            return GetItemByNameId<GameGenre>(GameGenre.GetPath, name);
        }

        /// <summary>
        /// Gets a Genre
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Genre}.</returns>
        public Genre GetGenre(string name)
        {
            return CreateItemByName<Genre>(Genre.GetPath, name, new DtoOptions(true));
        }

        /// <summary>
        /// Gets the genre.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{MusicGenre}.</returns>
        public MusicGenre GetMusicGenre(string name)
        {
            return CreateItemByName<MusicGenre>(MusicGenre.GetPath, name, new DtoOptions(true));
        }

        /// <summary>
        /// Gets the game genre.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{GameGenre}.</returns>
        public GameGenre GetGameGenre(string name)
        {
            return CreateItemByName<GameGenre>(GameGenre.GetPath, name, new DtoOptions(true));
        }

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

            var name = value.ToString(CultureInfo.InvariantCulture);

            return CreateItemByName<Year>(Year.GetPath, name, new DtoOptions(true));
        }

        /// <summary>
        /// Gets a Genre
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Genre}.</returns>
        public MusicArtist GetArtist(string name)
        {
            return GetArtist(name, new DtoOptions(true));
        }

        public MusicArtist GetArtist(string name, DtoOptions options)
        {
            return CreateItemByName<MusicArtist>(MusicArtist.GetPath, name, options);
        }

        private T CreateItemByName<T>(Func<string, string> getPathFn, string name, DtoOptions options)
            where T : BaseItem, new()
        {
            if (typeof(T) == typeof(MusicArtist))
            {
                var existing = GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { typeof(T).Name },
                    Name = name,
                    DtoOptions = options

                }).Cast<MusicArtist>()
                .OrderBy(i => i.IsAccessedByName ? 1 : 0)
                .Cast<T>()
                .FirstOrDefault();

                if (existing != null)
                {
                    return existing;
                }
            }

            var id = GetItemByNameId<T>(getPathFn, name);

            var item = GetItemById(id) as T;

            if (item == null)
            {
                var path = getPathFn(name);
                item = new T
                {
                    Name = name,
                    Id = id,
                    DateCreated = DateTime.UtcNow,
                    DateModified = DateTime.UtcNow,
                    Path = path
                };

                CreateItem(item, null);
            }

            return item;
        }

        private Guid GetItemByNameId<T>(Func<string, string> getPathFn, string name)
              where T : BaseItem, new()
        {
            var path = getPathFn(name);
            var forceCaseInsensitiveId = ConfigurationManager.Configuration.EnableNormalizedItemByNameIds;
            return GetNewItemIdInternal(path, typeof(T), forceCaseInsensitiveId);
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

            return Task.CompletedTask;
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

        private async Task ValidateTopLibraryFolders(CancellationToken cancellationToken)
        {
            var rootChildren = RootFolder.Children.ToList();
            rootChildren = GetUserRootFolder().Children.ToList();

            await RootFolder.RefreshMetadata(cancellationToken).ConfigureAwait(false);

            // Start by just validating the children of the root, but go no further
            await RootFolder.ValidateChildren(new SimpleProgress<double>(), cancellationToken, new MetadataRefreshOptions(new DirectoryService(_logger, _fileSystem)), recursive: false);

            await GetUserRootFolder().RefreshMetadata(cancellationToken).ConfigureAwait(false);

            await GetUserRootFolder().ValidateChildren(new SimpleProgress<double>(), cancellationToken, new MetadataRefreshOptions(new DirectoryService(_logger, _fileSystem)), recursive: false).ConfigureAwait(false);

            // Quickly scan CollectionFolders for changes
            foreach (var folder in GetUserRootFolder().Children.OfType<Folder>().ToList())
            {
                await folder.RefreshMetadata(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task PerformLibraryValidation(IProgress<double> progress, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Validating media library");

            await ValidateTopLibraryFolders(cancellationToken).ConfigureAwait(false);

            var innerProgress = new ActionableProgress<double>();

            innerProgress.RegisterAction(pct => progress.Report(pct * .96));

            // Now validate the entire media library
            await RootFolder.ValidateChildren(innerProgress, cancellationToken, new MetadataRefreshOptions(new DirectoryService(_logger, _fileSystem)), recursive: true).ConfigureAwait(false);

            progress.Report(96);

            innerProgress = new ActionableProgress<double>();

            innerProgress.RegisterAction(pct => progress.Report(96 + (pct * .04)));

            // Run post-scan tasks
            await RunPostScanTasks(innerProgress, cancellationToken).ConfigureAwait(false);

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
                    double innerPercent = pct;
                    innerPercent /= 100;
                    innerPercent += currentNumComplete;

                    innerPercent /= numTasks;
                    innerPercent *= 100;

                    progress.Report(innerPercent);
                });

                _logger.LogDebug("Running post-scan task {0}", task.GetType().Name);

                try
                {
                    await task.Run(innerProgress, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Post-scan task cancelled: {0}", task.GetType().Name);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running postscan task");
                }

                numComplete++;
                double percent = numComplete;
                percent /= numTasks;
                progress.Report(percent * 100);
            }

            ItemRepository.UpdateInheritedValues(cancellationToken);

            progress.Report(100);
        }

        /// <summary>
        /// Gets the default view.
        /// </summary>
        /// <returns>IEnumerable{VirtualFolderInfo}.</returns>
        public List<VirtualFolderInfo> GetVirtualFolders()
        {
            return GetVirtualFolders(false);
        }

        public List<VirtualFolderInfo> GetVirtualFolders(bool includeRefreshState)
        {
            var topLibraryFolders = GetUserRootFolder().Children.ToList();

            var refreshQueue = includeRefreshState ? _providerManagerFactory().GetRefreshQueue() : null;

            return _fileSystem.GetDirectoryPaths(ConfigurationManager.ApplicationPaths.DefaultUserViewsPath)
                .Select(dir => GetVirtualFolderInfo(dir, topLibraryFolders, refreshQueue))
                .OrderBy(i => i.Name)
                .ToList();
        }

        private VirtualFolderInfo GetVirtualFolderInfo(string dir, List<BaseItem> allCollectionFolders, Dictionary<Guid, Guid> refreshQueue)
        {
            var info = new VirtualFolderInfo
            {
                Name = Path.GetFileName(dir),

                Locations = _fileSystem.GetFilePaths(dir, false)
                .Where(i => string.Equals(ShortcutFileExtension, Path.GetExtension(i), StringComparison.OrdinalIgnoreCase))
                    .Select(i =>
                    {
                        try
                        {
                            return _appHost.ExpandVirtualPath(_fileSystem.ResolveShortcut(i));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error resolving shortcut file {file}", i);
                            return null;
                        }
                    })
                    .Where(i => i != null)
                    .OrderBy(i => i)
                    .ToArray(),

                CollectionType = GetCollectionType(dir)
            };

            var libraryFolder = allCollectionFolders.FirstOrDefault(i => string.Equals(i.Path, dir, StringComparison.OrdinalIgnoreCase));

            if (libraryFolder != null && libraryFolder.HasImage(ImageType.Primary))
            {
                info.PrimaryImageItemId = libraryFolder.Id.ToString("N");
            }

            if (libraryFolder != null)
            {
                info.ItemId = libraryFolder.Id.ToString("N");
                info.LibraryOptions = GetLibraryOptions(libraryFolder);

                if (refreshQueue != null)
                {
                    info.RefreshProgress = libraryFolder.GetRefreshProgress();

                    info.RefreshStatus = info.RefreshProgress.HasValue ? "Active" : refreshQueue.ContainsKey(libraryFolder.Id) ? "Queued" : "Idle";
                }
            }

            return info;
        }

        private string GetCollectionType(string path)
        {
            return _fileSystem.GetFilePaths(path, new[] { ".collection" }, true, false)
                .Select(i => _fileSystem.GetFileNameWithoutExtension(i))
                .FirstOrDefault(i => !string.IsNullOrEmpty(i));
        }

        /// <summary>
        /// Gets the item by id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>BaseItem.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        public BaseItem GetItemById(Guid id)
        {
            if (id.Equals(Guid.Empty))
            {
                throw new ArgumentNullException("id");
            }

            BaseItem item;

            if (LibraryItemsCache.TryGetValue(id, out item))
            {
                return item;
            }

            item = RetrieveItem(id);

            //_logger.LogDebug("GetitemById {0}", id);

            if (item != null)
            {
                RegisterItem(item);
            }

            return item;
        }

        public List<BaseItem> GetItemList(InternalItemsQuery query, bool allowExternalContent)
        {
            if (query.Recursive && !query.ParentId.Equals(Guid.Empty))
            {
                var parent = GetItemById(query.ParentId);
                if (parent != null)
                {
                    SetTopParentIdsOrAncestors(query, new List<BaseItem> { parent });
                }
            }

            if (query.User != null)
            {
                AddUserToQuery(query, query.User, allowExternalContent);
            }

            return ItemRepository.GetItemList(query);
        }

        public List<BaseItem> GetItemList(InternalItemsQuery query)
        {
            return GetItemList(query, true);
        }

        public int GetCount(InternalItemsQuery query)
        {
            if (query.Recursive && !query.ParentId.Equals(Guid.Empty))
            {
                var parent = GetItemById(query.ParentId);
                if (parent != null)
                {
                    SetTopParentIdsOrAncestors(query, new List<BaseItem> { parent });
                }
            }

            if (query.User != null)
            {
                AddUserToQuery(query, query.User);
            }

            return ItemRepository.GetCount(query);
        }

        public List<BaseItem> GetItemList(InternalItemsQuery query, List<BaseItem> parents)
        {
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

        public QueryResult<Tuple<BaseItem, ItemCounts>> GetAllArtists(InternalItemsQuery query)
        {
            if (query.User != null)
            {
                AddUserToQuery(query, query.User);
            }

            SetTopParentOrAncestorIds(query);
            return ItemRepository.GetAllArtists(query);
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

            var parents = query.AncestorIds.Select(i => GetItemById(i)).ToList();

            if (parents.All(i =>
            {
                if (i is ICollectionFolder || i is UserView)
                {
                    return true;
                }

                //_logger.LogDebug("Query requires ancestor query due to type: " + i.GetType().Name);
                return false;

            }))
            {
                // Optimize by querying against top level views
                query.TopParentIds = parents.SelectMany(i => GetTopParentIdsForQuery(i, query.User)).ToArray();
                query.AncestorIds = Array.Empty<Guid>();

                // Prevent searching in all libraries due to empty filter
                if (query.TopParentIds.Length == 0)
                {
                    query.TopParentIds = new[] { Guid.NewGuid() };
                }
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
            if (query.Recursive && !query.ParentId.Equals(Guid.Empty))
            {
                var parent = GetItemById(query.ParentId);
                if (parent != null)
                {
                    SetTopParentIdsOrAncestors(query, new List<BaseItem> { parent });
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

            var list = ItemRepository.GetItemList(query);

            return new QueryResult<BaseItem>
            {
                Items = list.ToArray()
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

                //_logger.LogDebug("Query requires ancestor query due to type: " + i.GetType().Name);
                return false;

            }))
            {
                // Optimize by querying against top level views
                query.TopParentIds = parents.SelectMany(i => GetTopParentIdsForQuery(i, query.User)).ToArray();

                // Prevent searching in all libraries due to empty filter
                if (query.TopParentIds.Length == 0)
                {
                    query.TopParentIds = new[] { Guid.NewGuid() };
                }
            }
            else
            {
                // We need to be able to query from any arbitrary ancestor up the tree
                query.AncestorIds = parents.SelectMany(i => i.GetIdsForAncestorQuery()).ToArray();

                // Prevent searching in all libraries due to empty filter
                if (query.AncestorIds.Length == 0)
                {
                    query.AncestorIds = new[] { Guid.NewGuid() };
                }
            }

            query.Parent = null;
        }

        private void AddUserToQuery(InternalItemsQuery query, User user, bool allowExternalContent = true)
        {
            if (query.AncestorIds.Length == 0 &&
                query.ParentId.Equals(Guid.Empty) &&
                query.ChannelIds.Length == 0 &&
                query.TopParentIds.Length == 0 &&
                string.IsNullOrEmpty(query.AncestorWithPresentationUniqueKey) &&
                string.IsNullOrEmpty(query.SeriesPresentationUniqueKey) &&
                query.ItemIds.Length == 0)
            {
                var userViews = _userviewManager().GetUserViews(new UserViewQuery
                {
                    UserId = user.Id,
                    IncludeHidden = true,
                    IncludeExternalContent = allowExternalContent
                });

                query.TopParentIds = userViews.SelectMany(i => GetTopParentIdsForQuery(i, user)).ToArray();
            }
        }

        private IEnumerable<Guid> GetTopParentIdsForQuery(BaseItem item, User user)
        {
            var view = item as UserView;

            if (view != null)
            {
                if (string.Equals(view.ViewType, CollectionType.LiveTv))
                {
                    return new[] { view.Id };
                }

                // Translate view into folders
                if (!view.DisplayParentId.Equals(Guid.Empty))
                {
                    var displayParent = GetItemById(view.DisplayParentId);
                    if (displayParent != null)
                    {
                        return GetTopParentIdsForQuery(displayParent, user);
                    }
                    return Array.Empty<Guid>();
                }
                if (!view.ParentId.Equals(Guid.Empty))
                {
                    var displayParent = GetItemById(view.ParentId);
                    if (displayParent != null)
                    {
                        return GetTopParentIdsForQuery(displayParent, user);
                    }
                    return Array.Empty<Guid>();
                }

                // Handle grouping
                if (user != null && !string.IsNullOrEmpty(view.ViewType) && UserView.IsEligibleForGrouping(view.ViewType) && user.Configuration.GroupedFolders.Length > 0)
                {
                    return GetUserRootFolder()
                        .GetChildren(user, true)
                        .OfType<CollectionFolder>()
                        .Where(i => string.IsNullOrEmpty(i.CollectionType) || string.Equals(i.CollectionType, view.ViewType, StringComparison.OrdinalIgnoreCase))
                        .Where(i => user.IsFolderGrouped(i.Id))
                        .SelectMany(i => GetTopParentIdsForQuery(i, user));
                }
                return Array.Empty<Guid>();
            }

            var collectionFolder = item as CollectionFolder;
            if (collectionFolder != null)
            {
                return collectionFolder.PhysicalFolderIds;
            }

            var topParent = item.GetTopParent();
            if (topParent != null)
            {
                return new[] { topParent.Id };
            }
            return Array.Empty<Guid>();
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
                _logger.LogError(ex, "Error getting intros");

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
                    _logger.LogError(ex, "Error getting intro files");

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
                    _logger.LogError("Unable to locate item with Id {ID}.", info.ItemId.Value);
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
                        _logger.LogError("Intro resolver returned null for {path}.", info.Path);
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
                    _logger.LogError(ex, "Error resolving path {path}.", info.Path);
                }
            }
            else
            {
                _logger.LogError("IntroProvider returned an IntroInfo with null Path and ItemId.");
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

        public IEnumerable<BaseItem> Sort(IEnumerable<BaseItem> items, User user, IEnumerable<ValueTuple<string, SortOrder>> orderByList)
        {
            var isFirst = true;

            IOrderedEnumerable<BaseItem> orderedItems = null;

            foreach (var orderBy in orderByList)
            {
                var comparer = GetComparer(orderBy.Item1, user);
                if (comparer == null)
                {
                    continue;
                }

                var sortOrder = orderBy.Item2;

                if (isFirst)
                {
                    orderedItems = sortOrder == SortOrder.Descending ? items.OrderByDescending(i => i, comparer) : items.OrderBy(i => i, comparer);
                }
                else
                {
                    orderedItems = sortOrder == SortOrder.Descending ? orderedItems.ThenByDescending(i => i, comparer) : orderedItems.ThenBy(i => i, comparer);
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
        public void CreateItem(BaseItem item, BaseItem parent)
        {
            CreateItems(new[] { item }, parent, CancellationToken.None);
        }

        /// <summary>
        /// Creates the items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public void CreateItems(IEnumerable<BaseItem> items, BaseItem parent, CancellationToken cancellationToken)
        {
            var list = items.ToList();

            ItemRepository.SaveItems(list, cancellationToken);

            foreach (var item in list)
            {
                RegisterItem(item);
            }

            if (ItemAdded != null)
            {
                foreach (var item in list)
                {
                    // With the live tv guide this just creates too much noise
                    if (item.SourceType != SourceType.Library)
                    {
                        continue;
                    }

                    try
                    {
                        ItemAdded(this, new ItemChangeEventArgs
                        {
                            Item = item,
                            Parent = parent ?? item.GetParent()
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in ItemAdded event handler");
                    }
                }
            }
        }

        public void UpdateImages(BaseItem item)
        {
            ItemRepository.SaveImages(item);

            RegisterItem(item);
        }

        /// <summary>
        /// Updates the item.
        /// </summary>
        public void UpdateItems(List<BaseItem> items, BaseItem parent, ItemUpdateType updateReason, CancellationToken cancellationToken)
        {
            foreach (var item in items)
            {
                if (item.IsFileProtocol)
                {
                    _providerManagerFactory().SaveMetadata(item, updateReason);
                }

                item.DateLastSaved = DateTime.UtcNow;

                RegisterItem(item);
            }

            //var logName = item.LocationType == LocationType.Remote ? item.Name ?? item.Path : item.Path ?? item.Name;
            //_logger.LogDebug("Saving {0} to database.", logName);

            ItemRepository.SaveItems(items, cancellationToken);

            if (ItemUpdated != null)
            {
                foreach (var item in items)
                {
                    // With the live tv guide this just creates too much noise
                    if (item.SourceType != SourceType.Library)
                    {
                        continue;
                    }

                    try
                    {
                        ItemUpdated(this, new ItemChangeEventArgs
                        {
                            Item = item,
                            Parent = parent,
                            UpdateReason = updateReason
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in ItemUpdated event handler");
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
        public void UpdateItem(BaseItem item, BaseItem parent, ItemUpdateType updateReason, CancellationToken cancellationToken)
        {
            UpdateItems(new List<BaseItem> { item }, parent, updateReason, cancellationToken);
        }

        /// <summary>
        /// Reports the item removed.
        /// </summary>
        /// <param name="item">The item.</param>
        public void ReportItemRemoved(BaseItem item, BaseItem parent)
        {
            if (ItemRemoved != null)
            {
                try
                {
                    ItemRemoved(this, new ItemChangeEventArgs
                    {
                        Item = item,
                        Parent = parent
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ItemRemoved event handler");
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

        public List<Folder> GetCollectionFolders(BaseItem item)
        {
            while (item != null)
            {
                var parent = item.GetParent();

                if (parent == null || parent is AggregateFolder)
                {
                    break;
                }

                item = parent;
            }

            if (item == null)
            {
                return new List<Folder>();
            }

            return GetCollectionFoldersInternal(item, GetUserRootFolder().Children.OfType<Folder>().ToList());
        }

        public List<Folder> GetCollectionFolders(BaseItem item, List<Folder> allUserRootChildren)
        {
            while (item != null)
            {
                var parent = item.GetParent();

                if (parent == null || parent is AggregateFolder)
                {
                    break;
                }

                item = parent;
            }

            if (item == null)
            {
                return new List<Folder>();
            }

            return GetCollectionFoldersInternal(item, allUserRootChildren);
        }

        private List<Folder> GetCollectionFoldersInternal(BaseItem item, List<Folder> allUserRootChildren)
        {
            return allUserRootChildren
                .Where(i => string.Equals(i.Path, item.Path, StringComparison.OrdinalIgnoreCase) || i.PhysicalLocations.Contains(item.Path, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        public LibraryOptions GetLibraryOptions(BaseItem item)
        {
            var collectionFolder = item as CollectionFolder;
            if (collectionFolder == null)
            {
                collectionFolder = GetCollectionFolders(item)
                   .OfType<CollectionFolder>()
                   .FirstOrDefault();
            }

            var options = collectionFolder == null ? new LibraryOptions() : collectionFolder.GetLibraryOptions();

            return options;
        }

        public string GetContentType(BaseItem item)
        {
            string configuredContentType = GetConfiguredContentType(item, false);
            if (!string.IsNullOrEmpty(configuredContentType))
            {
                return configuredContentType;
            }
            configuredContentType = GetConfiguredContentType(item, true);
            if (!string.IsNullOrEmpty(configuredContentType))
            {
                return configuredContentType;
            }
            return GetInheritedContentType(item);
        }

        public string GetInheritedContentType(BaseItem item)
        {
            var type = GetTopFolderContentType(item);

            if (!string.IsNullOrEmpty(type))
            {
                return type;
            }

            return item.GetParents()
                .Select(GetConfiguredContentType)
                .LastOrDefault(i => !string.IsNullOrEmpty(i));
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
            var nameValuePair = ConfigurationManager.Configuration.ContentTypes.FirstOrDefault(i => _fileSystem.AreEqual(i.Name, path) || (inherit && !string.IsNullOrEmpty(i.Name) && _fileSystem.ContainsSubPath(i.Name, path)));
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

            while (!item.ParentId.Equals(Guid.Empty))
            {
                var parent = item.GetParent();
                if (parent == null || parent is AggregateFolder)
                {
                    break;
                }
                item = parent;
            }

            return GetUserRootFolder().Children
                .OfType<ICollectionFolder>()
                .Where(i => string.Equals(i.Path, item.Path, StringComparison.OrdinalIgnoreCase) || i.PhysicalLocations.Contains(item.Path))
                .Select(i => i.CollectionType)
                .FirstOrDefault(i => !string.IsNullOrEmpty(i));
        }

        private readonly TimeSpan _viewRefreshInterval = TimeSpan.FromHours(24);
        //private readonly TimeSpan _viewRefreshInterval = TimeSpan.FromMinutes(1);

        public UserView GetNamedView(User user,
            string name,
            string viewType,
            string sortName)
        {
            return GetNamedView(user, name, Guid.Empty, viewType, sortName);
        }

        public UserView GetNamedView(string name,
            string viewType,
            string sortName)
        {
            var path = Path.Combine(ConfigurationManager.ApplicationPaths.InternalMetadataPath, "views");

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

                CreateItem(item, null);

                refresh = true;
            }

            if (refresh)
            {
                item.UpdateToRepository(ItemUpdateType.MetadataImport, CancellationToken.None);
                _providerManagerFactory().QueueRefresh(item.Id, new MetadataRefreshOptions(new DirectoryService(_logger, _fileSystem)), RefreshPriority.Normal);
            }

            return item;
        }

        public UserView GetNamedView(User user,
            string name,
            Guid parentId,
            string viewType,
            string sortName)
        {
            var parentIdString = parentId.Equals(Guid.Empty) ? null : parentId.ToString("N");
            var idValues = "38_namedview_" + name + user.Id.ToString("N") + (parentIdString ?? string.Empty) + (viewType ?? string.Empty);

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

                item.DisplayParentId = parentId;

                CreateItem(item, null);

                isNew = true;
            }

            var refresh = isNew || DateTime.UtcNow - item.DateLastRefreshed >= _viewRefreshInterval;

            if (!refresh && !item.DisplayParentId.Equals(Guid.Empty))
            {
                var displayParent = GetItemById(item.DisplayParentId);
                refresh = displayParent != null && displayParent.DateLastSaved > item.DateLastRefreshed;
            }

            if (refresh)
            {
                _providerManagerFactory().QueueRefresh(item.Id, new MetadataRefreshOptions(new DirectoryService(_logger, _fileSystem))
                {
                    // Need to force save to increment DateLastSaved
                    ForceSave = true

                }, RefreshPriority.Normal);
            }

            return item;
        }

        public UserView GetShadowView(BaseItem parent,
        string viewType,
        string sortName)
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

                CreateItem(item, null);

                isNew = true;
            }

            var refresh = isNew || DateTime.UtcNow - item.DateLastRefreshed >= _viewRefreshInterval;

            if (!refresh && !item.DisplayParentId.Equals(Guid.Empty))
            {
                var displayParent = GetItemById(item.DisplayParentId);
                refresh = displayParent != null && displayParent.DateLastSaved > item.DateLastRefreshed;
            }

            if (refresh)
            {
                _providerManagerFactory().QueueRefresh(item.Id, new MetadataRefreshOptions(new DirectoryService(_logger, _fileSystem))
                {
                    // Need to force save to increment DateLastSaved
                    ForceSave = true

                }, RefreshPriority.Normal);
            }

            return item;
        }

        public UserView GetNamedView(string name,
            Guid parentId,
            string viewType,
            string sortName,
            string uniqueId)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            var parentIdString = parentId.Equals(Guid.Empty) ? null : parentId.ToString("N");
            var idValues = "37_namedview_" + name + (parentIdString ?? string.Empty) + (viewType ?? string.Empty);
            if (!string.IsNullOrEmpty(uniqueId))
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

                item.DisplayParentId = parentId;

                CreateItem(item, null);

                isNew = true;
            }

            if (!string.Equals(viewType, item.ViewType, StringComparison.OrdinalIgnoreCase))
            {
                item.ViewType = viewType;
                item.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None);
            }

            var refresh = isNew || DateTime.UtcNow - item.DateLastRefreshed >= _viewRefreshInterval;

            if (!refresh && !item.DisplayParentId.Equals(Guid.Empty))
            {
                var displayParent = GetItemById(item.DisplayParentId);
                refresh = displayParent != null && displayParent.DateLastSaved > item.DateLastRefreshed;
            }

            if (refresh)
            {
                _providerManagerFactory().QueueRefresh(item.Id, new MetadataRefreshOptions(new DirectoryService(_logger, _fileSystem))
                {
                    // Need to force save to increment DateLastSaved
                    ForceSave = true
                }, RefreshPriority.Normal);
            }

            return item;
        }

        public void AddExternalSubtitleStreams(List<MediaStream> streams,
            string videoPath,
            string[] files)
        {
             new SubtitleResolver(BaseItem.LocalizationManager, _fileSystem).AddExternalSubtitleStreams(streams, videoPath, streams.Count, files);
        }

        public bool IsVideoFile(string path, LibraryOptions libraryOptions)
        {
            var resolver = new VideoResolver(GetNamingOptions());
            return resolver.IsVideoFile(path);
        }

        public bool IsVideoFile(string path)
        {
            return IsVideoFile(path, new LibraryOptions());
        }

        public bool IsAudioFile(string path, LibraryOptions libraryOptions)
        {
            var parser = new AudioFileParser(GetNamingOptions());
            return parser.IsAudioFile(path);
        }

        public bool IsAudioFile(string path)
        {
            return IsAudioFile(path, new LibraryOptions());
        }

        public int? GetSeasonNumberFromPath(string path)
        {
            return new SeasonPathParser(GetNamingOptions()).Parse(path, true, true).SeasonNumber;
        }

        public bool FillMissingEpisodeNumbersFromPath(Episode episode, bool forceRefresh)
        {
            var series = episode.Series;
            bool? isAbsoluteNaming = series == null ? false : string.Equals(series.DisplayOrder, "absolute", StringComparison.OrdinalIgnoreCase);
            if (!isAbsoluteNaming.Value)
            {
                // In other words, no filter applied
                isAbsoluteNaming = null;
            }

            var resolver = new EpisodeResolver(GetNamingOptions());

            var isFolder = episode.VideoType == VideoType.BluRay || episode.VideoType == VideoType.Dvd;

            var episodeInfo = episode.IsFileProtocol ?
                resolver.Resolve(episode.Path, isFolder, null, null, isAbsoluteNaming) :
                new Emby.Naming.TV.EpisodeInfo();

            if (episodeInfo == null)
            {
                episodeInfo = new Emby.Naming.TV.EpisodeInfo();
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
            }
            else
            {
                if (!episode.IndexNumber.HasValue || forceRefresh)
                {
                    if (episode.IndexNumber != episodeInfo.EpisodeNumber)
                    {
                        changed = true;
                    }
                    episode.IndexNumber = episodeInfo.EpisodeNumber;
                }

                if (!episode.IndexNumberEnd.HasValue || forceRefresh)
                {
                    if (episode.IndexNumberEnd != episodeInfo.EndingEpsiodeNumber)
                    {
                        changed = true;
                    }
                    episode.IndexNumberEnd = episodeInfo.EndingEpsiodeNumber;
                }

                if (!episode.ParentIndexNumber.HasValue || forceRefresh)
                {
                    if (episode.ParentIndexNumber != episodeInfo.SeasonNumber)
                    {
                        changed = true;
                    }
                    episode.ParentIndexNumber = episodeInfo.SeasonNumber;
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

            return changed;
        }

        public NamingOptions GetNamingOptions()
        {
            return GetNamingOptionsInternal();
        }

        private NamingOptions _namingOptions;
        private string[] _videoFileExtensions;
        private NamingOptions GetNamingOptionsInternal()
        {
            if (_namingOptions == null)
            {
                var options = new NamingOptions();

                _namingOptions = options;
                _videoFileExtensions = _namingOptions.VideoFileExtensions.ToArray();
            }

            return _namingOptions;
        }

        public ItemLookupInfo ParseName(string name)
        {
            var resolver = new VideoResolver(GetNamingOptions());

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
            var namingOptions = GetNamingOptions();

            var files = owner.IsInMixedFolder ? new List<FileSystemMetadata>() : fileSystemChildren.Where(i => i.IsDirectory)
                .Where(i => string.Equals(i.Name, BaseItem.TrailerFolderName, StringComparison.OrdinalIgnoreCase))
                .SelectMany(i => _fileSystem.GetFiles(i.FullName, _videoFileExtensions, false, false))
                .ToList();

            var videoListResolver = new VideoListResolver(namingOptions);

            var videos = videoListResolver.Resolve(fileSystemChildren);

            var currentVideo = videos.FirstOrDefault(i => string.Equals(owner.Path, i.Files.First().Path, StringComparison.OrdinalIgnoreCase));

            if (currentVideo != null)
            {
                files.AddRange(currentVideo.Extras.Where(i => string.Equals(i.ExtraType, "trailer", StringComparison.OrdinalIgnoreCase)).Select(i => _fileSystem.GetFileInfo(i.Path)));
            }

            var resolvers = new IItemResolver[]
            {
                new GenericVideoResolver<Trailer>(this, _fileSystem)
            };

            return ResolvePaths(files, directoryService, null, new LibraryOptions(), null, resolvers)
                .OfType<Trailer>()
                .Select(video =>
                {
                    // Try to retrieve it from the db. If we don't find it, use the resolved version
                    var dbItem = GetItemById(video.Id) as Trailer;

                    if (dbItem != null)
                    {
                        video = dbItem;
                    }

                    video.ParentId = Guid.Empty;
                    video.OwnerId = owner.Id;
                    video.ExtraType = ExtraType.Trailer;
                    video.TrailerTypes = new [] { TrailerType.LocalTrailer };

                    return video;

                    // Sort them so that the list can be easily compared for changes
                }).OrderBy(i => i.Path);
        }

        private static readonly string[] ExtrasSubfolderNames = new[] { "extras", "specials", "shorts", "scenes", "featurettes", "behind the scenes", "deleted scenes", "interviews" };

        public IEnumerable<Video> FindExtras(BaseItem owner, List<FileSystemMetadata> fileSystemChildren, IDirectoryService directoryService)
        {
            var namingOptions = GetNamingOptions();

            var files = owner.IsInMixedFolder ? new List<FileSystemMetadata>() : fileSystemChildren.Where(i => i.IsDirectory)
                .Where(i => ExtrasSubfolderNames.Contains(i.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                .SelectMany(i => _fileSystem.GetFiles(i.FullName, _videoFileExtensions, false, false))
                .ToList();

            var videoListResolver = new VideoListResolver(namingOptions);

            var videos = videoListResolver.Resolve(fileSystemChildren);

            var currentVideo = videos.FirstOrDefault(i => string.Equals(owner.Path, i.Files.First().Path, StringComparison.OrdinalIgnoreCase));

            if (currentVideo != null)
            {
                files.AddRange(currentVideo.Extras.Where(i => !string.Equals(i.ExtraType, "trailer", StringComparison.OrdinalIgnoreCase)).Select(i => _fileSystem.GetFileInfo(i.Path)));
            }

            return ResolvePaths(files, directoryService, null, new LibraryOptions(), null)
                .OfType<Video>()
                .Select(video =>
                {
                    // Try to retrieve it from the db. If we don't find it, use the resolved version
                    var dbItem = GetItemById(video.Id) as Video;

                    if (dbItem != null)
                    {
                        video = dbItem;
                    }

                    video.ParentId = Guid.Empty;
                    video.OwnerId = owner.Id;

                    SetExtraTypeFromFilename(video);

                    return video;

                    // Sort them so that the list can be easily compared for changes
                }).OrderBy(i => i.Path);
        }

        public string GetPathAfterNetworkSubstitution(string path, BaseItem ownerItem)
        {
            if (ownerItem != null)
            {
                var libraryOptions = GetLibraryOptions(ownerItem);
                if (libraryOptions != null)
                {
                    foreach (var pathInfo in libraryOptions.PathInfos)
                    {
                        if (string.IsNullOrWhiteSpace(pathInfo.Path) || string.IsNullOrWhiteSpace(pathInfo.NetworkPath))
                        {
                            continue;
                        }

                        var substitutionResult = SubstitutePathInternal(path, pathInfo.Path, pathInfo.NetworkPath);
                        if (substitutionResult.Item2)
                        {
                            return substitutionResult.Item1;
                        }
                    }
                }
            }

            var metadataPath = ConfigurationManager.Configuration.MetadataPath;
            var metadataNetworkPath = ConfigurationManager.Configuration.MetadataNetworkPath;

            if (!string.IsNullOrWhiteSpace(metadataPath) && !string.IsNullOrWhiteSpace(metadataNetworkPath))
            {
                var metadataSubstitutionResult = SubstitutePathInternal(path, metadataPath, metadataNetworkPath);
                if (metadataSubstitutionResult.Item2)
                {
                    return metadataSubstitutionResult.Item1;
                }
            }

            foreach (var map in ConfigurationManager.Configuration.PathSubstitutions)
            {
                if (!string.IsNullOrWhiteSpace(map.From))
                {
                    var substitutionResult = SubstitutePathInternal(path, map.From, map.To);
                    if (substitutionResult.Item2)
                    {
                        return substitutionResult.Item1;
                    }
                }
            }

            return path;
        }

        public string SubstitutePath(string path, string from, string to)
        {
            return SubstitutePathInternal(path, from, to).Item1;
        }

        private Tuple<string, bool> SubstitutePathInternal(string path, string from, string to)
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

            from = from.Trim();
            to = to.Trim();

            var newPath = path.Replace(from, to, StringComparison.OrdinalIgnoreCase);
            var changed = false;

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

                changed = true;
            }

            return new Tuple<string, bool>(newPath, changed);
        }

        private void SetExtraTypeFromFilename(Video item)
        {
            var resolver = new ExtraResolver(GetNamingOptions());

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
                    _logger.LogError(ex, "Error getting person");
                    return null;
                }

            }).Where(i => i != null).ToList();
        }

        public List<string> GetPeopleNames(InternalPeopleQuery query)
        {
            return ItemRepository.GetPeopleNames(query);
        }

        public void UpdatePeople(BaseItem item, List<PersonInfo> people)
        {
            if (!item.SupportsPeople)
            {
                return;
            }

            ItemRepository.UpdatePeople(item.Id, people);
        }

        public async Task<ItemImageInfo> ConvertImageToLocal(BaseItem item, ItemImageInfo image, int imageIndex)
        {
            foreach (var url in image.Path.Split('|'))
            {
                try
                {
                    _logger.LogDebug("ConvertImageToLocal item {0} - image url: {1}", item.Id, url);

                    await _providerManagerFactory().SaveImage(item, url, image.Type, imageIndex, CancellationToken.None).ConfigureAwait(false);

                    item.UpdateToRepository(ItemUpdateType.ImageUpdate, CancellationToken.None);

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
            item.UpdateToRepository(ItemUpdateType.ImageUpdate, CancellationToken.None);

            throw new InvalidOperationException();
        }

        public async Task AddVirtualFolder(string name, string collectionType, LibraryOptions options, bool refreshLibrary)
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

            var mediaPathInfos = options.PathInfos;
            if (mediaPathInfos != null)
            {
                var invalidpath = mediaPathInfos.FirstOrDefault(i => !_fileSystem.DirectoryExists(i.Path));
                if (invalidpath != null)
                {
                    throw new ArgumentException("The specified path does not exist: " + invalidpath.Path + ".");
                }
            }

            _libraryMonitorFactory().Stop();

            try
            {
                _fileSystem.CreateDirectory(virtualFolderPath);

                if (!string.IsNullOrEmpty(collectionType))
                {
                    var path = Path.Combine(virtualFolderPath, collectionType + ".collection");

                    _fileSystem.WriteAllBytes(path, Array.Empty<byte>());
                }

                CollectionFolder.SaveLibraryOptions(virtualFolderPath, options);

                if (mediaPathInfos != null)
                {
                    foreach (var path in mediaPathInfos)
                    {
                        AddMediaPathInternal(name, path, false);
                    }
                }
            }
            finally
            {
                if (refreshLibrary)
                {
                    await ValidateTopLibraryFolders(CancellationToken.None).ConfigureAwait(false);

                    StartScanInBackground();
                }
                else
                {
                    // Need to add a delay here or directory watchers may still pick up the changes
                    await Task.Delay(1000).ConfigureAwait(false);
                    _libraryMonitorFactory().Start();
                }
            }
        }

        private void StartScanInBackground()
        {
            Task.Run(() =>
            {
                // No need to start if scanning the library because it will handle it
                ValidateMediaLibrary(new SimpleProgress<double>(), CancellationToken.None);
            });
        }

        private bool ValidateNetworkPath(string path)
        {
            //if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            //{
            //    // We can't validate protocol-based paths, so just allow them
            //    if (path.IndexOf("://", StringComparison.OrdinalIgnoreCase) == -1)
            //    {
            //        return _fileSystem.DirectoryExists(path);
            //    }
            //}

            // Without native support for unc, we cannot validate this when running under mono
            return true;
        }

        private const string ShortcutFileExtension = ".mblink";
        public void AddMediaPath(string virtualFolderName, MediaPathInfo pathInfo)
        {
            AddMediaPathInternal(virtualFolderName, pathInfo, true);
        }

        private void AddMediaPathInternal(string virtualFolderName, MediaPathInfo pathInfo, bool saveLibraryOptions)
        {
            if (pathInfo == null)
            {
                throw new ArgumentNullException("path");
            }

            var path = pathInfo.Path;

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException("path");
            }

            if (!_fileSystem.DirectoryExists(path))
            {
                throw new FileNotFoundException("The path does not exist.");
            }

            if (!string.IsNullOrWhiteSpace(pathInfo.NetworkPath) && !ValidateNetworkPath(pathInfo.NetworkPath))
            {
                throw new FileNotFoundException("The network path does not exist.");
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

            _fileSystem.CreateShortcut(lnk, _appHost.ReverseVirtualPath(path));

            RemoveContentTypeOverrides(path);

            if (saveLibraryOptions)
            {
                var libraryOptions = CollectionFolder.GetLibraryOptions(virtualFolderPath);

                var list = libraryOptions.PathInfos.ToList();
                list.Add(pathInfo);
                libraryOptions.PathInfos = list.ToArray();

                SyncLibraryOptionsToLocations(virtualFolderPath, libraryOptions);

                CollectionFolder.SaveLibraryOptions(virtualFolderPath, libraryOptions);
            }
        }

        public void UpdateMediaPath(string virtualFolderName, MediaPathInfo pathInfo)
        {
            if (pathInfo == null)
            {
                throw new ArgumentNullException("path");
            }

            if (!string.IsNullOrWhiteSpace(pathInfo.NetworkPath) && !ValidateNetworkPath(pathInfo.NetworkPath))
            {
                throw new FileNotFoundException("The network path does not exist.");
            }

            var rootFolderPath = ConfigurationManager.ApplicationPaths.DefaultUserViewsPath;
            var virtualFolderPath = Path.Combine(rootFolderPath, virtualFolderName);

            var libraryOptions = CollectionFolder.GetLibraryOptions(virtualFolderPath);

            SyncLibraryOptionsToLocations(virtualFolderPath, libraryOptions);

            var list = libraryOptions.PathInfos.ToList();
            foreach (var originalPathInfo in list)
            {
                if (string.Equals(pathInfo.Path, originalPathInfo.Path, StringComparison.Ordinal))
                {
                    originalPathInfo.NetworkPath = pathInfo.NetworkPath;
                    break;
                }
            }

            libraryOptions.PathInfos = list.ToArray();

            CollectionFolder.SaveLibraryOptions(virtualFolderPath, libraryOptions);
        }

        private void SyncLibraryOptionsToLocations(string virtualFolderPath, LibraryOptions options)
        {
            var topLibraryFolders = GetUserRootFolder().Children.ToList();
            var info = GetVirtualFolderInfo(virtualFolderPath, topLibraryFolders, null);

            if (info.Locations.Length > 0 && info.Locations.Length != options.PathInfos.Length)
            {
                var list = options.PathInfos.ToList();

                foreach (var location in info.Locations)
                {
                    if (!list.Any(i => string.Equals(i.Path, location, StringComparison.Ordinal)))
                    {
                        list.Add(new MediaPathInfo
                        {
                            Path = location
                        });
                    }
                }

                options.PathInfos = list.ToArray();
            }
        }

        public async Task RemoveVirtualFolder(string name, bool refreshLibrary)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            var rootFolderPath = ConfigurationManager.ApplicationPaths.DefaultUserViewsPath;

            var path = Path.Combine(rootFolderPath, name);

            if (!_fileSystem.DirectoryExists(path))
            {
                throw new FileNotFoundException("The media folder does not exist");
            }

            _libraryMonitorFactory().Stop();

            try
            {
                _fileSystem.DeleteDirectory(path, true);
            }
            finally
            {
                CollectionFolder.OnCollectionFolderChange();

                if (refreshLibrary)
                {
                    await ValidateTopLibraryFolders(CancellationToken.None).ConfigureAwait(false);

                    StartScanInBackground();
                }
                else
                {
                    // Need to add a delay here or directory watchers may still pick up the changes
                    await Task.Delay(1000).ConfigureAwait(false);
                    _libraryMonitorFactory().Start();
                }
            }
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
                if (string.IsNullOrWhiteSpace(contentType.Name))
                {
                    removeList.Add(contentType);
                }
                else if (_fileSystem.AreEqual(path, contentType.Name)
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
            if (string.IsNullOrEmpty(mediaPath))
            {
                throw new ArgumentNullException("mediaPath");
            }

            var rootFolderPath = ConfigurationManager.ApplicationPaths.DefaultUserViewsPath;
            var virtualFolderPath = Path.Combine(rootFolderPath, virtualFolderName);

            if (!_fileSystem.DirectoryExists(virtualFolderPath))
            {
                throw new FileNotFoundException(string.Format("The media collection {0} does not exist", virtualFolderName));
            }

            var shortcut = _fileSystem.GetFilePaths(virtualFolderPath, true)
                .Where(i => string.Equals(ShortcutFileExtension, Path.GetExtension(i), StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault(f => _appHost.ExpandVirtualPath(_fileSystem.ResolveShortcut(f)).Equals(mediaPath, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(shortcut))
            {
                _fileSystem.DeleteFile(shortcut);
            }

            var libraryOptions = CollectionFolder.GetLibraryOptions(virtualFolderPath);

            libraryOptions.PathInfos = libraryOptions
                .PathInfos
                .Where(i => !string.Equals(i.Path, mediaPath, StringComparison.Ordinal))
                .ToArray();

            CollectionFolder.SaveLibraryOptions(virtualFolderPath, libraryOptions);
        }
    }
}
