#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Images
{
    public class CollectionFolderImageProvider : BaseDynamicImageProvider<CollectionFolder>
    {
        public CollectionFolderImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor) : base(fileSystem, providerManager, applicationPaths, imageProcessor)
        {
        }

        protected override IReadOnlyList<BaseItem> GetItemsWithImages(BaseItem item)
        {
            var view = (CollectionFolder)item;
            var viewType = view.CollectionType;

            BaseItemKind[] includeItemTypes;

            switch (viewType)
            {
                case CollectionType.movies:
                    includeItemTypes = new[] { BaseItemKind.Movie };
                    break;
                case CollectionType.tvshows:
                    includeItemTypes = new[] { BaseItemKind.Series };
                    break;
                case CollectionType.music:
                    includeItemTypes = new[] { BaseItemKind.MusicAlbum };
                    break;
                case CollectionType.musicvideos:
                    includeItemTypes = new[] { BaseItemKind.MusicVideo };
                    break;
                case CollectionType.books:
                    includeItemTypes = new[] { BaseItemKind.Book, BaseItemKind.AudioBook };
                    break;
                case CollectionType.boxsets:
                    includeItemTypes = new[] { BaseItemKind.BoxSet };
                    break;
                case CollectionType.homevideos:
                case CollectionType.photos:
                    includeItemTypes = new[] { BaseItemKind.Video, BaseItemKind.Photo };
                    break;
                default:
                    includeItemTypes = new[] { BaseItemKind.Video, BaseItemKind.Audio, BaseItemKind.Photo, BaseItemKind.Movie, BaseItemKind.Series };
                    break;
            }

            var recursive = viewType != CollectionType.playlists;

            return view.GetItemList(new InternalItemsQuery
            {
                CollapseBoxSetItems = false,
                Recursive = recursive,
                DtoOptions = new DtoOptions(false),
                ImageTypes = new[] { ImageType.Primary },
                Limit = 8,
                OrderBy = new[]
                {
                    (ItemSortBy.Random, SortOrder.Ascending)
                },
                IncludeItemTypes = includeItemTypes
            });
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
    }
}
