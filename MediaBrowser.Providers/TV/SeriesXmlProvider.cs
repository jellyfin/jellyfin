using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Threading;

namespace MediaBrowser.Providers.TV
{
    /// <summary>
    /// Class SeriesProviderFromXml
    /// </summary>
    public class SeriesXmlProvider : BaseXmlProvider<Series>
    {
        private readonly ILogger _logger;

        public SeriesXmlProvider(IFileSystem fileSystem, ILogger logger)
            : base(fileSystem)
        {
            _logger = logger;
        }

        protected override void Fetch(Series item, string path, CancellationToken cancellationToken)
        {
            new SeriesXmlParser(_logger).Fetch(item, path, cancellationToken);
        }

        protected override FileInfo GetXmlFile(ItemInfo info)
        {
            return new FileInfo(Path.Combine(info.Path, "series.xml"));
        }
    }
}
