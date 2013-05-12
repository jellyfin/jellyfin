using System.Globalization;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Controller.Providers.Music
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

        private readonly IProviderManager _providerManager;

        public FanArtArtistProvider(IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager)
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
        /// The fan art base URL
        /// </summary>
        protected string FanArtBaseUrl = "http://api.fanart.tv/webservice/artist/{0}/{1}/xml/all/1/1";

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is MusicArtist;
        }

        protected virtual bool SaveLocalMeta
        {
            get { return ConfigurationManager.Configuration.SaveLocalMeta; }
        }

        protected override bool RefreshOnVersionChange
        {
            get
            {
                return true;
            }
        }

        protected override string ProviderVersion
        {
            get
            {
                return "4";
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
                !ConfigurationManager.Configuration.DownloadMusicArtistImages.Primary)
            {
                return false;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }

        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");
        
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

            //var artist = item;

            var url = string.Format(FanArtBaseUrl, APIKey, item.GetProviderId(MetadataProviders.Musicbrainz));
            var doc = new XmlDocument();

            try
            {
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
            }
            catch (HttpException)
            {
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (doc.HasChildNodes)
            {
                string path;
                var hd = ConfigurationManager.Configuration.DownloadHDFanArt ? "hd" : "";
                if (ConfigurationManager.Configuration.DownloadMusicArtistImages.Logo && !item.ResolveArgs.ContainsMetaFileByName(LOGO_FILE))
                {
                    var node =
                        doc.SelectSingleNode("//fanart/music/musiclogos/" + hd + "musiclogo/@url") ??
                        doc.SelectSingleNode("//fanart/music/musiclogos/musiclogo/@url");
                    path = node != null ? node.Value : null;
                    if (!string.IsNullOrEmpty(path))
                    {
                        Logger.Debug("FanArtProvider getting ClearLogo for " + item.Name);
                        try
                        {
                            item.SetImage(ImageType.Logo, await _providerManager.DownloadAndSaveImage(item, path, LOGO_FILE, SaveLocalMeta, FanArtResourcePool, cancellationToken).ConfigureAwait(false));
                        }
                        catch (HttpException)
                        {
                        }
                        catch (IOException)
                        {

                        }
                    }
                }
                cancellationToken.ThrowIfCancellationRequested();

                if (ConfigurationManager.Configuration.DownloadMusicArtistImages.Backdrops && !item.ResolveArgs.ContainsMetaFileByName(BACKDROP_FILE))
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
                                Logger.Debug("FanArtProvider getting Backdrop for " + item.Name);
                                try
                                {
                                    item.BackdropImagePaths.Add(await _providerManager.DownloadAndSaveImage(item, path, ("Backdrop" + (numBackdrops > 0 ? numBackdrops.ToString(UsCulture) : "") + ".jpg"), SaveLocalMeta, FanArtResourcePool, cancellationToken).ConfigureAwait(false));
                                    numBackdrops++;
                                    if (numBackdrops >= ConfigurationManager.Configuration.MaxBackdrops) break;
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

                cancellationToken.ThrowIfCancellationRequested();

                if (ConfigurationManager.Configuration.DownloadMusicArtistImages.Art && !item.ResolveArgs.ContainsMetaFileByName(ART_FILE))
                {
                    var node =
                        doc.SelectSingleNode("//fanart/music/musicarts/" + hd + "musicart/@url") ??
                        doc.SelectSingleNode("//fanart/music/musicarts/musicart/@url");
                    path = node != null ? node.Value : null;
                    if (!string.IsNullOrEmpty(path))
                    {
                        Logger.Debug("FanArtProvider getting ClearArt for " + item.Name);
                        try
                        {
                            item.SetImage(ImageType.Art, await _providerManager.DownloadAndSaveImage(item, path, ART_FILE, SaveLocalMeta, FanArtResourcePool, cancellationToken).ConfigureAwait(false));
                        }
                        catch (HttpException)
                        {
                        }
                        catch (IOException)
                        {

                        }
                    }
                }
                cancellationToken.ThrowIfCancellationRequested();

                if (ConfigurationManager.Configuration.DownloadMusicArtistImages.Banner && !item.ResolveArgs.ContainsMetaFileByName(BANNER_FILE))
                {
                    var node = doc.SelectSingleNode("//fanart/music/musicbanners/" + hd + "musicbanner/@url") ??
                               doc.SelectSingleNode("//fanart/music/musicbanners/musicbanner/@url");
                    path = node != null ? node.Value : null;
                    if (!string.IsNullOrEmpty(path))
                    {
                        Logger.Debug("FanArtProvider getting Banner for " + item.Name);
                        try
                        {
                            item.SetImage(ImageType.Banner, await _providerManager.DownloadAndSaveImage(item, path, BANNER_FILE, SaveLocalMeta, FanArtResourcePool, cancellationToken).ConfigureAwait(false));
                        }
                        catch (HttpException)
                        {
                        }
                        catch (IOException)
                        {

                        }
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Artist thumbs are actually primary images (they are square/portrait)
                if (ConfigurationManager.Configuration.DownloadMusicArtistImages.Primary && !item.ResolveArgs.ContainsMetaFileByName(PRIMARY_FILE))
                {
                    var node = doc.SelectSingleNode("//fanart/music/artistthumbs/artistthumb/@url");
                    path = node != null ? node.Value : null;
                    if (!string.IsNullOrEmpty(path))
                    {
                        Logger.Debug("FanArtProvider getting Primary image for " + item.Name);
                        try
                        {
                            item.SetImage(ImageType.Primary, await _providerManager.DownloadAndSaveImage(item, path, PRIMARY_FILE, SaveLocalMeta, FanArtResourcePool, cancellationToken).ConfigureAwait(false));
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

            SetLastRefreshed(item, DateTime.UtcNow);
            return true;
        }
    }
}
