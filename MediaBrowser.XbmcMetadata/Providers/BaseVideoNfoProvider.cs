#pragma warning disable CS1591

using System.Linq;
using System.Threading;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.XbmcMetadata.Parsers;
using MediaBrowser.XbmcMetadata.Savers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Providers
{
    public abstract class BaseVideoNfoProvider<T> : BaseNfoProvider<T>
        where T : Video, new()
    {
        private readonly ILogger _logger;
        private readonly IConfigurationManager _config;
        private readonly IProviderManager _providerManager;

        public BaseVideoNfoProvider(
            ILogger logger,
            IFileSystem fileSystem,
            IConfigurationManager config,
            IProviderManager providerManager)
            : base(fileSystem)
        {
            _logger = logger;
            _config = config;
            _providerManager = providerManager;
        }

        /// <inheritdoc />
        protected override void Fetch(MetadataResult<T> result, string path, CancellationToken cancellationToken)
        {
            var tmpItem = new MetadataResult<Video>
            {
                Item = result.Item
            };
            new MovieNfoParser(_logger, _config, _providerManager).Fetch(tmpItem, path, cancellationToken);

            result.Item = (T)tmpItem.Item;
            result.People = tmpItem.People;

            if (tmpItem.UserDataList != null)
            {
                result.UserDataList = tmpItem.UserDataList;
            }
        }

        /// <inheritdoc />
        protected override FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return MovieNfoSaver.GetMovieSavePaths(info)
                .Select(directoryService.GetFile)
                .FirstOrDefault(i => i != null);
        }
    }
}
