using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using MediaBrowser.XbmcMetadata.Parsers;
using MediaBrowser.XbmcMetadata.Savers;
using System.IO;
using System.Linq;
using System.Threading;

namespace MediaBrowser.XbmcMetadata.Providers
{
    public class BaseVideoNfoProvider<T> : BaseNfoProvider<T>
        where T : Video, new ()
    {
        private readonly ILogger _logger;
        private readonly IConfigurationManager _config;

        public BaseVideoNfoProvider(IFileSystem fileSystem, ILogger logger, IConfigurationManager config)
            : base(fileSystem)
        {
            _logger = logger;
            _config = config;
        }

        protected override void Fetch(LocalMetadataResult<T> result, string path, CancellationToken cancellationToken)
        {
            var tmpItem = new LocalMetadataResult<Video>
            {
                Item = result.Item
            };
            new MovieNfoParser(_logger, _config).Fetch(tmpItem, path, cancellationToken);

            result.Item = (T)tmpItem.Item;
            result.People = tmpItem.People;
            result.UserDataLIst = tmpItem.UserDataLIst;
        }

        protected override FileSystemInfo GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return MovieNfoSaver.GetMovieSavePaths(info, FileSystem)
                .Select(directoryService.GetFile)
                .FirstOrDefault(i => i != null);
        }
    }
}