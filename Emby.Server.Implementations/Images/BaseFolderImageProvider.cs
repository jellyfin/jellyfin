#pragma warning disable CS1591

using System.Collections.Generic;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Images
{
    public abstract class BaseFolderImageProvider<T> : BaseDynamicImageProvider<T>
        where T : Folder, new()
    {
        private readonly ILibraryManager _libraryManager;

        protected BaseFolderImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor, ILibraryManager libraryManager)
            : base(fileSystem, providerManager, applicationPaths, imageProcessor)
        {
            _libraryManager = libraryManager;
        }

        protected override IReadOnlyList<BaseItem> GetItemsWithImages(BaseItem item)
        {
            return _libraryManager.GetItemList(new InternalItemsQuery
            {
                Parent = item,
                Recursive = true,
                DtoOptions = new DtoOptions(true),
                ImageTypes = [ImageType.Primary],
                OrderBy =
                [
                    (ItemSortBy.IsFolder, SortOrder.Ascending),
                    (ItemSortBy.SortName, SortOrder.Ascending)
                ],
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
}
