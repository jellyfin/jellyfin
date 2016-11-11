using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Server.Implementations.Devices;

namespace Emby.Server.Implementations.Devices
{
    public class CameraUploadsDynamicFolder : IVirtualFolderCreator
    {
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;

        public CameraUploadsDynamicFolder(IApplicationPaths appPaths, IFileSystem fileSystem)
        {
            _appPaths = appPaths;
            _fileSystem = fileSystem;
        }

        public BasePluginFolder GetFolder()
        {
            var path = Path.Combine(_appPaths.DataPath, "camerauploads");

            _fileSystem.CreateDirectory(path);

            return new CameraUploadsFolder
            {
                Path = path
            };
        }
    }

}
