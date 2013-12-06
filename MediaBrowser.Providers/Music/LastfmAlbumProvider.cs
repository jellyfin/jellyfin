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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Music
{
    public class LastfmAlbumProvider : LastfmBaseProvider
    {
        internal static LastfmAlbumProvider Current;

        public LastfmAlbumProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager)
            : base(jsonSerializer, httpClient, logManager, configurationManager)
        {
            Current = this;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Fourth; }
        }

        protected override string ProviderVersion
        {
            get
            {
                return "9";
            }
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
            var hasId = !string.IsNullOrEmpty(item.GetProviderId(MetadataProviders.Musicbrainz)) &&
                        !string.IsNullOrEmpty(item.GetProviderId(MetadataProviders.MusicBrainzReleaseGroup));

            if (hasId && HasAltMeta(item))
            {
                return false;
            }

            // If song metadata has changed and we don't have an mbid, refresh
            if (!hasId && GetComparisonData(item as MusicAlbum) != providerInfo.FileStamp)
            {
                return true;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var album = (MusicAlbum)item;

            var result = await GetAlbumResult(album, cancellationToken).ConfigureAwait(false);

            if (result != null && result.album != null)
            {
                LastfmHelper.ProcessAlbumData(item, result.album);
            }

            providerInfo.FileStamp = GetComparisonData(album);

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
            return true;
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
                using (var reader = new StreamReader(json))
                {
                    var jsonText = await reader.ReadToEndAsync().ConfigureAwait(false);

                    // Fix their bad json
                    jsonText = jsonText.Replace("\"#text\"", "\"url\"");

                    return JsonSerializer.DeserializeFromString<LastfmGetAlbumResult>(jsonText);
                }
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
