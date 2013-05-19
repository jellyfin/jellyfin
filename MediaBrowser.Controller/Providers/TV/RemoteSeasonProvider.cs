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

        private readonly IProviderManager _providerManager;
        
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

        protected override bool RefreshOnFileSystemStampChange
        {
            get
            {
                return ConfigurationManager.Configuration.SaveLocalMeta;
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
            if (HasLocalMeta(item))
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

            if (!HasLocalMeta(item))
            {
                var seriesId = season.Series != null ? season.Series.GetProviderId(MetadataProviders.Tvdb) : null;

                if (seriesId != null)
                {
                    var status = await FetchSeasonData(season, seriesId, cancellationToken).ConfigureAwait(false);

                    SetLastRefreshed(item, DateTime.UtcNow, status);

                    return true;
                }
                Logger.Info("Season provider not fetching because series does not have a tvdb id: " + season.Path);
            }
            else
            {
                Logger.Info("Season provider not fetching because local meta exists: " + season.Name);
            }
            return false;
        }


        /// <summary>
        /// Fetches the season data.
        /// </summary>
        /// <param name="season">The season.</param>
        /// <param name="seriesId">The series id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        private async Task<ProviderRefreshStatus> FetchSeasonData(Season season, string seriesId, CancellationToken cancellationToken)
        {
            var seasonNumber = TVUtils.GetSeasonNumberFromPath(season.Path) ?? -1;

            season.IndexNumber = seasonNumber;

            if (seasonNumber == 0)
            {
                season.Name = "Specials";
            }

            var status = ProviderRefreshStatus.Success;

            if (string.IsNullOrEmpty(seriesId))
            {
                return status;
            }

            if ((season.PrimaryImagePath == null) || (!season.HasImage(ImageType.Banner)) || (season.BackdropImagePaths == null))
            {
                var images = new XmlDocument();
                var url = string.Format("http://www.thetvdb.com/api/" + TVUtils.TvdbApiKey + "/series/{0}/banners.xml", seriesId);

                using (var imgs = await HttpClient.Get(new HttpRequestOptions
                {
                    Url = url,
                    ResourcePool = RemoteSeriesProvider.Current.TvDbResourcePool,
                    CancellationToken = cancellationToken,
                    EnableResponseCache = true

                }).ConfigureAwait(false))
                {
                    images.Load(imgs);
                }

                if (images.HasChildNodes)
                {
                    if (ConfigurationManager.Configuration.RefreshItemImages || !season.HasLocalImage("folder"))
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

                    if (ConfigurationManager.Configuration.DownloadSeasonImages.Banner && (ConfigurationManager.Configuration.RefreshItemImages || !season.HasLocalImage("banner")))
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

                    if (ConfigurationManager.Configuration.DownloadSeasonImages.Backdrops && (ConfigurationManager.Configuration.RefreshItemImages || !season.HasLocalImage("backdrop")))
                    {
                        var n = images.SelectSingleNode("//Banner[BannerType='fanart'][Season='" + seasonNumber + "']");
                        if (n != null)
                        {
                            n = n.SelectSingleNode("./BannerPath");
                            if (n != null)
                            {
                                if (season.BackdropImagePaths == null) season.BackdropImagePaths = new List<string>();
                                season.BackdropImagePaths.Add(await _providerManager.DownloadAndSaveImage(season, TVUtils.BannerUrl + n.InnerText, "backdrop" + Path.GetExtension(n.InnerText), ConfigurationManager.Configuration.SaveLocalMeta, RemoteSeriesProvider.Current.TvDbResourcePool, cancellationToken).ConfigureAwait(false));
                            }
                        }
                        else if (!ConfigurationManager.Configuration.SaveLocalMeta) //if saving local - season will inherit from series
                        {
                            // not necessarily accurate but will give a different bit of art to each season
                            var lst = images.SelectNodes("//Banner[BannerType='fanart']");
                            if (lst != null && lst.Count > 0)
                            {
                                var num = seasonNumber % lst.Count;
                                n = lst[num];
                                n = n.SelectSingleNode("./BannerPath");
                                if (n != null)
                                {
                                    if (season.BackdropImagePaths == null)
                                        season.BackdropImagePaths = new List<string>();

                                    season.BackdropImagePaths.Add(
                                        await _providerManager.DownloadAndSaveImage(season,
                                                                                         TVUtils.BannerUrl +
                                                                                         n.InnerText,
                                                                                         "backdrop" +
                                                                                         Path.GetExtension(
                                                                                             n.InnerText),
                                                                                         ConfigurationManager.Configuration.SaveLocalMeta, RemoteSeriesProvider.Current.TvDbResourcePool, cancellationToken)
                                                          .ConfigureAwait(false));
                                }
                            }
                        }
                    }
                }
            }
            return status;
        }

        /// <summary>
        /// Determines whether [has local meta] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if [has local meta] [the specified item]; otherwise, <c>false</c>.</returns>
        private bool HasLocalMeta(BaseItem item)
        {
            //just folder.jpg/png
            return (item.ResolveArgs.ContainsMetaFileByName("folder.jpg") ||
                    item.ResolveArgs.ContainsMetaFileByName("folder.png"));
        }

    }
}
