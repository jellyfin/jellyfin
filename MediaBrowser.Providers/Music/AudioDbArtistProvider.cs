using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Providers.Music
{
    public class AudioDbArtistProvider : IRemoteMetadataProvider<MusicArtist, ArtistInfo>, IHasOrder
    {
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _json;

        public static AudioDbArtistProvider Current;

        public SemaphoreSlim AudioDbResourcePool = new SemaphoreSlim(2, 2);
        private const string ApiKey = "49jhsf8248yfahka89724011";
        public const string BaseUrl = "http://www.theaudiodb.com/api/v1/json/" + ApiKey;

        public AudioDbArtistProvider(IServerConfigurationManager config, IFileSystem fileSystem, IHttpClient httpClient, IJsonSerializer json)
        {
            _config = config;
            _fileSystem = fileSystem;
            _httpClient = httpClient;
            _json = json;
            Current = this;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ArtistInfo searchInfo, CancellationToken cancellationToken)
        {
            return new List<RemoteSearchResult>();
        }

        public async Task<MetadataResult<MusicArtist>> GetMetadata(ArtistInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<MusicArtist>();

            var id = info.GetMusicBrainzArtistId();

            if (!string.IsNullOrWhiteSpace(id))
            {
                await EnsureArtistInfo(id, cancellationToken).ConfigureAwait(false);

                var path = GetArtistInfoPath(_config.ApplicationPaths, id);

                var obj = _json.DeserializeFromFile<RootObject>(path);

                if (obj != null && obj.artists != null && obj.artists.Count > 0)
                {
                    result.Item = new MusicArtist();
                    result.HasMetadata = true;
                    ProcessResult(result.Item, obj.artists[0]);
                }
            }

            return result;
        }

        private void ProcessResult(MusicArtist item, Artist result)
        {
            item.HomePageUrl = result.strWebsite;
            item.Overview = (result.strBiographyEN ?? string.Empty).StripHtml();

            if (!string.IsNullOrEmpty(result.strGenre))
            {
                item.Genres = new List<string> { result.strGenre };
            }

            item.SetProviderId(MetadataProviders.AudioDbArtist, result.idArtist);
            item.SetProviderId(MetadataProviders.MusicBrainzArtist, result.strMusicBrainzID);
        }

        public string Name
        {
            get { return "TheAudioDB"; }
        }

        private readonly Task _cachedTask = Task.FromResult(true);
        internal Task EnsureArtistInfo(string musicBrainzId, CancellationToken cancellationToken)
        {
            var xmlPath = GetArtistInfoPath(_config.ApplicationPaths, musicBrainzId);

            var fileInfo = _fileSystem.GetFileSystemInfo(xmlPath);

            if (fileInfo.Exists)
            {
                if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 7)
                {
                    return _cachedTask;
                }
            }

            return DownloadArtistInfo(musicBrainzId, cancellationToken);
        }

        internal async Task DownloadArtistInfo(string musicBrainzId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = BaseUrl + "/artist-mb.php?i=" + musicBrainzId;

            var path = GetArtistInfoPath(_config.ApplicationPaths, musicBrainzId);

            using (var response = await _httpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = AudioDbResourcePool,
                CancellationToken = cancellationToken

            }).ConfigureAwait(false))
            {
				_fileSystem.CreateDirectory(Path.GetDirectoryName(path));

                using (var xmlFileStream = _fileSystem.GetFileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                {
                    await response.CopyToAsync(xmlFileStream).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Gets the artist data path.
        /// </summary>
        /// <param name="appPaths">The application paths.</param>
        /// <param name="musicBrainzArtistId">The music brainz artist identifier.</param>
        /// <returns>System.String.</returns>
        private static string GetArtistDataPath(IApplicationPaths appPaths, string musicBrainzArtistId)
        {
            var dataPath = Path.Combine(GetArtistDataPath(appPaths), musicBrainzArtistId);

            return dataPath;
        }

        /// <summary>
        /// Gets the artist data path.
        /// </summary>
        /// <param name="appPaths">The application paths.</param>
        /// <returns>System.String.</returns>
        private static string GetArtistDataPath(IApplicationPaths appPaths)
        {
            var dataPath = Path.Combine(appPaths.CachePath, "audiodb-artist");

            return dataPath;
        }

        internal static string GetArtistInfoPath(IApplicationPaths appPaths, string musicBrainzArtistId)
        {
            var dataPath = GetArtistDataPath(appPaths, musicBrainzArtistId);

            return Path.Combine(dataPath, "artist.json");
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
            public object strBiographyCN { get; set; }
            public string strBiographyIT { get; set; }
            public object strBiographyJP { get; set; }
            public object strBiographyRU { get; set; }
            public object strBiographyES { get; set; }
            public object strBiographyPT { get; set; }
            public object strBiographySE { get; set; }
            public object strBiographyNL { get; set; }
            public object strBiographyHU { get; set; }
            public object strBiographyNO { get; set; }
            public object strBiographyIL { get; set; }
            public object strBiographyPL { get; set; }
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

        public class RootObject
        {
            public List<Artist> artists { get; set; }
        }

        public int Order
        {
            get
            {
                // After musicbrainz
                return 1;
            }
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
