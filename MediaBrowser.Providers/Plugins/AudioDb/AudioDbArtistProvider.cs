#nullable disable

#pragma warning disable CA1034, CS1591, CA1002, SA1028, SA1300

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ArtistInfo searchInfo, CancellationToken cancellationToken)
            => Task.FromResult(Enumerable.Empty<RemoteSearchResult>());

        /// <inheritdoc />
        public async Task<MetadataResult<MusicArtist>> GetMetadata(ArtistInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<MusicArtist>();
            var id = info.GetMusicBrainzArtistId();

            if (!string.IsNullOrWhiteSpace(id))
            {
                await EnsureArtistInfo(id, cancellationToken).ConfigureAwait(false);

                var path = GetArtistInfoPath(_config.ApplicationPaths, id);

                FileStream jsonStream = AsyncFile.OpenRead(path);
                await using (jsonStream.ConfigureAwait(false))
                {
                    var obj = await JsonSerializer.DeserializeAsync<RootObject>(jsonStream, _jsonOptions, cancellationToken).ConfigureAwait(false);

                    if (obj is not null && obj.artists is not null && obj.artists.Count > 0)
                    {
                        result.Item = new MusicArtist();
                        result.HasMetadata = true;
                        ProcessResult(result.Item, obj.artists[0], info.MetadataLanguage);
                    }
                }
            }

            return result;
        }

        private void ProcessResult(MusicArtist item, Artist result, string preferredLanguage)
        {
            // item.HomePageUrl = result.strWebsite;

            if (!string.IsNullOrEmpty(result.strGenre))
            {
                item.Genres = new[] { result.strGenre };
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
                overview = result.strBiographyEN;
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
            cancellationToken.ThrowIfCancellationRequested();

            var url = BaseUrl + "/artist-mb.php?i=" + musicBrainzId;

            using var response = await _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var path = GetArtistInfoPath(_config.ApplicationPaths, musicBrainzId);
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var fileStreamOptions = AsyncFile.WriteOptions;
            fileStreamOptions.Mode = FileMode.Create;
            var xmlFileStream = new FileStream(path, fileStreamOptions);
            await using (xmlFileStream.ConfigureAwait(false))
            {
                await response.Content.CopyToAsync(xmlFileStream, cancellationToken).ConfigureAwait(false);
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
