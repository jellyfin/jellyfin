#nullable disable

#pragma warning disable CA1034, CS1591, CA1002, SA1028, SA1300

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Music;

namespace MediaBrowser.Providers.Plugins.AudioDb
{
    public class AudioDbArtistProvider : IRemoteMetadataProvider<MusicArtist, ArtistInfo>, IHasOrder
    {
        private const string ApiKey = "195003";
        public const string BaseUrl = "https://www.theaudiodb.com/api/v1/json/" + ApiKey;

        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;

        public AudioDbArtistProvider(IServerConfigurationManager config, IFileSystem fileSystem, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _fileSystem = fileSystem;
            _httpClientFactory = httpClientFactory;
            Current = this;
        }

        public static AudioDbArtistProvider Current { get; private set; }

        /// <inheritdoc />
        public string Name => "TheAudioDB";

        /// <inheritdoc />
        // After musicbrainz
        public int Order => 1;

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ArtistInfo searchInfo, CancellationToken cancellationToken)
        {
            // Prefer a known TheAudioDB artist id.
            var audioDbId = searchInfo.GetProviderId(MetadataProvider.AudioDbArtist);
            if (!string.IsNullOrWhiteSpace(audioDbId))
            {
                var artists = await FetchArtists(BaseUrl + "/artist.php?i=" + audioDbId, cancellationToken).ConfigureAwait(false);
                return artists.Select(ToRemoteSearchResult);
            }

            // Fall back to the MusicBrainz artist id, reusing the on-disk cache also used by GetMetadata.
            var musicBrainzId = searchInfo.GetMusicBrainzArtistId();
            if (!string.IsNullOrWhiteSpace(musicBrainzId))
            {
                await EnsureArtistInfo(musicBrainzId, cancellationToken).ConfigureAwait(false);

                var path = GetArtistInfoPath(_config.ApplicationPaths, musicBrainzId);

                FileStream jsonStream = AsyncFile.OpenRead(path);
                await using (jsonStream.ConfigureAwait(false))
                {
                    var obj = await JsonSerializer.DeserializeAsync<RootObject>(jsonStream, _jsonOptions, cancellationToken).ConfigureAwait(false);

                    if (obj is not null && obj.artists is not null)
                    {
                        return obj.artists.Select(ToRemoteSearchResult);
                    }
                }

                return [];
            }

            // Finally, search by name.
            if (!string.IsNullOrWhiteSpace(searchInfo.Name))
            {
                var artists = await FetchArtists(BaseUrl + "/search.php?s=" + Uri.EscapeDataString(searchInfo.Name), cancellationToken).ConfigureAwait(false);
                return artists.Select(ToRemoteSearchResult);
            }

            return [];
        }

        private async Task<List<Artist>> FetchArtists(string url, CancellationToken cancellationToken)
        {
            using var response = await _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var obj = await response.Content.ReadFromJsonAsync<RootObject>(_jsonOptions, cancellationToken).ConfigureAwait(false);

            return obj?.artists ?? [];
        }

