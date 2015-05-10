using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Photos
{
    //public class PhotoAlbumImageProvider : IDynamicImageProvider
    //{
    //    public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
    //    {
    //        return new List<ImageType> { ImageType.Primary };
    //    }

    //    public Task<DynamicImageResponse> GetImage(IHasImages item, ImageType type, CancellationToken cancellationToken)
    //    {
    //        var album = (PhotoAlbum)item;

    //        var image = album.Children
    //            .OfType<Photo>()
    //            .Select(i => i.GetImagePath(type))
    //            .FirstOrDefault(i => !string.IsNullOrEmpty(i));

    //        return Task.FromResult(new DynamicImageResponse
    //        {
    //            Path = image,
    //            HasImage = !string.IsNullOrEmpty(image)
    //        });
    //    }

    //    public string Name
    //    {
    //        get { return "Image Extractor"; }
    //    }

    //    public bool Supports(IHasImages item)
    //    {
    //        return item is PhotoAlbum;
    //    }
    //}
}
