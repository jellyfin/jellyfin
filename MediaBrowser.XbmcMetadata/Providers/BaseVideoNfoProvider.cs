using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.XbmcMetadata.Parsers;
using MediaBrowser.XbmcMetadata.Savers;
using System.Collections.Generic;
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
            var chapters = new List<ChapterInfo>();

            new MovieNfoParser(_logger, _config).Fetch(result.Item, result.UserDataLIst, chapters, path, cancellationToken);

            result.Chapters = chapters;
        }

        protected override FileSystemInfo GetXmlFile(ItemInfo info, IDirectoryService directoryService)
        {
            return MovieNfoSaver.GetMovieSavePaths(info, FileSystem)
                .Select(directoryService.GetFile)
                .FirstOrDefault(i => i != null);
        }
    }
}