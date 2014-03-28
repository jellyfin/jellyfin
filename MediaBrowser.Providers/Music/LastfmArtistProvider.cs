using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Music
{
    public class LastfmArtistProvider : IRemoteMetadataProvider<MusicArtist, ArtistInfo>, IHasOrder
    {
        private readonly IJsonSerializer _json;
        private readonly IHttpClient _httpClient;

        internal static readonly SemaphoreSlim LastfmResourcePool = new SemaphoreSlim(4, 4);

        internal const string RootUrl = @"http://ws.audioscrobbler.com/2.0/?";
        internal static string ApiKey = "7b76553c3eb1d341d642755aecc40a33";

        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;

        public LastfmArtistProvider(IHttpClient httpClient, IJsonSerializer json, IServerConfigurationManager config, ILogger logger)
        {
            _httpClient = httpClient;
            _json = json;
            _config = config;
            _logger = logger;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ArtistInfo searchInfo, CancellationToken cancellationToken)
        {
            return new List<RemoteSearchResult>();
        }

        public async Task<MetadataResult<MusicArtist>> GetMetadata(ArtistInfo id, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<MusicArtist>();

            var musicBrainzId = id.GetMusicBrainzArtistId();

            if (!String.IsNullOrWhiteSpace(musicBrainzId))
            {
                cancellationToken.ThrowIfCancellationRequested();

                result.Item = new MusicArtist();
                result.HasMetadata = true;

                await FetchLastfmData(result.Item, musicBrainzId, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }

        protected virtual async Task FetchLastfmData(MusicArtist item, string musicBrainzId, CancellationToken cancellationToken)
        {
            // Get artist info with provided id
            var url = RootUrl + String.Format("method=artist.getInfo&mbid={0}&api_key={1}&format=json", UrlEncode(musicBrainzId), ApiKey);

            LastfmGetArtistResult result;

            using (var json = await _httpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = LastfmResourcePool,
                CancellationToken = cancellationToken,
                EnableHttpCompression = false

            }).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(json))
                {
                    var jsonText = await reader.ReadToEndAsync().ConfigureAwait(false);

                    // Fix their bad json
                    jsonText = jsonText.Replace("\"#text\"", "\"url\"");

                    result = _json.DeserializeFromString<LastfmGetArtistResult>(jsonText);
                }
            }

            if (result != null && result.artist != null)
            {
                ProcessArtistData(item, result.artist, musicBrainzId);
            }
        }

        private void ProcessArtistData(MusicArtist artist, LastfmArtist data, string musicBrainzId)
        {
            var yearFormed = 0;

            if (data.bio != null)
            {
                Int32.TryParse(data.bio.yearformed, out yearFormed);
                if (!artist.LockedFields.Contains(MetadataFields.Overview))
                {
                    artist.Overview = data.bio.content;
                }
                if (!string.IsNullOrEmpty(data.bio.placeformed) && !artist.LockedFields.Contains(MetadataFields.ProductionLocations))
                {
                    artist.AddProductionLocation(data.bio.placeformed);
                }
            }

            if (yearFormed > 0)
            {
                artist.PremiereDate = new DateTime(yearFormed, 1, 1, 0, 0, 0, DateTimeKind.Utc);

                artist.ProductionYear = yearFormed;
            }

            string imageSize;
            var url = LastfmHelper.GetImageUrl(data, out imageSize);

            if (!string.IsNullOrEmpty(musicBrainzId) && !string.IsNullOrEmpty(url))
            {
                LastfmHelper.SaveImageInfo(_config.ApplicationPaths, _logger, musicBrainzId, url, imageSize);
            }
        }

        /// <summary>
        /// Encodes an URL.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>System.String.</returns>
        private string UrlEncode(string name)
        {
            return WebUtility.UrlEncode(name);
        }

        public string Name
        {
            get { return "last.fm"; }
        }

        public int Order
        {
            get
            {
                // After fanart & audiodb
                return 2;
            }
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
