#nullable disable

#pragma warning disable CA1002, CS1591, SA1300

using System;
using System.Collections.Generic;
using System.Globalization;
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
    public class AudioDbAlbumProvider : IRemoteMetadataProvider<MusicAlbum, AlbumInfo>, IHasOrder
    {
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;

#pragma warning disable SA1401, CA2211
        public static AudioDbAlbumProvider Current;
#pragma warning restore SA1401, CA2211

        public AudioDbAlbumProvider(IServerConfigurationManager config, IFileSystem fileSystem, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _fileSystem = fileSystem;
            _httpClientFactory = httpClientFactory;

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
            var id = info.GetReleaseGroupId();

            if (!string.IsNullOrWhiteSpace(id))
            {
                await EnsureInfo(id, cancellationToken).ConfigureAwait(false);

                var path = GetAlbumInfoPath(_config.ApplicationPaths, id);

                FileStream jsonStream = AsyncFile.OpenRead(path);
                await using (jsonStream.ConfigureAwait(false))
                {
                    var obj = await JsonSerializer.DeserializeAsync<RootObject>(jsonStream, _jsonOptions, cancellationToken).ConfigureAwait(false);

                    if (obj is not null && obj.album is not null && obj.album.Count > 0)
                    {
                        result.Item = new MusicAlbum();
                        result.HasMetadata = true;
                        ProcessResult(result.Item, obj.album[0], info.MetadataLanguage);
                    }
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

            item.SetProviderId(MetadataProvider.AudioDbArtist, result.idArtist);
            item.SetProviderId(MetadataProvider.AudioDbAlbum, result.idAlbum);

            item.SetProviderId(MetadataProvider.MusicBrainzAlbumArtist, result.strMusicBrainzArtistID);
            item.SetProviderId(MetadataProvider.MusicBrainzReleaseGroup, result.strMusicBrainzID);

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

        internal async Task EnsureInfo(string musicBrainzReleaseGroupId, CancellationToken cancellationToken)
        {
            var xmlPath = GetAlbumInfoPath(_config.ApplicationPaths, musicBrainzReleaseGroupId);

            var fileInfo = _fileSystem.GetFileSystemInfo(xmlPath);

            if (fileInfo.Exists
                && (DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 2)
            {
                return;
            }

            await DownloadInfo(musicBrainzReleaseGroupId, cancellationToken).ConfigureAwait(false);
        }

        internal async Task DownloadInfo(string musicBrainzReleaseGroupId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = AudioDbArtistProvider.BaseUrl + "/album-mb.php?i=" + musicBrainzReleaseGroupId;

            var path = GetAlbumInfoPath(_config.ApplicationPaths, musicBrainzReleaseGroupId);

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using var response = await _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(url, cancellationToken).ConfigureAwait(false);
            var fileStreamOptions = AsyncFile.WriteOptions;
            fileStreamOptions.Mode = FileMode.Create;
            var fs = new FileStream(path, fileStreamOptions);
            await using (fs.ConfigureAwait(false))
            {
                await response.Content.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
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

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

#pragma warning disable CA1034, CA2227
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
    }
}
