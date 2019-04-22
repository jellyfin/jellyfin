using System.IO;
using System.Threading;
using Jellyfin.Controller.Entities.Movies;
using Jellyfin.Controller.Providers;
using Jellyfin.LocalMetadata.Parsers;
using Jellyfin.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LocalMetadata.Providers
{
    /// <summary>
    /// Class BoxSetXmlProvider.
    /// </summary>
    public class BoxSetXmlProvider : BaseXmlProvider<BoxSet>
    {
        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager;

        public BoxSetXmlProvider(IFileSystem fileSystem, ILogger logger, IProviderManager providerManager)
            : base(fileSystem)
        {
            _logger = logger;
            _providerManager = providerManager;
        }

        protected override void Fetch(MetadataResult<BoxSet> result, string path, CancellationToken cancellationToken)
        {
            new BoxSetXmlParser(_logger, _providerManager).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return directoryService.GetFile(Path.Combine(info.Path, "collection.xml"));
        }
    }
}
