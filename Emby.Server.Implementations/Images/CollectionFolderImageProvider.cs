#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Api.Extensions;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Images
{
    public class CollectionFolderImageProvider : BaseDynamicImageProvider<CollectionFolder>
    {
        private readonly ILibraryManager _libraryManager;

        public CollectionFolderImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor, ILibraryManager libraryManager) : base(fileSystem, providerManager, applicationPaths, imageProcessor)
        {
            _libraryManager = libraryManager;
        }

        protected override IReadOnlyList<BaseItem> GetItemsWithImages(BaseItem item)
        {
            var view = (CollectionFolder)item;
            var viewType = view.CollectionType;
            var includeItemTypes = DtoExtensions.GetBaseItemKindsForCollectionType(viewType);
            var recursive = viewType != CollectionType.playlists;

            if (viewType == CollectionType.music)
            {
                // Music albums usually don't have dedicated backdrops, so use artist instead.
                // Artists carry no library of their own, so an item query for them is not
                // restricted to this library and would collage the artists of every music
                // library. Resolve them through the tracks that credit them instead.
                return GetArtistsWithImages(view);
            }

            return view.GetItemList(new InternalItemsQuery
            {
                CollapseBoxSetItems = false,
                Recursive = recursive,
                DtoOptions = new DtoOptions(false),
                ImageTypes = [ImageType.Primary],
                Limit = 8,
                OrderBy = [(ItemSortBy.Random, SortOrder.Ascending)],
                IncludeItemTypes = includeItemTypes
            });
        }

        private IReadOnlyList<BaseItem> GetArtistsWithImages(CollectionFolder view)
        {
            return _libraryManager.GetAllArtists(new InternalItemsQuery
            {
                AncestorIds = [view.Id],
                DtoOptions = new DtoOptions(false),
                EnableTotalRecordCount = false,
                ImageTypes = [ImageType.Primary],
                Limit = 8,
                OrderBy = [(ItemSortBy.Random, SortOrder.Ascending)]
            }).Items.Select(i => i.Item).ToArray();
        }

        protected override bool Supports(BaseItem item)
        {
            return item is CollectionFolder;
        }

        protected override string CreateImage(BaseItem item, IReadOnlyCollection<BaseItem> itemsWithImages, string outputPathWithoutExtension, ImageType imageType, int imageIndex)
        {
            var outputPath = Path.ChangeExtension(outputPathWithoutExtension, ".png");

            if (imageType == ImageType.Primary)
            {
                if (itemsWithImages.Count == 0)
                {
                    return null;
                }

                return CreateThumbCollage(item, itemsWithImages, outputPath, 960, 540);
            }

            return base.CreateImage(item, itemsWithImages, outputPath, imageType, imageIndex);
        }

        protected override bool HasChangedByDate(BaseItem item, ItemImageInfo image)
        {
            var age = DateTime.UtcNow - image.DateModified;
            return age.TotalDays > 7;
        }
    }
}
