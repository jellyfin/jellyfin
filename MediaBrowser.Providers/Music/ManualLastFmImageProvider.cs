using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Music
{
    public class ManualLastFmImageProvider : IImageProvider
    {
        public string Name
        {
            get { return ProviderName; }
        }

        public static string ProviderName
        {
            get { return "last.fm"; }
        }

        public bool Supports(BaseItem item)
        {
            return item is MusicAlbum || item is MusicArtist || item is Artist;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, ImageType imageType, CancellationToken cancellationToken)
        {
            var images = await GetAllImages(item, cancellationToken).ConfigureAwait(false);

            return images.Where(i => i.Type == imageType);
        }

        public Task<IEnumerable<RemoteImageInfo>> GetAllImages(BaseItem item, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();

            RemoteImageInfo info = null;

            var artist = item as Artist;

            if (artist != null)
            {
                info = GetInfo(artist.LastFmImageUrl, artist.LastFmImageSize);
            }

            var album = item as MusicAlbum;
            if (album != null)
            {
                info = GetInfo(album.LastFmImageUrl, album.LastFmImageSize);
            }

            var musicArtist = item as MusicArtist;
            if (musicArtist != null)
            {
                info = GetInfo(musicArtist.LastFmImageUrl, musicArtist.LastFmImageSize);
            }

            if (info != null)
            {
                list.Add(info);
            }

            // The only info we have is size
            return Task.FromResult<IEnumerable<RemoteImageInfo>>(list.OrderByDescending(i => i.Width ?? 0));
        }

        private RemoteImageInfo GetInfo(string url, string size)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            var info = new RemoteImageInfo
            {
                ProviderName = Name,
                Url = url
            };

            if (string.Equals(size, "mega", StringComparison.OrdinalIgnoreCase))
            {
                
            }
            else if (string.Equals(size, "extralarge", StringComparison.OrdinalIgnoreCase))
            {

            }
            else if (string.Equals(size, "large", StringComparison.OrdinalIgnoreCase))
            {

            }
            else if (string.Equals(size, "medium", StringComparison.OrdinalIgnoreCase))
            {

            }

            return info;
        }

        public int Priority
        {
            get { return 0; }
        }
    }
}
