#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
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
    public class AlbumProvider : IRemoteMetadataProvider<MusicAlbum, AlbumInfo>, IHasOrder
    {
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.GetOptions();

        public AlbumProvider(IServerConfigurationManager config, IFileSystem fileSystem, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _fileSystem = fileSystem;
            _httpClientFactory = httpClientFactory;

            Current = this;
        }

        public static AlbumProvider Current { get; private set; }

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

                await using FileStream jsonStream = File.OpenRead(path);
                var obj = await JsonSerializer.DeserializeAsync<RootObject>(jsonStream, _jsonOptions, cancellationToken).ConfigureAwait(false);

                if (obj?.Album?.Count > 0)
                {
                    result.Item = new MusicAlbum();
                    result.HasMetadata = true;
                    ProcessResult(result.Item, obj.Album[0], info?.MetadataLanguage);
                }
            }

            return result;
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        internal static string GetAlbumInfoPath(IApplicationPaths appPaths, string musicBrainzReleaseGroupId)
        {
            var dataPath = GetAlbumDataPath(appPaths, musicBrainzReleaseGroupId);

            return Path.Combine(dataPath, "album.json");
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

            var url = ArtistProvider.BaseUrl + "/album-mb.php?i=" + musicBrainzReleaseGroupId;

            var path = GetAlbumInfoPath(_config.ApplicationPaths, musicBrainzReleaseGroupId);

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using var response = await _httpClientFactory.CreateClient(NamedClient.Default)
                                                         .GetAsync(new Uri(url), cancellationToken)
                                                         .ConfigureAwait(false);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var xmlFileStream = new FileStream(
                path, FileMode.Create, FileAccess.Write, FileShare.Read, IODefaults.FileStreamBufferSize, true);
            await stream.CopyToAsync(xmlFileStream, cancellationToken).ConfigureAwait(false);
        }

        private static void ProcessResult(MusicAlbum item, Album result, string preferredLanguage)
        {
            if (Plugin.Instance.Configuration.ReplaceAlbumName && !string.IsNullOrWhiteSpace(result.StrAlbum))
            {
                item.Album = result.StrAlbum;
            }

            if (!string.IsNullOrWhiteSpace(result.StrArtist))
            {
                item.AlbumArtists = new string[] { result.StrArtist };
            }

            if (!string.IsNullOrEmpty(result.IntYearReleased))
            {
                item.ProductionYear = int.Parse(result.IntYearReleased, CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrEmpty(result.StrGenre))
            {
                item.Genres = new[] { result.StrGenre };
            }

            item.SetProviderId(MetadataProvider.AudioDbArtist, result.IdArtist);
            item.SetProviderId(MetadataProvider.AudioDbAlbum, result.IdAlbum);

            item.SetProviderId(MetadataProvider.MusicBrainzAlbumArtist, result.StrMusicBrainzArtistID);
            item.SetProviderId(MetadataProvider.MusicBrainzReleaseGroup, result.StrMusicBrainzID);

            string overview = null;

            if (string.Equals(preferredLanguage, "de", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.StrDescriptionDE;
            }
            else if (string.Equals(preferredLanguage, "fr", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.StrDescriptionFR;
            }
            else if (string.Equals(preferredLanguage, "nl", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.StrDescriptionNL;
            }
            else if (string.Equals(preferredLanguage, "ru", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.StrDescriptionRU;
            }
            else if (string.Equals(preferredLanguage, "it", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.StrDescriptionIT;
            }
            else if ((preferredLanguage ?? string.Empty).StartsWith("pt", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.StrDescriptionPT;
            }

            if (string.IsNullOrWhiteSpace(overview))
            {
                overview = result.StrDescriptionEN;
            }

            item.Overview = (overview ?? string.Empty).StripHtml();
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

        protected internal class Album
        {
            public string IdAlbum { get; set; }

            public string IdArtist { get; set; }

            public string StrAlbum { get; set; }

            public string StrArtist { get; set; }

            public string IntYearReleased { get; set; }

            public string StrGenre { get; set; }

            public string StrSubGenre { get; set; }

            public string StrReleaseFormat { get; set; }

            public string IntSales { get; set; }

            public string StrAlbumThumb { get; set; }

            public string StrAlbumCDart { get; set; }

            public string StrDescriptionEN { get; set; }

            public string StrDescriptionDE { get; set; }

            public string StrDescriptionFR { get; set; }

            public string StrDescriptionCN { get; set; }

            public string StrDescriptionIT { get; set; }

            public string StrDescriptionJP { get; set; }

            public string StrDescriptionRU { get; set; }

            public string StrDescriptionES { get; set; }

            public string StrDescriptionPT { get; set; }

            public string StrDescriptionSE { get; set; }

            public string StrDescriptionNL { get; set; }

            public string StrDescriptionHU { get; set; }

            public string StrDescriptionNO { get; set; }

            public string StrDescriptionIL { get; set; }

            public string StrDescriptionPL { get; set; }

            public object IntLoved { get; set; }

            public object IntScore { get; set; }

            public string StrReview { get; set; }

            public object StrMood { get; set; }

            public object StrTheme { get; set; }

            public object StrSpeed { get; set; }

            public object StrLocation { get; set; }

            public string StrMusicBrainzID { get; set; }

            public string StrMusicBrainzArtistID { get; set; }

            public object StrItunesID { get; set; }

            public object StrAmazonID { get; set; }

            public string StrLocked { get; set; }
        }

        protected internal class RootObject
        {
            public List<Album> Album { get; set; }
        }
    }
}
