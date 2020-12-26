#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Music;

namespace MediaBrowser.Providers.Plugins.AudioDb
{
    public class AudioDbArtistProvider : IRemoteMetadataProvider<MusicArtist, ArtistInfo>, IHasOrder
    {
        public const string BaseUrl = "https://www.theaudiodb.com/api/v1/json/" + ApiKey;
        private const string ApiKey = "195003";

        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IJsonSerializer _json;

        public AudioDbArtistProvider(IServerConfigurationManager config, IFileSystem fileSystem, IHttpClientFactory httpClientFactory, IJsonSerializer json)
        {
            _config = config;
            _fileSystem = fileSystem;
            _httpClientFactory = httpClientFactory;
            _json = json;
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
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            var result = new MetadataResult<MusicArtist>();
            var id = info.GetMusicBrainzArtistId();

            if (!string.IsNullOrWhiteSpace(id))
            {
                await EnsureArtistInfo(id, cancellationToken).ConfigureAwait(false);

                var path = GetArtistInfoPath(_config.ApplicationPaths, id);

                var obj = _json.DeserializeFromFile<RootObject>(path);

                if (obj != null && obj.Artists != null && obj.Artists.Count > 0)
                {
                    result.Item = new MusicArtist();
                    result.HasMetadata = true;
                    ProcessResult(result.Item, obj.Artists[0], info.MetadataLanguage);
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
                .GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            Directory.CreateDirectory(Path.GetDirectoryName(path));

#pragma warning disable CA2000 // Dispose objects before losing scope: Wrongly identified
            await using var xmlFileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, IODefaults.FileStreamBufferSize, true);
#pragma warning restore CA2000 // Dispose objects before losing scope
            await stream.CopyToAsync(xmlFileStream, cancellationToken).ConfigureAwait(false);
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

        private static void ProcessResult(MusicArtist item, Artist result, string preferredLanguage)
        {
            // item.HomePageUrl = result.strWebsite;

            if (!string.IsNullOrEmpty(result.Genre))
            {
                item.Genres = new[] { result.Genre };
            }

            item.SetProviderId(MetadataProvider.AudioDbArtist, result.ArtistId);
            item.SetProviderId(MetadataProvider.MusicBrainzArtist, result.MusicBrainzID);

            string overview = null;

            if (string.Equals(preferredLanguage, "de", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.BiographyDE;
            }
            else if (string.Equals(preferredLanguage, "fr", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.BiographyFR;
            }
            else if (string.Equals(preferredLanguage, "nl", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.BiographyNL;
            }
            else if (string.Equals(preferredLanguage, "ru", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.BiographyRU;
            }
            else if (string.Equals(preferredLanguage, "it", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.BiographyIT;
            }
            else if ((preferredLanguage ?? string.Empty).StartsWith("pt", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.BiographyPT;
            }

            if (string.IsNullOrWhiteSpace(overview))
            {
                overview = result.BiographyEN;
            }

            item.Overview = (overview ?? string.Empty).StripHtml();
        }

        internal class Artist
        {
            public string ArtistId { get; set; }

            public string ArtistName { get; set; }

            public string ArtistAlternate { get; set; }

            public object Label { get; set; }

            public string FormedYear { get; set; }

            public string BornYear { get; set; }

            public object DiedYear { get; set; }

            public object Disbanded { get; set; }

            public string Genre { get; set; }

            public string SubGenre { get; set; }

            public string Website { get; set; }

            public string Facebook { get; set; }

            public string Twitter { get; set; }

            public string BiographyEN { get; set; }

            public string BiographyDE { get; set; }

            public string BiographyFR { get; set; }

            public string BiographyCN { get; set; }

            public string BiographyIT { get; set; }

            public string BiographyJP { get; set; }

            public string BiographyRU { get; set; }

            public string BiographyES { get; set; }

            public string BiographyPT { get; set; }

            public string BiographySE { get; set; }

            public string BiographyNL { get; set; }

            public string BiographyHU { get; set; }

            public string BiographyNO { get; set; }

            public string BiographyIL { get; set; }

            public string BiographyPL { get; set; }

            public string Gender { get; set; }

            public string Members { get; set; }

            public string Country { get; set; }

            public string CountryCode { get; set; }

            public string ArtistThumb { get; set; }

            public string ArtistLogo { get; set; }

            public string ArtistFanart { get; set; }

            public string ArtistFanart2 { get; set; }

            public string ArtistFanart3 { get; set; }

            public string ArtistBanner { get; set; }

            public string MusicBrainzID { get; set; }

            public object LastFMChart { get; set; }

            public string Locked { get; set; }
        }

        internal class RootObject
        {
            public List<Artist> Artists { get; set; }
        }
    }
}
