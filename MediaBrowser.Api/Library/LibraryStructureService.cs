using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

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


        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryStructureService" /> class.
        /// </summary>
        public LibraryStructureService(
            ILogger<LibraryStructureService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            ILibraryManager libraryManager,
            ILibraryMonitor libraryMonitor)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _appPaths = serverConfigurationManager.ApplicationPaths;
            _libraryManager = libraryManager;
            _libraryMonitor = libraryMonitor;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetVirtualFolders request)
        {
            var result = _libraryManager.GetVirtualFolders(true);

            return ToOptimizedResult(result);
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
        public Task Post(AddVirtualFolder request)
        {
            var libraryOptions = request.LibraryOptions ?? new LibraryOptions();

            if (request.Paths != null && request.Paths.Length > 0)
            {
                libraryOptions.PathInfos = request.Paths.Select(i => new MediaPathInfo { Path = i }).ToArray();
            }

            return _libraryManager.AddVirtualFolder(request.Name, request.CollectionType, libraryOptions, request.RefreshLibrary);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(RenameVirtualFolder request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.NewName))
            {
                throw new ArgumentNullException(nameof(request));
            }

            var rootFolderPath = _appPaths.DefaultUserViewsPath;

            var currentPath = Path.Combine(rootFolderPath, request.Name);
            var newPath = Path.Combine(rootFolderPath, request.NewName);

            if (!Directory.Exists(currentPath))
            {
                throw new FileNotFoundException("The media collection does not exist");
            }

            if (!string.Equals(currentPath, newPath, StringComparison.OrdinalIgnoreCase) && Directory.Exists(newPath))
            {
                throw new ArgumentException("Media library already exists at " + newPath + ".");
            }

            _libraryMonitor.Stop();

            try
            {
                // Changing capitalization. Handle windows case insensitivity
                if (string.Equals(currentPath, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    var tempPath = Path.Combine(rootFolderPath, Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
                    Directory.Move(currentPath, tempPath);
                    currentPath = tempPath;
                }

                Directory.Move(currentPath, newPath);
            }
            finally
            {
                CollectionFolder.OnCollectionFolderChange();

                Task.Run(() =>
                {
                    // No need to start if scanning the library because it will handle it
                    if (request.RefreshLibrary)
                    {
                        _libraryManager.ValidateMediaLibrary(new SimpleProgress<double>(), CancellationToken.None);
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
        public Task Delete(RemoveVirtualFolder request)
        {
            return _libraryManager.RemoveVirtualFolder(request.Name, request.RefreshLibrary);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(AddMediaPath request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentNullException(nameof(request));
            }

            _libraryMonitor.Stop();

            try
            {
                var mediaPath = request.PathInfo ?? new MediaPathInfo
                {
                    Path = request.Path
                };

                _libraryManager.AddMediaPath(request.Name, mediaPath);
            }
            finally
            {
                Task.Run(() =>
                {
                    // No need to start if scanning the library because it will handle it
                    if (request.RefreshLibrary)
                    {
                        _libraryManager.ValidateMediaLibrary(new SimpleProgress<double>(), CancellationToken.None);
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
                throw new ArgumentNullException(nameof(request));
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
                throw new ArgumentNullException(nameof(request));
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
                        _libraryManager.ValidateMediaLibrary(new SimpleProgress<double>(), CancellationToken.None);
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
