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

        protected override void Fetch(Folder item, string path, CancellationToken cancellationToken)
        {
            new BaseItemXmlParser<Folder>(_logger).Fetch(item, path, cancellationToken);
        }

        protected override FileInfo GetXmlFile(ItemInfo info)
        {
            return new FileInfo(Path.Combine(info.Path, "folder.xml"));
        }
    }
}
