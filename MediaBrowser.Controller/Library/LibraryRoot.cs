using System;
using System.IO;
using System.Threading;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Represents the media library root directories.
    /// </summary>
    public class LibraryRoot : ILibraryRoot
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly ILogger<LibraryRoot> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryRoot"/> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="configurationManager">the configuration manager.</param>
        /// <param name="logger">The logger.</param>
        public LibraryRoot(
            ILibraryManager libraryManager,
            IFileSystem fileSystem,
            IServerConfigurationManager configurationManager,
            ILogger<LibraryRoot> logger)
        {
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
            _configurationManager = configurationManager;
            _logger = logger;
        }

        /// <summary>
        /// Gets the library root folder.
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public static AggregateFolder RootFolder { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        /// <summary>
        /// Gets the library root user folder.
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public static Folder UserRootFolder { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        /// <summary>
        /// Called on application start. Create root folders if they do not exist and setup static references.
        /// </summary>
        public void Initialize()
        {
            CreateUserRootFolder();
            CreateRootFolder();
        }

        private void CreateRootFolder()
        {
            if (RootFolder != null)
            {
                return;
            }

            var rootFolderPath = _configurationManager.ApplicationPaths.RootFolderPath;

            Directory.CreateDirectory(rootFolderPath);

            var rootFolder = _libraryManager.GetItemById(_libraryManager.GetNewItemId(rootFolderPath, typeof(AggregateFolder))) as AggregateFolder ??
                             ((Folder)_libraryManager.ResolvePath(_fileSystem.GetDirectoryInfo(rootFolderPath)))
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

            if (folder.Id.Equals(Guid.Empty))
            {
                if (string.IsNullOrEmpty(folder.Path))
                {
                    folder.Id = _libraryManager.GetNewItemId(folder.GetType().Name, folder.GetType());
                }
                else
                {
                    folder.Id = _libraryManager.GetNewItemId(folder.Path, folder.GetType());
                }
            }

            var dbItem = _libraryManager.GetItemById(folder.Id) as BasePluginFolder;

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

            RootFolder = rootFolder;
        }

        private void CreateUserRootFolder()
        {
            if (UserRootFolder != null)
            {
                return;
            }

            var userRootPath = _configurationManager.ApplicationPaths.DefaultUserViewsPath;
            _logger.LogDebug("Creating userRootPath at {Path}", userRootPath);

            var newItemId = _libraryManager.GetNewItemId(userRootPath, typeof(UserRootFolder));
            UserRootFolder? tmpItem = null;

            try
            {
                Directory.CreateDirectory(userRootPath);
                tmpItem = _libraryManager.GetItemById(newItemId) as UserRootFolder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating UserRootFolder {Path}", newItemId);
            }

            if (tmpItem is null)
            {
                _logger.LogDebug("Creating new userRootFolder with DeepCopy");
                tmpItem = ((Folder)_libraryManager.ResolvePath(_fileSystem.GetDirectoryInfo(userRootPath))).DeepCopy<Folder, UserRootFolder>();
            }

            // In case program data folder was moved
            if (!string.Equals(tmpItem.Path, userRootPath, StringComparison.Ordinal))
            {
                _logger.LogInformation("Resetting user root folder path to {0}", userRootPath);
                tmpItem.Path = userRootPath;
            }

            _logger.LogDebug("Setting userRootFolder: {Folder}", tmpItem);

            UserRootFolder = tmpItem;
        }
    }
}
