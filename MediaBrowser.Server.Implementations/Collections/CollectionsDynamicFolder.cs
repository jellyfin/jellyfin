using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using System.IO;
using CommonIO;

namespace MediaBrowser.Server.Implementations.Collections
{
    public class CollectionsDynamicFolder : IVirtualFolderCreator
    {
        private readonly IApplicationPaths _appPaths;
        private IFileSystem _fileSystem;

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
