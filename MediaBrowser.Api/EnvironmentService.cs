using MediaBrowser.Common.Net;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using MediaBrowser.Server.Implementations.HttpServer;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetDirectoryContents
    /// </summary>
    [Route("/Environment/DirectoryContents", "GET")]
    public class GetDirectoryContents : IReturn<List<FileSystemEntryInfo>>
    {
        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [include files].
        /// </summary>
        /// <value><c>true</c> if [include files]; otherwise, <c>false</c>.</value>
        public bool IncludeFiles { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [include directories].
        /// </summary>
        /// <value><c>true</c> if [include directories]; otherwise, <c>false</c>.</value>
        public bool IncludeDirectories { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [include hidden].
        /// </summary>
        /// <value><c>true</c> if [include hidden]; otherwise, <c>false</c>.</value>
        public bool IncludeHidden { get; set; }
    }

    /// <summary>
    /// Class GetDrives
    /// </summary>
    [Route("/Environment/Drives", "GET")]
    public class GetDrives : IReturn<List<FileSystemEntryInfo>>
    {
    }

    /// <summary>
    /// Class GetNetworkComputers
    /// </summary>
    [Route("/Environment/NetworkComputers", "GET")]
    public class GetNetworkComputers : IReturn<List<FileSystemEntryInfo>>
    {
    }

    /// <summary>
    /// Class EnvironmentService
    /// </summary>
    public class EnvironmentService : BaseRestService
    {
        /// <summary>
        /// The _network manager
        /// </summary>
        private readonly INetworkManager _networkManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentService" /> class.
        /// </summary>
        /// <param name="networkManager">The network manager.</param>
        /// <exception cref="System.ArgumentNullException">networkManager</exception>
        public EnvironmentService(INetworkManager networkManager)
        {
            if (networkManager == null)
            {
                throw new ArgumentNullException("networkManager");
            }

            _networkManager = networkManager;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.ArgumentNullException">Path</exception>
        /// <exception cref="System.ArgumentException"></exception>
        public object Get(GetDirectoryContents request)
        {
            var path = request.Path;

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("Path");
            }

            if (path.StartsWith(NetworkPrefix, StringComparison.OrdinalIgnoreCase) && path.LastIndexOf('\\') == 1)
            {
                return ToOptimizedResult(GetNetworkShares(path).ToList());
            }

            // Reject invalid input
            if (!Path.IsPathRooted(path))
            {
                throw new ArgumentException(string.Format("Invalid path: {0}", path));
            }

            return ToOptimizedResult(GetFileSystemEntries(request).ToList());
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetDrives request)
        {
            var result = GetDrives().ToList();

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetNetworkComputers request)
        {
            var result = GetNetworkComputers().ToList();

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the list that is returned when an empty path is supplied
        /// </summary>
        /// <returns>IEnumerable{FileSystemEntryInfo}.</returns>
        private IEnumerable<FileSystemEntryInfo> GetDrives()
        {
            // Only include drives in the ready state or this method could end up being very slow, waiting for drives to timeout
            return DriveInfo.GetDrives().Where(d => d.IsReady).Select(d => new FileSystemEntryInfo
            {
                Name = GetName(d),
                Path = d.RootDirectory.FullName,
                Type = FileSystemEntryType.Directory

            });
        }

        /// <summary>
        /// Gets the network computers.
        /// </summary>
        /// <returns>IEnumerable{FileSystemEntryInfo}.</returns>
        private IEnumerable<FileSystemEntryInfo> GetNetworkComputers()
        {
            return _networkManager.GetNetworkDevices().Select(c => new FileSystemEntryInfo
            {
                Name = c,
                Path = NetworkPrefix + c,
                Type = FileSystemEntryType.NetworkComputer
            });
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <param name="drive">The drive.</param>
        /// <returns>System.String.</returns>
        private string GetName(DriveInfo drive)
        {
            return drive.Name;
        }

        /// <summary>
        /// Gets the network shares.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>IEnumerable{FileSystemEntryInfo}.</returns>
        private IEnumerable<FileSystemEntryInfo> GetNetworkShares(string path)
        {
            return _networkManager.GetNetworkShares(path).Where(s => s.ShareType == NetworkShareType.Disk).Select(c => new FileSystemEntryInfo
            {
                Name = c.Name,
                Path = Path.Combine(path, c.Name),
                Type = FileSystemEntryType.NetworkShare
            });
        }

        /// <summary>
        /// Gets the file system entries.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>IEnumerable{FileSystemEntryInfo}.</returns>
        private IEnumerable<FileSystemEntryInfo> GetFileSystemEntries(GetDirectoryContents request)
        {
            var fileSystemEntries = FileSystem.GetFileSystemEntries(request.Path, "*", request.IncludeFiles, request.IncludeDirectories).Where(f => !f.IsSystemFile);

            if (!request.IncludeHidden)
            {
                fileSystemEntries = fileSystemEntries.Where(f => !f.IsHidden);
            }

            return fileSystemEntries.Select(f => new FileSystemEntryInfo
            {
                Name = f.cFileName,
                Path = f.Path,
                Type = f.IsDirectory ? FileSystemEntryType.Directory : FileSystemEntryType.File
            });
        }

        /// <summary>
        /// Gets the network prefix.
        /// </summary>
        /// <value>The network prefix.</value>
        private string NetworkPrefix
        {
            get { return Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture) + Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture); }
        }
    }
}
