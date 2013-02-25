using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Resolvers.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
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

        public RemoteSeasonProvider(IHttpClient httpClient)
            : base()
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }
            HttpClient = httpClient;
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
            get { return MetadataProviderPriority.Second; }
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
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            bool fetch = false;
            var downloadDate = providerInfo.LastRefreshed;

            if (Kernel.Instance.Configuration.MetadataRefreshDays == -1 && downloadDate != DateTime.MinValue)
                return false;

            if (!HasLocalMeta(item))
            {
                fetch = Kernel.Instance.Configuration.MetadataRefreshDays != -1 &&
                    DateTime.UtcNow.Subtract(downloadDate).TotalDays > Kernel.Instance.Configuration.MetadataRefreshDays;
            }

            return fetch;
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        protected override async Task<bool> FetchAsyncInternal(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var season = (Season)item;

            if (!HasLocalMeta(item))
            {
                var seriesId = season.Series != null ? season.Series.GetProviderId(MetadataProviders.Tvdb) : null;

                if (seriesId != null)
                {
                    await FetchSeasonData(season, seriesId, cancellationToken).ConfigureAwait(false);
                    SetLastRefreshed(item, DateTime.UtcNow);
                    return true;
                }
                Logger.Info("Season provider unable to obtain series id for {0}", item.Path);
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
        private async Task<bool> FetchSeasonData(Season season, string seriesId, CancellationToken cancellationToken)
        {
            string name = season.Name;

            Logger.Debug("TvDbProvider: Fetching season data: " + name);
            var seasonNumber = TVUtils.GetSeasonNumberFromPath(season.Path) ?? -1;

            season.IndexNumber = seasonNumber;

            if (seasonNumber == 0)
            {
                season.Name = "Specials";
            }

            if (!string.IsNullOrEmpty(seriesId))
            {
                if ((season.PrimaryImagePath == null) || (!season.HasImage(ImageType.Banner)) || (season.BackdropImagePaths == null))
                {
                    var images = new XmlDocument();
                    var url = string.Format("http://www.thetvdb.com/api/" + TVUtils.TVDBApiKey + "/series/{0}/banners.xml", seriesId);

                    try
                    {
                        using (var imgs = await HttpClient.Get(url, Kernel.Instance.ResourcePools.TvDb, cancellationToken).ConfigureAwait(false))
                        {
                            images.Load(imgs);
                        }
                    }
                    catch (HttpException)
                    {
                    }

                    if (images.HasChildNodes)
                    {
                        if (Kernel.Instance.Configuration.RefreshItemImages || !season.HasLocalImage("folder"))
                        {
                            var n = images.SelectSingleNode("//Banner[BannerType='season'][BannerType2='season'][Season='" + seasonNumber + "']");
                            if (n != null)
                            {
                                n = n.SelectSingleNode("./BannerPath");

                                try
                                {
                                    if (n != null)
                                        season.PrimaryImagePath = await Kernel.Instance.ProviderManager.DownloadAndSaveImage(season, TVUtils.BannerUrl + n.InnerText, "folder" + Path.GetExtension(n.InnerText), Kernel.Instance.ResourcePools.TvDb, cancellationToken).ConfigureAwait(false);
                                }
                                catch (HttpException)
                                {
                                }
                                catch (IOException)
                                {

                                }
                            }
                        }

                        if (Kernel.Instance.Configuration.DownloadTVSeasonBanner && (Kernel.Instance.Configuration.RefreshItemImages || !season.HasLocalImage("banner")))
                        {
                            var n = images.SelectSingleNode("//Banner[BannerType='season'][BannerType2='seasonwide'][Season='" + seasonNumber + "']");
                            if (n != null)
                            {
                                n = n.SelectSingleNode("./BannerPath");
                                if (n != null)
                                {
                                    try
                                    {
                                        var bannerImagePath =
                                            await
                                            Kernel.Instance.ProviderManager.DownloadAndSaveImage(season,
                                                                                             TVUtils.BannerUrl + n.InnerText,
                                                                                             "banner" +
                                                                                             Path.GetExtension(n.InnerText),
                                                                                             Kernel.Instance.ResourcePools.TvDb, cancellationToken).
                                                               ConfigureAwait(false);

                                        season.SetImage(ImageType.Banner, bannerImagePath);
                                    }
                                    catch (HttpException)
                                    {
                                    }
                                    catch (IOException)
                                    {

                                    }
                                }
                            }
                        }

                        if (Kernel.Instance.Configuration.DownloadTVSeasonBackdrops && (Kernel.Instance.Configuration.RefreshItemImages || !season.HasLocalImage("backdrop")))
                        {
                            var n = images.SelectSingleNode("//Banner[BannerType='fanart'][Season='" + seasonNumber + "']");
                            if (n != null)
                            {
                                n = n.SelectSingleNode("./BannerPath");
                                if (n != null)
                                {
                                    try
                                    {
                                        if (season.BackdropImagePaths == null) season.BackdropImagePaths = new List<string>();
                                        season.BackdropImagePaths.Add(await Kernel.Instance.ProviderManager.DownloadAndSaveImage(season, TVUtils.BannerUrl + n.InnerText, "backdrop" + Path.GetExtension(n.InnerText), Kernel.Instance.ResourcePools.TvDb, cancellationToken).ConfigureAwait(false));
                                    }
                                    catch (HttpException)
                                    {
                                    }
                                    catch (IOException)
                                    {

                                    }
                                }
                            }
                            else if (!Kernel.Instance.Configuration.SaveLocalMeta) //if saving local - season will inherit from series
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

                                        try
                                        {
                                            season.BackdropImagePaths.Add(
                                                await
                                                Kernel.Instance.ProviderManager.DownloadAndSaveImage(season,
                                                                                                 TVUtils.BannerUrl +
                                                                                                 n.InnerText,
                                                                                                 "backdrop" +
                                                                                                 Path.GetExtension(
                                                                                                     n.InnerText),
                                                                                                 Kernel.Instance.ResourcePools.TvDb, cancellationToken)
                                                                  .ConfigureAwait(false));
                                        }
                                        catch (HttpException)
                                        {
                                        }
                                        catch (IOException)
                                        {

                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return true;
            }

            return false;
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
