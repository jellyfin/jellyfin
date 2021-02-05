#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Json;
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
    public class ArtistProvider : IRemoteMetadataProvider<MusicArtist, ArtistInfo>, IHasOrder
    {
        public const string BaseUrl = "https://www.theaudiodb.com/api/v1/json/" + ApiKey;
        private const string ApiKey = "195003";

        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.GetOptions();

        public ArtistProvider(IServerConfigurationManager config, IFileSystem fileSystem, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _fileSystem = fileSystem;
            _httpClientFactory = httpClientFactory;
            Current = this;
        }

        public static ArtistProvider Current { get; private set; }

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

                await using FileStream jsonStream = File.OpenRead(path);
                var obj = await JsonSerializer.DeserializeAsync<RootObject>(jsonStream, _jsonOptions, cancellationToken)
                                              .ConfigureAwait(false);

                if (obj?.Artists?.Count > 0)
                {
                    result.Item = new MusicArtist();
                    result.HasMetadata = true;
                    ProcessResult(result.Item, obj.Artists[0], info?.MetadataLanguage);
                }
            }

            return result;
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        internal static string GetArtistInfoPath(IApplicationPaths appPaths, string musicBrainzArtistId)
        {
            var dataPath = GetArtistDataPath(appPaths, musicBrainzArtistId);

            return Path.Combine(dataPath, "artist.json");
        }

        internal Task EnsureArtistInfo(string musicBrainzId, CancellationToken cancellationToken)
        {
            var xmlPath = GetArtistInfoPath(_config.ApplicationPaths, musicBrainzId);

            var fileInfo = _fileSystem.GetFileSystemInfo(xmlPath);

            if (fileInfo.Exists
                && (DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 2)
            {
                return Task.CompletedTask;
            }

            return DownloadArtistInfo(musicBrainzId, cancellationToken);
        }

        internal async Task DownloadArtistInfo(string musicBrainzId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = BaseUrl + "/artist-mb.php?i=" + musicBrainzId;

            var path = GetArtistInfoPath(_config.ApplicationPaths, musicBrainzId);

            using var response = await _httpClientFactory.CreateClient(NamedClient.Default)
                                                         .GetAsync(new Uri(url), cancellationToken)
                                                         .ConfigureAwait(false);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            await using var xmlFileStream = new FileStream(
                path, FileMode.Create, FileAccess.Write, FileShare.Read, IODefaults.FileStreamBufferSize, true);
            await stream.CopyToAsync(xmlFileStream, cancellationToken).ConfigureAwait(false);
        }

        private void ProcessResult(MusicArtist item, Artist result, string preferredLanguage)
        {
            // item.HomePageUrl = result.strWebsite;

            if (!string.IsNullOrEmpty(result.StrGenre))
            {
                item.Genres = new[] { result.StrGenre };
            }

            item.SetProviderId(MetadataProvider.AudioDbArtist, result.IdArtist);
            item.SetProviderId(MetadataProvider.MusicBrainzArtist, result.StrMusicBrainzID);

            string overview = null;

            if (string.Equals(preferredLanguage, "de", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.StrBiographyDE;
            }
            else if (string.Equals(preferredLanguage, "fr", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.StrBiographyFR;
            }
            else if (string.Equals(preferredLanguage, "nl", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.StrBiographyNL;
            }
            else if (string.Equals(preferredLanguage, "ru", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.StrBiographyRU;
            }
            else if (string.Equals(preferredLanguage, "it", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.StrBiographyIT;
            }
            else if ((preferredLanguage ?? string.Empty).StartsWith("pt", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.StrBiographyPT;
            }

            if (string.IsNullOrWhiteSpace(overview))
            {
                overview = result.StrBiographyEN;
            }

            item.Overview = (overview ?? string.Empty).StripHtml();
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

        protected internal class Artist
        {
            public string IdArtist { get; set; }

            public string StrArtist { get; set; }

            public string StrArtistAlternate { get; set; }

            public object IdLabel { get; set; }

            public string IntFormedYear { get; set; }

            public string IntBornYear { get; set; }

            public object IntDiedYear { get; set; }

            public object StrDisbanded { get; set; }

            public string StrGenre { get; set; }

            public string StrSubGenre { get; set; }

            public string StrWebsite { get; set; }

            public string StrFacebook { get; set; }

            public string StrTwitter { get; set; }

            public string StrBiographyEN { get; set; }

            public string StrBiographyDE { get; set; }

            public string StrBiographyFR { get; set; }

            public string StrBiographyCN { get; set; }

            public string StrBiographyIT { get; set; }

            public string StrBiographyJP { get; set; }

            public string StrBiographyRU { get; set; }

            public string StrBiographyES { get; set; }

            public string StrBiographyPT { get; set; }

            public string StrBiographySE { get; set; }

            public string StrBiographyNL { get; set; }

            public string StrBiographyHU { get; set; }

            public string StrBiographyNO { get; set; }

            public string StrBiographyIL { get; set; }

            public string StrBiographyPL { get; set; }

            public string StrGender { get; set; }

            public string IntMembers { get; set; }

            public string StrCountry { get; set; }

            public string StrCountryCode { get; set; }

            public string StrArtistThumb { get; set; }

            public string StrArtistLogo { get; set; }

            public string StrArtistFanart { get; set; }

            public string StrArtistFanart2 { get; set; }

            public string StrArtistFanart3 { get; set; }

            public string StrArtistBanner { get; set; }

            public string StrMusicBrainzID { get; set; }

            public object StrLastFMChart { get; set; }

            public string StrLocked { get; set; }
        }

        protected internal class RootObject
        {
            public List<Artist> Artists { get; set; }
        }
    }
}
