using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Globalization;
using System.IO;
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
        /// Gets the series data path.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="seriesId">The series id.</param>
        /// <returns>System.String.</returns>
        internal static string GetSeriesDataPath(IApplicationPaths appPaths, string seriesId)
        {
            var seriesDataPath = Path.Combine(GetSeriesDataPath(appPaths), seriesId);

            if (!Directory.Exists(seriesDataPath))
            {
                Directory.CreateDirectory(seriesDataPath);
            }

            return seriesDataPath;
        }

        /// <summary>
        /// Gets the series data path.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <returns>System.String.</returns>
        internal static string GetSeriesDataPath(IApplicationPaths appPaths)
        {
            var dataPath = Path.Combine(appPaths.DataPath, "fanart-tv");

            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }

            return dataPath;
        }
        
        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");
        
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

            var seriesId = series.GetProviderId(MetadataProviders.Tvdb);
            string url = string.Format(FanArtBaseUrl, ApiKey, seriesId);

            var xmlPath = Path.Combine(GetSeriesDataPath(ConfigurationManager.ApplicationPaths, seriesId), "fanart.xml");

            using (var response = await HttpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = FanArtResourcePool,
                CancellationToken = cancellationToken

            }).ConfigureAwait(false))
            {
                using (var xmlFileStream = new FileStream(xmlPath, FileMode.Create, FileAccess.Write, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous))
                {
                    await response.CopyToAsync(xmlFileStream).ConfigureAwait(false);
                }
            }

            var doc = new XmlDocument();
            doc.Load(xmlPath);

            cancellationToken.ThrowIfCancellationRequested();

            string path;
            var hd = ConfigurationManager.Configuration.DownloadHDFanArt ? "hdtv" : "clear";
            if (ConfigurationManager.Configuration.DownloadSeriesImages.Logo && !series.HasImage(ImageType.Logo))
            {
                var node = doc.SelectSingleNode("//fanart/series/" + hd + "logos/" + hd + "logo[@lang = \"" + language + "\"]/@url") ??
                            doc.SelectSingleNode("//fanart/series/clearlogos/clearlogo[@lang = \"" + language + "\"]/@url") ??
                            doc.SelectSingleNode("//fanart/series/" + hd + "logos/" + hd + "logo/@url") ??
                            doc.SelectSingleNode("//fanart/series/clearlogos/clearlogo/@url");
                path = node != null ? node.Value : null;
                if (!string.IsNullOrEmpty(path))
                {
                    series.SetImage(ImageType.Logo, await _providerManager.DownloadAndSaveImage(series, path, LogoFile, ConfigurationManager.Configuration.SaveLocalMeta, FanArtResourcePool, cancellationToken).ConfigureAwait(false));
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            hd = ConfigurationManager.Configuration.DownloadHDFanArt ? "hd" : "";
            if (ConfigurationManager.Configuration.DownloadSeriesImages.Art && !series.HasImage(ImageType.Art))
            {
                var node = doc.SelectSingleNode("//fanart/series/" + hd + "cleararts/" + hd + "clearart[@lang = \"" + language + "\"]/@url") ??
                           doc.SelectSingleNode("//fanart/series/cleararts/clearart[@lang = \"" + language + "\"]/@url") ??
                           doc.SelectSingleNode("//fanart/series/" + hd + "cleararts/" + hd + "clearart/@url") ??
                           doc.SelectSingleNode("//fanart/series/cleararts/clearart/@url");
                path = node != null ? node.Value : null;
                if (!string.IsNullOrEmpty(path))
                {
                    series.SetImage(ImageType.Art, await _providerManager.DownloadAndSaveImage(series, path, ArtFile, ConfigurationManager.Configuration.SaveLocalMeta, FanArtResourcePool, cancellationToken).ConfigureAwait(false));
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (ConfigurationManager.Configuration.DownloadSeriesImages.Thumb && !series.HasImage(ImageType.Thumb))
            {
                var node = doc.SelectSingleNode("//fanart/series/tvthumbs/tvthumb[@lang = \"" + language + "\"]/@url") ??
                           doc.SelectSingleNode("//fanart/series/tvthumbs/tvthumb/@url");
                path = node != null ? node.Value : null;
                if (!string.IsNullOrEmpty(path))
                {
                    series.SetImage(ImageType.Thumb, await _providerManager.DownloadAndSaveImage(series, path, ThumbFile, ConfigurationManager.Configuration.SaveLocalMeta, FanArtResourcePool, cancellationToken).ConfigureAwait(false));
                }
            }

            if (ConfigurationManager.Configuration.DownloadSeriesImages.Banner && !series.HasImage(ImageType.Banner))
            {
                var node = doc.SelectSingleNode("//fanart/series/tbbanners/tvbanner[@lang = \"" + language + "\"]/@url") ??
                           doc.SelectSingleNode("//fanart/series/tbbanners/tvbanner/@url");
                path = node != null ? node.Value : null;
                if (!string.IsNullOrEmpty(path))
                {
                    series.SetImage(ImageType.Banner, await _providerManager.DownloadAndSaveImage(series, path, BannerFile, ConfigurationManager.Configuration.SaveLocalMeta, FanArtResourcePool, cancellationToken).ConfigureAwait(false));
                }
            }

            if (ConfigurationManager.Configuration.DownloadMovieImages.Backdrops && item.BackdropImagePaths.Count < ConfigurationManager.Configuration.MaxBackdrops)
            {
                var nodes = doc.SelectNodes("//fanart/series/showbackgrounds//@url");

                if (nodes != null)
                {
                    var numBackdrops = item.BackdropImagePaths.Count;

                    foreach (XmlNode node in nodes)
                    {
                        path = node.Value;

                        if (!string.IsNullOrEmpty(path))
                        {
                            item.BackdropImagePaths.Add(await _providerManager.DownloadAndSaveImage(item, path, ("backdrop" + (numBackdrops > 0 ? numBackdrops.ToString(UsCulture) : "") + ".jpg"), ConfigurationManager.Configuration.SaveLocalMeta, FanArtResourcePool, cancellationToken).ConfigureAwait(false));

                            numBackdrops++;

                            if (item.BackdropImagePaths.Count >= ConfigurationManager.Configuration.MaxBackdrops) break;
                        }
                    }

                }
            }


            data.Data = GetComparisonData(item.GetProviderId(MetadataProviders.Tvdb));
            SetLastRefreshed(series, DateTime.UtcNow, status);

            return true;
        }
    }
}
