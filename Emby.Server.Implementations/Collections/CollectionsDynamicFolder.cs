using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using System.IO;
using MediaBrowser.Common.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.IO;

namespace Emby.Server.Implementations.Collections
{
    public class CollectionsDynamicFolder : IVirtualFolderCreator
    {
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;

        public CollectionsDynamicFolder(IApplicationPaths appPaths, IFileSystem fileSystem)
        {
            _appPaths = appPaths;
            _fileSystem = fileSystem;
        }

        public BasePluginFolder GetFolder()
        {
            var path = Path.Combine(_appPaths.DataPath, "collections");

			_fileSystem.CreateDirectory(path);

            return new ManualCollectionsFolder
            {
                Path = path
            };
        }
    }
}
