#pragma warning disable CS1591

using System.Linq;
using System.Threading;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
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
        private readonly ILogger<BaseVideoNfoProvider<T>> _logger;
        private readonly IConfigurationManager _config;
        private readonly IProviderManager _providerManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;
        private readonly IDirectoryService _directoryService;

        protected BaseVideoNfoProvider(
            ILogger<BaseVideoNfoProvider<T>> logger,
            IFileSystem fileSystem,
            IConfigurationManager config,
            IProviderManager providerManager,
            IUserManager userManager,
            IUserDataManager userDataManager,
            IDirectoryService directoryService)
            : base(fileSystem)
        {
            _logger = logger;
            _config = config;
            _providerManager = providerManager;
            _userManager = userManager;
            _userDataManager = userDataManager;
            _directoryService = directoryService;
        }

        /// <inheritdoc />
        protected override void Fetch(MetadataResult<T> result, string path, CancellationToken cancellationToken)
        {
            var tmpItem = new MetadataResult<Video>
            {
                Item = result.Item
            };
            new MovieNfoParser(_logger, _config, _providerManager, _userManager, _userDataManager, _directoryService).Fetch(tmpItem, path, cancellationToken);

            result.Item = (T)tmpItem.Item;
            result.People = tmpItem.People;
            result.Images = tmpItem.Images;
            result.RemoteImages = tmpItem.RemoteImages;
        }

        /// <inheritdoc />
        protected override FileSystemMetadata? GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return MovieNfoSaver.GetMovieSavePaths(info)
                .Select(directoryService.GetFile)
                .FirstOrDefault(i => i is not null);
        }
    }
}
