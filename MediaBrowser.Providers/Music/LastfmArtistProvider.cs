using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Providers.Music
{
    public class LastFmArtistProvider : IRemoteMetadataProvider<MusicArtist>
    {
        private readonly IJsonSerializer _json;
        private readonly IHttpClient _httpClient;

        internal static readonly SemaphoreSlim LastfmResourcePool = new SemaphoreSlim(4, 4);

        internal const string RootUrl = @"http://ws.audioscrobbler.com/2.0/?";
        internal static string ApiKey = "7b76553c3eb1d341d642755aecc40a33";

        private readonly IServerConfigurationManager _config;
        private ILogger _logger;

        public LastFmArtistProvider(IHttpClient httpClient, IJsonSerializer json)
        {
            _httpClient = httpClient;
            _json = json;
        }

        public async Task<MetadataResult<MusicArtist>> GetMetadata(ItemId id, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<MusicArtist>();

            var musicBrainzId = id.GetProviderId(MetadataProviders.Musicbrainz) ?? await FindId(id, cancellationToken).ConfigureAwait(false);

            if (!String.IsNullOrWhiteSpace(musicBrainzId))
            {
                cancellationToken.ThrowIfCancellationRequested();

                result.Item = new MusicArtist();
                result.HasMetadata = true;

                result.Item.SetProviderId(MetadataProviders.Musicbrainz, musicBrainzId);

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

            var cachePath = Path.Combine(_config.ApplicationPaths.CachePath, "lastfm", musicBrainzId, "image.txt");

            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    File.Delete(cachePath);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
                    File.WriteAllText(cachePath, url + "|" + imageSize);
                }
            }
            catch (IOException ex)
            {
                // Don't fail if this is unable to write
                _logger.ErrorException("Error saving to {0}", ex, cachePath);
            }
        }
        
        private async Task<string> FindId(ItemId item, CancellationToken cancellationToken)
        {
            try
            {
                // If we don't get anything, go directly to music brainz
                return await FindIdFromMusicBrainz(item, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpException e)
            {
                if (e.StatusCode.HasValue && e.StatusCode.Value == HttpStatusCode.BadRequest)
                {
                    // They didn't like a character in the name. Handle the exception so that the provider doesn't keep retrying over and over
                    return null;
                }

                throw;
            }
        }

        /// <summary>
        /// Finds the id from music brainz.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        private async Task<string> FindIdFromMusicBrainz(ItemId item, CancellationToken cancellationToken)
        {
            // They seem to throw bad request failures on any term with a slash
            var nameToSearch = item.Name.Replace('/', ' ');

            var url = String.Format("http://www.musicbrainz.org/ws/2/artist/?query=artist:\"{0}\"", UrlEncode(nameToSearch));

            var doc = await MusicBrainzAlbumProvider.Current.GetMusicBrainzResponse(url, cancellationToken).ConfigureAwait(false);

            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("mb", "http://musicbrainz.org/ns/mmd-2.0#");
            var node = doc.SelectSingleNode("//mb:artist-list/mb:artist/@id", ns);

            if (node != null && node.Value != null)
            {
                return node.Value;
            }

            if (HasDiacritics(item.Name))
            {
                // Try again using the search with accent characters url
                url = String.Format("http://www.musicbrainz.org/ws/2/artist/?query=artistaccent:\"{0}\"", UrlEncode(nameToSearch));

                doc = await MusicBrainzAlbumProvider.Current.GetMusicBrainzResponse(url, cancellationToken).ConfigureAwait(false);

                ns = new XmlNamespaceManager(doc.NameTable);
                ns.AddNamespace("mb", "http://musicbrainz.org/ns/mmd-2.0#");
                node = doc.SelectSingleNode("//mb:artist-list/mb:artist/@id", ns);

                if (node != null && node.Value != null)
                {
                    return node.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Determines whether the specified text has diacritics.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns><c>true</c> if the specified text has diacritics; otherwise, <c>false</c>.</returns>
        private bool HasDiacritics(string text)
        {
            return !String.Equals(text, RemoveDiacritics(text), StringComparison.Ordinal);
        }

        /// <summary>
        /// Removes the diacritics.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>System.String.</returns>
        private string RemoveDiacritics(string text)
        {
            return String.Concat(
                text.Normalize(NormalizationForm.FormD)
                .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) !=
                                              UnicodeCategory.NonSpacingMark)
              ).Normalize(NormalizationForm.FormC);
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
    }
}
