using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.Providers.Music
{
    public class MusicBrainzAlbumProvider : IRemoteMetadataProvider<MusicAlbum, AlbumInfo>, IHasOrder
    {
        internal static MusicBrainzAlbumProvider Current;

        private readonly IHttpClient _httpClient;
        private readonly IApplicationHost _appHost;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _json;
        private readonly IXmlReaderSettingsFactory _xmlSettings;

        public static string MusicBrainzBaseUrl = "https://www.musicbrainz.org";

        public MusicBrainzAlbumProvider(IHttpClient httpClient, IApplicationHost appHost, ILogger logger, IJsonSerializer json, IXmlReaderSettingsFactory xmlSettings)
        {
            _httpClient = httpClient;
            _appHost = appHost;
            _logger = logger;
            _json = json;
            _xmlSettings = xmlSettings;
            Current = this;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(AlbumInfo searchInfo, CancellationToken cancellationToken)
        {
            var releaseId = searchInfo.GetReleaseId();
            var releaseGroupId = searchInfo.GetReleaseGroupId();

            string url = null;
            var isNameSearch = false;

            if (!string.IsNullOrEmpty(releaseId))
            {
                url = string.Format("/ws/2/release/?query=reid:{0}", releaseId);
            }
            else if (!string.IsNullOrEmpty(releaseGroupId))
            {
                url = string.Format("/ws/2/release?release-group={0}", releaseGroupId);
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
                using (var stream = await GetMusicBrainzResponse(url, isNameSearch, cancellationToken).ConfigureAwait(false))
                {
                    return GetResultsFromResponse(stream);
                }
            }

            return new List<RemoteSearchResult>();
        }

        private List<RemoteSearchResult> GetResultsFromResponse(Stream stream)
        {
            using (var oReader = new StreamReader(stream, Encoding.UTF8))
            {
                var settings = _xmlSettings.Create(false);

                settings.CheckCharacters = false;
                settings.IgnoreProcessingInstructions = true;
                settings.IgnoreComments = true;

                using (var reader = XmlReader.Create(oReader, settings))
                {
                    var results = ReleaseResult.Parse(reader);

                    return results.Select(i =>
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
                    }).ToList();
                }
            }
        }

        public async Task<MetadataResult<MusicAlbum>> GetMetadata(AlbumInfo id, CancellationToken cancellationToken)
        {
            var releaseId = id.GetReleaseId();
            var releaseGroupId = id.GetReleaseGroupId();

            var result = new MetadataResult<MusicAlbum>
            {
                Item = new MusicAlbum()
            };

            // If we have a release group Id but not a release Id...
            if (string.IsNullOrWhiteSpace(releaseId) && !string.IsNullOrWhiteSpace(releaseGroupId))
            {
                releaseId = await GetReleaseIdFromReleaseGroupId(releaseGroupId, cancellationToken).ConfigureAwait(false);
                result.HasMetadata = true;
            }

            if (string.IsNullOrWhiteSpace(releaseId))
            {
                var artistMusicBrainzId = id.GetMusicBrainzArtistId();

                var releaseResult = await GetReleaseResult(artistMusicBrainzId, id.GetAlbumArtist(), id.Name, cancellationToken).ConfigureAwait(false);

                if (releaseResult != null)
                {
                    if (!string.IsNullOrWhiteSpace(releaseResult.ReleaseId))
                    {
                        releaseId = releaseResult.ReleaseId;
                        result.HasMetadata = true;
                    }

                    if (!string.IsNullOrWhiteSpace(releaseResult.ReleaseGroupId))
                    {
                        releaseGroupId = releaseResult.ReleaseGroupId;
                        result.HasMetadata = true;
                    }

                    result.Item.ProductionYear = releaseResult.Year;
                    result.Item.Overview = releaseResult.Overview;
                }
            }

            // If we have a release Id but not a release group Id...
            if (!string.IsNullOrWhiteSpace(releaseId) && string.IsNullOrWhiteSpace(releaseGroupId))
            {
                releaseGroupId = await GetReleaseGroupFromReleaseId(releaseId, cancellationToken).ConfigureAwait(false);
                result.HasMetadata = true;
            }

            if (!string.IsNullOrWhiteSpace(releaseId) || !string.IsNullOrWhiteSpace(releaseGroupId))
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

            using (var stream = await GetMusicBrainzResponse(url, true, cancellationToken).ConfigureAwait(false))
            {
                using (var oReader = new StreamReader(stream, Encoding.UTF8))
                {
                    var settings = _xmlSettings.Create(false);

                    settings.CheckCharacters = false;
                    settings.IgnoreProcessingInstructions = true;
                    settings.IgnoreComments = true;

                    using (var reader = XmlReader.Create(oReader, settings))
                    {
                        return ReleaseResult.Parse(reader).FirstOrDefault();
                    }
                }
            }
        }

        private async Task<ReleaseResult> GetReleaseResultByArtistName(string albumName, string artistName, CancellationToken cancellationToken)
        {
            var url = string.Format("/ws/2/release/?query=\"{0}\" AND artist:\"{1}\"",
                WebUtility.UrlEncode(albumName),
                WebUtility.UrlEncode(artistName));

            using (var stream = await GetMusicBrainzResponse(url, true, cancellationToken).ConfigureAwait(false))
            {
                using (var oReader = new StreamReader(stream, Encoding.UTF8))
                {
                    var settings = _xmlSettings.Create(false);

                    settings.CheckCharacters = false;
                    settings.IgnoreProcessingInstructions = true;
                    settings.IgnoreComments = true;

                    using (var reader = XmlReader.Create(oReader, settings))
                    {
                        return ReleaseResult.Parse(reader).FirstOrDefault();
                    }
                }
            }
        }

        private class ReleaseResult
        {
            public string ReleaseId;
            public string ReleaseGroupId;
            public string Title;
            public string Overview;
            public int? Year;

            public static List<ReleaseResult> Parse(XmlReader reader)
            {
                reader.MoveToContent();
                reader.Read();

                // Loop through each element
                while (!reader.EOF && reader.ReadState == ReadState.Interactive)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.Name)
                        {
                            case "release-list":
                                {
                                    if (reader.IsEmptyElement)
                                    {
                                        reader.Read();
                                        continue;
                                    }
                                    using (var subReader = reader.ReadSubtree())
                                    {
                                        return ParseReleaseList(subReader);
                                    }
                                }
                            default:
                                {
                                    reader.Skip();
                                    break;
                                }
                        }
                    }
                    else
                    {
                        reader.Read();
                    }
                }

                return new List<ReleaseResult>();
            }

            private static List<ReleaseResult> ParseReleaseList(XmlReader reader)
            {
                var list = new List<ReleaseResult>();

                reader.MoveToContent();
                reader.Read();

                // Loop through each element
                while (!reader.EOF && reader.ReadState == ReadState.Interactive)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.Name)
                        {
                            case "release":
                                {
                                    if (reader.IsEmptyElement)
                                    {
                                        reader.Read();
                                        continue;
                                    }
                                    var releaseId = reader.GetAttribute("id");

                                    using (var subReader = reader.ReadSubtree())
                                    {
                                        var release = ParseRelease(subReader, releaseId);
                                        if (release != null)
                                        {
                                            list.Add(release);
                                        }
                                    }
                                    break;
                                }
                            default:
                                {
                                    reader.Skip();
                                    break;
                                }
                        }
                    }
                    else
                    {
                        reader.Read();
                    }
                }

                return list;
            }

            private static ReleaseResult ParseRelease(XmlReader reader, string releaseId)
            {
                var result = new ReleaseResult
                {
                    ReleaseId = releaseId
                };

                reader.MoveToContent();
                reader.Read();

                // http://stackoverflow.com/questions/2299632/why-does-xmlreader-skip-every-other-element-if-there-is-no-whitespace-separator

                // Loop through each element
                while (!reader.EOF && reader.ReadState == ReadState.Interactive)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.Name)
                        {
                            case "title":
                                {
                                    result.Title = reader.ReadElementContentAsString();
                                    break;
                                }
                            case "date":
                                {
                                    var val = reader.ReadElementContentAsString();
                                    DateTime date;
                                    if (DateTime.TryParse(val, out date))
                                    {
                                        result.Year = date.Year;
                                    }
                                    break;
                                }
                            case "annotation":
                                {
                                    result.Overview = reader.ReadElementContentAsString();
                                    break;
                                }
                            case "release-group":
                                {
                                    result.ReleaseGroupId = reader.GetAttribute("id");
                                    reader.Skip();
                                    break;
                                }
                            default:
                                {
                                    reader.Skip();
                                    break;
                                }
                        }
                    }
                    else
                    {
                        reader.Read();
                    }
                }

                return result;
            }
        }

        private async Task<string> GetReleaseIdFromReleaseGroupId(string releaseGroupId, CancellationToken cancellationToken)
        {
            var url = string.Format("/ws/2/release?release-group={0}", releaseGroupId);

            using (var stream = await GetMusicBrainzResponse(url, true, cancellationToken).ConfigureAwait(false))
            {
                using (var oReader = new StreamReader(stream, Encoding.UTF8))
                {
                    var settings = _xmlSettings.Create(false);

                    settings.CheckCharacters = false;
                    settings.IgnoreProcessingInstructions = true;
                    settings.IgnoreComments = true;

                    using (var reader = XmlReader.Create(oReader, settings))
                    {
                        var result = ReleaseResult.Parse(reader).FirstOrDefault();

                        if (result != null)
                        {
                            return result.ReleaseId;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the release group id internal.
        /// </summary>
        /// <param name="releaseEntryId">The release entry id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        private async Task<string> GetReleaseGroupFromReleaseId(string releaseEntryId, CancellationToken cancellationToken)
        {
            var url = string.Format("/ws/2/release-group/?query=reid:{0}", releaseEntryId);

            using (var stream = await GetMusicBrainzResponse(url, false, cancellationToken).ConfigureAwait(false))
            {
                using (var oReader = new StreamReader(stream, Encoding.UTF8))
                {
                    var settings = _xmlSettings.Create(false);

                    settings.CheckCharacters = false;
                    settings.IgnoreProcessingInstructions = true;
                    settings.IgnoreComments = true;

                    using (var reader = XmlReader.Create(oReader, settings))
                    {
                        reader.MoveToContent();
                        reader.Read();

                        // Loop through each element
                        while (!reader.EOF && reader.ReadState == ReadState.Interactive)
                        {
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                switch (reader.Name)
                                {
                                    case "release-group-list":
                                        {
                                            if (reader.IsEmptyElement)
                                            {
                                                reader.Read();
                                                continue;
                                            }
                                            using (var subReader = reader.ReadSubtree())
                                            {
                                                return GetFirstReleaseGroupId(subReader);
                                            }
                                        }
                                    default:
                                        {
                                            reader.Skip();
                                            break;
                                        }
                                }
                            }
                            else
                            {
                                reader.Read();
                            }
                        }
                        return null;
                    }
                }
            }
        }

        private string GetFirstReleaseGroupId(XmlReader reader)
        {
            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "release-group":
                            {
                                return reader.GetAttribute("id");
                            }
                        default:
                            {
                                reader.Skip();
                                break;
                            }
                    }
                }
                else
                {
                    reader.Read();
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
                    var results = _json.DeserializeFromStream<List<MbzUrl>>(stream);

                    list = results;
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
        internal async Task<Stream> GetMusicBrainzResponse(string url, bool isSearch, CancellationToken cancellationToken)
        {
            var urlInfo = await GetMbzUrl().ConfigureAwait(false);
            var throttleMs = urlInfo.throttleMs;

            if (throttleMs > 0)
            {
                // MusicBrainz is extremely adamant about limiting to one request per second
                _logger.Debug("Throttling MusicBrainz by {0}ms", throttleMs.ToString(CultureInfo.InvariantCulture));
                await Task.Delay(throttleMs, cancellationToken).ConfigureAwait(false);
            }

            url = urlInfo.url.TrimEnd('/') + url;

            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                UserAgent = _appHost.Name + "/" + _appHost.ApplicationVersion,
                ResourcePool = _musicBrainzResourcePool,
                BufferContent = throttleMs > 0
            };

            return await _httpClient.Get(options).ConfigureAwait(false);
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
