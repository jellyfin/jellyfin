using System.IO;
using System.Threading;
using CommonIO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.LocalMetadata.Providers
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

        protected override void Fetch(MetadataResult<Folder> result, string path, CancellationToken cancellationToken)
        {
            new BaseItemXmlParser<Folder>(_logger).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return directoryService.GetFile(Path.Combine(info.Path, "folder.xml"));
        }
    }
}
