using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Emby.Server.Implementations.Images;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Dto;

namespace Emby.Server.Implementations.Photos
{
    public abstract class BaseFolderImageProvider<T> : BaseDynamicImageProvider<T>
        where T : Folder, new ()
    {
        protected ILibraryManager _libraryManager;

        public BaseFolderImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor, ILibraryManager libraryManager)
            : base(fileSystem, providerManager, applicationPaths, imageProcessor)
        {
            _libraryManager = libraryManager;
        }

        protected override List<BaseItem> GetItemsWithImages(IHasMetadata item)
        {
            return _libraryManager.GetItemList(new InternalItemsQuery
            {
                Parent = item as BaseItem,
                GroupByPresentationUniqueKey = false,
                DtoOptions = new DtoOptions(true),
                ImageTypes = new ImageType[] { ImageType.Primary }
            });
        }

        protected override string CreateImage(IHasMetadata item, List<BaseItem> itemsWithImages, string outputPathWithoutExtension, ImageType imageType, int imageIndex)
        {
            return CreateSingleImage(itemsWithImages, outputPathWithoutExtension, ImageType.Primary);
        }

        protected override bool Supports(IHasMetadata item)
        {
            if (item is PhotoAlbum || item is MusicAlbum)
            {
                return true;
            }

            if (item.GetType() == typeof(Folder))
            {
                var folder = item as Folder;
                if (folder.IsTopParent)
                {
                    return false;
                }
                return true;
            }

            return false;
        }

        protected override bool HasChangedByDate(IHasMetadata item, ItemImageInfo image)
        {
            if (item is MusicAlbum)
            {
                return false;
            }

            return base.HasChangedByDate(item, image);
        }
    }

    public class FolderImageProvider : BaseFolderImageProvider<Folder>
    {
        public FolderImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor, ILibraryManager libraryManager)
            : base(fileSystem, providerManager, applicationPaths, imageProcessor, libraryManager)
        {
        }
    }

    public class MusicAlbumImageProvider : BaseFolderImageProvider<MusicAlbum>
    {
        public MusicAlbumImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor, ILibraryManager libraryManager)
            : base(fileSystem, providerManager, applicationPaths, imageProcessor, libraryManager)
        {
        }
    }

    public class PhotoAlbumImageProvider : BaseFolderImageProvider<PhotoAlbum>
    {
        public PhotoAlbumImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor, ILibraryManager libraryManager)
            : base(fileSystem, providerManager, applicationPaths, imageProcessor, libraryManager)
        {
        }
    }
}
