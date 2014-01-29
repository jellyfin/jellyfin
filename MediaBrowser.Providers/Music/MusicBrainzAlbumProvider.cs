using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Providers.Music
{
    public class MusicBrainzAlbumProvider : BaseMetadataProvider
    {
        internal static MusicBrainzAlbumProvider Current;

        private readonly IHttpClient _httpClient;
        private readonly IApplicationHost _appHost;

        public MusicBrainzAlbumProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IHttpClient httpClient, IApplicationHost appHost)
            : base(logManager, configurationManager)
        {
            _httpClient = httpClient;
            _appHost = appHost;

            Current = this;
        }

        public override bool Supports(BaseItem item)
        {
            return item is MusicAlbum;
        }

        public override async Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken)
        {
            var releaseId = item.GetProviderId(MetadataProviders.Musicbrainz);
            var releaseGroupId = item.GetProviderId(MetadataProviders.MusicBrainzReleaseGroup);

            if (string.IsNullOrEmpty(releaseId))
            {
                var result = await GetReleaseResult((MusicAlbum)item, cancellationToken).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(result.ReleaseId))
                {
                    releaseId = result.ReleaseId;
                    item.SetProviderId(MetadataProviders.Musicbrainz, releaseId);
                }

                if (!string.IsNullOrEmpty(result.ReleaseGroupId))
                {
                    releaseGroupId = result.ReleaseGroupId;
                    item.SetProviderId(MetadataProviders.MusicBrainzReleaseGroup, releaseGroupId);
                }
            }

            // If we have a release Id but not a release group Id...
            if (!string.IsNullOrEmpty(releaseId) && string.IsNullOrEmpty(releaseGroupId))
            {
                releaseGroupId = await GetReleaseGroupId(releaseId, cancellationToken).ConfigureAwait(false);

                item.SetProviderId(MetadataProviders.MusicBrainzReleaseGroup, releaseGroupId);
            }

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
            return true;
        }

        private Task<ReleaseResult> GetReleaseResult(MusicAlbum album, CancellationToken cancellationToken)
        {
            var artist = album.Parent;
            var artistId = artist.GetProviderId(MetadataProviders.Musicbrainz);

            if (!string.IsNullOrEmpty(artistId))
            {
                return GetReleaseResult(album.Name, artistId, cancellationToken);
            }

            return GetReleaseResultByArtistName(album.Name, artist.Name, cancellationToken);
        }

        private async Task<ReleaseResult> GetReleaseResult(string albumName, string artistId, CancellationToken cancellationToken)
        {
            var url = string.Format("http://www.musicbrainz.org/ws/2/release/?query=\"{0}\" and arid:{1}",
                WebUtility.UrlEncode(albumName),
                artistId);

            var doc = await GetMusicBrainzResponse(url, cancellationToken).ConfigureAwait(false);

            return GetReleaseResult(doc);
        }

        private async Task<ReleaseResult> GetReleaseResultByArtistName(string albumName, string artistName, CancellationToken cancellationToken)
        {
            var url = string.Format("http://www.musicbrainz.org/ws/2/release/?query=\"{0}\" and artist:\"{1}\"",
                WebUtility.UrlEncode(albumName),
                WebUtility.UrlEncode(artistName));

            var doc = await GetMusicBrainzResponse(url, cancellationToken).ConfigureAwait(false);

            return GetReleaseResult(doc);
        }

        private ReleaseResult GetReleaseResult(XmlDocument doc)
        {
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("mb", "http://musicbrainz.org/ns/mmd-2.0#");

            var result = new ReleaseResult
            {

            };

            var releaseIdNode = doc.SelectSingleNode("//mb:release-list/mb:release/@id", ns);

            if (releaseIdNode != null)
            {
                result.ReleaseId = releaseIdNode.Value;
            }

            var releaseGroupIdNode = doc.SelectSingleNode("//mb:release-list/mb:release/mb:release-group/@id", ns);

            if (releaseGroupIdNode != null)
            {
                result.ReleaseGroupId = releaseGroupIdNode.Value;
            }
            
            return result;
        }

        private class ReleaseResult
        {
            public string ReleaseId;
            public string ReleaseGroupId;
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

        /// <summary>
        /// The _last music brainz request
        /// </summary>
        private DateTime _lastRequestDate = DateTime.MinValue;
        /// <summary>
        /// The _music brainz resource pool
        /// </summary>
        private readonly SemaphoreSlim _musicBrainzResourcePool = new SemaphoreSlim(1, 1);

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

                var userAgent = _appHost.Name + "/" + _appHost.ApplicationVersion;

                using (var xml = await _httpClient.Get(new HttpRequestOptions
                {
                    Url = url,
                    CancellationToken = cancellationToken,
                    UserAgent = userAgent

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


        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Third; }
        }
    }
}
