using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;

namespace MediaBrowser.Providers.Folders
{
    /// <summary>
    /// Provides metadata for Folders and all subclasses by parsing folder.xml
    /// </summary>
    public class FolderXmlProvider : BaseXmlProvider<Folder>
    {
        private readonly ILogger _logger;

        public FolderXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        protected override void Fetch(LocalMetadataResult<Folder> result, string path, CancellationToken cancellationToken)
        {
            new BaseItemXmlParser<Folder>(_logger).Fetch(result.Item, path, cancellationToken);
        }

        protected override FileSystemInfo GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return new FileInfo(Path.Combine(info.Path, "folder.xml"));
        }
    }
}
