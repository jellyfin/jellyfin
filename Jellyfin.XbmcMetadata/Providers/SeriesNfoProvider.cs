using System.IO;
using System.Threading;
using Jellyfin.Common.Configuration;
using Jellyfin.Controller.Entities.TV;
using Jellyfin.Controller.Providers;
using Jellyfin.Model.IO;
using Jellyfin.XbmcMetadata.Parsers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.XbmcMetadata.Providers
{
    public class SeriesNfoProvider : BaseNfoProvider<Series>
    {
        private readonly ILogger _logger;
        private readonly IConfigurationManager _config;
        private readonly IProviderManager _providerManager;

        public SeriesNfoProvider(IFileSystem fileSystem, ILogger logger, IConfigurationManager config, IProviderManager providerManager)
            : base(fileSystem)
        {
            _logger = logger;
            _config = config;
            _providerManager = providerManager;
        }

        protected override void Fetch(MetadataResult<Series> result, string path, CancellationToken cancellationToken)
        {
            new SeriesNfoParser(_logger, _config, _providerManager).Fetch(result, path, cancellationToken);
        }

        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
            => directoryService.GetFile(Path.Combine(info.Path, "tvshow.nfo"));
    }
}
