using System.Net;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Model.Net;

namespace MediaBrowser.Providers.Music
{
    /// <summary>
    /// Class FanArtArtistProvider
    /// </summary>
    public class FanArtArtistProvider : FanartBaseProvider
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

        internal static FanArtArtistProvider Current;

        /// <summary>
        /// Initializes a new instance of the <see cref="FanArtArtistProvider"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        /// <exception cref="System.ArgumentNullException">httpClient</exception>
        public FanArtArtistProvider(IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager)
            : base(logManager, configurationManager)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }
            HttpClient = httpClient;
            _providerManager = providerManager;

            Current = this;
        }

        /// <summary>
        /// The fan art base URL
        /// </summary>
        protected string FanArtBaseUrl = "http://api.fanart.tv/webservice/artist/{0}/{1}/xml/all/1/1";

        public override ItemUpdateType ItemUpdateType
        {
            get
            {
                return ItemUpdateType.ImageUpdate;
            }
        }
        
        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is MusicArtist;
        }

        /// <summary>
        /// Gets a value indicating whether [save local meta].
        /// </summary>
        /// <value><c>true</c> if [save local meta]; otherwise, <c>false</c>.</value>
        protected virtual bool SaveLocalMeta
        {
            get { return ConfigurationManager.Configuration.SaveLocalMeta; }
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
                return "7";
            }
        }

        public override MetadataProviderPriority Priority
        {
            get
            {
                return MetadataProviderPriority.Fourth;
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
            if (string.IsNullOrEmpty(item.GetProviderId(MetadataProviders.Musicbrainz)))
            {
                return false;
            }

            if (!ConfigurationManager.Configuration.DownloadMusicArtistImages.Art &&
              !ConfigurationManager.Configuration.DownloadMusicArtistImages.Backdrops &&
              !ConfigurationManager.Configuration.DownloadMusicArtistImages.Banner &&
              !ConfigurationManager.Configuration.DownloadMusicArtistImages.Logo &&
              !ConfigurationManager.Configuration.DownloadMusicArtistImages.Primary &&

                // The fanart album provider depends on xml downloaded here, so honor it's settings too
                !ConfigurationManager.Configuration.DownloadMusicAlbumImages.Disc &&
                !ConfigurationManager.Configuration.DownloadMusicAlbumImages.Primary)
            {
                return false;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }

        protected override DateTime CompareDate(BaseItem item)
        {
            var musicBrainzId = item.GetProviderId(MetadataProviders.Musicbrainz);

            if (!string.IsNullOrEmpty(musicBrainzId))
            {
                // Process images
                var path = GetArtistDataPath(ConfigurationManager.ApplicationPaths, musicBrainzId);

                try
                {
                    var files = new DirectoryInfo(path)
                        .EnumerateFiles("*.xml", SearchOption.TopDirectoryOnly)
                        .Select(i => i.LastWriteTimeUtc)
                        .ToList();

                    if (files.Count > 0)
                    {
                        return files.Max();
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    
                }
            }

            return base.CompareDate(item);
        }

        /// <summary>
        /// The us culture
        /// </summary>
        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Gets the series data path.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="musicBrainzArtistId">The music brainz artist id.</param>
        /// <returns>System.String.</returns>
        internal static string GetArtistDataPath(IApplicationPaths appPaths, string musicBrainzArtistId)
        {
            var seriesDataPath = Path.Combine(GetArtistDataPath(appPaths), musicBrainzArtistId);

            return seriesDataPath;
        }

        /// <summary>
        /// Gets the series data path.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <returns>System.String.</returns>
        internal static string GetArtistDataPath(IApplicationPaths appPaths)
        {
            var dataPath = Path.Combine(appPaths.DataPath, "fanart-music");

            return dataPath;
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

            var musicBrainzId = item.GetProviderId(MetadataProviders.Musicbrainz);

            var artistDataPath = GetArtistDataPath(ConfigurationManager.ApplicationPaths, musicBrainzId);
            var xmlPath = Path.Combine(artistDataPath, "fanart.xml");

            // Only download the xml if it doesn't already exist. The prescan task will take care of getting updates
            if (!File.Exists(xmlPath))
            {
                await DownloadArtistXml(artistDataPath, musicBrainzId, cancellationToken).ConfigureAwait(false);
            }

            if (ConfigurationManager.Configuration.DownloadMusicArtistImages.Art ||
              ConfigurationManager.Configuration.DownloadMusicArtistImages.Backdrops ||
              ConfigurationManager.Configuration.DownloadMusicArtistImages.Banner ||
              ConfigurationManager.Configuration.DownloadMusicArtistImages.Logo ||
              ConfigurationManager.Configuration.DownloadMusicArtistImages.Primary)
            {
                if (File.Exists(xmlPath))
                {
                    await FetchFromXml(item, xmlPath, cancellationToken).ConfigureAwait(false);
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

        /// <summary>
        /// Downloads the artist XML.
        /// </summary>
        /// <param name="artistPath">The artist path.</param>
        /// <param name="musicBrainzId">The music brainz id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        internal async Task DownloadArtistXml(string artistPath, string musicBrainzId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = string.Format(FanArtBaseUrl, ApiKey, musicBrainzId);

            var xmlPath = Path.Combine(artistPath, "fanart.xml");

            Directory.CreateDirectory(artistPath);
            
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
        }

        /// <summary>
        /// Fetches from XML.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="xmlFilePath">The XML file path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task FetchFromXml(BaseItem item, string xmlFilePath, CancellationToken cancellationToken)
        {
            var doc = new XmlDocument();
            doc.Load(xmlFilePath);

            cancellationToken.ThrowIfCancellationRequested();

            string path;

            if (ConfigurationManager.Configuration.DownloadMusicArtistImages.Logo && !item.HasImage(ImageType.Logo))
            {
                var node =
                    doc.SelectSingleNode("//fanart/music/musiclogos/hdmusiclogo/@url") ??
                    doc.SelectSingleNode("//fanart/music/musiclogos/musiclogo/@url");
                path = node != null ? node.Value : null;
                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        await _providerManager.SaveImage(item, path, FanArtResourcePool, ImageType.Logo, null, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (HttpException ex)
                    {
                        // Sometimes fanart has bad url's in their xml. Nothing we can do here but catch it
                        if (!ex.StatusCode.HasValue || ex.StatusCode.Value != HttpStatusCode.NotFound)
                        {
                            throw;
                        }
                    }
                }
            }
            cancellationToken.ThrowIfCancellationRequested();

            if (ConfigurationManager.Configuration.DownloadMusicArtistImages.Backdrops && item.BackdropImagePaths.Count == 0)
            {
                var nodes = doc.SelectNodes("//fanart/music/artistbackgrounds//@url");
                if (nodes != null)
                {
                    var numBackdrops = 0;
                    item.BackdropImagePaths = new List<string>();
                    foreach (XmlNode node in nodes)
                    {
                        path = node.Value;
                        if (!string.IsNullOrEmpty(path))
                        {
                            try
                            {
                                await _providerManager.SaveImage(item, path, FanArtResourcePool, ImageType.Backdrop, numBackdrops, cancellationToken)
                                    .ConfigureAwait(false);
                                numBackdrops++;
                                if (numBackdrops >= ConfigurationManager.Configuration.MaxBackdrops) break;
                            }
                            catch (HttpException ex)
                            {
                                // Sometimes fanart has bad url's in their xml. Nothing we can do here but catch it
                                if (!ex.StatusCode.HasValue || ex.StatusCode.Value != HttpStatusCode.NotFound)
                                {
                                    throw;
                                }
                            }
                        }
                    }

                }

            }

            cancellationToken.ThrowIfCancellationRequested();

            if (ConfigurationManager.Configuration.DownloadMusicArtistImages.Art && !item.HasImage(ImageType.Art))
            {
                var node =
                    doc.SelectSingleNode("//fanart/music/hdmusicarts/hdmusicart/@url") ??
                    doc.SelectSingleNode("//fanart/music/musicarts/musicart/@url");
                path = node != null ? node.Value : null;
                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        await _providerManager.SaveImage(item, path, FanArtResourcePool, ImageType.Art, null, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (HttpException ex)
                    {
                        // Sometimes fanart has bad url's in their xml. Nothing we can do here but catch it
                        if (!ex.StatusCode.HasValue || ex.StatusCode.Value != HttpStatusCode.NotFound)
                        {
                            throw;
                        }
                    }
                }
            }
            cancellationToken.ThrowIfCancellationRequested();

            if (ConfigurationManager.Configuration.DownloadMusicArtistImages.Banner && !item.HasImage(ImageType.Banner))
            {
                var node = doc.SelectSingleNode("//fanart/music/hdmusicbanners/hdmusicbanner/@url") ??
                           doc.SelectSingleNode("//fanart/music/musicbanners/musicbanner/@url");
                path = node != null ? node.Value : null;
                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        await _providerManager.SaveImage(item, path, FanArtResourcePool, ImageType.Banner, null, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (HttpException ex)
                    {
                        // Sometimes fanart has bad url's in their xml. Nothing we can do here but catch it
                        if (!ex.StatusCode.HasValue || ex.StatusCode.Value != HttpStatusCode.NotFound)
                        {
                            throw;
                        }
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Artist thumbs are actually primary images (they are square/portrait)
            if (ConfigurationManager.Configuration.DownloadMusicArtistImages.Primary && !item.HasImage(ImageType.Primary))
            {
                var node = doc.SelectSingleNode("//fanart/music/artistthumbs/artistthumb/@url");
                path = node != null ? node.Value : null;
                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        await _providerManager.SaveImage(item, path, FanArtResourcePool, ImageType.Primary, null, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (HttpException ex)
                    {
                        // Sometimes fanart has bad url's in their xml. Nothing we can do here but catch it
                        if (!ex.StatusCode.HasValue || ex.StatusCode.Value != HttpStatusCode.NotFound)
                        {
                            throw;
                        }
                    }
                }
            }
        }
    }
}
