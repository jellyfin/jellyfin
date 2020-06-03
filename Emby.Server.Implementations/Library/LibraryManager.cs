#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Emby.Naming.Audio;
using Emby.Naming.Common;
using Emby.Naming.TV;
using Emby.Naming.Video;
using Emby.Server.Implementations.Library.Resolvers;
using Emby.Server.Implementations.Library.Validators;
using Emby.Server.Implementations.Playlists;
using Emby.Server.Implementations.ScheduledTasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Providers.MediaInfo;
using Microsoft.Extensions.Logging;
using SortOrder = MediaBrowser.Model.Entities.SortOrder;
using VideoResolver = Emby.Naming.Video.VideoResolver;

namespace Emby.Server.Implementations.Library
{
    /// <summary>
    /// Class LibraryManager.
    /// </summary>
    public class LibraryManager : ILibraryManager
    {
        private readonly ILogger _logger;
        private readonly ITaskManager _taskManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataRepository;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly Lazy<ILibraryMonitor> _libraryMonitorFactory;
        private readonly Lazy<IProviderManager> _providerManagerFactory;
        private readonly Lazy<IUserViewManager> _userviewManagerFactory;
        private readonly IServerApplicationHost _appHost;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IFileSystem _fileSystem;
        private readonly IItemRepository _itemRepository;
        private readonly ConcurrentDictionary<Guid, BaseItem> _libraryItemsCache;
        private readonly IImageProcessor _imageProcessor;

        private NamingOptions _namingOptions;
        private string[] _videoFileExtensions;

        private ILibraryMonitor LibraryMonitor => _libraryMonitorFactory.Value;

        private IProviderManager ProviderManager => _providerManagerFactory.Value;

        private IUserViewManager UserViewManager => _userviewManagerFactory.Value;

        /// <summary>
        /// Gets or sets the postscan tasks.
        /// </summary>
        /// <value>The postscan tasks.</value>
        private ILibraryPostScanTask[] PostscanTasks { get; set; }

        /// <summary>
        /// Gets or sets the intro providers.
        /// </summary>
        /// <value>The intro providers.</value>
        private IIntroProvider[] IntroProviders { get; set; }

        /// <summary>
        /// Gets or sets the list of entity resolution ignore rules
        /// </summary>
        /// <value>The entity resolution ignore rules.</value>
        private IResolverIgnoreRule[] EntityResolutionIgnoreRules { get; set; }

        /// <summary>
        /// Gets or sets the list of currently registered entity resolvers
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

        public bool IsScanRunning { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryManager" /> class.
        /// </summary>
        /// <param name="appHost">The application host</param>
        /// <param name="logger">The logger.</param>
        /// <param name="taskManager">The task manager.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="libraryMonitorFactory">The library monitor.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="providerManagerFactory">The provider manager.</param>
        /// <param name="userviewManagerFactory">The userview manager.</param>
        /// <param name="mediaEncoder">The media encoder.</param>
        /// <param name="itemRepository">The item repository.</param>
        /// <param name="imageProcessor">The image processor.</param>
        public LibraryManager(
            IServerApplicationHost appHost,
            ILogger<LibraryManager> logger,
            ITaskManager taskManager,
            IUserManager userManager,
            IServerConfigurationManager configurationManager,
            IUserDataManager userDataRepository,
            Lazy<ILibraryMonitor> libraryMonitorFactory,
            IFileSystem fileSystem,
            Lazy<IProviderManager> providerManagerFactory,
            Lazy<IUserViewManager> userviewManagerFactory,
            IMediaEncoder mediaEncoder,
            IItemRepository itemRepository,
            IImageProcessor imageProcessor)
        {
            _appHost = appHost;
            _logger = logger;
            _taskManager = taskManager;
            _userManager = userManager;
            _configurationManager = configurationManager;
            _userDataRepository = userDataRepository;
            _libraryMonitorFactory = libraryMonitorFactory;
            _fileSystem = fileSystem;
            _providerManagerFactory = providerManagerFactory;
            _userviewManagerFactory = userviewManagerFactory;
            _mediaEncoder = mediaEncoder;
            _itemRepository = itemRepository;
            _imageProcessor = imageProcessor;

            _libraryItemsCache = new ConcurrentDictionary<Guid, BaseItem>();

            _configurationManager.ConfigurationUpdated += ConfigurationUpdated;

            RecordConfigurationValues(configurationManager.Configuration);
        }

        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="rules">The rules.</param>
        /// <param name="resolvers">The resolvers.</param>
        /// <param name="introProviders">The intro providers.</param>
        /// <param name="itemComparers">The item comparers.</param>
        /// <param name="postscanTasks">The post scan tasks.</param>
        public void AddParts(
            IEnumerable<IResolverIgnoreRule> rules,
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
            PostscanTasks = postscanTasks.ToArray();
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
        private void ConfigurationUpdated(object sender, EventArgs e)
        {
            var config = _configurationManager.Configuration;

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
                throw new ArgumentNullException(nameof(item));
            }

            if (item is IItemByName)
            {
                if (!(item is MusicArtist))
                {
                    return;
                }
            }
            else if (!item.IsFolder)
            {
                if (!(item is Video) && !(item is LiveTvChannel))
                {
                    return;
                }
            }

            _libraryItemsCache.AddOrUpdate(item.Id, item, delegate { return item; });
        }

        public void DeleteItem(BaseItem item, DeleteOptions options)
        {
            DeleteItem(item, options, false);
        }

        public void DeleteItem(BaseItem item, DeleteOptions options, bool notifyParentItem)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var parent = item.GetOwner() ?? item.GetParent();

            DeleteItem(item, options, parent, notifyParentItem);
        }

