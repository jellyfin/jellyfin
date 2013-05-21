using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Controller.Providers.TV
{
    /// <summary>
    /// Class RemoteSeasonProvider
    /// </summary>
    class RemoteSeasonProvider : BaseMetadataProvider
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
        /// Initializes a new instance of the <see cref="RemoteSeasonProvider"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        /// <exception cref="System.ArgumentNullException">httpClient</exception>
        public RemoteSeasonProvider(IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager)
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

        /// <summary>
        /// Returns true or false indicating if the provider should refresh when the contents of it's directory changes
        /// </summary>
        /// <value><c>true</c> if [refresh on file system stamp change]; otherwise, <c>false</c>.</value>
        protected override bool RefreshOnFileSystemStampChange
        {
            get
            {
                return ConfigurationManager.Configuration.SaveLocalMeta;
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

        /// <summary>
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (GetComparisonData(item) != providerInfo.Data)
            {
                return true;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }

        /// <summary>
        /// Gets the comparison data.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Guid.</returns>
        private Guid GetComparisonData(BaseItem item)
        {
            var season = (Season)item;
            var seriesId = season.Series != null ? season.Series.GetProviderId(MetadataProviders.Tvdb) : null;

            if (!string.IsNullOrEmpty(seriesId))
            {
                // Process images
                var imagesXmlPath = Path.Combine(RemoteSeriesProvider.GetSeriesDataPath(ConfigurationManager.ApplicationPaths, seriesId), "banners.xml");

                var imagesFileInfo = new FileInfo(imagesXmlPath);

                return GetComparisonData(imagesFileInfo);
            }

            return Guid.Empty;
        }

        /// <summary>
        /// Gets the comparison data.
        /// </summary>
        /// <param name="imagesFileInfo">The images file info.</param>
        /// <returns>Guid.</returns>
        private Guid GetComparisonData(FileInfo imagesFileInfo)
        {
            var date = imagesFileInfo.Exists ? imagesFileInfo.LastWriteTimeUtc : DateTime.MinValue;

            var key = date.Ticks + imagesFileInfo.FullName;

            return key.GetMD5();
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

            if (!string.IsNullOrEmpty(seriesId))
            {
                // Process images
                var imagesXmlPath = Path.Combine(RemoteSeriesProvider.GetSeriesDataPath(ConfigurationManager.ApplicationPaths, seriesId), "banners.xml");

                var imagesFileInfo = new FileInfo(imagesXmlPath);

                if (imagesFileInfo.Exists)
                {
                    if (!season.HasImage(ImageType.Primary) || !season.HasImage(ImageType.Banner) || season.BackdropImagePaths.Count == 0)
                    {
                        var xmlDoc = new XmlDocument();
                        xmlDoc.Load(imagesXmlPath);

                        await FetchImages(season, xmlDoc, cancellationToken).ConfigureAwait(false);
                    }
                }

                BaseProviderInfo data;
                if (!item.ProviderData.TryGetValue(Id, out data))
                {
                    data = new BaseProviderInfo();
                    item.ProviderData[Id] = data;
                }

                data.Data = GetComparisonData(imagesFileInfo);
                
                SetLastRefreshed(item, DateTime.UtcNow);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Fetches the images.
        /// </summary>
        /// <param name="season">The season.</param>
        /// <param name="images">The images.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task FetchImages(Season season, XmlDocument images, CancellationToken cancellationToken)
        {
            var seasonNumber = season.IndexNumber ?? -1;

            if (seasonNumber == -1)
            {
                return;
            }

            if (ConfigurationManager.Configuration.RefreshItemImages || !season.HasImage(ImageType.Primary))
            {
                var n = images.SelectSingleNode("//Banner[BannerType='season'][BannerType2='season'][Season='" + seasonNumber + "'][Language='" + ConfigurationManager.Configuration.PreferredMetadataLanguage + "']") ??
                        images.SelectSingleNode("//Banner[BannerType='season'][BannerType2='season'][Season='" + seasonNumber + "'][Language='en']");
                if (n != null)
                {
                    n = n.SelectSingleNode("./BannerPath");

                    if (n != null)
                        season.PrimaryImagePath = await _providerManager.DownloadAndSaveImage(season, TVUtils.BannerUrl + n.InnerText, "folder" + Path.GetExtension(n.InnerText), ConfigurationManager.Configuration.SaveLocalMeta, RemoteSeriesProvider.Current.TvDbResourcePool, cancellationToken).ConfigureAwait(false);
                }
            }

            if (ConfigurationManager.Configuration.DownloadSeasonImages.Banner && (ConfigurationManager.Configuration.RefreshItemImages || !season.HasImage(ImageType.Banner)))
            {
                var n = images.SelectSingleNode("//Banner[BannerType='season'][BannerType2='seasonwide'][Season='" + seasonNumber + "'][Language='" + ConfigurationManager.Configuration.PreferredMetadataLanguage + "']") ??
                        images.SelectSingleNode("//Banner[BannerType='season'][BannerType2='seasonwide'][Season='" + seasonNumber + "'][Language='en']");
                if (n != null)
                {
                    n = n.SelectSingleNode("./BannerPath");
                    if (n != null)
                    {
                        var bannerImagePath =
                            await _providerManager.DownloadAndSaveImage(season,
                                                                             TVUtils.BannerUrl + n.InnerText,
                                                                             "banner" +
                                                                             Path.GetExtension(n.InnerText),
                                                                             ConfigurationManager.Configuration.SaveLocalMeta, RemoteSeriesProvider.Current.TvDbResourcePool, cancellationToken).
                                               ConfigureAwait(false);

                        season.SetImage(ImageType.Banner, bannerImagePath);
                    }
                }
            }

            if (ConfigurationManager.Configuration.DownloadSeasonImages.Backdrops && (ConfigurationManager.Configuration.RefreshItemImages || season.BackdropImagePaths.Count == 0))
            {
                var n = images.SelectSingleNode("//Banner[BannerType='fanart'][Season='" + seasonNumber + "']");
                if (n != null)
                {
                    n = n.SelectSingleNode("./BannerPath");
                    if (n != null)
                    {
                        season.BackdropImagePaths.Add(await _providerManager.DownloadAndSaveImage(season, TVUtils.BannerUrl + n.InnerText, "backdrop" + Path.GetExtension(n.InnerText), ConfigurationManager.Configuration.SaveLocalMeta, RemoteSeriesProvider.Current.TvDbResourcePool, cancellationToken).ConfigureAwait(false));
                    }
                }
            }
        }
    }
}
