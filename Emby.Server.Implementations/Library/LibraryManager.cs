#pragma warning disable CS1591
#pragma warning disable CA5394

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Emby.Naming.Common;
using Emby.Naming.TV;
using Emby.Server.Implementations.Library.Resolvers;
using Emby.Server.Implementations.Library.Validators;
using Emby.Server.Implementations.Playlists;
using Emby.Server.Implementations.ScheduledTasks.Tasks;
using Emby.Server.Implementations.Sorting;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
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
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Episode = MediaBrowser.Controller.Entities.TV.Episode;
using EpisodeInfo = Emby.Naming.TV.EpisodeInfo;
using Genre = MediaBrowser.Controller.Entities.Genre;
using Person = MediaBrowser.Controller.Entities.Person;
using VideoResolver = Emby.Naming.Video.VideoResolver;

namespace Emby.Server.Implementations.Library
{
    /// <summary>
    /// Class LibraryManager.
    /// </summary>
    public class LibraryManager : ILibraryManager
    {
        private const string ShortcutFileExtension = ".mblink";

        private readonly ILogger<LibraryManager> _logger;
        private readonly ConcurrentDictionary<Guid, BaseItem> _cache;
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
        private readonly IImageProcessor _imageProcessor;
        private readonly NamingOptions _namingOptions;
        private readonly ExtraResolver _extraResolver;

        /// <summary>
        /// The _root folder sync lock.
        /// </summary>
        private readonly object _rootFolderSyncLock = new object();
        private readonly object _userRootFolderSyncLock = new object();

        private readonly TimeSpan _viewRefreshInterval = TimeSpan.FromHours(24);

        /// <summary>
        /// The _root folder.
        /// </summary>
        private volatile AggregateFolder? _rootFolder;
        private volatile UserRootFolder? _userRootFolder;

        private bool _wizardCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryManager" /> class.
        /// </summary>
        /// <param name="appHost">The application host.</param>
        /// <param name="loggerFactory">The logger factory.</param>
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
        /// <param name="namingOptions">The naming options.</param>
        /// <param name="directoryService">The directory service.</param>
        public LibraryManager(
            IServerApplicationHost appHost,
            ILoggerFactory loggerFactory,
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
            IImageProcessor imageProcessor,
            NamingOptions namingOptions,
            IDirectoryService directoryService)
        {
            _appHost = appHost;
            _logger = loggerFactory.CreateLogger<LibraryManager>();
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
            _cache = new ConcurrentDictionary<Guid, BaseItem>();
            _namingOptions = namingOptions;

            _extraResolver = new ExtraResolver(loggerFactory.CreateLogger<ExtraResolver>(), namingOptions, directoryService);

            _configurationManager.ConfigurationUpdated += ConfigurationUpdated;

            RecordConfigurationValues(configurationManager.Configuration);
        }

        /// <summary>
        /// Occurs when [item added].
        /// </summary>
        public event EventHandler<ItemChangeEventArgs>? ItemAdded;

        /// <summary>
        /// Occurs when [item updated].
        /// </summary>
        public event EventHandler<ItemChangeEventArgs>? ItemUpdated;

        /// <summary>
        /// Occurs when [item removed].
        /// </summary>
        public event EventHandler<ItemChangeEventArgs>? ItemRemoved;

        /// <summary>
        /// Gets the root folder.
        /// </summary>
        /// <value>The root folder.</value>
        public AggregateFolder RootFolder
        {
            get
            {
                if (_rootFolder is null)
                {
                    lock (_rootFolderSyncLock)
                    {
                        _rootFolder ??= CreateRootFolder();
                    }
                }

                return _rootFolder;
            }
        }

        private ILibraryMonitor LibraryMonitor => _libraryMonitorFactory.Value;

        private IProviderManager ProviderManager => _providerManagerFactory.Value;

        private IUserViewManager UserViewManager => _userviewManagerFactory.Value;

        /// <summary>
        /// Gets or sets the postscan tasks.
        /// </summary>
        /// <value>The postscan tasks.</value>
        private ILibraryPostScanTask[] PostscanTasks { get; set; } = Array.Empty<ILibraryPostScanTask>();

        /// <summary>
        /// Gets or sets the intro providers.
        /// </summary>
        /// <value>The intro providers.</value>
        private IIntroProvider[] IntroProviders { get; set; } = Array.Empty<IIntroProvider>();

        /// <summary>
        /// Gets or sets the list of entity resolution ignore rules.
        /// </summary>
        /// <value>The entity resolution ignore rules.</value>
        private IResolverIgnoreRule[] EntityResolutionIgnoreRules { get; set; } = Array.Empty<IResolverIgnoreRule>();

        /// <summary>
        /// Gets or sets the list of currently registered entity resolvers.
        /// </summary>
        /// <value>The entity resolvers enumerable.</value>
        private IItemResolver[] EntityResolvers { get; set; } = Array.Empty<IItemResolver>();

        private IMultiItemResolver[] MultiItemResolvers { get; set; } = Array.Empty<IMultiItemResolver>();

        /// <summary>
        /// Gets or sets the comparers.
        /// </summary>
        /// <value>The comparers.</value>
        private IBaseItemComparer[] Comparers { get; set; } = Array.Empty<IBaseItemComparer>();

        public bool IsScanRunning { get; private set; }

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
        private void ConfigurationUpdated(object? sender, EventArgs e)
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
            ArgumentNullException.ThrowIfNull(item);

            if (item is IItemByName)
            {
                if (item is not MusicArtist)
                {
                    return;
                }
            }
            else if (!item.IsFolder)
            {
                if (item is not Video && item is not LiveTvChannel)
                {
                    return;
                }
            }

            _cache[item.Id] = item;
        }

        public void DeleteItem(BaseItem item, DeleteOptions options)
        {
            DeleteItem(item, options, false);
        }

        public void DeleteItem(BaseItem item, DeleteOptions options, bool notifyParentItem)
        {
            ArgumentNullException.ThrowIfNull(item);

            var parent = item.GetOwner() ?? item.GetParent();

            DeleteItem(item, options, parent, notifyParentItem);
        }

        public void DeleteItem(BaseItem item, DeleteOptions options, BaseItem parent, bool notifyParentItem)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (item.SourceType == SourceType.Channel)
            {
                if (options.DeleteFromExternalProvider)
                {
                    try
                    {
                        BaseItem.ChannelManager.DeleteItem(item).GetAwaiter().GetResult();
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
                    "Removing item, Type: {Type}, Name: {Name}, Path: {Path}, Id: {Id}",
                    item.GetType().Name,
                    item.Name ?? "Unknown name",
                    item.Path ?? string.Empty,
                    item.Id);
            }
            else
            {
                _logger.LogInformation(
                    "Removing item, Type: {Type}, Name: {Name}, Path: {Path}, Id: {Id}",
                    item.GetType().Name,
                    item.Name ?? "Unknown name",
                    item.Path ?? string.Empty,
                    item.Id);
            }

            var children = item.IsFolder
                ? ((Folder)item).GetRecursiveChildren(false)
                : Array.Empty<BaseItem>();

            foreach (var metadataPath in GetMetadataPaths(item, children))
            {
                if (!Directory.Exists(metadataPath))
                {
                    continue;
                }

                _logger.LogDebug(
                    "Deleting metadata path, Type: {Type}, Name: {Name}, Path: {Path}, Id: {Id}",
                    item.GetType().Name,
                    item.Name ?? "Unknown name",
                    metadataPath,
                    item.Id);

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
                            _logger.LogInformation(
                                "Deleting item path, Type: {Type}, Name: {Name}, Path: {Path}, Id: {Id}",
                                item.GetType().Name,
                                item.Name ?? "Unknown name",
                                fileSystemInfo.FullName,
                                item.Id);

                            if (fileSystemInfo.IsDirectory)
                            {
                                Directory.Delete(fileSystemInfo.FullName, true);
                            }
                            else
                            {
                                File.Delete(fileSystemInfo.FullName);
                            }
                        }
                        catch (DirectoryNotFoundException)
                        {
                            _logger.LogInformation(
                                "Directory not found, only removing from database, Type: {Type}, Name: {Name}, Path: {Path}, Id: {Id}",
                                item.GetType().Name,
                                item.Name ?? "Unknown name",
                                fileSystemInfo.FullName,
                                item.Id);
                        }
                        catch (FileNotFoundException)
                        {
                            _logger.LogInformation(
                                "File not found, only removing from database, Type: {Type}, Name: {Name}, Path: {Path}, Id: {Id}",
                                item.GetType().Name,
                                item.Name ?? "Unknown name",
                                fileSystemInfo.FullName,
                                item.Id);
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
                _cache.TryRemove(child.Id, out _);
            }

            _cache.TryRemove(item.Id, out _);

            ReportItemRemoved(item, parent);
        }

