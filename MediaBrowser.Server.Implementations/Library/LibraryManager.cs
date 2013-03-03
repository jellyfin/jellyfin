using System.Collections;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.ScheduledTasks;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Implementations.Library.Resolvers;
using MoreLinq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        #region LibraryChanged Event
        /// <summary>
        /// Fires whenever any validation routine adds or removes items.  The added and removed items are properties of the args.
        /// *** Will fire asynchronously. ***
        /// </summary>
        public event EventHandler<ChildrenChangedEventArgs> LibraryChanged;

        /// <summary>
        /// Raises the <see cref="E:LibraryChanged" /> event.
        /// </summary>
        /// <param name="args">The <see cref="ChildrenChangedEventArgs" /> instance containing the event data.</param>
        public void ReportLibraryChanged(ChildrenChangedEventArgs args)
        {
            EventHelper.QueueEventIfNotNull(LibraryChanged, this, args, _logger);
        }
        #endregion

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
        /// Gets or sets the kernel.
        /// </summary>
        /// <value>The kernel.</value>
        private Kernel Kernel { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryManager" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="taskManager">The task manager.</param>
        /// <param name="userManager">The user manager.</param>
        public LibraryManager(Kernel kernel, ILogger logger, ITaskManager taskManager, IUserManager userManager)
        {
            Kernel = kernel;
            _logger = logger;
            _taskManager = taskManager;
            _userManager = userManager;

            kernel.ConfigurationUpdated += kernel_ConfigurationUpdated;
        }

        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="rules">The rules.</param>
        /// <param name="pluginFolders">The plugin folders.</param>
        /// <param name="resolvers">The resolvers.</param>
        /// <param name="introProviders">The intro providers.</param>
        public void AddParts(IEnumerable<IResolverIgnoreRule> rules, IEnumerable<IVirtualFolderCreator> pluginFolders, IEnumerable<IItemResolver> resolvers, IEnumerable<IIntroProvider> introProviders)
        {
            EntityResolutionIgnoreRules = rules;
            PluginFolderCreators = pluginFolders;
            EntityResolvers = resolvers.OrderBy(i => i.Priority).ToArray();
            IntroProviders = introProviders;
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
        /// Handles the ConfigurationUpdated event of the kernel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void kernel_ConfigurationUpdated(object sender, EventArgs e)
        {
            //// Figure out whether or not we should refresh people after the update is finished
            //var refreshPeopleAfterUpdate = !oldConfiguration.EnableInternetProviders && config.EnableInternetProviders;

            //// This is true if internet providers has just been turned on, or if People have just been removed from InternetProviderExcludeTypes
            //if (!refreshPeopleAfterUpdate)
            //{
            //    var oldConfigurationFetchesPeopleImages = oldConfiguration.InternetProviderExcludeTypes == null || !oldConfiguration.InternetProviderExcludeTypes.Contains(typeof(Person).Name, StringComparer.OrdinalIgnoreCase);
            //    var newConfigurationFetchesPeopleImages = config.InternetProviderExcludeTypes == null || !config.InternetProviderExcludeTypes.Contains(typeof(Person).Name, StringComparer.OrdinalIgnoreCase);

            //    refreshPeopleAfterUpdate = newConfigurationFetchesPeopleImages && !oldConfigurationFetchesPeopleImages;
            //}

            Task.Run(() =>
            {
                // Any number of configuration settings could change the way the library is refreshed, so do that now
                _taskManager.CancelIfRunningAndQueue<RefreshMediaLibraryTask>();
                _taskManager.CancelIfRunningAndQueue<PeopleValidationTask>();
            });
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
        public BaseItem ResolvePath(string path, Folder parent = null, WIN32_FIND_DATA? fileInfo = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException();
            }

            fileInfo = fileInfo ?? FileSystem.GetFileData(path);

            if (!fileInfo.HasValue)
            {
                return null;
            }

            var args = new ItemResolveArgs
            {
                Parent = parent,
                Path = path,
                FileInfo = fileInfo.Value
            };

            // Return null if ignore rules deem that we should do so
            if (EntityResolutionIgnoreRules.Any(r => r.ShouldIgnore(args)))
            {
                return null;
            }

            // Gather child folder and files
            if (args.IsDirectory)
            {
                // When resolving the root, we need it's grandchildren (children of user views)
                var flattenFolderDepth = args.IsPhysicalRoot ? 2 : 0;

                args.FileSystemDictionary = FileData.GetFilteredFileSystemEntries(args.Path, _logger, flattenFolderDepth: flattenFolderDepth, args: args);
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
        public List<T> ResolvePaths<T>(IEnumerable<WIN32_FIND_DATA> files, Folder parent)
            where T : BaseItem
        {
            var list = new List<T>();

            Parallel.ForEach(files, f =>
            {
                try
                {
                    var item = ResolvePath(f.Path, parent, f) as T;

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
                    _logger.ErrorException("Error resolving path {0}", ex, f.Path);
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
            var rootFolderPath = Kernel.ApplicationPaths.RootFolderPath;
            var rootFolder = Kernel.ItemRepository.RetrieveItem(rootFolderPath.GetMBId(typeof(AggregateFolder))) as AggregateFolder ?? (AggregateFolder)ResolvePath(rootFolderPath);

            // Add in the plug-in folders
            foreach (var child in PluginFolderCreators)
            {
                rootFolder.AddVirtualChild(child.GetFolder());
            }

            return rootFolder;
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
        /// <returns>Task{Person}.</returns>
        private Task<Person> GetPerson(string name, CancellationToken cancellationToken, bool allowSlowProviders = false)
        {
            return GetImagesByNameItem<Person>(Kernel.ApplicationPaths.PeoplePath, name, cancellationToken, allowSlowProviders);
        }

        /// <summary>
        /// Gets a Studio
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <returns>Task{Studio}.</returns>
        public Task<Studio> GetStudio(string name, bool allowSlowProviders = false)
        {
            return GetImagesByNameItem<Studio>(Kernel.ApplicationPaths.StudioPath, name, CancellationToken.None, allowSlowProviders);
        }

        /// <summary>
        /// Gets a Genre
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <returns>Task{Genre}.</returns>
        public Task<Genre> GetGenre(string name, bool allowSlowProviders = false)
        {
            return GetImagesByNameItem<Genre>(Kernel.ApplicationPaths.GenrePath, name, CancellationToken.None, allowSlowProviders);
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

            return GetImagesByNameItem<Year>(Kernel.ApplicationPaths.YearPath, value.ToString(UsCulture), CancellationToken.None, allowSlowProviders);
        }

        /// <summary>
        /// The images by name item cache
        /// </summary>
        private readonly ConcurrentDictionary<string, object> ImagesByNameItemCache = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Generically retrieves an IBN item
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path">The path.</param>
        /// <param name="name">The name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <returns>Task{``0}.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        private Task<T> GetImagesByNameItem<T>(string path, string name, CancellationToken cancellationToken, bool allowSlowProviders = true)
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

            var obj = ImagesByNameItemCache.GetOrAdd(key, keyname => CreateImagesByNameItem<T>(path, name, cancellationToken, allowSlowProviders));

            return obj as Task<T>;
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
        private async Task<T> CreateImagesByNameItem<T>(string path, string name, CancellationToken cancellationToken, bool allowSlowProviders = true)
            where T : BaseItem, new()
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.Debug("Creating {0}: {1}", typeof(T).Name, name);

            path = Path.Combine(path, FileSystem.GetValidFilename(name));

            var fileInfo = FileSystem.GetFileData(path);

            var isNew = false;

            if (!fileInfo.HasValue)
            {
                Directory.CreateDirectory(path);
                fileInfo = FileSystem.GetFileData(path);

                if (!fileInfo.HasValue)
                {
                    throw new IOException("Path not created: " + path);
                }

                isNew = true;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var id = path.GetMBId(typeof(T));

            var item = Kernel.ItemRepository.RetrieveItem(id) as T;
            if (item == null)
            {
                item = new T
                {
                    Name = name,
                    Id = id,
                    DateCreated = fileInfo.Value.CreationTimeUtc,
                    DateModified = fileInfo.Value.LastWriteTimeUtc,
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
            // Clear the IBN cache
            ImagesByNameItemCache.Clear();

            const int maxTasks = 250;

            var tasks = new List<Task>();

            var includedPersonTypes = new[] { PersonType.Actor, PersonType.Director };

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
                        await GetPerson(currentPerson.Name, cancellationToken, allowSlowProviders: true).ConfigureAwait(false);
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

        /// <summary>
        /// Reloads the root media folder
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task ValidateMediaLibrary(IProgress<double> progress, CancellationToken cancellationToken)
        {
            _logger.Info("Validating media library");

            await RootFolder.RefreshMetadata(cancellationToken).ConfigureAwait(false);

            // Start by just validating the children of the root, but go no further
            await RootFolder.ValidateChildren(new Progress<double> { }, cancellationToken, recursive: false);

            // Validate only the collection folders for each user, just to make them available as quickly as possible
            var userCollectionFolderTasks = _userManager.Users.AsParallel().Select(user => user.ValidateCollectionFolders(new Progress<double> { }, cancellationToken));
            await Task.WhenAll(userCollectionFolderTasks).ConfigureAwait(false);

            // Now validate the entire media library
            await RootFolder.ValidateChildren(progress, cancellationToken, recursive: true).ConfigureAwait(false);

            foreach (var user in _userManager.Users)
            {
                await user.ValidateMediaLibrary(new Progress<double> { }, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Saves display preferences for a Folder
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="folder">The folder.</param>
        /// <param name="data">The data.</param>
        /// <returns>Task.</returns>
        public Task SaveDisplayPreferencesForFolder(User user, Folder folder, DisplayPreferences data)
        {
            // Need to update all items with the same DisplayPrefsId
            foreach (var child in RootFolder.GetRecursiveChildren(user)
                .OfType<Folder>()
                .Where(i => i.DisplayPrefsId == folder.DisplayPrefsId))
            {
                child.AddOrUpdateDisplayPrefs(user, data);
            }

            return Kernel.DisplayPreferencesRepository.SaveDisplayPrefs(folder, CancellationToken.None);
        }

        /// <summary>
        /// Gets the default view.
        /// </summary>
        /// <returns>IEnumerable{VirtualFolderInfo}.</returns>
        public IEnumerable<VirtualFolderInfo> GetDefaultVirtualFolders()
        {
            return GetView(Kernel.ApplicationPaths.DefaultUserViewsPath);
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
                    Locations = Directory.EnumerateFiles(dir, "*.lnk", SearchOption.TopDirectoryOnly).Select(FileSystem.ResolveShortcut).ToList()
                });
        }

        /// <summary>
        /// Finds a library item by Id and UserId.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="userManager">The user manager.</param>
        /// <returns>BaseItem.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        public BaseItem GetItemById(Guid id, Guid userId)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }

            var user = _userManager.GetUserById(userId);
            var userRoot = user.RootFolder;

            return userRoot.FindItemById(id, user);
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

            return RootFolder.FindItemById(id, null);
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
    }
}
