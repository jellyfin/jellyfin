using System;
using System.Collections.Generic;
using System.Globalization;
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
    public class AudioDbAlbumProvider : IRemoteMetadataProvider<MusicAlbum, AlbumInfo>, IHasOrder
    {
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _json;

        public static AudioDbAlbumProvider Current;

        public AudioDbAlbumProvider(IServerConfigurationManager config, IFileSystem fileSystem, IHttpClient httpClient, IJsonSerializer json)
        {
            _config = config;
            _fileSystem = fileSystem;
            _httpClient = httpClient;
            _json = json;

            Current = this;
        }

        /// <inheritdoc />
        public string Name => "TheAudioDB";

        /// <inheritdoc />
        // After music brainz
        public int Order => 1;

        /// <inheritdoc />
        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(AlbumInfo searchInfo, CancellationToken cancellationToken)
            => Task.FromResult(Enumerable.Empty<RemoteSearchResult>());

        /// <inheritdoc />
        public async Task<MetadataResult<MusicAlbum>> GetMetadata(AlbumInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<MusicAlbum>();

            // TODO maybe remove when artist metadata can be disabled
            if (!Plugin.Instance.Configuration.Enable)
            {
                return result;
            }

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
            if (Plugin.Instance.Configuration.ReplaceAlbumName && !string.IsNullOrWhiteSpace(result.strAlbum))
            {
                item.Album = result.strAlbum;
            }

            if (!string.IsNullOrWhiteSpace(result.strArtist))
            {
                item.AlbumArtists = new string[] { result.strArtist };
            }

            if (!string.IsNullOrEmpty(result.intYearReleased))
            {
                item.ProductionYear = int.Parse(result.intYearReleased, CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrEmpty(result.strGenre))
            {
                item.Genres = new[] { result.strGenre };
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

        internal Task EnsureInfo(string musicBrainzReleaseGroupId, CancellationToken cancellationToken)
        {
            var xmlPath = GetAlbumInfoPath(_config.ApplicationPaths, musicBrainzReleaseGroupId);

            var fileInfo = _fileSystem.GetFileSystemInfo(xmlPath);

            if (fileInfo.Exists)
            {
                if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 2)
                {
                    return Task.CompletedTask;
                }
            }

            return DownloadInfo(musicBrainzReleaseGroupId, cancellationToken);
        }

        internal async Task DownloadInfo(string musicBrainzReleaseGroupId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = AudioDbArtistProvider.BaseUrl + "/album-mb.php?i=" + musicBrainzReleaseGroupId;

            var path = GetAlbumInfoPath(_config.ApplicationPaths, musicBrainzReleaseGroupId);

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (var httpResponse = await _httpClient.SendAsync(
                new HttpRequestOptions
                {
                    Url = url,
                    CancellationToken = cancellationToken
                },
                HttpMethod.Get).ConfigureAwait(false))
            using (var response = httpResponse.Content)
            using (var xmlFileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, IODefaults.FileStreamBufferSize, true))
            {
                await response.CopyToAsync(xmlFileStream).ConfigureAwait(false);
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

        /// <inheritdoc />
        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
