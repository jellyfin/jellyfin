using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Providers.Music
{
    public class MusicBrainzAlbumProvider : IRemoteMetadataProvider<MusicAlbum, AlbumInfo>, IHasOrder
    {
        internal static MusicBrainzAlbumProvider Current;

        private readonly IHttpClient _httpClient;
        private readonly IApplicationHost _appHost;
        private readonly ILogger _logger;

        public MusicBrainzAlbumProvider(IHttpClient httpClient, IApplicationHost appHost, ILogger logger)
        {
            _httpClient = httpClient;
            _appHost = appHost;
            _logger = logger;
            Current = this;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(AlbumInfo searchInfo, CancellationToken cancellationToken)
        {
            var releaseId = searchInfo.GetReleaseId();

            string url = null;
            var isNameSearch = false;

            if (!string.IsNullOrEmpty(releaseId))
            {
                url = string.Format("https://www.musicbrainz.org/ws/2/release/?query=reid:{0}", releaseId);
            }
            else
            {
                var artistMusicBrainzId = searchInfo.GetMusicBrainzArtistId();

                if (!string.IsNullOrWhiteSpace(artistMusicBrainzId))
                {
                    url = string.Format("https://www.musicbrainz.org/ws/2/release/?query=\"{0}\" AND arid:{1}",
                        WebUtility.UrlEncode(searchInfo.Name),
                        artistMusicBrainzId);
                }
                else
                {
                    isNameSearch = true;

                    url = string.Format("https://www.musicbrainz.org/ws/2/release/?query=\"{0}\" AND artist:\"{1}\"",
                       WebUtility.UrlEncode(searchInfo.Name),
                       WebUtility.UrlEncode(searchInfo.GetAlbumArtist()));
                }
            }

            if (!string.IsNullOrWhiteSpace(url))
            {
                var doc = await GetMusicBrainzResponse(url, isNameSearch, cancellationToken).ConfigureAwait(false);

                return GetResultsFromResponse(doc);
            }

            return new List<RemoteSearchResult>();
        }

        private IEnumerable<RemoteSearchResult> GetResultsFromResponse(XmlDocument doc)
        {
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("mb", "https://musicbrainz.org/ns/mmd-2.0#");

            var list = new List<RemoteSearchResult>();

            var nodes = doc.SelectNodes("//mb:release-list/mb:release", ns);

            if (nodes != null)
            {
                foreach (var node in nodes.Cast<XmlNode>())
                {
                    if (node.Attributes != null)
                    {
                        string name = null;

                        string mbzId = node.Attributes["id"].Value;

                        var nameNode = node.SelectSingleNode("//mb:title", ns);

                        if (nameNode != null)
                        {
                            name = nameNode.InnerText;
                        }

                        if (!string.IsNullOrWhiteSpace(mbzId) && !string.IsNullOrWhiteSpace(name))
                        {
                            var result = new RemoteSearchResult
                            {
                                Name = name
                            };

                            result.SetProviderId(MetadataProviders.MusicBrainzAlbum, mbzId);

                            list.Add(result);
                        }
                    }
                }
            }

            return list;
        }

        public async Task<MetadataResult<MusicAlbum>> GetMetadata(AlbumInfo id, CancellationToken cancellationToken)
        {
            var releaseId = id.GetReleaseId();
            var releaseGroupId = id.GetReleaseGroupId();

            var result = new MetadataResult<MusicAlbum>
            {
                Item = new MusicAlbum()
            };

            if (string.IsNullOrEmpty(releaseId))
            {
                var artistMusicBrainzId = id.GetMusicBrainzArtistId();

                var releaseResult = await GetReleaseResult(artistMusicBrainzId, id.GetAlbumArtist(), id.Name, cancellationToken).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(releaseResult.ReleaseId))
                {
                    releaseId = releaseResult.ReleaseId;
                    result.HasMetadata = true;
                }

                if (!string.IsNullOrEmpty(releaseResult.ReleaseGroupId))
                {
                    releaseGroupId = releaseResult.ReleaseGroupId;
                    result.HasMetadata = true;
                }
            }

            // If we have a release Id but not a release group Id...
            if (!string.IsNullOrEmpty(releaseId) && string.IsNullOrEmpty(releaseGroupId))
            {
                releaseGroupId = await GetReleaseGroupId(releaseId, cancellationToken).ConfigureAwait(false);
                result.HasMetadata = true;
            }

            if (!string.IsNullOrEmpty(releaseId) || !string.IsNullOrEmpty(releaseGroupId))
            {
                result.HasMetadata = true;
            }

            if (result.HasMetadata)
            {
                if (!string.IsNullOrEmpty(releaseId))
                {
                    result.Item.SetProviderId(MetadataProviders.MusicBrainzAlbum, releaseId);
                }

                if (!string.IsNullOrEmpty(releaseGroupId))
                {
                    result.Item.SetProviderId(MetadataProviders.MusicBrainzReleaseGroup, releaseGroupId);
                }
            }

            return result;
        }

        public string Name
        {
            get { return "MusicBrainz"; }
        }

        private Task<ReleaseResult> GetReleaseResult(string artistMusicBrainId, string artistName, string albumName, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(artistMusicBrainId))
            {
                return GetReleaseResult(albumName, artistMusicBrainId, cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(artistName))
            {
                return Task.FromResult(new ReleaseResult());
            }

            return GetReleaseResultByArtistName(albumName, artistName, cancellationToken);
        }

        private async Task<ReleaseResult> GetReleaseResult(string albumName, string artistId, CancellationToken cancellationToken)
        {
            var url = string.Format("https://www.musicbrainz.org/ws/2/release/?query=\"{0}\" AND arid:{1}",
                WebUtility.UrlEncode(albumName),
                artistId);

            var doc = await GetMusicBrainzResponse(url, true, cancellationToken).ConfigureAwait(false);

            return GetReleaseResult(doc);
        }

        private async Task<ReleaseResult> GetReleaseResultByArtistName(string albumName, string artistName, CancellationToken cancellationToken)
        {
            var url = string.Format("https://www.musicbrainz.org/ws/2/release/?query=\"{0}\" AND artist:\"{1}\"",
                WebUtility.UrlEncode(albumName),
                WebUtility.UrlEncode(artistName));

            var doc = await GetMusicBrainzResponse(url, true, cancellationToken).ConfigureAwait(false);

            return GetReleaseResult(doc);
        }

        private ReleaseResult GetReleaseResult(XmlDocument doc)
        {
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("mb", "https://musicbrainz.org/ns/mmd-2.0#");

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
            var url = string.Format("https://www.musicbrainz.org/ws/2/release-group/?query=reid:{0}", releaseEntryId);

            var doc = await GetMusicBrainzResponse(url, false, cancellationToken).ConfigureAwait(false);

            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("mb", "https://musicbrainz.org/ns/mmd-2.0#");
            var node = doc.SelectSingleNode("//mb:release-group-list/mb:release-group/@id", ns);

            return node != null ? node.Value : null;
        }

        /// <summary>
        /// The _music brainz resource pool
        /// </summary>
        private readonly SemaphoreSlim _musicBrainzResourcePool = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Gets the music brainz response.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="isSearch">if set to <c>true</c> [is search].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{XmlDocument}.</returns>
        internal async Task<XmlDocument> GetMusicBrainzResponse(string url, bool isSearch, CancellationToken cancellationToken)
        {
            // MusicBrainz is extremely adamant about limiting to one request per second

            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

            var doc = new XmlDocument();

            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                UserAgent = _appHost.Name + "/" + _appHost.ApplicationVersion,
                ResourcePool = _musicBrainzResourcePool
            };

            using (var xml = await _httpClient.Get(options).ConfigureAwait(false))
            {
                using (var oReader = new StreamReader(xml, Encoding.UTF8))
                {
                    doc.Load(oReader);
                }
            }

            return doc;
        }

        public int Order
        {
            get { return 0; }
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
