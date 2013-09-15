using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text;
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

            var url = FetchImageUrl(item, actorXmlPath, cancellationToken);

            if (!string.IsNullOrEmpty(url))
            {
                url = TVUtils.BannerUrl + url;

                await _providerManager.SaveImage(item, url, RemoteSeriesProvider.Current.TvDbResourcePool,
                                               ImageType.Primary, null, cancellationToken).ConfigureAwait(false);
            }
        }
        private string FetchImageUrl(BaseItem item, string actorsXmlPath, CancellationToken cancellationToken)
        {
            var settings = new XmlReaderSettings
            {
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                ValidationType = ValidationType.None
            };

            using (var streamReader = new StreamReader(actorsXmlPath, Encoding.UTF8))
            {
                // Use XmlReader for best performance
                using (var reader = XmlReader.Create(streamReader, settings))
                {
                    reader.MoveToContent();

                    // Loop through each element
                    while (reader.Read())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            switch (reader.Name)
                            {
                                case "Actor":
                                    {
                                        using (var subtree = reader.ReadSubtree())
                                        {
                                            var url = FetchImageUrlFromActorNode(item, subtree);

                                            if (!string.IsNullOrEmpty(url))
                                            {
                                                return url;
                                            }
                                        }
                                        break;
                                    }
                                default:
                                    reader.Skip();
                                    break;
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Fetches the data from actor node.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="reader">The reader.</param>
        private string FetchImageUrlFromActorNode(BaseItem item, XmlReader reader)
        {
            reader.MoveToContent();

            string name = null;
            string image = null;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Name":
                            {
                                name = (reader.ReadElementContentAsString() ?? string.Empty).Trim();
                                break;
                            }

                        case "Image":
                            {
                                image = (reader.ReadElementContentAsString() ?? string.Empty).Trim();
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(image) &&
                string.Equals(name, item.Name, StringComparison.OrdinalIgnoreCase))
            {
                return image;
            }

            return null;
        }


        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Third; }
        }
    }
}
