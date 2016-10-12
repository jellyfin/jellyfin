using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Entities;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Api.Library
{
    /// <summary>
    /// Class GetDefaultVirtualFolders
    /// </summary>
    [Route("/Library/VirtualFolders", "GET")]
    public class GetVirtualFolders : IReturn<List<VirtualFolderInfo>>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public string UserId { get; set; }
    }

    [Route("/Library/VirtualFolders", "POST")]
    public class AddVirtualFolder : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the collection.
        /// </summary>
        /// <value>The type of the collection.</value>
        public string CollectionType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [refresh library].
        /// </summary>
        /// <value><c>true</c> if [refresh library]; otherwise, <c>false</c>.</value>
        public bool RefreshLibrary { get; set; }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string[] Paths { get; set; }

        public LibraryOptions LibraryOptions { get; set; }
    }

    [Route("/Library/VirtualFolders", "DELETE")]
    public class RemoveVirtualFolder : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [refresh library].
        /// </summary>
        /// <value><c>true</c> if [refresh library]; otherwise, <c>false</c>.</value>
        public bool RefreshLibrary { get; set; }
    }

    [Route("/Library/VirtualFolders/Name", "POST")]
    public class RenameVirtualFolder : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string NewName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [refresh library].
        /// </summary>
        /// <value><c>true</c> if [refresh library]; otherwise, <c>false</c>.</value>
        public bool RefreshLibrary { get; set; }
    }

    [Route("/Library/VirtualFolders/Paths", "POST")]
    public class AddMediaPath : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Path { get; set; }

        public MediaPathInfo PathInfo { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [refresh library].
        /// </summary>
        /// <value><c>true</c> if [refresh library]; otherwise, <c>false</c>.</value>
        public bool RefreshLibrary { get; set; }
    }

    [Route("/Library/VirtualFolders/Paths/Update", "POST")]
    public class UpdateMediaPath : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        public MediaPathInfo PathInfo { get; set; }
    }

    [Route("/Library/VirtualFolders/Paths", "DELETE")]
    public class RemoveMediaPath : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [refresh library].
        /// </summary>
        /// <value><c>true</c> if [refresh library]; otherwise, <c>false</c>.</value>
        public bool RefreshLibrary { get; set; }
    }

    [Route("/Library/VirtualFolders/LibraryOptions", "POST")]
    public class UpdateLibraryOptions : IReturnVoid
    {
        public string Id { get; set; }

        public LibraryOptions LibraryOptions { get; set; }
    }

    /// <summary>
    /// Class LibraryStructureService
    /// </summary>
    [Authenticated(Roles = "Admin", AllowBeforeStartupWizard = true)]
    public class LibraryStructureService : BaseApiService
    {
        /// <summary>
        /// The _app paths
        /// </summary>
        private readonly IServerApplicationPaths _appPaths;

        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        private readonly ILibraryMonitor _libraryMonitor;

        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryStructureService" /> class.
        /// </summary>
        public LibraryStructureService(IServerApplicationPaths appPaths, ILibraryManager libraryManager, ILibraryMonitor libraryMonitor, IFileSystem fileSystem)
        {
            if (appPaths == null)
            {
                throw new ArgumentNullException("appPaths");
            }

            _appPaths = appPaths;
            _libraryManager = libraryManager;
            _libraryMonitor = libraryMonitor;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetVirtualFolders request)
        {
            var result = _libraryManager.GetVirtualFolders().OrderBy(i => i.Name).ToList();

            return ToOptimizedSerializedResultUsingCache(result);
        }

        public void Post(UpdateLibraryOptions request)
        {
            var collectionFolder = (CollectionFolder)_libraryManager.GetItemById(request.Id);

            collectionFolder.UpdateLibraryOptions(request.LibraryOptions);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(AddVirtualFolder request)
        {
            var libraryOptions = request.LibraryOptions ?? new LibraryOptions();

            if (request.Paths != null && request.Paths.Length > 0)
            {
                libraryOptions.PathInfos = request.Paths.Select(i => new MediaPathInfo { Path = i }).ToArray();
            }

            _libraryManager.AddVirtualFolder(request.Name, request.CollectionType, libraryOptions, request.RefreshLibrary);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(RenameVirtualFolder request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentNullException("request");
            }

            if (string.IsNullOrWhiteSpace(request.NewName))
            {
                throw new ArgumentNullException("request");
            }

            var rootFolderPath = _appPaths.DefaultUserViewsPath;

            var currentPath = Path.Combine(rootFolderPath, request.Name);
            var newPath = Path.Combine(rootFolderPath, request.NewName);

            if (!_fileSystem.DirectoryExists(currentPath))
            {
                throw new DirectoryNotFoundException("The media collection does not exist");
            }

            if (!string.Equals(currentPath, newPath, StringComparison.OrdinalIgnoreCase) && _fileSystem.DirectoryExists(newPath))
            {
                throw new ArgumentException("There is already a media collection with the name " + newPath + ".");
            }

            _libraryMonitor.Stop();

            try
            {
                // Only make a two-phase move when changing capitalization
                if (string.Equals(currentPath, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    //Create an unique name
                    var temporaryName = Guid.NewGuid().ToString();
                    var temporaryPath = Path.Combine(rootFolderPath, temporaryName);
                    _fileSystem.MoveDirectory(currentPath, temporaryPath);
                    currentPath = temporaryPath;
                }

                _fileSystem.MoveDirectory(currentPath, newPath);
            }
            finally
            {
                Task.Run(() =>
                {
                    // No need to start if scanning the library because it will handle it
                    if (request.RefreshLibrary)
                    {
                        _libraryManager.ValidateMediaLibrary(new Progress<double>(), CancellationToken.None);
                    }
                    else
                    {
                        // Need to add a delay here or directory watchers may still pick up the changes
                        var task = Task.Delay(1000);
                        // Have to block here to allow exceptions to bubble
                        Task.WaitAll(task);

                        _libraryMonitor.Start();
                    }
                });
            }
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(RemoveVirtualFolder request)
        {
            _libraryManager.RemoveVirtualFolder(request.Name, request.RefreshLibrary);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(AddMediaPath request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentNullException("request");
            }

            _libraryMonitor.Stop();

            try
            {
                var mediaPath = request.PathInfo;

                if (mediaPath == null)
                {
                    mediaPath = new MediaPathInfo
                    {
                        Path = request.Path
                    };
                }
                _libraryManager.AddMediaPath(request.Name, mediaPath);
            }
            finally
            {
                Task.Run(() =>
                {
                    // No need to start if scanning the library because it will handle it
                    if (request.RefreshLibrary)
                    {
                        _libraryManager.ValidateMediaLibrary(new Progress<double>(), CancellationToken.None);
                    }
                    else
                    {
                        // Need to add a delay here or directory watchers may still pick up the changes
                        var task = Task.Delay(1000);
                        // Have to block here to allow exceptions to bubble
                        Task.WaitAll(task);

                        _libraryMonitor.Start();
                    }
                });
            }
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(UpdateMediaPath request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentNullException("request");
            }

            _libraryManager.UpdateMediaPath(request.Name, request.PathInfo);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(RemoveMediaPath request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentNullException("request");
            }

            _libraryMonitor.Stop();

            try
            {
                _libraryManager.RemoveMediaPath(request.Name, request.Path);
            }
            finally
            {
                Task.Run(() =>
                {
                    // No need to start if scanning the library because it will handle it
                    if (request.RefreshLibrary)
                    {
                        _libraryManager.ValidateMediaLibrary(new Progress<double>(), CancellationToken.None);
                    }
                    else
                    {
                        // Need to add a delay here or directory watchers may still pick up the changes
                        var task = Task.Delay(1000);
                        // Have to block here to allow exceptions to bubble
                        Task.WaitAll(task);

                        _libraryMonitor.Start();
                    }
                });
            }
        }
    }
}
