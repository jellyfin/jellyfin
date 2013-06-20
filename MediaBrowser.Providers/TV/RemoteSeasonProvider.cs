using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.IO;
using System.Net;
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
            var season = (Season)item;
            var seriesId = season.Series != null ? season.Series.GetProviderId(MetadataProviders.Tvdb) : null;

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

            if (!season.HasImage(ImageType.Primary))
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

            if (ConfigurationManager.Configuration.DownloadSeasonImages.Banner && !season.HasImage(ImageType.Banner))
            {
                var n = images.SelectSingleNode("//Banner[BannerType='season'][BannerType2='seasonwide'][Season='" + seasonNumber + "'][Language='" + ConfigurationManager.Configuration.PreferredMetadataLanguage + "']") ??
                        images.SelectSingleNode("//Banner[BannerType='season'][BannerType2='seasonwide'][Season='" + seasonNumber + "'][Language='en']");
                if (n != null)
                {
                    n = n.SelectSingleNode("./BannerPath");
                    if (n != null)
                    {
                        try
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
                        catch (HttpException ex)
                        {
                            Logger.ErrorException("Error downloading season banner for {0}", ex, season.Path);

                            // Sometimes banners will come up not found even though they're reported in tvdb xml
                            if (ex.StatusCode.HasValue && ex.StatusCode.Value != HttpStatusCode.NotFound)
                            {
                                throw;
                            }
                        }
                    }
                }
            }

            if (ConfigurationManager.Configuration.DownloadSeasonImages.Backdrops && season.BackdropImagePaths.Count == 0)
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
