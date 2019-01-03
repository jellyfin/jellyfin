using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
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
            bool forceMusicBrainzProper = false;

            if (!string.IsNullOrEmpty(releaseId))
            {
                url = string.Format("/ws/2/release/?query=reid:{0}", releaseId);
            }
            else if (!string.IsNullOrEmpty(releaseGroupId))
            {
                url = string.Format("/ws/2/release?release-group={0}", releaseGroupId);
                forceMusicBrainzProper = true;
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

                    // I'm sure there is a better way but for now it resolves search for 12" Mixes
                    var queryName = searchInfo.Name.Replace("\"", string.Empty);

                    url = string.Format("/ws/2/release/?query=\"{0}\" AND artist:\"{1}\"",
                       WebUtility.UrlEncode(queryName),
                       WebUtility.UrlEncode(searchInfo.GetAlbumArtist()));
                }
            }

            if (!string.IsNullOrWhiteSpace(url))
            {
                using (var response = await GetMusicBrainzResponse(url, isNameSearch, forceMusicBrainzProper, cancellationToken).ConfigureAwait(false))
                {
                    using (var stream = response.Content)
                    {
                        return GetResultsFromResponse(stream);
                    }
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

                        if (i.Artists.Count > 0)
                        {
                            result.AlbumArtist = new RemoteSearchResult
                            {
                                SearchProviderName = Name,
                                Name = i.Artists[0].Item1
                            };

                            result.AlbumArtist.SetProviderId(MetadataProviders.MusicBrainzArtist, i.Artists[0].Item2);
                        }

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

            using (var response = await GetMusicBrainzResponse(url, true, cancellationToken).ConfigureAwait(false))
            {
                using (var stream = response.Content)
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
        }

        private async Task<ReleaseResult> GetReleaseResultByArtistName(string albumName, string artistName, CancellationToken cancellationToken)
        {
            var url = string.Format("/ws/2/release/?query=\"{0}\" AND artist:\"{1}\"",
                WebUtility.UrlEncode(albumName),
                WebUtility.UrlEncode(artistName));

            using (var response = await GetMusicBrainzResponse(url, true, cancellationToken).ConfigureAwait(false))
            {
                using (var stream = response.Content)
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
        }

        private class ReleaseResult
        {
            public string ReleaseId;
            public string ReleaseGroupId;
            public string Title;
            public string Overview;
            public int? Year;

            public List<ValueTuple<string, string>> Artists = new List<ValueTuple<string, string>>();

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
                            case "artist-credit":
                                {
                                    // TODO

                                    /*
                                     * <artist-credit>
<name-credit>
<artist id="e225cda5-882d-4b80-b8a3-b36d7175b1ea">
<name>SARCASTIC+ZOOKEEPER</name>
<sort-name>SARCASTIC+ZOOKEEPER</sort-name>
</artist>
</name-credit>
</artist-credit>
                                     */
                                    using (var subReader = reader.ReadSubtree())
                                    {
                                        var artist = ParseArtistCredit(subReader);

                                        if (!string.IsNullOrEmpty(artist.Item1))
                                        {
                                            result.Artists.Add(artist);
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

                return result;
            }
        }

        private static ValueTuple<string, string> ParseArtistCredit(XmlReader reader)
        {
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
                        case "name-credit":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    return ParseArtistNameCredit(subReader);
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

            return new ValueTuple<string, string>();
        }

        private static ValueTuple<string, string> ParseArtistNameCredit(XmlReader reader)
        {
            reader.MoveToContent();
            reader.Read();

            string name = null;

            // http://stackoverflow.com/questions/2299632/why-does-xmlreader-skip-every-other-element-if-there-is-no-whitespace-separator

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "artist":
                            {
                                var id = reader.GetAttribute("id");
                                using (var subReader = reader.ReadSubtree())
                                {
                                    return ParseArtistArtistCredit(subReader, id);
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

            return new ValueTuple<string, string>(name, null);
        }

        private static ValueTuple<string, string> ParseArtistArtistCredit(XmlReader reader, string artistId)
        {
            reader.MoveToContent();
            reader.Read();

            string name = null;

            // http://stackoverflow.com/questions/2299632/why-does-xmlreader-skip-every-other-element-if-there-is-no-whitespace-separator

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "name":
                            {
                                name = reader.ReadElementContentAsString();
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

            return new ValueTuple<string, string>(name, artistId);
        }

        private async Task<string> GetReleaseIdFromReleaseGroupId(string releaseGroupId, CancellationToken cancellationToken)
        {
            var url = string.Format("/ws/2/release?release-group={0}", releaseGroupId);

            using (var response = await GetMusicBrainzResponse(url, true, true, cancellationToken).ConfigureAwait(false))
            {
                using (var stream = response.Content)
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

            using (var response = await GetMusicBrainzResponse(url, false, cancellationToken).ConfigureAwait(false))
            {
                using (var stream = response.Content)
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

        internal Task<HttpResponseInfo> GetMusicBrainzResponse(string url, bool isSearch, CancellationToken cancellationToken)
        {
            return GetMusicBrainzResponse(url, isSearch, false, cancellationToken);
        }

        /// <summary>
        /// Gets the music brainz response.
        /// </summary>
        internal async Task<HttpResponseInfo> GetMusicBrainzResponse(string url, bool isSearch, bool forceMusicBrainzProper, CancellationToken cancellationToken)
        {
            var urlInfo = new MbzUrl(MusicBrainzBaseUrl, 1000);
            var throttleMs = urlInfo.throttleMs;

            if (throttleMs > 0)
            {
                // MusicBrainz is extremely adamant about limiting to one request per second
                _logger.LogDebug("Throttling MusicBrainz by {0}ms", throttleMs.ToString(CultureInfo.InvariantCulture));
                await Task.Delay(throttleMs, cancellationToken).ConfigureAwait(false);
            }

            url = urlInfo.url.TrimEnd('/') + url;

            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                UserAgent = _appHost.Name + "/" + _appHost.ApplicationVersion,
                BufferContent = throttleMs > 0
            };

            return await _httpClient.SendAsync(options, "GET").ConfigureAwait(false);
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
            internal MbzUrl(string url, int throttleMs)
            {
                this.url = url;
                this.throttleMs = throttleMs;
            }

            public string url { get; set; }
            public int throttleMs { get; set; }
        }
    }
}
