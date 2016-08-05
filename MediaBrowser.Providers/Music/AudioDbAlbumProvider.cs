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
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Providers.Music
{
    public class AudioDbAlbumProvider : IRemoteMetadataProvider<MusicAlbum, AlbumInfo>, IHasOrder
    {
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _json;

        public static AudioDbAlbumProvider Current;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public AudioDbAlbumProvider(IServerConfigurationManager config, IFileSystem fileSystem, IHttpClient httpClient, IJsonSerializer json)
        {
            _config = config;
            _fileSystem = fileSystem;
            _httpClient = httpClient;
            _json = json;

            Current = this;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(AlbumInfo searchInfo, CancellationToken cancellationToken)
        {
            return new List<RemoteSearchResult>();
        }

        public async Task<MetadataResult<MusicAlbum>> GetMetadata(AlbumInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<MusicAlbum>();

            var id = info.GetReleaseGroupId();

            if (!string.IsNullOrWhiteSpace(id))
            {
                await EnsureInfo(id, cancellationToken).ConfigureAwait(false);

                var path = GetAlbumInfoPath(_config.ApplicationPaths, id);

                var obj = _json.DeserializeFromFile<RootObject>(path);

                if (obj != null && obj.album != null && obj.album.Count > 0)
                {
                    result.Item = new MusicAlbum();
                    result.HasMetadata = true;
                    ProcessResult(result.Item, obj.album[0], info.MetadataLanguage);
                }
            }

            return result;
        }

        private void ProcessResult(MusicAlbum item, Album result, string preferredLanguage)
        {
            if (!string.IsNullOrWhiteSpace(result.strArtist))
            {
                item.AlbumArtists = new List<string> { result.strArtist };
            }

            if (!string.IsNullOrEmpty(result.intYearReleased))
            {
                item.ProductionYear = int.Parse(result.intYearReleased, _usCulture);
            }

            if (!string.IsNullOrEmpty(result.strGenre))
            {
                item.Genres = new List<string> { result.strGenre };
            }

            item.SetProviderId(MetadataProviders.AudioDbArtist, result.idArtist);
            item.SetProviderId(MetadataProviders.AudioDbAlbum, result.idAlbum);

            item.SetProviderId(MetadataProviders.MusicBrainzAlbumArtist, result.strMusicBrainzArtistID);
            item.SetProviderId(MetadataProviders.MusicBrainzReleaseGroup, result.strMusicBrainzID);

            string overview = null;

            if (string.Equals(preferredLanguage, "de", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.strDescriptionDE;
            }
            else if (string.Equals(preferredLanguage, "fr", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.strDescriptionFR;
            }
            else if (string.Equals(preferredLanguage, "nl", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.strDescriptionNL;
            }
            else if (string.Equals(preferredLanguage, "ru", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.strDescriptionRU;
            }
            else if (string.Equals(preferredLanguage, "it", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.strDescriptionIT;
            }
            else if ((preferredLanguage ?? string.Empty).StartsWith("pt", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.strDescriptionPT;
            }

            if (string.IsNullOrWhiteSpace(overview))
            {
                overview = result.strDescriptionEN;
            }

            item.Overview = (overview ?? string.Empty).StripHtml();
        }

        public string Name
        {
            get { return "TheAudioDB"; }
        }

        private readonly Task _cachedTask = Task.FromResult(true);
        internal Task EnsureInfo(string musicBrainzReleaseGroupId, CancellationToken cancellationToken)
        {
            var xmlPath = GetAlbumInfoPath(_config.ApplicationPaths, musicBrainzReleaseGroupId);

            var fileInfo = _fileSystem.GetFileSystemInfo(xmlPath);

            if (fileInfo.Exists)
            {
                if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 7)
                {
                    return _cachedTask;
                }
            }

            return DownloadInfo(musicBrainzReleaseGroupId, cancellationToken);
        }

        internal async Task DownloadInfo(string musicBrainzReleaseGroupId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = AudioDbArtistProvider.BaseUrl + "/album-mb.php?i=" + musicBrainzReleaseGroupId;

            var path = GetAlbumInfoPath(_config.ApplicationPaths, musicBrainzReleaseGroupId);

			_fileSystem.CreateDirectory(Path.GetDirectoryName(path));

            using (var response = await _httpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = AudioDbArtistProvider.Current.AudioDbResourcePool,
                CancellationToken = cancellationToken

            }).ConfigureAwait(false))
            {
                using (var xmlFileStream = _fileSystem.GetFileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                {
                    await response.CopyToAsync(xmlFileStream).ConfigureAwait(false);
                }
            }
        }

        private static string GetAlbumDataPath(IApplicationPaths appPaths, string musicBrainzReleaseGroupId)
        {
            var dataPath = Path.Combine(GetAlbumDataPath(appPaths), musicBrainzReleaseGroupId);

            return dataPath;
        }

        private static string GetAlbumDataPath(IApplicationPaths appPaths)
        {
            var dataPath = Path.Combine(appPaths.CachePath, "audiodb-album");

            return dataPath;
        }

        internal static string GetAlbumInfoPath(IApplicationPaths appPaths, string musicBrainzReleaseGroupId)
        {
            var dataPath = GetAlbumDataPath(appPaths, musicBrainzReleaseGroupId);

            return Path.Combine(dataPath, "album.json");
        }

        public int Order
        {
            get
            {
                // After music brainz
                return 1;
            }
        }

        public class Album
        {
            public string idAlbum { get; set; }
            public string idArtist { get; set; }
            public string strAlbum { get; set; }
            public string strArtist { get; set; }
            public string intYearReleased { get; set; }
            public string strGenre { get; set; }
            public string strSubGenre { get; set; }
            public string strReleaseFormat { get; set; }
            public string intSales { get; set; }
            public string strAlbumThumb { get; set; }
            public string strAlbumCDart { get; set; }
            public string strDescriptionEN { get; set; }
            public string strDescriptionDE { get; set; }
            public string strDescriptionFR { get; set; }
            public string strDescriptionCN { get; set; }
            public string strDescriptionIT { get; set; }
            public string strDescriptionJP { get; set; }
            public string strDescriptionRU { get; set; }
            public string strDescriptionES { get; set; }
            public string strDescriptionPT { get; set; }
            public string strDescriptionSE { get; set; }
            public string strDescriptionNL { get; set; }
            public string strDescriptionHU { get; set; }
            public string strDescriptionNO { get; set; }
            public string strDescriptionIL { get; set; }
            public string strDescriptionPL { get; set; }
            public object intLoved { get; set; }
            public object intScore { get; set; }
            public string strReview { get; set; }
            public object strMood { get; set; }
            public object strTheme { get; set; }
            public object strSpeed { get; set; }
            public object strLocation { get; set; }
            public string strMusicBrainzID { get; set; }
            public string strMusicBrainzArtistID { get; set; }
            public object strItunesID { get; set; }
            public object strAmazonID { get; set; }
            public string strLocked { get; set; }
        }

        public class RootObject
        {
            public List<Album> album { get; set; }
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
