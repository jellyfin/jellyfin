using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Music
{
    public class AlbumImageFromSongProvider : IDynamicImageProvider
    {
        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new List<ImageType> { ImageType.Primary };
        }

        public Task<DynamicImageResponse> GetImage(IHasImages item, ImageType type, CancellationToken cancellationToken)
        {
            var album = (MusicAlbum)item;

            var image = album.RecursiveChildren.OfType<Audio>()
                .Select(i => i.GetImagePath(type))
                .FirstOrDefault(i => !string.IsNullOrEmpty(i));

            return Task.FromResult(new DynamicImageResponse
            {
                Path = image,
                HasImage = !string.IsNullOrEmpty(image)
            });
        }

        public string Name
        {
            get { return "Embedded Image"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is MusicAlbum;
        }
    }
}
