#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
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

        protected override IReadOnlyList<BaseItem> GetItemsWithImages(CollectionFolder item)
        {
            var viewType = item.CollectionType;
            BaseItemKind[] includeItemTypes = viewType switch
            {
                CollectionType.movies => [BaseItemKind.Movie],
                CollectionType.tvshows => [BaseItemKind.Series],
                CollectionType.music => [BaseItemKind.MusicAlbum],
                CollectionType.musicvideos => [BaseItemKind.MusicVideo],
                CollectionType.books => [BaseItemKind.Book, BaseItemKind.AudioBook],
                CollectionType.boxsets => [BaseItemKind.BoxSet],
                CollectionType.homevideos or CollectionType.photos => [BaseItemKind.Video, BaseItemKind.Photo],
                _ => [BaseItemKind.Video, BaseItemKind.Audio, BaseItemKind.Photo, BaseItemKind.Movie, BaseItemKind.Series],
            };
            var recursive = viewType != CollectionType.playlists;

            return item.GetItemList(new InternalItemsQuery
            {
                CollapseBoxSetItems = false,
                Recursive = recursive,
                DtoOptions = new DtoOptions(false),
                ImageTypes = [ImageType.Primary],
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