        private RemoteSearchResult ToRemoteSearchResult(Artist artist)
        {
            var result = new RemoteSearchResult
            {
                Name = artist.strArtist,
                ImageUrl = artist.strArtistThumb,
                SearchProviderName = Name,
                Overview = (artist.strBiographyEN ?? string.Empty).StripHtml()
            };

            if (!string.IsNullOrEmpty(artist.idArtist))
            {
                result.SetProviderId(MetadataProvider.AudioDbArtist, artist.idArtist);
            }

            if (!string.IsNullOrEmpty(artist.strMusicBrainzID))
            {
                result.SetProviderId(MetadataProvider.MusicBrainzArtist, artist.strMusicBrainzID);
            }

            if (int.TryParse(artist.intFormedYear, NumberStyles.Integer, CultureInfo.InvariantCulture, out var formedYear))
            {
                result.ProductionYear = formedYear;
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<MetadataResult<MusicArtist>> GetMetadata(ArtistInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<MusicArtist>();

            var artist = await GetArtist(
                info.GetMusicBrainzArtistId(),
                info.GetProviderId(MetadataProvider.AudioDbArtist),
                cancellationToken).ConfigureAwait(false);

            if (artist is not null)
            {
                result.Item = new MusicArtist();
                result.HasMetadata = true;
                ProcessResult(result.Item, artist, info.MetadataLanguage);
            }

            return result;
        }

        /// <summary>
        /// Resolves the cached AudioDB artist, preferring the MusicBrainz id and falling back to the AudioDB id.
        /// </summary>
        /// <param name="musicBrainzId">The MusicBrainz artist id, if known.</param>
        /// <param name="audioDbId">The TheAudioDB artist id, if known.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The matching artist, or <c>null</c> if none could be resolved.</returns>
        internal async Task<Artist> GetArtist(string musicBrainzId, string audioDbId, CancellationToken cancellationToken)
        {
            string path;
            if (!string.IsNullOrWhiteSpace(musicBrainzId))
            {
                await EnsureArtistInfo(musicBrainzId, cancellationToken).ConfigureAwait(false);
                path = GetArtistInfoPath(_config.ApplicationPaths, musicBrainzId);
            }
            else if (!string.IsNullOrWhiteSpace(audioDbId))
            {
                await EnsureArtistInfoByAudioDbId(audioDbId, cancellationToken).ConfigureAwait(false);
                path = GetArtistInfoPath(_config.ApplicationPaths, audioDbId);
            }
            else
            {
                return null;
            }

            FileStream jsonStream = AsyncFile.OpenRead(path);
            await using (jsonStream.ConfigureAwait(false))
            {
                var obj = await JsonSerializer.DeserializeAsync<RootObject>(jsonStream, _jsonOptions, cancellationToken).ConfigureAwait(false);

                if (obj is not null && obj.artists is not null && obj.artists.Count > 0)
                {
                    return obj.artists[0];
                }
            }

            return null;
        }

        private void ProcessResult(MusicArtist item, Artist result, string preferredLanguage)
        {
            if (!string.IsNullOrWhiteSpace(result.strWebsite))
            {
                item.HomePageUrl = result.strWebsite;
            }

            var genres = new List<string>();
            if (!string.IsNullOrWhiteSpace(result.strGenre))
            {
                genres.Add(result.strGenre);
            }

            if (!string.IsNullOrWhiteSpace(result.strSubGenre))
            {
                genres.Add(result.strSubGenre);
            }

            if (genres.Count > 0)
            {
                item.Genres = genres.ToArray();
            }

            if (int.TryParse(result.intFormedYear, NumberStyles.Integer, CultureInfo.InvariantCulture, out var formedYear))
            {
                item.ProductionYear = formedYear;
            }

            if (!string.IsNullOrWhiteSpace(result.strCountry))
            {
                item.ProductionLocations = new[] { result.strCountry };
            }

            item.SetProviderId(MetadataProvider.AudioDbArtist, result.idArtist);
            item.SetProviderId(MetadataProvider.MusicBrainzArtist, result.strMusicBrainzID);

            string overview = null;

            if (string.Equals(preferredLanguage, "de", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.strBiographyDE;
            }
            else if (string.Equals(preferredLanguage, "fr", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.strBiographyFR;
            }
            else if (string.Equals(preferredLanguage, "nl", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.strBiographyNL;
            }
            else if (string.Equals(preferredLanguage, "ru", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.strBiographyRU;
            }
            else if (string.Equals(preferredLanguage, "it", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.strBiographyIT;
            }
            else if ((preferredLanguage ?? string.Empty).StartsWith("pt", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.strBiographyPT;
            }

            if (string.IsNullOrWhiteSpace(overview))
            {
                overview = string.IsNullOrWhiteSpace(result.strBiographyEN)
                    ? result.strBiography
                    : result.strBiographyEN;
            }

            item.Overview = (overview ?? string.Empty).StripHtml();
        }

        internal async Task EnsureArtistInfo(string musicBrainzId, CancellationToken cancellationToken)
        {
            var xmlPath = GetArtistInfoPath(_config.ApplicationPaths, musicBrainzId);

            var fileInfo = _fileSystem.GetFileSystemInfo(xmlPath);

            if (fileInfo.Exists
                && (DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 2)
            {
                return;
            }

            await DownloadArtistInfo(musicBrainzId, cancellationToken).ConfigureAwait(false);
        }

        internal async Task DownloadArtistInfo(string musicBrainzId, CancellationToken cancellationToken)
        {
            var url = BaseUrl + "/artist-mb.php?i=" + musicBrainzId;
            await DownloadArtistInfo(url, GetArtistInfoPath(_config.ApplicationPaths, musicBrainzId), cancellationToken).ConfigureAwait(false);
        }

        internal async Task EnsureArtistInfoByAudioDbId(string audioDbId, CancellationToken cancellationToken)
        {
            var xmlPath = GetArtistInfoPath(_config.ApplicationPaths, audioDbId);

            var fileInfo = _fileSystem.GetFileSystemInfo(xmlPath);

            if (fileInfo.Exists
                && (DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 2)
            {
                return;
            }

            var url = BaseUrl + "/artist.php?i=" + audioDbId;
            await DownloadArtistInfo(url, xmlPath, cancellationToken).ConfigureAwait(false);
        }

        private async Task DownloadArtistInfo(string url, string path, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var response = await _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var tempPath = path + $".tmp-{Guid.NewGuid():N}";
            var fileStreamOptions = AsyncFile.WriteOptions;
            fileStreamOptions.Mode = FileMode.Create;
            var xmlFileStream = new FileStream(tempPath, fileStreamOptions);
            await using (xmlFileStream.ConfigureAwait(false))
            {
                await response.Content.CopyToAsync(xmlFileStream, cancellationToken).ConfigureAwait(false);
            }

            // Atomically move the file to avoid race with concurrent reader oder writer.
            if (!_fileSystem.MoveFile(tempPath, path, true))
            {
                _fileSystem.DeleteFile(tempPath);
            }
        }

        /// <summary>
        /// Gets the artist data path.
        /// </summary>
        /// <param name="appPaths">The application paths.</param>
        /// <param name="musicBrainzArtistId">The music brainz artist identifier.</param>
        /// <returns>System.String.</returns>
        private static string GetArtistDataPath(IApplicationPaths appPaths, string musicBrainzArtistId)
            => Path.Combine(GetArtistDataPath(appPaths), musicBrainzArtistId);

        /// <summary>
        /// Gets the artist data path.
        /// </summary>
        /// <param name="appPaths">The application paths.</param>
        /// <returns>System.String.</returns>
        private static string GetArtistDataPath(IApplicationPaths appPaths)
            => Path.Combine(appPaths.CachePath, "audiodb-artist");

        internal static string GetArtistInfoPath(IApplicationPaths appPaths, string musicBrainzArtistId)
        {
            var dataPath = GetArtistDataPath(appPaths, musicBrainzArtistId);

            return Path.Combine(dataPath, "artist.json");
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public class Artist
        {
            public string idArtist { get; set; }

            public string strArtist { get; set; }

            public string strArtistAlternate { get; set; }

            public object idLabel { get; set; }

            public string intFormedYear { get; set; }

            public string intBornYear { get; set; }

            public object intDiedYear { get; set; }

            public object strDisbanded { get; set; }

            public string strGenre { get; set; }

            public string strSubGenre { get; set; }

            public string strWebsite { get; set; }

            public string strFacebook { get; set; }

            public string strTwitter { get; set; }

            public string strBiography { get; set; }

            public string strBiographyEN { get; set; }

            public string strBiographyDE { get; set; }

            public string strBiographyFR { get; set; }

            public string strBiographyCN { get; set; }

            public string strBiographyIT { get; set; }

            public string strBiographyJP { get; set; }

            public string strBiographyRU { get; set; }

            public string strBiographyES { get; set; }

            public string strBiographyPT { get; set; }

            public string strBiographySE { get; set; }

            public string strBiographyNL { get; set; }

            public string strBiographyHU { get; set; }

            public string strBiographyNO { get; set; }

            public string strBiographyIL { get; set; }

            public string strBiographyPL { get; set; }

            public string strGender { get; set; }

            public string intMembers { get; set; }

            public string strCountry { get; set; }

            public string strCountryCode { get; set; }

            public string strArtistThumb { get; set; }

            public string strArtistLogo { get; set; }

            public string strArtistFanart { get; set; }

            public string strArtistFanart2 { get; set; }

            public string strArtistFanart3 { get; set; }

            public string strArtistBanner { get; set; }

            public string strMusicBrainzID { get; set; }

            public object strLastFMChart { get; set; }

            public string strLocked { get; set; }
        }

#pragma warning disable CA2227
        public class RootObject
        {
            public List<Artist> artists { get; set; }
        }
    }
}
