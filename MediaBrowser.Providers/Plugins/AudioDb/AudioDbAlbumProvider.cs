#pragma warning disable CS1591

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
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IJsonSerializer _json;

        public AudioDbAlbumProvider(IServerConfigurationManager config, IFileSystem fileSystem, IHttpClientFactory httpClientFactory, IJsonSerializer json)
        {
            _config = config;
            _fileSystem = fileSystem;
            _httpClientFactory = httpClientFactory;
            _json = json;

            Current = this;
        }

        public static AudioDbAlbumProvider Current { get; private set; }

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
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            var result = new MetadataResult<MusicAlbum>();
            var id = info.GetReleaseGroupId();

            if (!string.IsNullOrWhiteSpace(id))
            {
                await EnsureInfo(id, cancellationToken).ConfigureAwait(false);

                var path = GetAlbumInfoPath(_config.ApplicationPaths, id);

                var obj = _json.DeserializeFromFile<RootObject>(path);

                if (obj != null && obj.Album != null && obj.Album.Count > 0)
                {
                    result.Item = new MusicAlbum();
                    result.HasMetadata = true;
                    ProcessResult(result.Item, obj.Album[0], info.MetadataLanguage);
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

        internal async Task DownloadInfo(string musicBrainzReleaseGroupId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = AudioDbArtistProvider.BaseUrl + "/album-mb.php?i=" + musicBrainzReleaseGroupId;

            var path = GetAlbumInfoPath(_config.ApplicationPaths, musicBrainzReleaseGroupId);

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using var response = await _httpClientFactory.CreateClient(NamedClient.Default)
                .GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#pragma warning disable CA2000 // Dispose objects before losing scope: Wrongly identified
            await using var xmlFileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, IODefaults.FileStreamBufferSize, true);
#pragma warning restore CA2000 // Dispose objects before losing scope
            await stream.CopyToAsync(xmlFileStream, cancellationToken).ConfigureAwait(false);
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

        private static void ProcessResult(MusicAlbum item, Album result, string preferredLanguage)
        {
            if (Plugin.Instance.Configuration.ReplaceAlbumName && !string.IsNullOrWhiteSpace(result.AlbumName))
            {
                item.Album = result.AlbumName;
            }

            if (!string.IsNullOrWhiteSpace(result.Artist))
            {
                item.AlbumArtists = new string[] { result.Artist };
            }

            if (!string.IsNullOrEmpty(result.YearReleased))
            {
                item.ProductionYear = int.Parse(result.YearReleased, CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrEmpty(result.Genre))
            {
                item.Genres = new[] { result.Genre };
            }

            item.SetProviderId(MetadataProvider.AudioDbArtist, result.IdArtist);
            item.SetProviderId(MetadataProvider.AudioDbAlbum, result.IdAlbum);

            item.SetProviderId(MetadataProvider.MusicBrainzAlbumArtist, result.MusicBrainzArtistID);
            item.SetProviderId(MetadataProvider.MusicBrainzReleaseGroup, result.MusicBrainzID);

            string overview = null;

            if (string.Equals(preferredLanguage, "de", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.DescriptionDE;
            }
            else if (string.Equals(preferredLanguage, "fr", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.DescriptionFR;
            }
            else if (string.Equals(preferredLanguage, "nl", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.DescriptionNL;
            }
            else if (string.Equals(preferredLanguage, "ru", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.DescriptionRU;
            }
            else if (string.Equals(preferredLanguage, "it", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.DescriptionIT;
            }
            else if ((preferredLanguage ?? string.Empty).StartsWith("pt", StringComparison.OrdinalIgnoreCase))
            {
                overview = result.DescriptionPT;
            }

            if (string.IsNullOrWhiteSpace(overview))
            {
                overview = result.DescriptionEN;
            }

            item.Overview = (overview ?? string.Empty).StripHtml();
        }

        internal class Album
        {
            public string IdAlbum { get; set; }

            public string IdArtist { get; set; }

            public string AlbumName { get; set; }

            public string Artist { get; set; }

            public string YearReleased { get; set; }

            public string Genre { get; set; }

            public string SubGenre { get; set; }

            public string ReleaseFormat { get; set; }

            public string Sales { get; set; }

            public string AlbumThumb { get; set; }

            public string AlbumCDart { get; set; }

            public string DescriptionEN { get; set; }

            public string DescriptionDE { get; set; }

            public string DescriptionFR { get; set; }

            public string DescriptionCN { get; set; }

            public string DescriptionIT { get; set; }

            public string DescriptionJP { get; set; }

            public string DescriptionRU { get; set; }

            public string DescriptionES { get; set; }

            public string DescriptionPT { get; set; }

            public string DescriptionSE { get; set; }

            public string DescriptionNL { get; set; }

            public string DescriptionHU { get; set; }

            public string DescriptionNO { get; set; }

            public string DescriptionIL { get; set; }

            public string DescriptionPL { get; set; }

            public object Loved { get; set; }

            public object Score { get; set; }

            public string Review { get; set; }

            public object Mood { get; set; }

            public object Theme { get; set; }

            public object Speed { get; set; }

            public object Location { get; set; }

            public string MusicBrainzID { get; set; }

            public string MusicBrainzArtistID { get; set; }

            public object ItunesID { get; set; }

            public object AmazonID { get; set; }

            public string Locked { get; set; }
        }

        internal class RootObject
        {
            internal List<Album> Album { get; set; }
        }
    }
}
