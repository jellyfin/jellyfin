using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CommonIO;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetDirectoryContents
    /// </summary>
    [Route("/Environment/DirectoryContents", "GET", Summary = "Gets the contents of a given directory in the file system")]
    public class GetDirectoryContents : IReturn<List<FileSystemEntryInfo>>
    {
        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        [ApiMember(Name = "Path", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [include files].
        /// </summary>
        /// <value><c>true</c> if [include files]; otherwise, <c>false</c>.</value>
        [ApiMember(Name = "IncludeFiles", Description = "An optional filter to include or exclude files from the results. true/false", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool IncludeFiles { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [include directories].
        /// </summary>
        /// <value><c>true</c> if [include directories]; otherwise, <c>false</c>.</value>
        [ApiMember(Name = "IncludeDirectories", Description = "An optional filter to include or exclude folders from the results. true/false", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool IncludeDirectories { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [include hidden].
        /// </summary>
        /// <value><c>true</c> if [include hidden]; otherwise, <c>false</c>.</value>
        [ApiMember(Name = "IncludeHidden", Description = "An optional filter to include or exclude hidden files and folders. true/false", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool IncludeHidden { get; set; }

        public GetDirectoryContents()
        {
            IncludeHidden = true;
        }
    }

    [Route("/Environment/NetworkShares", "GET", Summary = "Gets shares from a network device")]
    public class GetNetworkShares : IReturn<List<FileSystemEntryInfo>>
    {
        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        [ApiMember(Name = "Path", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Path { get; set; }
    }

    /// <summary>
    /// Class GetDrives
    /// </summary>
    [Route("/Environment/Drives", "GET", Summary = "Gets available drives from the server's file system")]
    public class GetDrives : IReturn<List<FileSystemEntryInfo>>
    {
    }

    /// <summary>
    /// Class GetNetworkComputers
    /// </summary>
    [Route("/Environment/NetworkDevices", "GET", Summary = "Gets a list of devices on the network")]
    public class GetNetworkDevices : IReturn<List<FileSystemEntryInfo>>
    {
    }

    [Route("/Environment/ParentPath", "GET", Summary = "Gets the parent path of a given path")]
    public class GetParentPath : IReturn<string>
    {
        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        [ApiMember(Name = "Path", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Path { get; set; }
    }

    public class DefaultDirectoryBrowserInfo
    {
        public string Path { get; set; }
    }

    [Route("/Environment/DefaultDirectoryBrowser", "GET", Summary = "Gets the parent path of a given path")]
    public class GetDefaultDirectoryBrowser : IReturn<DefaultDirectoryBrowserInfo>
    {
        
    }

    /// <summary>
    /// Class EnvironmentService
    /// </summary>
    [Authenticated(Roles = "Admin", AllowBeforeStartupWizard = true)]
    public class EnvironmentService : BaseApiService
    {
        const char UncSeparator = '\\';

        /// <summary>
        /// The _network manager
        /// </summary>
        private readonly INetworkManager _networkManager;
        private IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentService" /> class.
        /// </summary>
        /// <param name="networkManager">The network manager.</param>
        public EnvironmentService(INetworkManager networkManager, IFileSystem fileSystem)
        {
            if (networkManager == null)
            {
                throw new ArgumentNullException("networkManager");
            }

            _networkManager = networkManager;
            _fileSystem = fileSystem;
        }

        public object Get(GetDefaultDirectoryBrowser request)
        {
            var result = new DefaultDirectoryBrowserInfo();

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                try
                {
                    var qnap = "/share/CACHEDEV1_DATA";
                    if (Directory.Exists(qnap))
                    {
                        result.Path = qnap;
                    }
                }
                catch
                {

                }
            }

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetDirectoryContents request)
        {
            var path = request.Path;

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("Path");
            }

            var networkPrefix = UncSeparator.ToString(CultureInfo.InvariantCulture) + UncSeparator.ToString(CultureInfo.InvariantCulture);

            if (path.StartsWith(networkPrefix, StringComparison.OrdinalIgnoreCase) && path.LastIndexOf(UncSeparator) == 1)
            {
                return ToOptimizedSerializedResultUsingCache(GetNetworkShares(path).OrderBy(i => i.Path).ToList());
            }

            return ToOptimizedSerializedResultUsingCache(GetFileSystemEntries(request).OrderBy(i => i.Path).ToList());
        }

        public object Get(GetNetworkShares request)
        {
            var path = request.Path;

            var shares = GetNetworkShares(path).OrderBy(i => i.Path).ToList();

            return ToOptimizedSerializedResultUsingCache(shares);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetDrives request)
        {
            var result = GetDrives().ToList();

            return ToOptimizedSerializedResultUsingCache(result);
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
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetNetworkDevices request)
        {
            var result = _networkManager.GetNetworkDevices()
                .OrderBy(i => i.Path)
                .ToList();

            return ToOptimizedSerializedResultUsingCache(result);
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
            // using EnumerateFileSystemInfos doesn't handle reparse points (symlinks)
            var entries = _fileSystem.GetFileSystemEntries(request.Path).Where(i =>
            {
                if (!request.IncludeHidden && i.Attributes.HasFlag(FileAttributes.Hidden))
                {
                    return false;
                }

                var isDirectory = i.IsDirectory;

                if (!request.IncludeFiles && !isDirectory)
                {
                    return false;
                }

                if (!request.IncludeDirectories && isDirectory)
                {
                    return false;
                }

                return true;
            });

            return entries.Select(f => new FileSystemEntryInfo
            {
                Name = f.Name,
                Path = f.FullName,
                Type = f.IsDirectory ? FileSystemEntryType.Directory : FileSystemEntryType.File

            }).ToList();
        }

        public object Get(GetParentPath request)
        {
            var parent = Path.GetDirectoryName(request.Path);

            if (string.IsNullOrEmpty(parent))
            {
                // Check if unc share
                var index = request.Path.LastIndexOf(UncSeparator);

                if (index != -1 && request.Path.IndexOf(UncSeparator) == 0)
                {
                    parent = request.Path.Substring(0, index);

                    if (string.IsNullOrWhiteSpace(parent.TrimStart(UncSeparator)))
                    {
                        parent = null;
                    }
                }
            }

            return parent;
        }
    }
}
