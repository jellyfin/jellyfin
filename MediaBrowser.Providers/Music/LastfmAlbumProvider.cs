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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Providers.Music
{
    public class LastfmAlbumProvider : LastfmBaseProvider
    {
        internal static LastfmAlbumProvider Current;

        /// <summary>
        /// The _music brainz resource pool
        /// </summary>
        private readonly SemaphoreSlim _musicBrainzResourcePool = new SemaphoreSlim(1, 1);

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
            get { return MetadataProviderPriority.Third; }
        }

        protected override string ProviderVersion
        {
            get
            {
                return "8";
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
        public override async Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var album = (MusicAlbum)item;

            var result = await GetAlbumResult(album, cancellationToken).ConfigureAwait(false);

            if (result != null && result.album != null)
            {
                LastfmHelper.ProcessAlbumData(item, result.album);
            }

            var releaseEntryId = item.GetProviderId(MetadataProviders.Musicbrainz);

            if (!string.IsNullOrEmpty(releaseEntryId))
            {
                var musicBrainzReleaseGroupId = album.GetProviderId(MetadataProviders.MusicBrainzReleaseGroup);
                
                if (string.IsNullOrEmpty(musicBrainzReleaseGroupId))
                {
                    musicBrainzReleaseGroupId = await GetReleaseGroupId(releaseEntryId, cancellationToken).ConfigureAwait(false);

                    album.SetProviderId(MetadataProviders.MusicBrainzReleaseGroup, musicBrainzReleaseGroupId);
                }
            }
            
            BaseProviderInfo data;
            if (!item.ProviderData.TryGetValue(Id, out data))
            {
                data = new BaseProviderInfo();
                item.ProviderData[Id] = data;
            }

            data.FileStamp = GetComparisonData(album);

            SetLastRefreshed(item, DateTime.UtcNow);
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

        /// <summary>
        /// Gets the release group id internal.
        /// </summary>
        /// <param name="releaseEntryId">The release entry id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        private async Task<string> GetReleaseGroupId(string releaseEntryId, CancellationToken cancellationToken)
        {
            var url = string.Format("http://www.musicbrainz.org/ws/2/release-group/?query=reid:{0}", releaseEntryId);

            var doc = await GetMusicBrainzResponse(url, cancellationToken).ConfigureAwait(false);

            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("mb", "http://musicbrainz.org/ns/mmd-2.0#");
            var node = doc.SelectSingleNode("//mb:release-group-list/mb:release-group/@id", ns);

            return node != null ? node.Value : null;
        }
        /// <summary>
        /// The _last music brainz request
        /// </summary>
        private DateTime _lastRequestDate = DateTime.MinValue;

        /// <summary>
        /// Gets the music brainz response.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{XmlDocument}.</returns>
        internal async Task<XmlDocument> GetMusicBrainzResponse(string url, CancellationToken cancellationToken)
        {
            await _musicBrainzResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var diff = 1500 - (DateTime.Now - _lastRequestDate).TotalMilliseconds;

                // MusicBrainz is extremely adamant about limiting to one request per second

                if (diff > 0)
                {
                    await Task.Delay(Convert.ToInt32(diff), cancellationToken).ConfigureAwait(false);
                }

                _lastRequestDate = DateTime.Now;

                var doc = new XmlDocument();

                using (var xml = await HttpClient.Get(new HttpRequestOptions
                {
                    Url = url,
                    CancellationToken = cancellationToken,
                    UserAgent = Environment.MachineName

                }).ConfigureAwait(false))
                {
                    using (var oReader = new StreamReader(xml, Encoding.UTF8))
                    {
                        doc.Load(oReader);
                    }
                }

                return doc;
            }
            finally
            {
                _lastRequestDate = DateTime.Now;

                _musicBrainzResourcePool.Release();
            }
        }
    }
}
