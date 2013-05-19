using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Controller.Providers.Music
{
    /// <summary>
    /// Class FanArtAlbumProvider
    /// </summary>
    public class FanArtAlbumProvider : FanartBaseProvider
    {
        /// <summary>
        /// The _provider manager
        /// </summary>
        private readonly IProviderManager _providerManager;

        /// <summary>
        /// The _music brainz resource pool
        /// </summary>
        private readonly SemaphoreSlim _musicBrainzResourcePool = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        protected IHttpClient HttpClient { get; private set; }

        internal static FanArtAlbumProvider Current { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FanArtAlbumProvider"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        public FanArtAlbumProvider(IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager)
            : base(logManager, configurationManager)
        {
            _providerManager = providerManager;
            HttpClient = httpClient;

            Current = this;
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is MusicAlbum;
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
                return "12";
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

            if (!ConfigurationManager.Configuration.DownloadMusicAlbumImages.Disc &&
                !ConfigurationManager.Configuration.DownloadMusicAlbumImages.Primary)
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

            var album = (MusicAlbum)item;

            if (string.IsNullOrEmpty(album.MusicBrainzReleaseGroupId))
            {
                album.MusicBrainzReleaseGroupId = await GetReleaseGroupId(item.GetProviderId(MetadataProviders.Musicbrainz), cancellationToken).ConfigureAwait(false);
            }

            // If still empty there's nothing more we can do
            if (string.IsNullOrEmpty(album.MusicBrainzReleaseGroupId))
            {
                SetLastRefreshed(item, DateTime.UtcNow);

                return true;
            }

            var url = string.Format("http://api.fanart.tv/webservice/album/{0}/{1}/xml/all/1/1", ApiKey, album.MusicBrainzReleaseGroupId);

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
                if (ConfigurationManager.Configuration.DownloadMusicAlbumImages.Disc && !item.ResolveArgs.ContainsMetaFileByName(DiscFile))
                {
                    var node = doc.SelectSingleNode("//fanart/music/albums/album/cdart/@url");

                    var path = node != null ? node.Value : null;

                    if (!string.IsNullOrEmpty(path))
                    {
                        Logger.Debug("FanArtProvider getting Disc for " + item.Name);
                        item.SetImage(ImageType.Disc, await _providerManager.DownloadAndSaveImage(item, path, DiscFile, ConfigurationManager.Configuration.SaveLocalMeta, FanArtResourcePool, cancellationToken).ConfigureAwait(false));
                    }
                }

                if (ConfigurationManager.Configuration.DownloadMusicAlbumImages.Primary && !item.ResolveArgs.ContainsMetaFileByName(PrimaryFile))
                {
                    var node = doc.SelectSingleNode("//fanart/music/albums/album/albumcover/@url");

                    var path = node != null ? node.Value : null;

                    if (!string.IsNullOrEmpty(path))
                    {
                        Logger.Debug("FanArtProvider getting albumcover for " + item.Name);
                        item.SetImage(ImageType.Primary, await _providerManager.DownloadAndSaveImage(item, path, PrimaryFile, ConfigurationManager.Configuration.SaveLocalMeta, FanArtResourcePool, cancellationToken).ConfigureAwait(false));
                    }
                }
            }

            SetLastRefreshed(item, DateTime.UtcNow);

            return true;
        }

        /// <summary>
        /// The _last music brainz request
        /// </summary>
        private DateTime _lastRequestDate = DateTime.MinValue;

        /// <summary>
        /// Gets the music brainz response.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{XmlDocument}.</returns>
        internal async Task<XmlDocument> GetMusicBrainzResponse(string url, CancellationToken cancellationToken)
        {
            await _musicBrainzResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var diff = 1500 - (DateTime.Now - _lastRequestDate).TotalMilliseconds;

                // MusicBrainz is extremely adamant about limiting to one request per second

                if (diff > 0)
                {
                    await Task.Delay(Convert.ToInt32(diff), cancellationToken).ConfigureAwait(false);
                }

                _lastRequestDate = DateTime.Now;

                var doc = new XmlDocument();

                using (var xml = await HttpClient.Get(new HttpRequestOptions
                {
                    Url = url,
                    CancellationToken = cancellationToken,
                    UserAgent = "MediaBrowserServer/www.mediabrowser3.com",
                    EnableResponseCache = true

                }).ConfigureAwait(false))
                {
                    using (var oReader = new StreamReader(xml, Encoding.UTF8))
                    {
                        doc.Load(oReader);
                    }
                }

                return doc;
            }
            finally
            {
                _lastRequestDate = DateTime.Now;

                _musicBrainzResourcePool.Release();
            }
        }

        /// <summary>
        /// Gets the release group id internal.
        /// </summary>
        /// <param name="releaseEntryId">The release entry id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        private async Task<string> GetReleaseGroupId(string releaseEntryId, CancellationToken cancellationToken)
        {
            var url = string.Format("http://www.musicbrainz.org/ws/2/release-group/?query=reid:{0}", releaseEntryId);

            var doc = await GetMusicBrainzResponse(url, cancellationToken).ConfigureAwait(false);

            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("mb", "http://musicbrainz.org/ns/mmd-2.0#");
            var node = doc.SelectSingleNode("//mb:release-group-list/mb:release-group/@id", ns);

            return node != null ? node.Value : null;
        }
    }
}