        private static List<string> GetMetadataPaths(BaseItem item, IEnumerable<BaseItem> children)
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
        private BaseItem? ResolveItem(ItemResolveArgs args, IItemResolver[]? resolvers)
        {
            var item = (resolvers ?? EntityResolvers).Select(r => Resolve(args, r))
                .FirstOrDefault(i => i is not null);

            if (item is not null)
            {
                ResolverHelper.SetInitialItemValues(item, args, _fileSystem, this);
            }

            return item;
        }

        private BaseItem? Resolve(ItemResolveArgs args, IItemResolver resolver)
        {
            try
            {
                return resolver.ResolvePath(args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Resolver} resolving {Path}", resolver.GetType().Name, args.Path);
                return null;
            }
        }

        public Guid GetNewItemId(string key, Type type)
        {
            return GetNewItemIdInternal(key, type, false);
        }

        private Guid GetNewItemIdInternal(string key, Type type, bool forceCaseInsensitive)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            ArgumentNullException.ThrowIfNull(type);

            string programDataPath = _configurationManager.ApplicationPaths.ProgramDataPath;
            if (key.StartsWith(programDataPath, StringComparison.Ordinal))
            {
                // Try to normalize paths located underneath program-data in an attempt to make them more portable
                key = key.Substring(programDataPath.Length)
                    .TrimStart('/', '\\')
                    .Replace('/', '\\');
            }

            if (forceCaseInsensitive || !_configurationManager.Configuration.EnableCaseSensitiveItemIds)
            {
                key = key.ToLowerInvariant();
            }

            key = type.FullName + key;

