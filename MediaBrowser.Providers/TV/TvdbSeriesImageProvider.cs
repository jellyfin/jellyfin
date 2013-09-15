using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Providers.TV
{
    public class TvdbSeriesImageProvider : BaseMetadataProvider
    {
        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        protected IHttpClient HttpClient { get; private set; }

        /// <summary>
        /// The _provider manager
        /// </summary>
        private readonly IProviderManager _providerManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TvdbSeriesImageProvider"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        /// <exception cref="System.ArgumentNullException">httpClient</exception>
        public TvdbSeriesImageProvider(IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager)
            : base(logManager, configurationManager)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }
            HttpClient = httpClient;
            _providerManager = providerManager;
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is Series;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            // Run after fanart
            get { return MetadataProviderPriority.Fourth; }
        }

        /// <summary>
        /// Gets a value indicating whether [requires internet].
        /// </summary>
        /// <value><c>true</c> if [requires internet]; otherwise, <c>false</c>.</value>
        public override bool RequiresInternet
        {
            get
            {
                return true;
            }
        }

        public override ItemUpdateType ItemUpdateType
        {
            get
            {
                return ItemUpdateType.ImageUpdate;
            }
        }

        /// <summary>
        /// Gets a value indicating whether [refresh on version change].
        /// </summary>
        /// <value><c>true</c> if [refresh on version change]; otherwise, <c>false</c>.</value>
        protected override bool RefreshOnVersionChange
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the provider version.
        /// </summary>
        /// <value>The provider version.</value>
        protected override string ProviderVersion
        {
            get
            {
                return "1";
            }
        }

        protected override DateTime CompareDate(BaseItem item)
        {
            var seriesId = item.GetProviderId(MetadataProviders.Tvdb);

            if (!string.IsNullOrEmpty(seriesId))
            {
                // Process images
                var imagesXmlPath = Path.Combine(RemoteSeriesProvider.GetSeriesDataPath(ConfigurationManager.ApplicationPaths, seriesId), "banners.xml");

                var imagesFileInfo = new FileInfo(imagesXmlPath);

                if (imagesFileInfo.Exists)
                {
                    return imagesFileInfo.LastWriteTimeUtc;
                }
            }

            return base.CompareDate(item);
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
            cancellationToken.ThrowIfCancellationRequested();

            var series = (Series)item;
            var seriesId = series.GetProviderId(MetadataProviders.Tvdb);

            if (!string.IsNullOrEmpty(seriesId))
            {
                // Process images
                var imagesXmlPath = Path.Combine(RemoteSeriesProvider.GetSeriesDataPath(ConfigurationManager.ApplicationPaths, seriesId), "banners.xml");

                if (!series.HasImage(ImageType.Primary) || !series.HasImage(ImageType.Banner) || series.BackdropImagePaths.Count == 0)
                {
                    var backdropLimit = ConfigurationManager.Configuration.MaxBackdrops;

                    try
                    {
                        var fanartData = FetchFanartXmlData(imagesXmlPath, backdropLimit, cancellationToken);
                        await DownloadImages(item, fanartData, backdropLimit, cancellationToken).ConfigureAwait(false);
                    }
                    catch (FileNotFoundException)
                    {
                        // No biggie. Not all series have images
                    }
                }

                BaseProviderInfo data;
                if (!item.ProviderData.TryGetValue(Id, out data))
                {
                    data = new BaseProviderInfo();
                    item.ProviderData[Id] = data;
                }

                SetLastRefreshed(item, DateTime.UtcNow);
                return true;
            }

            return false;
        }

        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");

        private async Task DownloadImages(BaseItem item, FanartXmlData data, int backdropLimit, CancellationToken cancellationToken)
        {
            if (!item.HasImage(ImageType.Primary))
            {
                var url = data.LanguagePoster ?? data.Poster;
                if (!string.IsNullOrEmpty(url))
                {
                    url = TVUtils.BannerUrl + url;

                    await _providerManager.SaveImage(item, url, RemoteSeriesProvider.Current.TvDbResourcePool, ImageType.Primary, null, cancellationToken)
                      .ConfigureAwait(false);
                }
            }

            if (ConfigurationManager.Configuration.DownloadSeriesImages.Banner && !item.HasImage(ImageType.Banner))
            {
                var url = data.LanguageBanner ?? data.Banner;
                if (!string.IsNullOrEmpty(url))
                {
                    url = TVUtils.BannerUrl + url;

                    await _providerManager.SaveImage(item, url, RemoteSeriesProvider.Current.TvDbResourcePool, ImageType.Banner, null, cancellationToken)
                      .ConfigureAwait(false);
                }
            }

            if (ConfigurationManager.Configuration.DownloadSeriesImages.Backdrops && item.BackdropImagePaths.Count == 0)
            {
                var bdNo = item.BackdropImagePaths.Count;

                foreach (var backdrop in data.Backdrops)
                {
                    var url = TVUtils.BannerUrl + backdrop;

                    await _providerManager.SaveImage(item, url, RemoteSeriesProvider.Current.TvDbResourcePool, ImageType.Backdrop, bdNo, cancellationToken)
                      .ConfigureAwait(false);

                    bdNo++;

                    if (item.BackdropImagePaths.Count >= backdropLimit) break;
                }
            }
        }

        private FanartXmlData FetchFanartXmlData(string bannersXmlPath, int backdropLimit, CancellationToken cancellationToken)
        {
            var settings = new XmlReaderSettings
            {
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                ValidationType = ValidationType.None
            };

            var data = new FanartXmlData();

            using (var streamReader = new StreamReader(bannersXmlPath, Encoding.UTF8))
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
                                case "Banner":
                                    {
                                        using (var subtree = reader.ReadSubtree())
                                        {
                                            FetchInfoFromBannerNode(data, subtree, backdropLimit);
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

            return data;
        }

        private void FetchInfoFromBannerNode(FanartXmlData data, XmlReader reader, int backdropLimit)
        {
            reader.MoveToContent();

            string type = null;
            string url = null;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "BannerType":
                            {
                                type = reader.ReadElementContentAsString() ?? string.Empty;

                                if (string.Equals(type, "poster", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Already got it
                                    if (!string.IsNullOrEmpty(data.Poster))
                                    {
                                        return;
                                    }
                                }
                                else if (string.Equals(type, "series", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Already got it
                                    if (!string.IsNullOrEmpty(data.Banner))
                                    {
                                        return;
                                    }
                                }
                                else if (string.Equals(type, "fanart", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (data.Backdrops.Count >= backdropLimit)
                                    {
                                        return;
                                    }
                                }
                                else
                                {
                                    return;
                                }

                                break;
                            }

                        case "BannerPath":
                            {
                                url = reader.ReadElementContentAsString() ?? string.Empty;
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(url))
            {
                if (string.Equals(type, "poster", StringComparison.OrdinalIgnoreCase))
                {
                    data.Poster = url;
                }
                else if (string.Equals(type, "series", StringComparison.OrdinalIgnoreCase))
                {
                    data.Banner = url;
                }
                else if (string.Equals(type, "fanart", StringComparison.OrdinalIgnoreCase))
                {
                    data.Backdrops.Add(url);
                }
            }
        }
    }

    internal class FanartXmlData
    {
        public string LanguagePoster { get; set; }
        public string LanguageBanner { get; set; }
        public string Poster { get; set; }
        public string Banner { get; set; }
        public List<string> Backdrops = new List<string>();
    }
}
