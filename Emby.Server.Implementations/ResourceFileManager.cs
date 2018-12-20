using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Extensions;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Reflection;
using MediaBrowser.Model.Services;

namespace Emby.Server.Implementations
{
    public class ResourceFileManager : IResourceFileManager
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly IHttpResultFactory _resultFactory;

        public ResourceFileManager(IHttpResultFactory resultFactory, ILogger logger, IFileSystem fileSystem)
        {
            _resultFactory = resultFactory;
            _logger = logger;
            _fileSystem = fileSystem;
        }

        public Stream GetResourceFileStream(string basePath, string virtualPath)
        {
            return _fileSystem.GetFileStream(GetResourcePath(basePath, virtualPath), FileOpenMode.Open, FileAccessMode.Read, FileShareMode.ReadWrite, true);
        }

        public Task<object> GetStaticFileResult(IRequest request, string basePath, string virtualPath, string contentType, TimeSpan? cacheDuration)
        {
            return _resultFactory.GetStaticFileResult(request, GetResourcePath(basePath, virtualPath));
        }

        public string ReadAllText(string basePath, string virtualPath)
        {
            return _fileSystem.ReadAllText(GetResourcePath(basePath, virtualPath));
        }

        private string GetResourcePath(string basePath, string virtualPath)
        {
            var fullPath = Path.Combine(basePath, virtualPath.Replace('/', _fileSystem.DirectorySeparatorChar));

            try
            {
                fullPath = _fileSystem.GetFullPath(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Path.GetFullPath");
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
