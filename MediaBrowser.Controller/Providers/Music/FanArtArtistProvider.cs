using System.Collections.Generic;
using System.Collections.Specialized;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Controller.Providers.Music
{
    /// <summary>
    /// Class FanArtArtistProvider
    /// </summary>
    class FanArtArtistProvider : FanartBaseProvider
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

        /// <summary>
        /// Shoulds the fetch.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool ShouldFetch(BaseItem item, BaseProviderInfo providerInfo)
        {
            var artist = (MusicArtist)item;
            if (item.Path == null || item.DontFetchMeta || string.IsNullOrEmpty(artist.GetProviderId(MetadataProviders.Musicbrainz))) return false; //nothing to do
            var artExists = item.ResolveArgs.ContainsMetaFileByName(ART_FILE);
            var logoExists = item.ResolveArgs.ContainsMetaFileByName(LOGO_FILE);
            var discExists = item.ResolveArgs.ContainsMetaFileByName(DISC_FILE);

            return (!artExists && ConfigurationManager.Configuration.DownloadMusicArtistImages.Art)
                || (!logoExists && ConfigurationManager.Configuration.DownloadMusicArtistImages.Logo)
                || (!discExists && ConfigurationManager.Configuration.DownloadMusicArtistImages.Disc)
                || ((artist.AlbumCovers == null || artist.AlbumCovers.Count == 0) && ConfigurationManager.Configuration.DownloadMusicAlbumImages.Primary);
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

            var artist = (MusicArtist)item;
            if (ShouldFetch(artist, artist.ProviderData.GetValueOrDefault(Id, new BaseProviderInfo { ProviderId = Id })))
            {
                var url = string.Format(FanArtBaseUrl, APIKey, artist.GetProviderId(MetadataProviders.Musicbrainz));
                var doc = new XmlDocument();

                try
                {
                    using (var xml = await HttpClient.Get(url, FanArtResourcePool, cancellationToken).ConfigureAwait(false))
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
                            Logger.Debug("FanArtProvider getting ClearLogo for " + artist.Name);
                            try
                            {
                                artist.SetImage(ImageType.Logo, await _providerManager.DownloadAndSaveImage(artist, path, LOGO_FILE, FanArtResourcePool, cancellationToken).ConfigureAwait(false));
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
                            artist.BackdropImagePaths = new List<string>();
                            foreach (XmlNode node in nodes)
                            {
                                path = node.Value;
                                if (!string.IsNullOrEmpty(path))
                                {
                                    Logger.Debug("FanArtProvider getting Backdrop for " + artist.Name);
                                    try
                                    {
                                        artist.BackdropImagePaths.Add(await _providerManager.DownloadAndSaveImage(artist, path, ("Backdrop" + (numBackdrops > 0 ? numBackdrops.ToString() : "") + ".jpg"), FanArtResourcePool, cancellationToken).ConfigureAwait(false));
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

                    if (ConfigurationManager.Configuration.DownloadMusicAlbumImages.Primary)
                    {
                        var nodes = doc.SelectNodes("//fanart/music/albums/*");
                        if (nodes != null)
                        {
                            artist.AlbumCovers = new Dictionary<string, string>();
                            foreach (XmlNode node in nodes)
                            {

                                var key = node.Attributes["id"] != null ? node.Attributes["id"].Value : null;
                                var cover = node.SelectSingleNode("albumcover/@url");
                                path = cover != null ? cover.Value : null;

                                if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(key))
                                {
                                    Logger.Debug("FanArtProvider getting Album Cover for " + artist.Name);
                                    artist.AlbumCovers[key] = path;
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
                            Logger.Debug("FanArtProvider getting ClearArt for " + artist.Name);
                            try
                            {
                                artist.SetImage(ImageType.Art, await _providerManager.DownloadAndSaveImage(artist, path, ART_FILE, FanArtResourcePool, cancellationToken).ConfigureAwait(false));
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
                        var node = doc.SelectSingleNode("//fanart/music/musicbanners/"+hd+"musicbanner/@url") ??
                                   doc.SelectSingleNode("//fanart/music/musicbanners/musicbanner/@url");
                        path = node != null ? node.Value : null;
                        if (!string.IsNullOrEmpty(path))
                        {
                            Logger.Debug("FanArtProvider getting Banner for " + artist.Name);
                            try
                            {
                                artist.SetImage(ImageType.Banner, await _providerManager.DownloadAndSaveImage(artist, path, BANNER_FILE, FanArtResourcePool, cancellationToken).ConfigureAwait(false));
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
                            Logger.Debug("FanArtProvider getting Primary image for " + artist.Name);
                            try
                            {
                                artist.SetImage(ImageType.Primary, await _providerManager.DownloadAndSaveImage(artist, path, PRIMARY_FILE, FanArtResourcePool, cancellationToken).ConfigureAwait(false));
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
            SetLastRefreshed(artist, DateTime.UtcNow);
            return true;
        }
    }
}
