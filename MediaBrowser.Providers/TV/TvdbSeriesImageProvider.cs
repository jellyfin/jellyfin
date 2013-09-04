using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Globalization;
using System.IO;
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

                var imagesFileInfo = new FileInfo(imagesXmlPath);

                if (imagesFileInfo.Exists)
                {
                    if (!series.HasImage(ImageType.Primary) || !series.HasImage(ImageType.Banner) || series.BackdropImagePaths.Count == 0)
                    {
                        var xmlDoc = new XmlDocument();
                        xmlDoc.Load(imagesXmlPath);

                        await FetchImages(series, xmlDoc, cancellationToken).ConfigureAwait(false);
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

        /// <summary>
        /// Fetches the images.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="images">The images.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task FetchImages(Series series, XmlDocument images, CancellationToken cancellationToken)
        {
            if (!series.HasImage(ImageType.Primary))
            {
                var n = images.SelectSingleNode("//Banner[BannerType='poster']");
                if (n != null)
                {
                    n = n.SelectSingleNode("./BannerPath");
                    if (n != null)
                    {
                        var url = TVUtils.BannerUrl + n.InnerText;

                        await _providerManager.SaveImage(series, url, RemoteSeriesProvider.Current.TvDbResourcePool, ImageType.Primary, null, cancellationToken)
                          .ConfigureAwait(false);
                    }
                }
            }

            if (ConfigurationManager.Configuration.DownloadSeriesImages.Banner && !series.HasImage(ImageType.Banner))
            {
                var n = images.SelectSingleNode("//Banner[BannerType='series']");
                if (n != null)
                {
                    n = n.SelectSingleNode("./BannerPath");
                    if (n != null)
                    {
                        var url = TVUtils.BannerUrl + n.InnerText;

                        await _providerManager.SaveImage(series, url, RemoteSeriesProvider.Current.TvDbResourcePool, ImageType.Banner, null, cancellationToken)
                          .ConfigureAwait(false);
                    }
                }
            }

            if (series.BackdropImagePaths.Count == 0)
            {
                var bdNo = series.BackdropImagePaths.Count;

                var xmlNodeList = images.SelectNodes("//Banner[BannerType='fanart']");
                if (xmlNodeList != null)
                {
                    foreach (XmlNode b in xmlNodeList)
                    {
                        var p = b.SelectSingleNode("./BannerPath");

                        if (p != null)
                        {
                            var url = TVUtils.BannerUrl + p.InnerText;

                            await _providerManager.SaveImage(series, url, RemoteSeriesProvider.Current.TvDbResourcePool, ImageType.Backdrop, bdNo, cancellationToken)
                              .ConfigureAwait(false);
                            
                            bdNo++;
                        }

                        if (series.BackdropImagePaths.Count >= ConfigurationManager.Configuration.MaxBackdrops) break;
                    }
                }
            }
        }
    }
}
