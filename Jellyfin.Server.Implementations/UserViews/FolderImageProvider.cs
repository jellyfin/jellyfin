using System.Collections.Generic;
using Jellyfin.Server.Implementations.Images;
using Jellyfin.Common.Configuration;
using Jellyfin.Controller.Drawing;
using Jellyfin.Controller.Dto;
using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Entities.Audio;
using Jellyfin.Controller.Library;
using Jellyfin.Controller.Providers;
using Jellyfin.Model.Entities;
using Jellyfin.Model.IO;
using Jellyfin.Model.Querying;

namespace Jellyfin.Server.Implementations.UserViews
{
    public abstract class BaseFolderImageProvider<T> : BaseDynamicImageProvider<T>
        where T : Folder, new()
    {
        protected ILibraryManager _libraryManager;

        public BaseFolderImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor, ILibraryManager libraryManager)
            : base(fileSystem, providerManager, applicationPaths, imageProcessor)
        {
            _libraryManager = libraryManager;
        }

        protected override IReadOnlyList<BaseItem> GetItemsWithImages(BaseItem item)
        {
            return _libraryManager.GetItemList(new InternalItemsQuery
            {
                Parent = item,
                DtoOptions = new DtoOptions(true),
                ImageTypes = new ImageType[] { ImageType.Primary },
                OrderBy = new System.ValueTuple<string, SortOrder>[]
                {
                    new System.ValueTuple<string, SortOrder>(ItemSortBy.IsFolder, SortOrder.Ascending),
                    new System.ValueTuple<string, SortOrder>(ItemSortBy.SortName, SortOrder.Ascending)
                },
                Limit = 1
            });
        }

        protected override string CreateImage(BaseItem item, IReadOnlyCollection<BaseItem> itemsWithImages, string outputPathWithoutExtension, ImageType imageType, int imageIndex)
        {
            return CreateSingleImage(itemsWithImages, outputPathWithoutExtension, ImageType.Primary);
        }

        protected override bool Supports(BaseItem item)
        {
            return item is T;
        }

        protected override bool HasChangedByDate(BaseItem item, ItemImageInfo image)
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

        protected override bool Supports(BaseItem item)
        {
            if (item is PhotoAlbum || item is MusicAlbum)
            {
                return false;
            }

            var folder = item as Folder;
            if (folder != null)
            {
                if (folder.IsTopParent)
                {
                    return false;
                }
            }
            return true;
            //return item.SourceType == SourceType.Library;
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
