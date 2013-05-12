using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Controller.Providers.TV
{
    class FanArtTvProvider : FanartBaseProvider
    {
        protected string FanArtBaseUrl = "http://api.fanart.tv/webservice/series/{0}/{1}/xml/all/1/1";

        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        protected IHttpClient HttpClient { get; private set; }

        private readonly IProviderManager _providerManager;

        public FanArtTvProvider(IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager)
            : base(logManager, configurationManager)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }
            HttpClient = httpClient;
            _providerManager = providerManager;
        }

        public override bool Supports(BaseItem item)
        {
            return item is Series;
        }

        /// <summary>
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (string.IsNullOrEmpty(item.GetProviderId(MetadataProviders.Tvdb)))
            {
                return false;
            }
            
            if (!ConfigurationManager.Configuration.DownloadSeriesImages.Art &&
                !ConfigurationManager.Configuration.DownloadSeriesImages.Logo &&
                !ConfigurationManager.Configuration.DownloadSeriesImages.Thumb &&
                !ConfigurationManager.Configuration.DownloadSeriesImages.Backdrops &&
                !ConfigurationManager.Configuration.DownloadSeriesImages.Banner)
            {
                return false;
            }

            if (providerInfo.Data != GetComparisonData(item.GetProviderId(MetadataProviders.Tvdb)))
            {
                return true;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }

        /// <summary>
        /// Gets the comparison data.
        /// </summary>
        /// <returns>Guid.</returns>
        private Guid GetComparisonData(string id)
        {
            return string.IsNullOrEmpty(id) ? Guid.Empty : id.GetMD5();
        }
        
        public override async Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var status = ProviderRefreshStatus.Success;
            BaseProviderInfo data;

            if (!item.ProviderData.TryGetValue(Id, out data))
            {
                data = new BaseProviderInfo();
                item.ProviderData[Id] = data;
            }
            
            var series = (Series)item;

            string language = ConfigurationManager.Configuration.PreferredMetadataLanguage.ToLower();
            string url = string.Format(FanArtBaseUrl, APIKey, series.GetProviderId(MetadataProviders.Tvdb));
            var doc = new XmlDocument();

            using (var xml = await HttpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = FanArtResourcePool,
                CancellationToken = cancellationToken,
                EnableResponseCache = true

            }).ConfigureAwait(false))
            {
                doc.Load(xml);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (doc.HasChildNodes)
            {
                string path;
                var hd = ConfigurationManager.Configuration.DownloadHDFanArt ? "hdtv" : "clear";
                if (ConfigurationManager.Configuration.DownloadSeriesImages.Logo && !series.ResolveArgs.ContainsMetaFileByName(LOGO_FILE))
                {
                    var node = doc.SelectSingleNode("//fanart/series/" + hd + "logos/" + hd + "logo[@lang = \"" + language + "\"]/@url") ??
                                doc.SelectSingleNode("//fanart/series/clearlogos/clearlogo[@lang = \"" + language + "\"]/@url") ??
                                doc.SelectSingleNode("//fanart/series/" + hd + "logos/" + hd + "logo/@url") ??
                                doc.SelectSingleNode("//fanart/series/clearlogos/clearlogo/@url");
                    path = node != null ? node.Value : null;
                    if (!string.IsNullOrEmpty(path))
                    {
                        Logger.Debug("FanArtProvider getting ClearLogo for " + series.Name);
                        try
                        {
                            series.SetImage(ImageType.Logo, await _providerManager.DownloadAndSaveImage(series, path, LOGO_FILE, ConfigurationManager.Configuration.SaveLocalMeta, FanArtResourcePool, cancellationToken).ConfigureAwait(false));
                        }
                        catch (HttpException)
                        {
                            status = ProviderRefreshStatus.CompletedWithErrors;
                        }
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                hd = ConfigurationManager.Configuration.DownloadHDFanArt ? "hd" : "";
                if (ConfigurationManager.Configuration.DownloadSeriesImages.Art && !series.ResolveArgs.ContainsMetaFileByName(ART_FILE))
                {
                    var node = doc.SelectSingleNode("//fanart/series/" + hd + "cleararts/" + hd + "clearart[@lang = \"" + language + "\"]/@url") ??
                               doc.SelectSingleNode("//fanart/series/cleararts/clearart[@lang = \"" + language + "\"]/@url") ??
                               doc.SelectSingleNode("//fanart/series/" + hd + "cleararts/" + hd + "clearart/@url") ??
                               doc.SelectSingleNode("//fanart/series/cleararts/clearart/@url");
                    path = node != null ? node.Value : null;
                    if (!string.IsNullOrEmpty(path))
                    {
                        Logger.Debug("FanArtProvider getting ClearArt for " + series.Name);
                        try
                        {
                            series.SetImage(ImageType.Art, await _providerManager.DownloadAndSaveImage(series, path, ART_FILE, ConfigurationManager.Configuration.SaveLocalMeta, FanArtResourcePool, cancellationToken).ConfigureAwait(false));
                        }
                        catch (HttpException)
                        {
                            status = ProviderRefreshStatus.CompletedWithErrors;
                        }
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (ConfigurationManager.Configuration.DownloadSeriesImages.Thumb && !series.ResolveArgs.ContainsMetaFileByName(THUMB_FILE))
                {
                    var node = doc.SelectSingleNode("//fanart/series/tvthumbs/tvthumb[@lang = \"" + language + "\"]/@url") ??
                               doc.SelectSingleNode("//fanart/series/tvthumbs/tvthumb/@url");
                    path = node != null ? node.Value : null;
                    if (!string.IsNullOrEmpty(path))
                    {
                        Logger.Debug("FanArtProvider getting ThumbArt for " + series.Name);
                        try
                        {
                            series.SetImage(ImageType.Thumb, await _providerManager.DownloadAndSaveImage(series, path, THUMB_FILE, ConfigurationManager.Configuration.SaveLocalMeta, FanArtResourcePool, cancellationToken).ConfigureAwait(false));
                        }
                        catch (HttpException)
                        {
                            status = ProviderRefreshStatus.CompletedWithErrors;
                        }
                    }
                }

                if (ConfigurationManager.Configuration.DownloadSeriesImages.Banner && !series.ResolveArgs.ContainsMetaFileByName(BANNER_FILE))
                {
                    var node = doc.SelectSingleNode("//fanart/series/tbbanners/tvbanner[@lang = \"" + language + "\"]/@url") ??
                               doc.SelectSingleNode("//fanart/series/tbbanners/tvbanner/@url");
                    path = node != null ? node.Value : null;
                    if (!string.IsNullOrEmpty(path))
                    {
                        Logger.Debug("FanArtProvider getting banner for " + series.Name);
                        try
                        {
                            series.SetImage(ImageType.Banner, await _providerManager.DownloadAndSaveImage(series, path, BANNER_FILE, ConfigurationManager.Configuration.SaveLocalMeta, FanArtResourcePool, cancellationToken).ConfigureAwait(false));
                        }
                        catch (HttpException)
                        {
                            status = ProviderRefreshStatus.CompletedWithErrors;
                        }
                    }
                }
            }

            data.Data = GetComparisonData(item.GetProviderId(MetadataProviders.Tvcom));
            SetLastRefreshed(series, DateTime.UtcNow, status);

            return true;
        }
    }
}
