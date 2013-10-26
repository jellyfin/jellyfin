using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Providers.TV
{
    /// <summary>
    /// Class RemoteSeasonProvider
    /// </summary>
    class RemoteSeasonProvider : BaseMetadataProvider
    {
        /// <summary>
        /// The _provider manager
        /// </summary>
        private readonly IProviderManager _providerManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteSeasonProvider"/> class.
        /// </summary>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        /// <exception cref="System.ArgumentNullException">httpClient</exception>
        public RemoteSeasonProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager)
            : base(logManager, configurationManager)
        {
            _providerManager = providerManager;
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is Season;
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
                return "2";
            }
        }

        protected override bool NeedsRefreshBasedOnCompareDate(BaseItem item, BaseProviderInfo providerInfo)
        {
            var season = (Season)item;
            var seriesId = season.Series != null ? season.Series.GetProviderId(MetadataProviders.Tvdb) : null;

            if (!string.IsNullOrEmpty(seriesId))
            {
                // Process images
                var imagesXmlPath = Path.Combine(RemoteSeriesProvider.GetSeriesDataPath(ConfigurationManager.ApplicationPaths, seriesId), "banners.xml");

                var imagesFileInfo = new FileInfo(imagesXmlPath);

                if (imagesFileInfo.Exists)
                {
                    return imagesFileInfo.LastWriteTimeUtc > providerInfo.LastRefreshed;
                }
            }
            return false;
        }

        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (item.HasImage(ImageType.Primary) && item.HasImage(ImageType.Banner) && item.BackdropImagePaths.Count > 0)
            {
                return false;
            }
            return base.NeedsRefreshInternal(item, providerInfo);
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

            var season = (Season)item;

            var seriesId = season.Series != null ? season.Series.GetProviderId(MetadataProviders.Tvdb) : null;

            var seasonNumber = season.IndexNumber;

            if (!string.IsNullOrEmpty(seriesId) && seasonNumber.HasValue)
            {
                // Process images
                var imagesXmlPath = Path.Combine(RemoteSeriesProvider.GetSeriesDataPath(ConfigurationManager.ApplicationPaths, seriesId), "banners.xml");

                try
                {
                    var fanartData = FetchFanartXmlData(imagesXmlPath, seasonNumber.Value, cancellationToken);
                    await DownloadImages(item, fanartData, 1, cancellationToken).ConfigureAwait(false);
                }
                catch (FileNotFoundException)
                {
                    // No biggie. Not all series have images
                }

                SetLastRefreshed(item, DateTime.UtcNow);
                return true;
            }

            return false;
        }

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

            if (ConfigurationManager.Configuration.DownloadSeasonImages.Banner && !item.HasImage(ImageType.Banner))
            {
                var url = data.LanguageBanner ?? data.Banner;
                if (!string.IsNullOrEmpty(url))
                {
                    url = TVUtils.BannerUrl + url;

                    await _providerManager.SaveImage(item, url, RemoteSeriesProvider.Current.TvDbResourcePool, ImageType.Banner, null, cancellationToken)
                      .ConfigureAwait(false);
                }
            }

            if (ConfigurationManager.Configuration.DownloadSeasonImages.Backdrops && item.BackdropImagePaths.Count < backdropLimit)
            {
                var bdNo = item.BackdropImagePaths.Count;

                foreach (var backdrop in data.Backdrops)
                {
                    var url = TVUtils.BannerUrl + backdrop.Url;

                    if (item.ContainsImageWithSourceUrl(url))
                    {
                        continue;
                    }

                    await _providerManager.SaveImage(item, url, RemoteSeriesProvider.Current.TvDbResourcePool, ImageType.Backdrop, bdNo, cancellationToken)
                      .ConfigureAwait(false);

                    bdNo++;

                    if (item.BackdropImagePaths.Count >= backdropLimit) break;
                }
            }
        }

        private FanartXmlData FetchFanartXmlData(string bannersXmlPath, int seasonNumber, CancellationToken cancellationToken)
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
                                            FetchInfoFromBannerNode(data, subtree, seasonNumber);
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

        private void FetchInfoFromBannerNode(FanartXmlData data, XmlReader reader, int seasonNumber)
        {
            reader.MoveToContent();

            string bannerType = null;
            string bannerType2 = null;
            string url = null;
            int? bannerSeason = null;
            string resolution = null;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "BannerType":
                            {
                                bannerType = reader.ReadElementContentAsString() ?? string.Empty;
                                break;
                            }

                        case "BannerType2":
                            {
                                bannerType2 = reader.ReadElementContentAsString() ?? string.Empty;
                                break;
                            }

                        case "BannerPath":
                            {
                                url = reader.ReadElementContentAsString() ?? string.Empty;
                                break;
                            }

                        case "Season":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    bannerSeason = int.Parse(val);
                                }
                                break;
                            }


                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(url) && bannerSeason.HasValue && bannerSeason.Value == seasonNumber)
            {
                if (string.Equals(bannerType, "season", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(bannerType2, "season", StringComparison.OrdinalIgnoreCase))
                    {
                        // Just grab the first
                        if (string.IsNullOrWhiteSpace(data.Poster))
                        {
                            data.Poster = url;
                        }
                    }
                    else if (string.Equals(bannerType2, "seasonwide", StringComparison.OrdinalIgnoreCase))
                    {
                        // Just grab the first
                        if (string.IsNullOrWhiteSpace(data.Banner))
                        {
                            data.Banner = url;
                        }
                    }
                }
                else if (string.Equals(bannerType, "fanart", StringComparison.OrdinalIgnoreCase))
                {
                    data.Backdrops.Add(new ImageInfo
                    {
                        Url = url,
                        Resolution = resolution
                    });
                }
            }
        }

    }
}
