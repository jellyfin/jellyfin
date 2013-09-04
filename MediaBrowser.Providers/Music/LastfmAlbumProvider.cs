using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MoreLinq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Music
{
    public class LastfmAlbumProvider : LastfmBaseProvider
    {
        private static readonly Task<string> BlankId = Task.FromResult("");

        public LastfmAlbumProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager)
            : base(jsonSerializer, httpClient, logManager, configurationManager)
        {
        }

        protected override Task<string> FindId(BaseItem item, CancellationToken cancellationToken)
        {
            // We don't fetch by id
            return BlankId;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Third; }
        }

        private bool HasAltMeta(BaseItem item)
        {
            return item.LocationType == LocationType.FileSystem && item.ResolveArgs.ContainsMetaFileByName("album.xml");
        }
        
        /// <summary>
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (HasAltMeta(item))
            {
                return false;
            }

            // If song metadata has changed and we don't have an mbid, refresh
            if (string.IsNullOrEmpty(item.GetProviderId(MetadataProviders.Musicbrainz)) &&
                GetComparisonData(item as MusicAlbum) != providerInfo.FileStamp)
            {
                return true;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }

        protected override async Task FetchLastfmData(BaseItem item, string id, CancellationToken cancellationToken)
        {
            var album = (MusicAlbum)item;

            var result = await GetAlbumResult(album, cancellationToken).ConfigureAwait(false);

            if (result != null && result.album != null)
            {
                LastfmHelper.ProcessAlbumData(item, result.album);
            }

            BaseProviderInfo data;
            if (!item.ProviderData.TryGetValue(Id, out data))
            {
                data = new BaseProviderInfo();
                item.ProviderData[Id] = data;
            }

            data.FileStamp = GetComparisonData(album);
        }

        private async Task<LastfmGetAlbumResult> GetAlbumResult(MusicAlbum item, CancellationToken cancellationToken)
        {
            // Try album release Id
            if (!string.IsNullOrEmpty(item.GetProviderId(MetadataProviders.Musicbrainz)))
            {
                var result = await GetAlbumResult(item.GetProviderId(MetadataProviders.Musicbrainz), cancellationToken).ConfigureAwait(false);

                if (result != null && result.album != null)
                {
                    return result;
                }
            }

            // Try album release group Id
            if (!string.IsNullOrEmpty(item.GetProviderId(MetadataProviders.MusicBrainzReleaseGroup)))
            {
                var result = await GetAlbumResult(item.GetProviderId(MetadataProviders.MusicBrainzReleaseGroup), cancellationToken).ConfigureAwait(false);

                if (result != null && result.album != null)
                {
                    return result;
                }
            }
            
            // Get each song, distinct by the combination of AlbumArtist and Album
            var songs = item.RecursiveChildren.OfType<Audio>().DistinctBy(i => (i.AlbumArtist ?? string.Empty) + (i.Album ?? string.Empty), StringComparer.OrdinalIgnoreCase).ToList();

            foreach (var song in songs.Where(song => !string.IsNullOrEmpty(song.Album) && !string.IsNullOrEmpty(song.AlbumArtist)))
            {
                var result = await GetAlbumResult(song.AlbumArtist, song.Album, cancellationToken).ConfigureAwait(false);

                if (result != null && result.album != null)
                {
                    return result;
                }
            }

            // Try the folder name
            return await GetAlbumResult(item.Parent.Name, item.Name, cancellationToken);
        }

        private async Task<LastfmGetAlbumResult> GetAlbumResult(string artist, string album, CancellationToken cancellationToken)
        {
            // Get albu info using artist and album name
            var url = RootUrl + string.Format("method=album.getInfo&artist={0}&album={1}&api_key={2}&format=json", UrlEncode(artist), UrlEncode(album), ApiKey);

            using (var json = await HttpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = LastfmResourcePool,
                CancellationToken = cancellationToken,
                EnableHttpCompression = false

            }).ConfigureAwait(false))
            {
                return JsonSerializer.DeserializeFromStream<LastfmGetAlbumResult>(json);
            }
        }

        private async Task<LastfmGetAlbumResult> GetAlbumResult(string musicbraizId, CancellationToken cancellationToken)
        {
            // Get albu info using artist and album name
            var url = RootUrl + string.Format("method=album.getInfo&mbid={0}&api_key={1}&format=json", musicbraizId, ApiKey);

            using (var json = await HttpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = LastfmResourcePool,
                CancellationToken = cancellationToken,
                EnableHttpCompression = false

            }).ConfigureAwait(false))
            {
                return JsonSerializer.DeserializeFromStream<LastfmGetAlbumResult>(json);
            }
        }
        
        protected override Task FetchData(BaseItem item, CancellationToken cancellationToken)
        {
            return FetchLastfmData(item, string.Empty, cancellationToken);
        }

        public override bool Supports(BaseItem item)
        {
            return item is MusicAlbum;
        }

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <param name="album">The album.</param>
        /// <returns>Guid.</returns>
        private Guid GetComparisonData(MusicAlbum album)
        {
            var songs = album.RecursiveChildren.OfType<Audio>().ToList();

            var albumArtists = songs.Select(i => i.AlbumArtist)
                .Where(i => !string.IsNullOrEmpty(i))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var albumNames = songs.Select(i => i.AlbumArtist)
                .Where(i => !string.IsNullOrEmpty(i))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            albumArtists.AddRange(albumNames);

            return string.Join(string.Empty, albumArtists.OrderBy(i => i).ToArray()).GetMD5();
        }
    }
}