        public void DeleteItem(BaseItem item, DeleteOptions options, BaseItem parent, bool notifyParentItem)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
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
                _logger.LogDebug(
                    "Deleting item, Type: {0}, Name: {1}, Path: {2}, Id: {3}",
                    item.GetType().Name,
                    item.Name ?? "Unknown name",
                    item.Path ?? string.Empty,
                    item.Id);
            }
            else
            {
                _logger.LogInformation(
                    "Deleting item, Type: {0}, Name: {1}, Path: {2}, Id: {3}",
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
                if (!Directory.Exists(metadataPath))
                {
                    continue;
                }

                _logger.LogDebug("Deleting path {MetadataPath}", metadataPath);

                try
                {
                    Directory.Delete(metadataPath, true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting {MetadataPath}", metadataPath);
                }
            }

            if (options.DeleteFileLocation && item.IsFileProtocol)
            {
                // Assume only the first is required
                // Add this flag to GetDeletePaths if required in the future
                var isRequiredForDelete = true;

                foreach (var fileSystemInfo in item.GetDeletePaths())
                {
                    if (Directory.Exists(fileSystemInfo.FullName) || File.Exists(fileSystemInfo.FullName))
                    {
                        try
                        {
                            _logger.LogDebug("Deleting path {path}", fileSystemInfo.FullName);
                            if (fileSystemInfo.IsDirectory)
                            {
                                Directory.Delete(fileSystemInfo.FullName, true);
                            }
                            else
                            {
                                File.Delete(fileSystemInfo.FullName);
                            }
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
                    }

                    isRequiredForDelete = false;
                }
            }

            item.SetParent(null);

            _itemRepository.DeleteItem(item.Id);
            foreach (var child in children)
            {
                _itemRepository.DeleteItem(child.Id);
            }

            _libraryItemsCache.TryRemove(item.Id, out BaseItem removed);

            ReportItemRemoved(item, parent);
        }

        private static IEnumerable<string> GetMetadataPaths(BaseItem item, IEnumerable<BaseItem> children)
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
                throw new ArgumentNullException(nameof(key));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (key.StartsWith(_configurationManager.ApplicationPaths.ProgramDataPath, StringComparison.Ordinal))
            {
                // Try to normalize paths located underneath program-data in an attempt to make them more portable
                key = key.Substring(_configurationManager.ApplicationPaths.ProgramDataPath.Length)
                    .TrimStart(new[] { '/', '\\' })
                    .Replace("/", "\\");
            }

            if (forceCaseInsensitive || !_configurationManager.Configuration.EnableCaseSensitiveItemIds)
            {
                key = key.ToLowerInvariant();
            }

            key = type.FullName + key;

            return key.GetMD5();
        }

        public BaseItem ResolvePath(FileSystemMetadata fileInfo, Folder parent = null)
            => ResolvePath(fileInfo, new DirectoryService(_fileSystem), null, parent);

        private BaseItem ResolvePath(
            FileSystemMetadata fileInfo,
            IDirectoryService directoryService,
            IItemResolver[] resolvers,
            Folder parent = null,
            string collectionType = null,
            LibraryOptions libraryOptions = null)
        {
            if (fileInfo == null)
            {
                throw new ArgumentNullException(nameof(fileInfo));
            }

            var fullPath = fileInfo.FullName;

            if (string.IsNullOrEmpty(collectionType) && parent != null)
            {
                collectionType = GetContentTypeOverride(fullPath, true);
            }

            var args = new ItemResolveArgs(_configurationManager.ApplicationPaths, directoryService)
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

                        files = Array.Empty<FileSystemMetadata>();
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
            => EntityResolutionIgnoreRules.Any(r => r.ShouldIgnore(file, parent));

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

        public IEnumerable<BaseItem> ResolvePaths(
            IEnumerable<FileSystemMetadata> files,
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

        private IEnumerable<BaseItem> ResolveFileList(
            IEnumerable<FileSystemMetadata> fileList,
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
        /// Creates the root media folder.
        /// </summary>
        /// <returns>AggregateFolder.</returns>
        /// <exception cref="InvalidOperationException">Cannot create the root folder until plugins have loaded.</exception>
        public AggregateFolder CreateRootFolder()
        {
            var rootFolderPath = _configurationManager.ApplicationPaths.RootFolderPath;

            Directory.CreateDirectory(rootFolderPath);

            var rootFolder = GetItemById(GetNewItemId(rootFolderPath, typeof(AggregateFolder))) as AggregateFolder ?? ((Folder)ResolvePath(_fileSystem.GetDirectoryInfo(rootFolderPath))).DeepCopy<Folder, AggregateFolder>();

            // In case program data folder was moved
            if (!string.Equals(rootFolder.Path, rootFolderPath, StringComparison.Ordinal))
            {
                _logger.LogInformation("Resetting root folder path to {0}", rootFolderPath);
                rootFolder.Path = rootFolderPath;
            }

            // Add in the plug-in folders
            var path = Path.Combine(_configurationManager.ApplicationPaths.DataPath, "playlists");

            Directory.CreateDirectory(path);

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
                        var userRootPath = _configurationManager.ApplicationPaths.DefaultUserViewsPath;

                        _logger.LogDebug("Creating userRootPath at {path}", userRootPath);
                        Directory.CreateDirectory(userRootPath);

                        var newItemId = GetNewItemId(userRootPath, typeof(UserRootFolder));
                        UserRootFolder tmpItem = null;
                        try
                        {
                            tmpItem = GetItemById(newItemId) as UserRootFolder;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error creating UserRootFolder {path}", newItemId);
                        }

                        if (tmpItem == null)
                        {
                            _logger.LogDebug("Creating new userRootFolder with DeepCopy");
                            tmpItem = ((Folder)ResolvePath(_fileSystem.GetDirectoryInfo(userRootPath))).DeepCopy<Folder, UserRootFolder>();
                        }

                        // In case program data folder was moved
                        if (!string.Equals(tmpItem.Path, userRootPath, StringComparison.Ordinal))
                        {
                            _logger.LogInformation("Resetting user root folder path to {0}", userRootPath);
                            tmpItem.Path = userRootPath;
                        }

                        _userRootFolder = tmpItem;
                        _logger.LogDebug("Setting userRootFolder: {folder}", _userRootFolder);
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
                throw new ArgumentNullException(nameof(path));
            }

            var query = new InternalItemsQuery
            {
                Path = path,
                IsFolder = isFolder,
                OrderBy = new[] { (ItemSortBy.DateCreated, SortOrder.Descending) },
                Limit = 1,
                DtoOptions = new DtoOptions(true)
            };

            return GetItemList(query)
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets the person.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Person}.</returns>
        public Person GetPerson(string name)
        {
            return CreateItemByName<Person>(Person.GetPath, name, new DtoOptions(true));
        }

        /// <summary>
        /// Gets the studio.
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

        /// <summary>
        /// Gets the genre.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Genre}.</returns>
        public Genre GetGenre(string name)
        {
            return CreateItemByName<Genre>(Genre.GetPath, name, new DtoOptions(true));
        }

        /// <summary>
        /// Gets the music genre.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{MusicGenre}.</returns>
        public MusicGenre GetMusicGenre(string name)
        {
            return CreateItemByName<MusicGenre>(MusicGenre.GetPath, name, new DtoOptions(true));
        }

        /// <summary>
        /// Gets the year.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Task{Year}.</returns>
        public Year GetYear(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Years less than or equal to 0 are invalid.");
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
            var forceCaseInsensitiveId = _configurationManager.Configuration.EnableNormalizedItemByNameIds;
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
            Directory.CreateDirectory(_configurationManager.ApplicationPaths.PeoplePath);

            return new PeopleValidator(this, _logger, _fileSystem).ValidatePeople(cancellationToken, progress);
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
            LibraryMonitor.Stop();

            try
            {
                await PerformLibraryValidation(progress, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                LibraryMonitor.Start();
                IsScanRunning = false;
            }
        }

        private async Task ValidateTopLibraryFolders(CancellationToken cancellationToken)
        {
            await RootFolder.RefreshMetadata(cancellationToken).ConfigureAwait(false);

            // Start by just validating the children of the root, but go no further
            await RootFolder.ValidateChildren(
                new SimpleProgress<double>(),
                cancellationToken,
                new MetadataRefreshOptions(new DirectoryService(_fileSystem)),
                recursive: false).ConfigureAwait(false);

            await GetUserRootFolder().RefreshMetadata(cancellationToken).ConfigureAwait(false);

            await GetUserRootFolder().ValidateChildren(
                new SimpleProgress<double>(),
                cancellationToken,
                new MetadataRefreshOptions(new DirectoryService(_fileSystem)),
                recursive: false).ConfigureAwait(false);

            // Quickly scan CollectionFolders for changes
            foreach (var folder in GetUserRootFolder().Children.OfType<Folder>())
            {
                await folder.RefreshMetadata(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task PerformLibraryValidation(IProgress<double> progress, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Validating media library");

            await ValidateTopLibraryFolders(cancellationToken).ConfigureAwait(false);

            var innerProgress = new ActionableProgress<double>();

            innerProgress.RegisterAction(pct => progress.Report(pct * 0.96));

            // Validate the entire media library
            await RootFolder.ValidateChildren(innerProgress, cancellationToken, new MetadataRefreshOptions(new DirectoryService(_fileSystem)), recursive: true).ConfigureAwait(false);

            progress.Report(96);

            innerProgress = new ActionableProgress<double>();

            innerProgress.RegisterAction(pct => progress.Report(96 + (pct * .04)));

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
                    _logger.LogError(ex, "Error running post-scan task");
                }

                numComplete++;
                double percent = numComplete;
                percent /= numTasks;
                progress.Report(percent * 100);
            }

            _itemRepository.UpdateInheritedValues(cancellationToken);

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
            _logger.LogDebug("Getting topLibraryFolders");
            var topLibraryFolders = GetUserRootFolder().Children.ToList();

            _logger.LogDebug("Getting refreshQueue");
            var refreshQueue = includeRefreshState ? ProviderManager.GetRefreshQueue() : null;

            return _fileSystem.GetDirectoryPaths(_configurationManager.ApplicationPaths.DefaultUserViewsPath)
                .Select(dir => GetVirtualFolderInfo(dir, topLibraryFolders, refreshQueue))
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
                info.PrimaryImageItemId = libraryFolder.Id.ToString("N", CultureInfo.InvariantCulture);
            }

            if (libraryFolder != null)
            {
                info.ItemId = libraryFolder.Id.ToString("N", CultureInfo.InvariantCulture);
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
                .Select(Path.GetFileNameWithoutExtension)
                .FirstOrDefault(i => !string.IsNullOrEmpty(i));
        }

        /// <summary>
        /// Gets the item by id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>BaseItem.</returns>
        /// <exception cref="ArgumentNullException">id</exception>
        public BaseItem GetItemById(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Guid can't be empty", nameof(id));
            }

            if (_libraryItemsCache.TryGetValue(id, out BaseItem item))
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

        public List<BaseItem> GetItemList(InternalItemsQuery query, bool allowExternalContent)
        {
            if (query.Recursive && query.ParentId != Guid.Empty)
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

            return _itemRepository.GetItemList(query);
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

            return _itemRepository.GetCount(query);
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

            return _itemRepository.GetItemList(query);
        }

        public QueryResult<BaseItem> QueryItems(InternalItemsQuery query)
        {
            if (query.User != null)
            {
                AddUserToQuery(query, query.User);
            }

            if (query.EnableTotalRecordCount)
            {
                return _itemRepository.GetItems(query);
            }

            return new QueryResult<BaseItem>
            {
                Items = _itemRepository.GetItemList(query).ToArray()
            };
        }

        public List<Guid> GetItemIds(InternalItemsQuery query)
        {
            if (query.User != null)
            {
                AddUserToQuery(query, query.User);
            }

            return _itemRepository.GetItemIdsList(query);
        }

        public QueryResult<(BaseItem, ItemCounts)> GetStudios(InternalItemsQuery query)
        {
            if (query.User != null)
            {
                AddUserToQuery(query, query.User);
            }

            SetTopParentOrAncestorIds(query);
            return _itemRepository.GetStudios(query);
        }

        public QueryResult<(BaseItem, ItemCounts)> GetGenres(InternalItemsQuery query)
        {
            if (query.User != null)
            {
                AddUserToQuery(query, query.User);
            }

            SetTopParentOrAncestorIds(query);
            return _itemRepository.GetGenres(query);
        }

        public QueryResult<(BaseItem, ItemCounts)> GetMusicGenres(InternalItemsQuery query)
        {
            if (query.User != null)
            {
                AddUserToQuery(query, query.User);
            }

            SetTopParentOrAncestorIds(query);
            return _itemRepository.GetMusicGenres(query);
        }

        public QueryResult<(BaseItem, ItemCounts)> GetAllArtists(InternalItemsQuery query)
        {
            if (query.User != null)
            {
                AddUserToQuery(query, query.User);
            }

            SetTopParentOrAncestorIds(query);
            return _itemRepository.GetAllArtists(query);
        }

        public QueryResult<(BaseItem, ItemCounts)> GetArtists(InternalItemsQuery query)
        {
            if (query.User != null)
            {
                AddUserToQuery(query, query.User);
            }

            SetTopParentOrAncestorIds(query);
            return _itemRepository.GetArtists(query);
        }

        private void SetTopParentOrAncestorIds(InternalItemsQuery query)
        {
            var ancestorIds = query.AncestorIds;
            int len = ancestorIds.Length;
            if (len == 0)
            {
                return;
            }

            var parents = new BaseItem[len];
            for (int i = 0; i < len; i++)
            {
                parents[i] = GetItemById(ancestorIds[i]);
                if (!(parents[i] is ICollectionFolder || parents[i] is UserView))
                {
                    return;
                }
            }

            // Optimize by querying against top level views
            query.TopParentIds = parents.SelectMany(i => GetTopParentIdsForQuery(i, query.User)).ToArray();
            query.AncestorIds = Array.Empty<Guid>();

            // Prevent searching in all libraries due to empty filter
            if (query.TopParentIds.Length == 0)
            {
                query.TopParentIds = new[] { Guid.NewGuid() };
            }
        }

        public QueryResult<(BaseItem, ItemCounts)> GetAlbumArtists(InternalItemsQuery query)
        {
            if (query.User != null)
            {
                AddUserToQuery(query, query.User);
            }

            SetTopParentOrAncestorIds(query);
            return _itemRepository.GetAlbumArtists(query);
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
                return _itemRepository.GetItems(query);
            }

            var list = _itemRepository.GetItemList(query);

            return new QueryResult<BaseItem>
            {
                Items = list
            };
        }

        private void SetTopParentIdsOrAncestors(InternalItemsQuery query, List<BaseItem> parents)
        {
            if (parents.All(i => i is ICollectionFolder || i is UserView))
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
                var userViews = UserViewManager.GetUserViews(new UserViewQuery
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
            if (item is UserView view)
            {
                if (string.Equals(view.ViewType, CollectionType.LiveTv, StringComparison.Ordinal))
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

            if (item is CollectionFolder collectionFolder)
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
                .OrderBy(i => i.GetType().Name.Contains("Default", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
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

            // If it requires a user, create a new one, and assign the user
            if (comparer is IUserBaseItemComparer)
            {
                var userComparer = (IUserBaseItemComparer)Activator.CreateInstance(comparer.GetType());

                userComparer.User = user;
                userComparer.UserManager = _userManager;
                userComparer.UserDataRepository = _userDataRepository;

                return userComparer;
            }

            return comparer;
        }

        /// <summary>
        /// Creates the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="parent">The parent item.</param>
        public void CreateItem(BaseItem item, BaseItem parent)
        {
            CreateItems(new[] { item }, parent, CancellationToken.None);
        }

        /// <summary>
        /// Creates the items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="parent">The parent item</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public void CreateItems(IEnumerable<BaseItem> items, BaseItem parent, CancellationToken cancellationToken)
        {
            // Don't iterate multiple times
            var itemsList = items.ToList();

            _itemRepository.SaveItems(itemsList, cancellationToken);

            foreach (var item in itemsList)
            {
                RegisterItem(item);
            }

            if (ItemAdded != null)
            {
                foreach (var item in itemsList)
                {
                    // With the live tv guide this just creates too much noise
                    if (item.SourceType != SourceType.Library)
                    {
                        continue;
                    }

                    try
                    {
                        ItemAdded(
                            this,
                            new ItemChangeEventArgs
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

        private bool ImageNeedsRefresh(ItemImageInfo image)
        {
            if (image.Path != null && image.IsLocalFile)
            {
                if (image.Width == 0 || image.Height == 0 || string.IsNullOrEmpty(image.BlurHash))
                {
                    return true;
                }

                try
                {
                    return _fileSystem.GetLastWriteTimeUtc(image.Path) != image.DateModified;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cannot get file info for {0}", image.Path);
                    return false;
                }
            }

            return image.Path != null && !image.IsLocalFile;
        }

        public void UpdateImages(BaseItem item, bool forceUpdate = false)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var outdated = forceUpdate ? item.ImageInfos.Where(i => i.Path != null).ToArray() : item.ImageInfos.Where(ImageNeedsRefresh).ToArray();
            if (outdated.Length == 0)
            {
                RegisterItem(item);
                return;
            }

            foreach (var img in outdated)
            {
                var image = img;
                if (!img.IsLocalFile)
                {
                    try
                    {
                        var index = item.GetImageIndex(img);
                        image = ConvertImageToLocal(item, img, index).ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                    catch (ArgumentException)
                    {
                        _logger.LogWarning("Cannot get image index for {0}", img.Path);
                        continue;
                    }
                    catch (InvalidOperationException)
                    {
                        _logger.LogWarning("Cannot fetch image from {0}", img.Path);
                        continue;
                    }
                }

                ImageDimensions size = _imageProcessor.GetImageDimensions(item, image);
                image.Width = size.Width;
                image.Height = size.Height;

                try
                {
                    image.BlurHash = _imageProcessor.GetImageBlurHash(image.Path);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cannot compute blurhash for {0}", image.Path);
                    image.BlurHash = string.Empty;
                }

                try
                {
                    image.DateModified = _fileSystem.GetLastWriteTimeUtc(image.Path);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cannot update DateModified for {0}", image.Path);
                }
            }

            _itemRepository.SaveImages(item);
            RegisterItem(item);
        }

        /// <summary>
        /// Updates the item.
        /// </summary>
        public void UpdateItems(IEnumerable<BaseItem> items, BaseItem parent, ItemUpdateType updateReason, CancellationToken cancellationToken)
        {
            // Don't iterate multiple times
            var itemsList = items.ToList();

            foreach (var item in itemsList)
            {
                if (item.IsFileProtocol)
                {
                    ProviderManager.SaveMetadata(item, updateReason);
                }

                item.DateLastSaved = DateTime.UtcNow;

                UpdateImages(item, updateReason >= ItemUpdateType.ImageUpdate);
            }

            _itemRepository.SaveItems(itemsList, cancellationToken);

            if (ItemUpdated != null)
            {
                foreach (var item in itemsList)
                {
                    // With the live tv guide this just creates too much noise
                    if (item.SourceType != SourceType.Library)
                    {
                        continue;
                    }

                    try
                    {
                        ItemUpdated(
                            this,
                            new ItemChangeEventArgs
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
        /// <param name="parent">The parent item.</param>
        /// <param name="updateReason">The update reason.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public void UpdateItem(BaseItem item, BaseItem parent, ItemUpdateType updateReason, CancellationToken cancellationToken)
        {
            UpdateItems(new[] { item }, parent, updateReason, cancellationToken);
        }

        /// <summary>
        /// Reports the item removed.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="parent">The parent item.</param>
        public void ReportItemRemoved(BaseItem item, BaseItem parent)
        {
            if (ItemRemoved != null)
            {
                try
                {
                    ItemRemoved(
                        this,
                        new ItemChangeEventArgs
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
            return _itemRepository.RetrieveItem(id);
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

        private static List<Folder> GetCollectionFoldersInternal(BaseItem item, List<Folder> allUserRootChildren)
        {
            return allUserRootChildren
                .Where(i => string.Equals(i.Path, item.Path, StringComparison.OrdinalIgnoreCase) || i.PhysicalLocations.Contains(item.Path, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        public LibraryOptions GetLibraryOptions(BaseItem item)
        {
            if (!(item is CollectionFolder collectionFolder))
            {
                collectionFolder = GetCollectionFolders(item)
                   .OfType<CollectionFolder>()
                   .FirstOrDefault();
            }

            return collectionFolder == null ? new LibraryOptions() : collectionFolder.GetLibraryOptions();
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
            if (item is ICollectionFolder collectionFolder)
            {
                return collectionFolder.CollectionType;
            }

            return GetContentTypeOverride(item.ContainingFolderPath, inheritConfiguredPath);
        }

        private string GetContentTypeOverride(string path, bool inherit)
        {
            var nameValuePair = _configurationManager.Configuration.ContentTypes
                                    .FirstOrDefault(i => _fileSystem.AreEqual(i.Name, path)
                                                         || (inherit && !string.IsNullOrEmpty(i.Name)
                                                                     && _fileSystem.ContainsSubPath(i.Name, path)));
            return nameValuePair?.Value;
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

        public UserView GetNamedView(
            User user,
            string name,
            string viewType,
            string sortName)
        {
            return GetNamedView(user, name, Guid.Empty, viewType, sortName);
        }

        public UserView GetNamedView(
            string name,
            string viewType,
            string sortName)
        {
            var path = Path.Combine(
                _configurationManager.ApplicationPaths.InternalMetadataPath,
                "views",
                _fileSystem.GetValidFilename(viewType));

            var id = GetNewItemId(path + "_namedview_" + name, typeof(UserView));

            var item = GetItemById(id) as UserView;

            var refresh = false;

            if (item == null || !string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase))
            {
                Directory.CreateDirectory(path);

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
                ProviderManager.QueueRefresh(item.Id, new MetadataRefreshOptions(new DirectoryService(_fileSystem)), RefreshPriority.Normal);
            }

            return item;
        }

        public UserView GetNamedView(
            User user,
            string name,
            Guid parentId,
            string viewType,
            string sortName)
        {
            var parentIdString = parentId.Equals(Guid.Empty) ? null : parentId.ToString("N", CultureInfo.InvariantCulture);
            var idValues = "38_namedview_" + name + user.Id.ToString("N", CultureInfo.InvariantCulture) + (parentIdString ?? string.Empty) + (viewType ?? string.Empty);

            var id = GetNewItemId(idValues, typeof(UserView));

            var path = Path.Combine(_configurationManager.ApplicationPaths.InternalMetadataPath, "views", id.ToString("N", CultureInfo.InvariantCulture));

            var item = GetItemById(id) as UserView;

            var isNew = false;

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
                    DisplayParentId = parentId
                };

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
                ProviderManager.QueueRefresh(
                    item.Id,
                    new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                    {
                        // Need to force save to increment DateLastSaved
                        ForceSave = true
                    },
                    RefreshPriority.Normal);
            }

            return item;
        }

        public UserView GetShadowView(
            BaseItem parent,
            string viewType,
            string sortName)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
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
                Directory.CreateDirectory(path);

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
                ProviderManager.QueueRefresh(
                    item.Id,
                    new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                    {
                        // Need to force save to increment DateLastSaved
                        ForceSave = true
                    },
                    RefreshPriority.Normal);
            }

            return item;
        }

        public UserView GetNamedView(
            string name,
            Guid parentId,
            string viewType,
            string sortName,
            string uniqueId)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var parentIdString = parentId.Equals(Guid.Empty) ? null : parentId.ToString("N", CultureInfo.InvariantCulture);
            var idValues = "37_namedview_" + name + (parentIdString ?? string.Empty) + (viewType ?? string.Empty);
            if (!string.IsNullOrEmpty(uniqueId))
            {
                idValues += uniqueId;
            }

            var id = GetNewItemId(idValues, typeof(UserView));

            var path = Path.Combine(_configurationManager.ApplicationPaths.InternalMetadataPath, "views", id.ToString("N", CultureInfo.InvariantCulture));

            var item = GetItemById(id) as UserView;

            var isNew = false;

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
                ProviderManager.QueueRefresh(
                    item.Id,
                    new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                    {
                        // Need to force save to increment DateLastSaved
                        ForceSave = true
                    },
                    RefreshPriority.Normal);
            }

            return item;
        }

        public void AddExternalSubtitleStreams(
            List<MediaStream> streams,
            string videoPath,
            string[] files)
        {
            new SubtitleResolver(BaseItem.LocalizationManager).AddExternalSubtitleStreams(streams, videoPath, streams.Count, files);
        }

        /// <inheritdoc />
        public bool IsVideoFile(string path)
        {
            var resolver = new VideoResolver(GetNamingOptions());
            return resolver.IsVideoFile(path);
        }

        /// <inheritdoc />
        public bool IsAudioFile(string path)
            => AudioFileParser.IsAudioFile(path, GetNamingOptions());

        /// <inheritdoc />
        public int? GetSeasonNumberFromPath(string path)
            => SeasonPathParser.Parse(path, true, true).SeasonNumber;

        /// <inheritdoc />
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
                new Naming.TV.EpisodeInfo();

            if (episodeInfo == null)
            {
                episodeInfo = new Naming.TV.EpisodeInfo();
            }

            try
            {
                var libraryOptions = GetLibraryOptions(episode);
                if (libraryOptions.EnableEmbeddedEpisodeInfos && string.Equals(episodeInfo.Container, "mp4", StringComparison.OrdinalIgnoreCase))
                {
                    // Read from metadata
                    var mediaInfo = _mediaEncoder.GetMediaInfo(new MediaInfoRequest
                    {
                        MediaSource = episode.GetMediaSources(false)[0],
                        MediaType = DlnaProfileType.Video
                    }, CancellationToken.None).GetAwaiter().GetResult();
                    if (mediaInfo.ParentIndexNumber > 0)
                    {
                        episodeInfo.SeasonNumber = mediaInfo.ParentIndexNumber;
                    }

                    if (mediaInfo.IndexNumber > 0)
                    {
                        episodeInfo.EpisodeNumber = mediaInfo.IndexNumber;
                    }

                    if (!string.IsNullOrEmpty(mediaInfo.ShowName))
                    {
                        episodeInfo.SeriesName = mediaInfo.ShowName;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading the episode informations with ffprobe. Episode: {EpisodeInfo}", episodeInfo.Path);
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
                else
                {
                    /*
                    Anime series don't generally have a season in their file name, however,
                    tvdb needs a season to correctly get the metadata.
                    Hence, a null season needs to be filled with something. */
                    //FIXME perhaps this would be better for tvdb parser to ask for season 1 if no season is specified
                    episode.ParentIndexNumber = 1;
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
            if (_namingOptions == null)
            {
                _namingOptions = new NamingOptions();
                _videoFileExtensions = _namingOptions.VideoFileExtensions;
            }

            return _namingOptions;
        }

        public ItemLookupInfo ParseName(string name)
        {
            var resolver = new VideoResolver(GetNamingOptions());

            var result = resolver.CleanDateTime(name);

            return new ItemLookupInfo
            {
                Name = resolver.TryCleanString(result.Name, out var newName) ? newName.ToString() : result.Name,
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
                files.AddRange(currentVideo.Extras.Where(i => i.ExtraType == ExtraType.Trailer).Select(i => _fileSystem.GetFileInfo(i.Path)));
            }

            var resolvers = new IItemResolver[]
            {
                new GenericVideoResolver<Trailer>(this)
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
                    video.TrailerTypes = new[] { TrailerType.LocalTrailer };

                    return video;

                    // Sort them so that the list can be easily compared for changes
                }).OrderBy(i => i.Path);
        }

        public IEnumerable<Video> FindExtras(BaseItem owner, List<FileSystemMetadata> fileSystemChildren, IDirectoryService directoryService)
        {
            var namingOptions = GetNamingOptions();

            var files = owner.IsInMixedFolder ? new List<FileSystemMetadata>() : fileSystemChildren.Where(i => i.IsDirectory)
                .Where(i => BaseItem.AllExtrasTypesFolderNames.Contains(i.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                .SelectMany(i => _fileSystem.GetFiles(i.FullName, _videoFileExtensions, false, false))
                .ToList();

            var videoListResolver = new VideoListResolver(namingOptions);

            var videos = videoListResolver.Resolve(fileSystemChildren);

            var currentVideo = videos.FirstOrDefault(i => string.Equals(owner.Path, i.Files.First().Path, StringComparison.OrdinalIgnoreCase));

            if (currentVideo != null)
            {
                files.AddRange(currentVideo.Extras.Where(i => i.ExtraType != ExtraType.Trailer).Select(i => _fileSystem.GetFileInfo(i.Path)));
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

            var metadataPath = _configurationManager.Configuration.MetadataPath;
            var metadataNetworkPath = _configurationManager.Configuration.MetadataNetworkPath;

            if (!string.IsNullOrWhiteSpace(metadataPath) && !string.IsNullOrWhiteSpace(metadataNetworkPath))
            {
                var metadataSubstitutionResult = SubstitutePathInternal(path, metadataPath, metadataNetworkPath);
                if (metadataSubstitutionResult.Item2)
                {
                    return metadataSubstitutionResult.Item1;
                }
            }

            foreach (var map in _configurationManager.Configuration.PathSubstitutions)
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
                throw new ArgumentNullException(nameof(path));
            }
            if (string.IsNullOrWhiteSpace(from))
            {
                throw new ArgumentNullException(nameof(from));
            }
            if (string.IsNullOrWhiteSpace(to))
            {
                throw new ArgumentNullException(nameof(to));
            }

            from = from.Trim();
            to = to.Trim();

            var newPath = path.Replace(from, to, StringComparison.OrdinalIgnoreCase);
            var changed = false;

            if (!string.Equals(newPath, path, StringComparison.Ordinal))
            {
                if (to.IndexOf('/', StringComparison.Ordinal) != -1)
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

            item.ExtraType = result.ExtraType;
        }

        public List<PersonInfo> GetPeople(InternalPeopleQuery query)
        {
            return _itemRepository.GetPeople(query);
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
            return _itemRepository.GetPeopleNames(query).Select(i =>
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
            return _itemRepository.GetPeopleNames(query);
        }

        public void UpdatePeople(BaseItem item, List<PersonInfo> people)
        {
            if (!item.SupportsPeople)
            {
                return;
            }

            _itemRepository.UpdatePeople(item.Id, people);
        }

        public async Task<ItemImageInfo> ConvertImageToLocal(BaseItem item, ItemImageInfo image, int imageIndex)
        {
            foreach (var url in image.Path.Split('|'))
            {
                try
                {
                    _logger.LogDebug("ConvertImageToLocal item {0} - image url: {1}", item.Id, url);

                    await ProviderManager.SaveImage(item, url, image.Type, imageIndex, CancellationToken.None).ConfigureAwait(false);

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
                throw new ArgumentNullException(nameof(name));
            }

            name = _fileSystem.GetValidFilename(name);

            var rootFolderPath = _configurationManager.ApplicationPaths.DefaultUserViewsPath;

            var virtualFolderPath = Path.Combine(rootFolderPath, name);
            while (Directory.Exists(virtualFolderPath))
            {
                name += "1";
                virtualFolderPath = Path.Combine(rootFolderPath, name);
            }

            var mediaPathInfos = options.PathInfos;
            if (mediaPathInfos != null)
            {
                var invalidpath = mediaPathInfos.FirstOrDefault(i => !Directory.Exists(i.Path));
                if (invalidpath != null)
                {
                    throw new ArgumentException("The specified path does not exist: " + invalidpath.Path + ".");
                }
            }

            LibraryMonitor.Stop();

            try
            {
                Directory.CreateDirectory(virtualFolderPath);

                if (!string.IsNullOrEmpty(collectionType))
                {
                    var path = Path.Combine(virtualFolderPath, collectionType + ".collection");

                    File.WriteAllBytes(path, Array.Empty<byte>());
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
                    LibraryMonitor.Start();
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

        private static bool ValidateNetworkPath(string path)
        {
            //if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            //{
            //    // We can't validate protocol-based paths, so just allow them
            //    if (path.IndexOf("://", StringComparison.OrdinalIgnoreCase) == -1)
            //    {
            //        return Directory.Exists(path);
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
                throw new ArgumentNullException(nameof(pathInfo));
            }

            var path = pathInfo.Path;

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(nameof(path));
            }

            if (!Directory.Exists(path))
            {
                throw new FileNotFoundException("The path does not exist.");
            }

            if (!string.IsNullOrWhiteSpace(pathInfo.NetworkPath) && !ValidateNetworkPath(pathInfo.NetworkPath))
            {
                throw new FileNotFoundException("The network path does not exist.");
            }

            var rootFolderPath = _configurationManager.ApplicationPaths.DefaultUserViewsPath;
            var virtualFolderPath = Path.Combine(rootFolderPath, virtualFolderName);

            var shortcutFilename = Path.GetFileNameWithoutExtension(path);

            var lnk = Path.Combine(virtualFolderPath, shortcutFilename + ShortcutFileExtension);

            while (File.Exists(lnk))
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
                throw new ArgumentNullException(nameof(pathInfo));
            }

            if (!string.IsNullOrWhiteSpace(pathInfo.NetworkPath) && !ValidateNetworkPath(pathInfo.NetworkPath))
            {
                throw new FileNotFoundException("The network path does not exist.");
            }

            var rootFolderPath = _configurationManager.ApplicationPaths.DefaultUserViewsPath;
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
                throw new ArgumentNullException(nameof(name));
            }

            var rootFolderPath = _configurationManager.ApplicationPaths.DefaultUserViewsPath;

            var path = Path.Combine(rootFolderPath, name);

            if (!Directory.Exists(path))
            {
                throw new FileNotFoundException("The media folder does not exist");
            }

            LibraryMonitor.Stop();

            try
            {
                Directory.Delete(path, true);
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
                    LibraryMonitor.Start();
                }
            }
        }

        private void RemoveContentTypeOverrides(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            var removeList = new List<NameValuePair>();

            foreach (var contentType in _configurationManager.Configuration.ContentTypes)
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
                _configurationManager.Configuration.ContentTypes = _configurationManager.Configuration.ContentTypes
                    .Except(removeList)
                    .ToArray();

                _configurationManager.SaveConfiguration();
            }
        }

        public void RemoveMediaPath(string virtualFolderName, string mediaPath)
        {
            if (string.IsNullOrEmpty(mediaPath))
            {
                throw new ArgumentNullException(nameof(mediaPath));
            }

            var rootFolderPath = _configurationManager.ApplicationPaths.DefaultUserViewsPath;
            var virtualFolderPath = Path.Combine(rootFolderPath, virtualFolderName);

            if (!Directory.Exists(virtualFolderPath))
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
