using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Providers.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Providers.TV
{
    public class TvdbPersonImageProvider : BaseMetadataProvider
    {
        private readonly ILibraryManager _library;
        private readonly IProviderManager _providerManager;

        public TvdbPersonImageProvider(ILogManager logManager, IServerConfigurationManager configurationManager, ILibraryManager library, IProviderManager providerManager)
            : base(logManager, configurationManager)
        {
            _library = library;
            _providerManager = providerManager;
        }

        public override bool Supports(BaseItem item)
        {
            return item is Person;
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(item.PrimaryImagePath))
            {
                var seriesWithPerson = _library.RootFolder
                    .RecursiveChildren
                    .OfType<Series>()
                    .Where(i => !string.IsNullOrEmpty(i.GetProviderId(MetadataProviders.Tvdb)) && i.People.Any(p => string.Equals(p.Name, item.Name, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                foreach (var series in seriesWithPerson)
                {
                    try
                    {
                        await DownloadImageFromSeries(item, series, cancellationToken).ConfigureAwait(false);
                    }
                    catch (FileNotFoundException)
                    {
                        // No biggie
                        continue;
                    }

                    // break once we have an image
                    if (!string.IsNullOrEmpty(item.PrimaryImagePath))
                    {
                        break;
                    }
                }

            }

            SetLastRefreshed(item, DateTime.UtcNow);
            return true;
        }

        /// <summary>
        /// Downloads the image from series.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="series">The series.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task DownloadImageFromSeries(BaseItem item, Series series, CancellationToken cancellationToken)
        {
            var tvdbPath = RemoteSeriesProvider.GetSeriesDataPath(ConfigurationManager.ApplicationPaths, series.GetProviderId(MetadataProviders.Tvdb));

            var actorXmlPath = Path.Combine(tvdbPath, "actors.xml");

            var xmlDoc = new XmlDocument();

            xmlDoc.Load(actorXmlPath);

            var actorNodes = xmlDoc.SelectNodes("//Actor");

            if (actorNodes == null)
            {
                return;
            }

            foreach (var actorNode in actorNodes.OfType<XmlNode>())
            {
                var name = actorNode.SafeGetString("Name");

                if (string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    var image = actorNode.SafeGetString("Image");

                    if (!string.IsNullOrEmpty(image))
                    {
                        var url = TVUtils.BannerUrl + image;

                        await _providerManager.SaveImage(item, url, RemoteSeriesProvider.Current.TvDbResourcePool,
                                                       ImageType.Primary, null, cancellationToken).ConfigureAwait(false);
                    }

                    break;
                }
            }
        }

        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Third; }
        }
    }
}
