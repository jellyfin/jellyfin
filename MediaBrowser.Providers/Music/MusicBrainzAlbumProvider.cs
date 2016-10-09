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
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Providers.Music
{
    public class MusicBrainzAlbumProvider : IRemoteMetadataProvider<MusicAlbum, AlbumInfo>, IHasOrder
    {
        internal static MusicBrainzAlbumProvider Current;

        private readonly IHttpClient _httpClient;
        private readonly IApplicationHost _appHost;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _json;

        public static string MusicBrainzBaseUrl = "https://www.musicbrainz.org";

        public MusicBrainzAlbumProvider(IHttpClient httpClient, IApplicationHost appHost, ILogger logger, IJsonSerializer json)
        {
            _httpClient = httpClient;
            _appHost = appHost;
            _logger = logger;
            _json = json;
            Current = this;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(AlbumInfo searchInfo, CancellationToken cancellationToken)
        {
            var releaseId = searchInfo.GetReleaseId();

            string url = null;
            var isNameSearch = false;

            if (!string.IsNullOrEmpty(releaseId))
            {
                url = string.Format("/ws/2/release/?query=reid:{0}", releaseId);
            }
            else
            {
                var artistMusicBrainzId = searchInfo.GetMusicBrainzArtistId();

                if (!string.IsNullOrWhiteSpace(artistMusicBrainzId))
                {
                    url = string.Format("/ws/2/release/?query=\"{0}\" AND arid:{1}",
                        WebUtility.UrlEncode(searchInfo.Name),
                        artistMusicBrainzId);
                }
                else
                {
                    isNameSearch = true;

                    url = string.Format("/ws/2/release/?query=\"{0}\" AND artist:\"{1}\"",
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
            return ReleaseResult.Parse(doc).Select(i =>
            {
                var result = new RemoteSearchResult
                {
                    Name = i.Title,
                    ProductionYear = i.Year
                };

                if (!string.IsNullOrWhiteSpace(i.ReleaseId))
                {
                    result.SetProviderId(MetadataProviders.MusicBrainzAlbum, i.ReleaseId);
                }
                if (!string.IsNullOrWhiteSpace(i.ReleaseGroupId))
                {
                    result.SetProviderId(MetadataProviders.MusicBrainzReleaseGroup, i.ReleaseGroupId);
                }

                return result;
            });
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

                if (releaseResult != null)
                {
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

                    result.Item.ProductionYear = releaseResult.Year;
                    result.Item.Overview = releaseResult.Overview;
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
            var url = string.Format("/ws/2/release/?query=\"{0}\" AND arid:{1}",
                WebUtility.UrlEncode(albumName),
                artistId);

            var doc = await GetMusicBrainzResponse(url, true, cancellationToken).ConfigureAwait(false);

            return ReleaseResult.Parse(doc, 1).FirstOrDefault();
        }

        private async Task<ReleaseResult> GetReleaseResultByArtistName(string albumName, string artistName, CancellationToken cancellationToken)
        {
            var url = string.Format("/ws/2/release/?query=\"{0}\" AND artist:\"{1}\"",
                WebUtility.UrlEncode(albumName),
                WebUtility.UrlEncode(artistName));

            var doc = await GetMusicBrainzResponse(url, true, cancellationToken).ConfigureAwait(false);

            return ReleaseResult.Parse(doc, 1).FirstOrDefault();
        }

        private class ReleaseResult
        {
            public string ReleaseId;
            public string ReleaseGroupId;
            public string Title;
            public string Overview;
            public int? Year;

            public static List<ReleaseResult> Parse(XmlDocument doc, int? limit = null)
            {
                var docElem = doc.DocumentElement;
                var list = new List<ReleaseResult>();

                if (docElem == null)
                {
                    return list;
                }

                var releaseList = docElem.FirstChild;
                if (releaseList == null)
                {
                    return list;
                }

                var nodes = releaseList.ChildNodes;

                if (nodes != null)
                {
                    foreach (var node in nodes.Cast<XmlNode>())
                    {
                        if (string.Equals(node.Name, "release", StringComparison.OrdinalIgnoreCase))
                        {
                            var releaseId = node.Attributes["id"].Value;
                            var releaseGroupId = GetReleaseGroupIdFromReleaseNode(node);

                            list.Add(new ReleaseResult
                            {
                                ReleaseId = releaseId,
                                ReleaseGroupId = releaseGroupId,
                                Title = GetValueFromReleaseNode(node, "title"),
                                Overview = GetValueFromReleaseNode(node, "annotation"),
                                Year = GetYearFromReleaseNode(node, "date")
                            });

                            if (limit.HasValue && list.Count >= limit.Value)
                            {
                                break;
                            }
                        }
                    }
                }

                return list;
            }

            private static int? GetYearFromReleaseNode(XmlNode node, string name)
            {
                var subNodes = node.ChildNodes;
                if (subNodes != null)
                {
                    foreach (var subNode in subNodes.Cast<XmlNode>())
                    {
                        if (string.Equals(subNode.Name, name, StringComparison.OrdinalIgnoreCase))
                        {
                            DateTime date;
                            if (DateTime.TryParse(subNode.InnerText, out date))
                            {
                                return date.Year;
                            }

                            return null;
                        }
                    }
                }

                return null;
            }

            private static string GetValueFromReleaseNode(XmlNode node, string name)
            {
                var subNodes = node.ChildNodes;
                if (subNodes != null)
                {
                    foreach (var subNode in subNodes.Cast<XmlNode>())
                    {
                        if (string.Equals(subNode.Name, name, StringComparison.OrdinalIgnoreCase))
                        {
                            return subNode.InnerText;
                        }
                    }
                }

                return null;
            }

            private static string GetReleaseGroupIdFromReleaseNode(XmlNode node)
            {
                var subNodes = node.ChildNodes;
                if (subNodes != null)
                {
                    foreach (var subNode in subNodes.Cast<XmlNode>())
                    {
                        if (string.Equals(subNode.Name, "release-group", StringComparison.OrdinalIgnoreCase))
                        {
                            return subNode.Attributes["id"].Value;
                        }
                    }
                }

                return null;
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
            var url = string.Format("/ws/2/release-group/?query=reid:{0}", releaseEntryId);

            var doc = await GetMusicBrainzResponse(url, false, cancellationToken).ConfigureAwait(false);

            var docElem = doc.DocumentElement;

            if (docElem == null)
            {
                return null;
            }

            var releaseList = docElem.FirstChild;
            if (releaseList == null)
            {
                return null;
            }

            var nodes = releaseList.ChildNodes;

            if (nodes != null)
            {
                foreach (var node in nodes.Cast<XmlNode>())
                {
                    if (string.Equals(node.Name, "release-group", StringComparison.OrdinalIgnoreCase))
                    {
                        return node.Attributes["id"].Value;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// The _music brainz resource pool
        /// </summary>
        private readonly SemaphoreSlim _musicBrainzResourcePool = new SemaphoreSlim(1, 1);

        private long _lastMbzUrlQueryTicks = 0;
        private List<MbzUrl> _mbzUrls = null;
        private MbzUrl _chosenUrl;

        private async Task<MbzUrl> GetMbzUrl()
        {
            if (_chosenUrl == null || _mbzUrls == null || (DateTime.UtcNow.Ticks - _lastMbzUrlQueryTicks) > TimeSpan.FromHours(12).Ticks)
            {
                var urls = await RefreshMzbUrls().ConfigureAwait(false);

                if (urls.Count > 1)
                {
                    _chosenUrl = urls[new Random().Next(0, urls.Count)];
                }
                else
                {
                    _chosenUrl = urls[0];
                }
            }

            return _chosenUrl;
        }

        private async Task<List<MbzUrl>> RefreshMzbUrls()
        {
            List<MbzUrl> list;

            try
            {
                var options = new HttpRequestOptions
                {
                    Url = "https://mb3admin.com/admin/service/standards/musicBrainzUrls",
                    UserAgent = _appHost.Name + "/" + _appHost.ApplicationVersion
                };

                using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
                {
                    list = _json.DeserializeFromStream<List<MbzUrl>>(stream);
                }
                _lastMbzUrlQueryTicks = DateTime.UtcNow.Ticks;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error getting music brainz info", ex);

                list = new List<MbzUrl>
                {
                    new MbzUrl
                    {
                        url = MusicBrainzBaseUrl,
                        throttleMs = 1000
                    }
                };
            }

            _mbzUrls = list.ToList();

            return list;
        }

        /// <summary>
        /// Gets the music brainz response.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="isSearch">if set to <c>true</c> [is search].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{XmlDocument}.</returns>
        internal async Task<XmlDocument> GetMusicBrainzResponse(string url, bool isSearch, CancellationToken cancellationToken)
        {
            var urlInfo = await GetMbzUrl().ConfigureAwait(false);

            if (urlInfo.throttleMs > 0)
            {
                // MusicBrainz is extremely adamant about limiting to one request per second
                await Task.Delay(urlInfo.throttleMs, cancellationToken).ConfigureAwait(false);
            }

            url = urlInfo.url.TrimEnd('/') + url;

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

        internal class MbzUrl
        {
            public string url { get; set; }
            public int throttleMs { get; set; }
        }
    }
}
