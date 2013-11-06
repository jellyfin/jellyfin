using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
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

        public async Task<IEnumerable<RemoteImageInfo>> GetAllImages(BaseItem item, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();

            // The only info we have is size
            return list.OrderByDescending(i => i.Width ?? 0);
        }

        public int Priority
        {
            get { return 0; }
        }
    }
}
