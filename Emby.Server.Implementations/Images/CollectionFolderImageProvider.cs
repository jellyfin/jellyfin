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

            if (string.Equals(viewType, CollectionType.Movies, StringComparison.Ordinal))
            {
                includeItemTypes = new[] { BaseItemKind.Movie };
            }
            else if (string.Equals(viewType, CollectionType.TvShows, StringComparison.Ordinal))
            {
                includeItemTypes = new[] { BaseItemKind.Series };
            }
            else if (string.Equals(viewType, CollectionType.Music, StringComparison.Ordinal))
            {
                includeItemTypes = new[] { BaseItemKind.MusicAlbum };
            }
            else if (string.Equals(viewType, CollectionType.MusicVideos, StringComparison.Ordinal))
            {
                includeItemTypes = new[] { BaseItemKind.MusicVideo };
            }
            else if (string.Equals(viewType, CollectionType.Books, StringComparison.Ordinal))
            {
                includeItemTypes = new[] { BaseItemKind.Book, BaseItemKind.AudioBook };
            }
            else if (string.Equals(viewType, CollectionType.BoxSets, StringComparison.Ordinal))
            {
                includeItemTypes = new[] { BaseItemKind.BoxSet };
            }
            else if (string.Equals(viewType, CollectionType.HomeVideos, StringComparison.Ordinal) || string.Equals(viewType, CollectionType.Photos, StringComparison.Ordinal))
            {
                includeItemTypes = new[] { BaseItemKind.Video, BaseItemKind.Photo };
            }
            else
            {
                includeItemTypes = new[] { BaseItemKind.Video, BaseItemKind.Audio, BaseItemKind.Photo, BaseItemKind.Movie, BaseItemKind.Series };
            }

            var recursive = !string.Equals(CollectionType.Playlists, viewType, StringComparison.OrdinalIgnoreCase);

            return view.GetItemList(new InternalItemsQuery
            {
                CollapseBoxSetItems = false,
                Recursive = recursive,
                DtoOptions = new DtoOptions(false),
                ImageTypes = new ImageType[] { ImageType.Primary },
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