            return key.GetMD5();
        }

        public BaseItem? ResolvePath(FileSystemMetadata fileInfo, Folder? parent = null, IDirectoryService? directoryService = null)
            => ResolvePath(fileInfo, directoryService ?? new DirectoryService(_fileSystem), null, parent);

        private BaseItem? ResolvePath(
            FileSystemMetadata fileInfo,
            IDirectoryService directoryService,
            IItemResolver[]? resolvers,
            Folder? parent = null,
            CollectionType? collectionType = null,
            LibraryOptions? libraryOptions = null)
        {
            ArgumentNullException.ThrowIfNull(fileInfo);

            var fullPath = fileInfo.FullName;

            if (collectionType is null && parent is not null)
            {
                collectionType = GetContentTypeOverride(fullPath, true);
            }

            var args = new ItemResolveArgs(_configurationManager.ApplicationPaths, this)
            {
                Parent = parent,
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
                    if (parent is not null && parent.IsPhysicalRoot)
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

        public bool IgnoreFile(FileSystemMetadata file, BaseItem? parent)
            => EntityResolutionIgnoreRules.Any(r => r.ShouldIgnore(file, parent));

        public List<FileSystemMetadata> NormalizeRootPathList(IEnumerable<FileSystemMetadata> paths)
        {
            var originalList = paths.ToList();

            var list = originalList.Where(i => i.IsDirectory)
                .Select(i => Path.TrimEndingDirectorySeparator(i.FullName))
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
        /// Determines whether a path should be ignored based on its contents - called after the contents have been read.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private static bool ShouldResolvePathContents(ItemResolveArgs args)
        {
            // Ignore any folders containing a file called .ignore
            return !args.ContainsFileSystemEntryByName(".ignore");
        }

        public IEnumerable<BaseItem> ResolvePaths(IEnumerable<FileSystemMetadata> files, IDirectoryService directoryService, Folder parent, LibraryOptions libraryOptions, CollectionType? collectionType = null)
        {
            return ResolvePaths(files, directoryService, parent, libraryOptions, collectionType, EntityResolvers);
        }

        public IEnumerable<BaseItem> ResolvePaths(
            IEnumerable<FileSystemMetadata> files,
            IDirectoryService directoryService,
            Folder parent,
            LibraryOptions libraryOptions,
            CollectionType? collectionType,
            IItemResolver[] resolvers)
        {
            var fileList = files.Where(i => !IgnoreFile(i, parent)).ToList();

            if (parent is not null)
            {
                var multiItemResolvers = resolvers is null ? MultiItemResolvers : resolvers.OfType<IMultiItemResolver>();

                foreach (var resolver in multiItemResolvers)
                {
                    var result = resolver.ResolveMultiple(parent, fileList, collectionType, directoryService);

                    if (result?.Items.Count > 0)
                    {
                        var items = result.Items;
                        items.RemoveAll(item => !ResolverHelper.SetInitialItemValues(item, parent, this, directoryService));
                        items.AddRange(ResolveFileList(result.ExtraFiles, directoryService, parent, collectionType, resolvers, libraryOptions));
                        return items;
                    }
                }
            }

            return ResolveFileList(fileList, directoryService, parent, collectionType, resolvers, libraryOptions);
        }

        private IEnumerable<BaseItem> ResolveFileList(
            IReadOnlyList<FileSystemMetadata> fileList,
            IDirectoryService directoryService,
            Folder? parent,
            CollectionType? collectionType,
            IItemResolver[]? resolvers,
            LibraryOptions libraryOptions)
        {
            // Given that fileList is a list we can save enumerator allocations by indexing
            for (var i = 0; i < fileList.Count; i++)
            {
                var file = fileList[i];
                BaseItem? result = null;
                try
                {
                    result = ResolvePath(file, directoryService, resolvers, parent, collectionType, libraryOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error resolving path {Path}", file.FullName);
                }

                if (result is not null)
                {
                    yield return result;
                }
            }
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

            var rootFolder = GetItemById(GetNewItemId(rootFolderPath, typeof(AggregateFolder))) as AggregateFolder ??
                             (ResolvePath(_fileSystem.GetDirectoryInfo(rootFolderPath)) as Folder ?? throw new InvalidOperationException("Something went very wong"))
                             .DeepCopy<Folder, AggregateFolder>();

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

            if (folder.Id.IsEmpty())
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

            if (dbItem is not null && string.Equals(dbItem.Path, folder.Path, StringComparison.OrdinalIgnoreCase))
            {
                folder = dbItem;
            }

            if (!folder.ParentId.Equals(rootFolder.Id))
            {
                folder.ParentId = rootFolder.Id;
                folder.UpdateToRepositoryAsync(ItemUpdateType.MetadataImport, CancellationToken.None).GetAwaiter().GetResult();
            }

            rootFolder.AddVirtualChild(folder);

            RegisterItem(folder);

            return rootFolder;
        }

        public Folder GetUserRootFolder()
        {
            if (_userRootFolder is null)
            {
                lock (_userRootFolderSyncLock)
                {
                    if (_userRootFolder is null)
                    {
                        var userRootPath = _configurationManager.ApplicationPaths.DefaultUserViewsPath;

                        _logger.LogDebug("Creating userRootPath at {Path}", userRootPath);
                        Directory.CreateDirectory(userRootPath);

                        var newItemId = GetNewItemId(userRootPath, typeof(UserRootFolder));
                        UserRootFolder? tmpItem = null;
                        try
                        {
                            tmpItem = GetItemById(newItemId) as UserRootFolder;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error creating UserRootFolder {Path}", newItemId);
                        }

                        if (tmpItem is null)
                        {
                            _logger.LogDebug("Creating new userRootFolder with DeepCopy");
                            tmpItem = (ResolvePath(_fileSystem.GetDirectoryInfo(userRootPath)) as Folder ?? throw new InvalidOperationException("Failed to get user root path"))
                                        .DeepCopy<Folder, UserRootFolder>();
                        }

                        // In case program data folder was moved
                        if (!string.Equals(tmpItem.Path, userRootPath, StringComparison.Ordinal))
                        {
                            _logger.LogInformation("Resetting user root folder path to {0}", userRootPath);
                            tmpItem.Path = userRootPath;
                        }

                        _userRootFolder = tmpItem;
                        _logger.LogDebug("Setting userRootFolder: {Folder}", _userRootFolder);
                    }
                }
            }

            return _userRootFolder;
        }

        /// <inheritdoc />
        public BaseItem? FindByPath(string path, bool? isFolder)
        {
            // If this returns multiple items it could be tricky figuring out which one is correct.
            // In most cases, the newest one will be and the others obsolete but not yet cleaned up
            ArgumentException.ThrowIfNullOrEmpty(path);

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

        /// <inheritdoc />
        public Person? GetPerson(string name)
        {
            var path = Person.GetPath(name);
            var id = GetItemByNameId<Person>(path);
            if (GetItemById(id) is Person item)
            {
                return item;
            }

            return null;
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
            return GetItemByNameId<Studio>(Studio.GetPath(name));
        }

        public Guid GetGenreId(string name)
        {
            return GetItemByNameId<Genre>(Genre.GetPath(name));
        }

        public Guid GetMusicGenreId(string name)
        {
            return GetItemByNameId<MusicGenre>(MusicGenre.GetPath(name));
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
        /// Gets a Genre.
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
                    IncludeItemTypes = new[] { BaseItemKind.MusicArtist },
                    Name = name,
                    DtoOptions = options
                }).Cast<MusicArtist>()
                .OrderBy(i => i.IsAccessedByName ? 1 : 0)
                .Cast<T>()
                .FirstOrDefault();

                if (existing is not null)
                {
                    return existing;
                }
            }

            var path = getPathFn(name);
            var id = GetItemByNameId<T>(path);
            var item = GetItemById(id) as T;
            if (item is null)
            {
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

        private Guid GetItemByNameId<T>(string path)
              where T : BaseItem, new()
        {
            var forceCaseInsensitiveId = _configurationManager.Configuration.EnableNormalizedItemByNameIds;
            return GetNewItemIdInternal(path, typeof(T), forceCaseInsensitiveId);
        }

        /// <inheritdoc />
        public Task ValidatePeopleAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            // Ensure the location is available.
            Directory.CreateDirectory(_configurationManager.ApplicationPaths.PeoplePath);

            return new PeopleValidator(this, _logger, _fileSystem).ValidatePeople(cancellationToken, progress);
        }

        /// <summary>
        /// Reloads the root media folder.
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

        public async Task ValidateTopLibraryFolders(CancellationToken cancellationToken, bool removeRoot = false)
        {
            await RootFolder.RefreshMetadata(cancellationToken).ConfigureAwait(false);

            // Start by just validating the children of the root, but go no further
            await RootFolder.ValidateChildren(
                new Progress<double>(),
                new MetadataRefreshOptions(new DirectoryService(_fileSystem)),
                recursive: false,
                allowRemoveRoot: removeRoot,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            await GetUserRootFolder().RefreshMetadata(cancellationToken).ConfigureAwait(false);

            await GetUserRootFolder().ValidateChildren(
                new Progress<double>(),
                new MetadataRefreshOptions(new DirectoryService(_fileSystem)),
                recursive: false,
                allowRemoveRoot: removeRoot,
                cancellationToken: cancellationToken).ConfigureAwait(false);

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

            var innerProgress = new Progress<double>(pct => progress.Report(pct * 0.96));

            // Validate the entire media library
            await RootFolder.ValidateChildren(innerProgress, new MetadataRefreshOptions(new DirectoryService(_fileSystem)), recursive: true, cancellationToken: cancellationToken).ConfigureAwait(false);

            progress.Report(96);

            innerProgress = new Progress<double>(pct => progress.Report(96 + (pct * .04)));

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
                // Prevent access to modified closure
                var currentNumComplete = numComplete;

                var innerProgress = new Progress<double>(pct =>
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

            _itemRepository.UpdateInheritedValues();

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

        private VirtualFolderInfo GetVirtualFolderInfo(string dir, List<BaseItem> allCollectionFolders, HashSet<Guid>? refreshQueue)
        {
            var info = new VirtualFolderInfo
            {
                Name = Path.GetFileName(dir),

                Locations = _fileSystem.GetFilePaths(dir, false)
                .Where(i => Path.GetExtension(i.AsSpan()).Equals(ShortcutFileExtension, StringComparison.OrdinalIgnoreCase))
                    .Select(i =>
                    {
                        try
                        {
                            return _appHost.ExpandVirtualPath(_fileSystem.ResolveShortcut(i));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error resolving shortcut file {File}", i);
                            return null;
                        }
                    })
                    .Where(i => i is not null)
                    .Order()
                    .ToArray(),

                CollectionType = GetCollectionType(dir)
            };

            var libraryFolder = allCollectionFolders.FirstOrDefault(i => string.Equals(i.Path, dir, StringComparison.OrdinalIgnoreCase));
            if (libraryFolder is not null)
            {
                var libraryFolderId = libraryFolder.Id.ToString("N", CultureInfo.InvariantCulture);
                info.ItemId = libraryFolderId;
                if (libraryFolder.HasImage(ImageType.Primary))
                {
                    info.PrimaryImageItemId = libraryFolderId;
                }

                info.LibraryOptions = GetLibraryOptions(libraryFolder);

                if (refreshQueue is not null)
                {
                    info.RefreshProgress = libraryFolder.GetRefreshProgress();

                    info.RefreshStatus = info.RefreshProgress.HasValue ? "Active" : refreshQueue.Contains(libraryFolder.Id) ? "Queued" : "Idle";
                }
            }

            return info;
        }

        private CollectionTypeOptions? GetCollectionType(string path)
        {
            var files = _fileSystem.GetFilePaths(path, new[] { ".collection" }, true, false);
            foreach (ReadOnlySpan<char> file in files)
            {
                if (Enum.TryParse<CollectionTypeOptions>(Path.GetFileNameWithoutExtension(file), true, out var res))
                {
                    return res;
                }
            }

            return null;
        }

        /// <inheritdoc />
        public BaseItem? GetItemById(Guid id)
        {
            if (id.IsEmpty())
            {
                throw new ArgumentException("Guid can't be empty", nameof(id));
            }

            if (_cache.TryGetValue(id, out BaseItem? item))
            {
                return item;
            }

            item = RetrieveItem(id);

            if (item is not null)
            {
                RegisterItem(item);
            }

            return item;
        }

        /// <inheritdoc />
        public T? GetItemById<T>(Guid id)
         where T : BaseItem
        {
            var item = GetItemById(id);
            if (item is T typedItem)
            {
                return typedItem;
            }

            return null;
        }

        /// <inheritdoc />
        public T? GetItemById<T>(Guid id, Guid userId)
            where T : BaseItem
        {
            var user = userId.IsEmpty() ? null : _userManager.GetUserById(userId);
            return GetItemById<T>(id, user);
        }

        /// <inheritdoc />
        public T? GetItemById<T>(Guid id, User? user)
            where T : BaseItem
        {
            var item = GetItemById<T>(id);
            return ItemIsVisible(item, user) ? item : null;
        }

        public List<BaseItem> GetItemList(InternalItemsQuery query, bool allowExternalContent)
        {
            if (query.Recursive && !query.ParentId.IsEmpty())
            {
                var parent = GetItemById(query.ParentId);
                if (parent is not null)
                {
                    SetTopParentIdsOrAncestors(query, new[] { parent });
                }
            }

            if (query.User is not null)
            {
                AddUserToQuery(query, query.User, allowExternalContent);
            }

            var itemList = _itemRepository.GetItemList(query);
            var user = query.User;
            if (user is not null)
            {
                return itemList.Where(i => i.IsVisible(user)).ToList();
            }

            return itemList;
        }

        public List<BaseItem> GetItemList(InternalItemsQuery query)
        {
            return GetItemList(query, true);
        }

        public int GetCount(InternalItemsQuery query)
        {
            if (query.Recursive && !query.ParentId.IsEmpty())
            {
                var parent = GetItemById(query.ParentId);
                if (parent is not null)
                {
                    SetTopParentIdsOrAncestors(query, new[] { parent });
                }
            }

            if (query.User is not null)
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
                if (query.User is not null)
                {
                    AddUserToQuery(query, query.User);
                }
            }

            return _itemRepository.GetItemList(query);
        }

        public QueryResult<BaseItem> QueryItems(InternalItemsQuery query)
        {
            if (query.User is not null)
            {
                AddUserToQuery(query, query.User);
            }

            if (query.EnableTotalRecordCount)
            {
                return _itemRepository.GetItems(query);
            }

            return new QueryResult<BaseItem>(
                query.StartIndex,
                null,
                _itemRepository.GetItemList(query));
        }

        public List<Guid> GetItemIds(InternalItemsQuery query)
        {
            if (query.User is not null)
            {
                AddUserToQuery(query, query.User);
            }

            return _itemRepository.GetItemIdsList(query);
        }

        public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetStudios(InternalItemsQuery query)
        {
            if (query.User is not null)
            {
                AddUserToQuery(query, query.User);
            }

            SetTopParentOrAncestorIds(query);
            return _itemRepository.GetStudios(query);
        }

        public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetGenres(InternalItemsQuery query)
        {
            if (query.User is not null)
            {
                AddUserToQuery(query, query.User);
            }

            SetTopParentOrAncestorIds(query);
            return _itemRepository.GetGenres(query);
        }

        public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetMusicGenres(InternalItemsQuery query)
        {
            if (query.User is not null)
            {
                AddUserToQuery(query, query.User);
            }

            SetTopParentOrAncestorIds(query);
            return _itemRepository.GetMusicGenres(query);
        }

        public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetAllArtists(InternalItemsQuery query)
        {
            if (query.User is not null)
            {
                AddUserToQuery(query, query.User);
            }

            SetTopParentOrAncestorIds(query);
            return _itemRepository.GetAllArtists(query);
        }

        public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetArtists(InternalItemsQuery query)
        {
            if (query.User is not null)
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
                parents[i] = GetItemById(ancestorIds[i]) ?? throw new ArgumentException($"Failed to find parent with id: {ancestorIds[i]}");
                if (parents[i] is not (ICollectionFolder or UserView))
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
                query.TopParentIds = [Guid.NewGuid()];
            }
        }

        public QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetAlbumArtists(InternalItemsQuery query)
        {
            if (query.User is not null)
            {
                AddUserToQuery(query, query.User);
            }

            SetTopParentOrAncestorIds(query);
            return _itemRepository.GetAlbumArtists(query);
        }

        public QueryResult<BaseItem> GetItemsResult(InternalItemsQuery query)
        {
            if (query.Recursive && !query.ParentId.IsEmpty())
            {
                var parent = GetItemById(query.ParentId);
                if (parent is not null)
                {
                    SetTopParentIdsOrAncestors(query, new[] { parent });
                }
            }

            if (query.User is not null)
            {
                AddUserToQuery(query, query.User);
            }

            if (query.EnableTotalRecordCount)
            {
                return _itemRepository.GetItems(query);
            }

            return new QueryResult<BaseItem>(
                query.StartIndex,
                null,
                _itemRepository.GetItemList(query));
        }

        private void SetTopParentIdsOrAncestors(InternalItemsQuery query, IReadOnlyCollection<BaseItem> parents)
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
                query.ParentId.IsEmpty() &&
                query.ChannelIds.Count == 0 &&
                query.TopParentIds.Length == 0 &&
                string.IsNullOrEmpty(query.AncestorWithPresentationUniqueKey) &&
                string.IsNullOrEmpty(query.SeriesPresentationUniqueKey) &&
                query.ItemIds.Length == 0)
            {
                var userViews = UserViewManager.GetUserViews(new UserViewQuery
                {
                    User = user,
                    IncludeHidden = true,
                    IncludeExternalContent = allowExternalContent
                });

                query.TopParentIds = userViews.SelectMany(i => GetTopParentIdsForQuery(i, user)).ToArray();

                // Prevent searching in all libraries due to empty filter
                if (query.TopParentIds.Length == 0)
                {
                    query.TopParentIds = new[] { Guid.NewGuid() };
                }
            }
        }

        private IEnumerable<Guid> GetTopParentIdsForQuery(BaseItem item, User? user)
        {
            if (item is UserView view)
            {
                if (view.ViewType == CollectionType.livetv)
                {
                    return new[] { view.Id };
                }

                // Translate view into folders
                if (!view.DisplayParentId.IsEmpty())
                {
                    var displayParent = GetItemById(view.DisplayParentId);
                    if (displayParent is not null)
                    {
                        return GetTopParentIdsForQuery(displayParent, user);
                    }

                    return Array.Empty<Guid>();
                }

                if (!view.ParentId.IsEmpty())
                {
                    var displayParent = GetItemById(view.ParentId);
                    if (displayParent is not null)
                    {
                        return GetTopParentIdsForQuery(displayParent, user);
                    }

                    return Array.Empty<Guid>();
                }

                // Handle grouping
                if (user is not null && view.ViewType != CollectionType.unknown && UserView.IsEligibleForGrouping(view.ViewType)
                    && user.GetPreference(PreferenceKind.GroupedFolders).Length > 0)
                {
                    return GetUserRootFolder()
                        .GetChildren(user, true)
                        .OfType<CollectionFolder>()
                        .Where(i => i.CollectionType is null || i.CollectionType == view.ViewType)
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
            if (topParent is not null)
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
            if (IntroProviders.Length == 0)
            {
                return [];
            }

            var tasks = IntroProviders
                .Select(i => GetIntros(i, item, user));

            var items = await Task.WhenAll(tasks).ConfigureAwait(false);

            return items
                .SelectMany(i => i)
                .Select(ResolveIntro)
                .Where(i => i is not null)!; // null values got filtered out
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

                return Enumerable.Empty<IntroInfo>();
            }
        }

        /// <summary>
        /// Resolves the intro.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>Video.</returns>
        private Video? ResolveIntro(IntroInfo info)
        {
            Video? video = null;

            if (info.ItemId.HasValue)
            {
                // Get an existing item by Id
                video = GetItemById(info.ItemId.Value) as Video;

                if (video is null)
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

                    if (video is null)
                    {
                        _logger.LogError("Intro resolver returned null for {Path}.", info.Path);
                    }
                    else
                    {
                        // Pull the saved db item that will include metadata
                        var dbItem = GetItemById(video.Id) as Video;

                        if (dbItem is not null)
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
                    _logger.LogError(ex, "Error resolving path {Path}.", info.Path);
                }
            }
            else
            {
                _logger.LogError("IntroProvider returned an IntroInfo with null Path and ItemId.");
            }

            return video;
        }

        /// <inheritdoc />
        public IEnumerable<BaseItem> Sort(IEnumerable<BaseItem> items, User? user, IEnumerable<ItemSortBy> sortBy, SortOrder sortOrder)
        {
            IOrderedEnumerable<BaseItem>? orderedItems = null;

            foreach (var orderBy in sortBy.Select(o => GetComparer(o, user)).Where(c => c is not null))
            {
                if (orderBy is RandomComparer)
                {
                    var randomItems = items.ToArray();
                    Random.Shared.Shuffle(randomItems);
                    items = randomItems;
                    // Items are no longer ordered at this point, so set orderedItems back to null
                    orderedItems = null;
                }
                else if (orderedItems is null)
                {
                    orderedItems = sortOrder == SortOrder.Descending
                        ? items.OrderByDescending(i => i, orderBy)
                        : items.OrderBy(i => i, orderBy);
                }
                else
                {
                    orderedItems = sortOrder == SortOrder.Descending
                        ? orderedItems!.ThenByDescending(i => i, orderBy)
                        : orderedItems!.ThenBy(i => i, orderBy); // orderedItems is set during the first iteration
                }
            }

            return orderedItems ?? items;
        }

        /// <inheritdoc />
        public IEnumerable<BaseItem> Sort(IEnumerable<BaseItem> items, User? user, IEnumerable<(ItemSortBy OrderBy, SortOrder SortOrder)> orderBy)
        {
            IOrderedEnumerable<BaseItem>? orderedItems = null;

            foreach (var (name, sortOrder) in orderBy)
            {
                var comparer = GetComparer(name, user);
                if (comparer is null)
                {
                    continue;
                }

                if (comparer is RandomComparer)
                {
                    var randomItems = items.ToArray();
                    Random.Shared.Shuffle(randomItems);
                    items = randomItems;
                    // Items are no longer ordered at this point, so set orderedItems back to null
                    orderedItems = null;
                }
                else if (orderedItems is null)
                {
                    orderedItems = sortOrder == SortOrder.Descending
                        ? items.OrderByDescending(i => i, comparer)
                        : items.OrderBy(i => i, comparer);
                }
                else
                {
                    orderedItems = sortOrder == SortOrder.Descending
                        ? orderedItems!.ThenByDescending(i => i, comparer)
                        : orderedItems!.ThenBy(i => i, comparer); // orderedItems is set during the first iteration
                }
            }

            return orderedItems ?? items;
        }

        /// <summary>
        /// Gets the comparer.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="user">The user.</param>
        /// <returns>IBaseItemComparer.</returns>
        private IBaseItemComparer? GetComparer(ItemSortBy name, User? user)
        {
            var comparer = Comparers.FirstOrDefault(c => name == c.Type);

            // If it requires a user, create a new one, and assign the user
            if (comparer is IUserBaseItemComparer)
            {
                var userComparer = (IUserBaseItemComparer)Activator.CreateInstance(comparer.GetType())!; // only null for Nullable<T> instances

                userComparer.User = user;
                userComparer.UserManager = _userManager;
                userComparer.UserDataRepository = _userDataRepository;

                return userComparer;
            }

            return comparer;
        }

        /// <inheritdoc />
        public void CreateItem(BaseItem item, BaseItem? parent)
        {
            CreateItems(new[] { item }, parent, CancellationToken.None);
        }

        /// <inheritdoc />
        public void CreateItems(IReadOnlyList<BaseItem> items, BaseItem? parent, CancellationToken cancellationToken)
        {
            _itemRepository.SaveItems(items, cancellationToken);

            foreach (var item in items)
            {
                RegisterItem(item);
            }

            if (ItemAdded is not null)
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
            if (image.Path is not null && image.IsLocalFile)
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

            return image.Path is not null && !image.IsLocalFile;
        }

        /// <inheritdoc />
        public async Task UpdateImagesAsync(BaseItem item, bool forceUpdate = false)
        {
            ArgumentNullException.ThrowIfNull(item);

            var outdated = forceUpdate
                ? item.ImageInfos.Where(i => i.Path is not null).ToArray()
                : item.ImageInfos.Where(ImageNeedsRefresh).ToArray();
            // Skip image processing if current or live tv source
            if (outdated.Length == 0 || item.SourceType != SourceType.Library)
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
                        image = await ConvertImageToLocal(item, img, index, true).ConfigureAwait(false);
                    }
                    catch (ArgumentException)
                    {
                        _logger.LogWarning("Cannot get image index for {ImagePath}", img.Path);
                        continue;
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or IOException)
                    {
                        _logger.LogWarning(ex, "Cannot fetch image from {ImagePath}", img.Path);
                        continue;
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogWarning(ex, "Cannot fetch image from {ImagePath}. Http status code: {HttpStatus}", img.Path, ex.StatusCode);
                        continue;
                    }
                }

                ImageDimensions size;
                try
                {
                    size = _imageProcessor.GetImageDimensions(item, image);
                    image.Width = size.Width;
                    image.Height = size.Height;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cannot get image dimensions for {ImagePath}", image.Path);
                    size = default;
                    image.Width = 0;
                    image.Height = 0;
                }

                try
                {
                    image.BlurHash = _imageProcessor.GetImageBlurHash(image.Path, size);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cannot compute blurhash for {ImagePath}", image.Path);
                    image.BlurHash = string.Empty;
                }

                try
                {
                    image.DateModified = _fileSystem.GetLastWriteTimeUtc(image.Path);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cannot update DateModified for {ImagePath}", image.Path);
                }
            }

            _itemRepository.SaveImages(item);
            RegisterItem(item);
        }

        /// <inheritdoc />
        public async Task UpdateItemsAsync(IReadOnlyList<BaseItem> items, BaseItem parent, ItemUpdateType updateReason, CancellationToken cancellationToken)
        {
            foreach (var item in items)
            {
                await RunMetadataSavers(item, updateReason).ConfigureAwait(false);
            }

            _itemRepository.SaveItems(items, cancellationToken);

            if (ItemUpdated is not null)
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

        /// <inheritdoc />
        public Task UpdateItemAsync(BaseItem item, BaseItem parent, ItemUpdateType updateReason, CancellationToken cancellationToken)
            => UpdateItemsAsync(new[] { item }, parent, updateReason, cancellationToken);

        public async Task RunMetadataSavers(BaseItem item, ItemUpdateType updateReason)
        {
            if (item.IsFileProtocol)
            {
                await ProviderManager.SaveMetadataAsync(item, updateReason).ConfigureAwait(false);
            }

            item.DateLastSaved = DateTime.UtcNow;

            await UpdateImagesAsync(item, updateReason >= ItemUpdateType.ImageUpdate).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports the item removed.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="parent">The parent item.</param>
        public void ReportItemRemoved(BaseItem item, BaseItem parent)
        {
            if (ItemRemoved is not null)
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
            return GetCollectionFolders(item, GetUserRootFolder().Children.OfType<Folder>());
        }

        public List<Folder> GetCollectionFolders(BaseItem item, IEnumerable<Folder> allUserRootChildren)
        {
            while (item is not null)
            {
                var parent = item.GetParent();

                if (parent is AggregateFolder)
                {
                    break;
                }

                if (parent is null)
                {
                    var owner = item.GetOwner();

                    if (owner is null)
                    {
                        break;
                    }

                    item = owner;
                }
                else
                {
                    item = parent;
                }
            }

            if (item is null)
            {
                return new List<Folder>();
            }

            return GetCollectionFoldersInternal(item, allUserRootChildren);
        }

        private static List<Folder> GetCollectionFoldersInternal(BaseItem item, IEnumerable<Folder> allUserRootChildren)
        {
            return allUserRootChildren
                .Where(i => string.Equals(i.Path, item.Path, StringComparison.OrdinalIgnoreCase) || i.PhysicalLocations.Contains(item.Path.AsSpan(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public LibraryOptions GetLibraryOptions(BaseItem item)
        {
            if (item is CollectionFolder collectionFolder)
            {
                return collectionFolder.GetLibraryOptions();
            }

            // List.Find is more performant than FirstOrDefault due to enumerator allocation
            return GetCollectionFolders(item)
                .Find(folder => folder is CollectionFolder) is CollectionFolder collectionFolder2
                ? collectionFolder2.GetLibraryOptions()
                : new LibraryOptions();
        }

        public CollectionType? GetContentType(BaseItem item)
        {
            var configuredContentType = GetConfiguredContentType(item, false);
            if (configuredContentType is not null)
            {
                return configuredContentType;
            }

            configuredContentType = GetConfiguredContentType(item, true);
            if (configuredContentType is not null)
            {
                return configuredContentType;
            }

            return GetInheritedContentType(item);
        }

        public CollectionType? GetInheritedContentType(BaseItem item)
        {
            var type = GetTopFolderContentType(item);

            if (type is not null)
            {
                return type;
            }

            return item.GetParents()
                .Select(GetConfiguredContentType)
                .LastOrDefault(i => i is not null);
        }

        public CollectionType? GetConfiguredContentType(BaseItem item)
        {
            return GetConfiguredContentType(item, false);
        }

        public CollectionType? GetConfiguredContentType(string path)
        {
            return GetContentTypeOverride(path, false);
        }

        public CollectionType? GetConfiguredContentType(BaseItem item, bool inheritConfiguredPath)
        {
            if (item is ICollectionFolder collectionFolder)
            {
                return collectionFolder.CollectionType;
            }

            return GetContentTypeOverride(item.ContainingFolderPath, inheritConfiguredPath);
        }

        private CollectionType? GetContentTypeOverride(string path, bool inherit)
        {
            var nameValuePair = _configurationManager.Configuration.ContentTypes
                                    .FirstOrDefault(i => _fileSystem.AreEqual(i.Name, path)
                                                         || (inherit && !string.IsNullOrEmpty(i.Name)
                                                                     && _fileSystem.ContainsSubPath(i.Name, path)));
            if (Enum.TryParse<CollectionType>(nameValuePair?.Value, out var collectionType))
            {
                return collectionType;
            }

            return null;
        }

        private CollectionType? GetTopFolderContentType(BaseItem item)
        {
            if (item is null)
            {
                return null;
            }

            while (!item.ParentId.IsEmpty())
            {
                var parent = item.GetParent();
                if (parent is null || parent is AggregateFolder)
                {
                    break;
                }

                item = parent;
            }

            return GetUserRootFolder().Children
                .OfType<ICollectionFolder>()
                .Where(i => string.Equals(i.Path, item.Path, StringComparison.OrdinalIgnoreCase) || i.PhysicalLocations.Contains(item.Path))
                .Select(i => i.CollectionType)
                .FirstOrDefault(i => i is not null);
        }

        public UserView GetNamedView(
            User user,
            string name,
            CollectionType? viewType,
            string sortName)
        {
            return GetNamedView(user, name, Guid.Empty, viewType, sortName);
        }

        public UserView GetNamedView(
            string name,
            CollectionType viewType,
            string sortName)
        {
            var path = Path.Combine(
                _configurationManager.ApplicationPaths.InternalMetadataPath,
                "views",
                _fileSystem.GetValidFilename(viewType.ToString()));

            var id = GetNewItemId(path + "_namedview_" + name, typeof(UserView));

            var item = GetItemById(id) as UserView;

            var refresh = false;

            if (item is null || !string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase))
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
                item.UpdateToRepositoryAsync(ItemUpdateType.MetadataImport, CancellationToken.None).GetAwaiter().GetResult();
                ProviderManager.QueueRefresh(item.Id, new MetadataRefreshOptions(new DirectoryService(_fileSystem)), RefreshPriority.Normal);
            }

            return item;
        }

        public UserView GetNamedView(
            User user,
            string name,
            Guid parentId,
            CollectionType? viewType,
            string sortName)
        {
            var parentIdString = parentId.IsEmpty()
                ? null
                : parentId.ToString("N", CultureInfo.InvariantCulture);
            var idValues = "38_namedview_" + name + user.Id.ToString("N", CultureInfo.InvariantCulture) + (parentIdString ?? string.Empty) + (viewType?.ToString() ?? string.Empty);

            var id = GetNewItemId(idValues, typeof(UserView));

            var path = Path.Combine(_configurationManager.ApplicationPaths.InternalMetadataPath, "views", id.ToString("N", CultureInfo.InvariantCulture));

            var item = GetItemById(id) as UserView;

            var isNew = false;

            if (item is null)
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

            if (!refresh && !item.DisplayParentId.IsEmpty())
            {
                var displayParent = GetItemById(item.DisplayParentId);
                refresh = displayParent is not null && displayParent.DateLastSaved > item.DateLastRefreshed;
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
            CollectionType? viewType,
            string sortName)
        {
            ArgumentNullException.ThrowIfNull(parent);

            var name = parent.Name;
            var parentId = parent.Id;

            var idValues = "38_namedview_" + name + parentId + (viewType?.ToString() ?? string.Empty);

            var id = GetNewItemId(idValues, typeof(UserView));

            var path = parent.Path;

            var item = GetItemById(id) as UserView;

            var isNew = false;

            if (item is null)
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

            if (!refresh && !item.DisplayParentId.IsEmpty())
            {
                var displayParent = GetItemById(item.DisplayParentId);
                refresh = displayParent is not null && displayParent.DateLastSaved > item.DateLastRefreshed;
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
            CollectionType? viewType,
            string sortName,
            string uniqueId)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            var parentIdString = parentId.IsEmpty()
                ? null
                : parentId.ToString("N", CultureInfo.InvariantCulture);
            var idValues = "37_namedview_" + name + (parentIdString ?? string.Empty) + (viewType?.ToString() ?? string.Empty);
            if (!string.IsNullOrEmpty(uniqueId))
            {
                idValues += uniqueId;
            }

            var id = GetNewItemId(idValues, typeof(UserView));

            var path = Path.Combine(_configurationManager.ApplicationPaths.InternalMetadataPath, "views", id.ToString("N", CultureInfo.InvariantCulture));

            var item = GetItemById(id) as UserView;

            var isNew = false;

            if (item is null)
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

            if (viewType != item.ViewType)
            {
                item.ViewType = viewType;
                item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).GetAwaiter().GetResult();
            }

            var refresh = isNew || DateTime.UtcNow - item.DateLastRefreshed >= _viewRefreshInterval;

            if (!refresh && !item.DisplayParentId.IsEmpty())
            {
                var displayParent = GetItemById(item.DisplayParentId);
                refresh = displayParent is not null && displayParent.DateLastSaved > item.DateLastRefreshed;
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

        public BaseItem GetParentItem(Guid? parentId, Guid? userId)
        {
            if (parentId.HasValue)
            {
                return GetItemById(parentId.Value) ?? throw new ArgumentException($"Invalid parent id: {parentId.Value}");
            }

            if (!userId.IsNullOrEmpty())
            {
                return GetUserRootFolder();
            }

            return RootFolder;
        }

        /// <inheritdoc />
        public void QueueLibraryScan()
        {
            _taskManager.QueueScheduledTask<RefreshMediaLibraryTask>();
        }

        /// <inheritdoc />
        public int? GetSeasonNumberFromPath(string path)
            => SeasonPathParser.Parse(path, true, true).SeasonNumber;

        /// <inheritdoc />
        public bool FillMissingEpisodeNumbersFromPath(Episode episode, bool forceRefresh)
        {
            var series = episode.Series;
            bool? isAbsoluteNaming = series is not null && string.Equals(series.DisplayOrder, "absolute", StringComparison.OrdinalIgnoreCase);
            if (!isAbsoluteNaming.Value)
            {
                // In other words, no filter applied
                isAbsoluteNaming = null;
            }

            var resolver = new EpisodeResolver(_namingOptions);

            var isFolder = episode.VideoType == VideoType.BluRay || episode.VideoType == VideoType.Dvd;

            // TODO nullable - what are we trying to do there with empty episodeInfo?
            EpisodeInfo? episodeInfo = null;
            if (episode.IsFileProtocol)
            {
                episodeInfo = resolver.Resolve(episode.Path, isFolder, null, null, isAbsoluteNaming);
                // Resolve from parent folder if it's not the Season folder
                var parent = episode.GetParent();
                if (episodeInfo is null && parent.GetType() == typeof(Folder))
                {
                    episodeInfo = resolver.Resolve(parent.Path, true, null, null, isAbsoluteNaming);
                    if (episodeInfo is not null)
                    {
                        // add the container
                        episodeInfo.Container = Path.GetExtension(episode.Path)?.TrimStart('.');
                    }
                }
            }

            episodeInfo ??= new EpisodeInfo(episode.Path);

            try
            {
                var libraryOptions = GetLibraryOptions(episode);
                if (libraryOptions.EnableEmbeddedEpisodeInfos && string.Equals(episodeInfo.Container, "mp4", StringComparison.OrdinalIgnoreCase))
                {
                    // Read from metadata
                    var mediaInfo = _mediaEncoder.GetMediaInfo(
                        new MediaInfoRequest
                        {
                            MediaSource = episode.GetMediaSources(false)[0],
                            MediaType = DlnaProfileType.Video
                        },
                        CancellationToken.None).GetAwaiter().GetResult();
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
                _logger.LogError(ex, "Error reading the episode information with ffprobe. Episode: {EpisodeInfo}", episodeInfo.Path);
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
                    if (episode.IndexNumberEnd != episodeInfo.EndingEpisodeNumber)
                    {
                        changed = true;
                    }

                    episode.IndexNumberEnd = episodeInfo.EndingEpisodeNumber;
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

                if (season is not null)
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

        public ItemLookupInfo ParseName(string name)
        {
            var namingOptions = _namingOptions;
            var result = VideoResolver.CleanDateTime(name, namingOptions);

            return new ItemLookupInfo
            {
                Name = VideoResolver.TryCleanString(result.Name, namingOptions, out var newName) ? newName : result.Name,
                Year = result.Year
            };
        }

        public IEnumerable<BaseItem> FindExtras(BaseItem owner, IReadOnlyList<FileSystemMetadata> fileSystemChildren, IDirectoryService directoryService)
        {
            var ownerVideoInfo = VideoResolver.Resolve(owner.Path, owner.IsFolder, _namingOptions);
            if (ownerVideoInfo is null)
            {
                yield break;
            }

            var count = fileSystemChildren.Count;
            for (var i = 0; i < count; i++)
            {
                var current = fileSystemChildren[i];
                if (current.IsDirectory && _namingOptions.AllExtrasTypesFolderNames.ContainsKey(current.Name))
                {
                    var filesInSubFolder = _fileSystem.GetFiles(current.FullName, null, false, false);
                    foreach (var file in filesInSubFolder)
                    {
                        if (!_extraResolver.TryGetExtraTypeForOwner(file.FullName, ownerVideoInfo, out var extraType))
                        {
                            continue;
                        }

                        var extra = GetExtra(file, extraType.Value);
                        if (extra is not null)
                        {
                            yield return extra;
                        }
                    }
                }
                else if (!current.IsDirectory && _extraResolver.TryGetExtraTypeForOwner(current.FullName, ownerVideoInfo, out var extraType))
                {
                    var extra = GetExtra(current, extraType.Value);
                    if (extra is not null)
                    {
                        yield return extra;
                    }
                }
            }

            BaseItem? GetExtra(FileSystemMetadata file, ExtraType extraType)
            {
                var extra = ResolvePath(_fileSystem.GetFileInfo(file.FullName), directoryService, _extraResolver.GetResolversForExtraType(extraType));
                if (extra is not Video && extra is not Audio)
                {
                    return null;
                }

                // Try to retrieve it from the db. If we don't find it, use the resolved version
                var itemById = GetItemById(extra.Id);
                if (itemById is not null)
                {
                    extra = itemById;
                }

                // Only update extra type if it is more specific then the currently known extra type
                if (extra.ExtraType is null or ExtraType.Unknown || extraType != ExtraType.Unknown)
                {
                    extra.ExtraType = extraType;
                }

                extra.ParentId = Guid.Empty;
                extra.OwnerId = owner.Id;
                return extra;
            }
        }

        public string GetPathAfterNetworkSubstitution(string path, BaseItem? ownerItem)
        {
            foreach (var map in _configurationManager.Configuration.PathSubstitutions)
            {
                if (path.TryReplaceSubPath(map.From, map.To, out var newPath))
                {
                    return newPath;
                }
            }

            return path;
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
            return _itemRepository.GetPeopleNames(query)
            .Select(i =>
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
            })
            .Where(i => i is not null)
            .Where(i => query.User is null || i!.IsVisible(query.User))
            .ToList()!; // null values are filtered out
        }

        public List<string> GetPeopleNames(InternalPeopleQuery query)
        {
            return _itemRepository.GetPeopleNames(query);
        }

        public void UpdatePeople(BaseItem item, List<PersonInfo> people)
        {
            UpdatePeopleAsync(item, people, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public async Task UpdatePeopleAsync(BaseItem item, List<PersonInfo> people, CancellationToken cancellationToken)
        {
            if (!item.SupportsPeople)
            {
                return;
            }

            _itemRepository.UpdatePeople(item.Id, people);
            if (people is not null)
            {
                await SavePeopleMetadataAsync(people, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<ItemImageInfo> ConvertImageToLocal(BaseItem item, ItemImageInfo image, int imageIndex, bool removeOnFailure)
        {
            foreach (var url in image.Path.Split('|'))
            {
                try
                {
                    _logger.LogDebug("ConvertImageToLocal item {0} - image url: {1}", item.Id, url);

                    await ProviderManager.SaveImage(item, url, image.Type, imageIndex, CancellationToken.None).ConfigureAwait(false);

                    await item.UpdateToRepositoryAsync(ItemUpdateType.ImageUpdate, CancellationToken.None).ConfigureAwait(false);

                    return item.GetImageInfo(image.Type, imageIndex);
                }
                catch (HttpRequestException ex)
                {
                    if (ex.StatusCode.HasValue
                        && (ex.StatusCode.Value == HttpStatusCode.NotFound || ex.StatusCode.Value == HttpStatusCode.Forbidden))
                    {
                        _logger.LogDebug(ex, "Error downloading image {Url}", url);
                        continue;
                    }

                    throw;
                }
            }

            if (removeOnFailure)
            {
                // Remove this image to prevent it from retrying over and over
                item.RemoveImage(image);
                await item.UpdateToRepositoryAsync(ItemUpdateType.ImageUpdate, CancellationToken.None).ConfigureAwait(false);
            }

            throw new InvalidOperationException("Unable to convert any images to local");
        }

        public async Task AddVirtualFolder(string name, CollectionTypeOptions? collectionType, LibraryOptions options, bool refreshLibrary)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            name = _fileSystem.GetValidFilename(name);

            var rootFolderPath = _configurationManager.ApplicationPaths.DefaultUserViewsPath;

            var existingNameCount = 1; // first numbered name will be 2
            var virtualFolderPath = Path.Combine(rootFolderPath, name);
            var originalName = name;
            while (Directory.Exists(virtualFolderPath))
            {
                existingNameCount++;
                name = originalName + existingNameCount;
                virtualFolderPath = Path.Combine(rootFolderPath, name);
            }

            var mediaPathInfos = options.PathInfos;
            if (mediaPathInfos is not null)
            {
                var invalidpath = mediaPathInfos.FirstOrDefault(i => !Directory.Exists(i.Path));
                if (invalidpath is not null)
                {
                    throw new ArgumentException("The specified path does not exist: " + invalidpath.Path + ".");
                }
            }

            LibraryMonitor.Stop();

            try
            {
                Directory.CreateDirectory(virtualFolderPath);

                if (collectionType is not null)
                {
                    var path = Path.Combine(virtualFolderPath, collectionType.ToString()!.ToLowerInvariant() + ".collection"); // Can't be null with legal values?

                    await File.WriteAllBytesAsync(path, Array.Empty<byte>()).ConfigureAwait(false);
                }

                CollectionFolder.SaveLibraryOptions(virtualFolderPath, options);

                if (mediaPathInfos is not null)
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

        private async Task SavePeopleMetadataAsync(IEnumerable<PersonInfo> people, CancellationToken cancellationToken)
        {
            List<BaseItem>? personsToSave = null;

            foreach (var person in people)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var itemUpdateType = ItemUpdateType.MetadataDownload;
                var saveEntity = false;
                var personEntity = GetPerson(person.Name);

                if (personEntity is null)
                {
                    var path = Person.GetPath(person.Name);
                    personEntity = new Person()
                    {
                        Name = person.Name,
                        Id = GetItemByNameId<Person>(path),
                        DateCreated = DateTime.UtcNow,
                        DateModified = DateTime.UtcNow,
                        Path = path
                    };

                    personEntity.PresentationUniqueKey = personEntity.CreatePresentationUniqueKey();
                    saveEntity = true;
                }

                foreach (var id in person.ProviderIds)
                {
                    if (!string.Equals(personEntity.GetProviderId(id.Key), id.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        personEntity.SetProviderId(id.Key, id.Value);
                        saveEntity = true;
                    }
                }

                if (!string.IsNullOrWhiteSpace(person.ImageUrl) && !personEntity.HasImage(ImageType.Primary))
                {
                    personEntity.SetImage(
                        new ItemImageInfo
                        {
                            Path = person.ImageUrl,
                            Type = ImageType.Primary
                        },
                        0);

                    saveEntity = true;
                    itemUpdateType = ItemUpdateType.ImageUpdate;
                }

                if (saveEntity)
                {
                    (personsToSave ??= new()).Add(personEntity);
                    await RunMetadataSavers(personEntity, itemUpdateType).ConfigureAwait(false);
                }
            }

            if (personsToSave is not null)
            {
                CreateItems(personsToSave, null, CancellationToken.None);
            }
        }

        private void StartScanInBackground()
        {
            Task.Run(() =>
            {
                // No need to start if scanning the library because it will handle it
                ValidateMediaLibrary(new Progress<double>(), CancellationToken.None);
            });
        }

        public void AddMediaPath(string virtualFolderName, MediaPathInfo mediaPath)
        {
            AddMediaPathInternal(virtualFolderName, mediaPath, true);
        }

        private void AddMediaPathInternal(string virtualFolderName, MediaPathInfo pathInfo, bool saveLibraryOptions)
        {
            ArgumentNullException.ThrowIfNull(pathInfo);

            var path = pathInfo.Path;

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(nameof(path));
            }

            if (!Directory.Exists(path))
            {
                throw new FileNotFoundException("The path does not exist.");
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

                libraryOptions.PathInfos = [..libraryOptions.PathInfos, pathInfo];

                SyncLibraryOptionsToLocations(virtualFolderPath, libraryOptions);

                CollectionFolder.SaveLibraryOptions(virtualFolderPath, libraryOptions);
            }
        }

        public void UpdateMediaPath(string virtualFolderName, MediaPathInfo mediaPath)
        {
            ArgumentNullException.ThrowIfNull(mediaPath);

            var rootFolderPath = _configurationManager.ApplicationPaths.DefaultUserViewsPath;
            var virtualFolderPath = Path.Combine(rootFolderPath, virtualFolderName);

            var libraryOptions = CollectionFolder.GetLibraryOptions(virtualFolderPath);

            SyncLibraryOptionsToLocations(virtualFolderPath, libraryOptions);

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
                        list.Add(new MediaPathInfo(location));
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
                    await ValidateTopLibraryFolders(CancellationToken.None, true).ConfigureAwait(false);

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

            List<NameValuePair>? removeList = null;

            foreach (var contentType in _configurationManager.Configuration.ContentTypes)
            {
                if (string.IsNullOrWhiteSpace(contentType.Name)
                    || _fileSystem.AreEqual(path, contentType.Name)
                    || _fileSystem.ContainsSubPath(path, contentType.Name))
                {
                    (removeList ??= new()).Add(contentType);
                }
            }

            if (removeList is not null)
            {
                _configurationManager.Configuration.ContentTypes = _configurationManager.Configuration.ContentTypes
                    .Except(removeList)
                    .ToArray();

                _configurationManager.SaveConfiguration();
            }
        }

        public void RemoveMediaPath(string virtualFolderName, string mediaPath)
        {
            ArgumentException.ThrowIfNullOrEmpty(mediaPath);

            var rootFolderPath = _configurationManager.ApplicationPaths.DefaultUserViewsPath;
            var virtualFolderPath = Path.Combine(rootFolderPath, virtualFolderName);

            if (!Directory.Exists(virtualFolderPath))
            {
                throw new FileNotFoundException(
                    string.Format(CultureInfo.InvariantCulture, "The media collection {0} does not exist", virtualFolderName));
            }

            var shortcut = _fileSystem.GetFilePaths(virtualFolderPath, true)
                .Where(i => Path.GetExtension(i.AsSpan()).Equals(ShortcutFileExtension, StringComparison.OrdinalIgnoreCase))
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

        private static bool ItemIsVisible(BaseItem? item, User? user)
        {
            if (item is null)
            {
                return false;
            }

            if (user is null)
            {
                return true;
            }

            return item is UserRootFolder || item.IsVisibleStandalone(user);
        }
    }
}
