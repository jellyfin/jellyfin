#pragma warning disable CS1591

using System;
using System.IO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations
{
    public class ResourceFileManager : IResourceFileManager
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;

        public ResourceFileManager(ILogger<ResourceFileManager> logger, IFileSystem fileSystem)
        {
            _logger = logger;
            _fileSystem = fileSystem;
        }

        public string GetResourcePath(string basePath, string virtualPath)
        {
            var fullPath = Path.Combine(basePath, virtualPath.Replace('/', Path.DirectorySeparatorChar));

            try
            {
                fullPath = Path.GetFullPath(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving full path");
            }

            // Don't allow file system access outside of the source folder
            if (!_fileSystem.ContainsSubPath(basePath, fullPath))
            {
                throw new SecurityException("Access denied");
            }

            return fullPath;
        }
    }
}
