#nullable disable

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
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Images
{
    /// <summary>
    /// Class MusicGenreImageProvider.
    /// </summary>
    public class MusicGenreImageProvider : BaseDynamicImageProvider<MusicGenre>
    {
        /// <summary>
        /// The library manager.
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        public MusicGenreImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor, ILibraryManager libraryManager) : base(fileSystem, providerManager, applicationPaths, imageProcessor)
        {
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Get children objects used to create an music genre image.
        /// </summary>
        /// <param name="item">The music genre used to create the image.</param>
        /// <returns>Any relevant children objects.</returns>
        protected override IReadOnlyList<BaseItem> GetItemsWithImages(BaseItem item)
        {
            return _libraryManager.GetItemList(new InternalItemsQuery
            {
                Genres = new[] { item.Name },
                IncludeItemTypes = new[]
                {
                    BaseItemKind.MusicAlbum,
                    BaseItemKind.MusicVideo,
                    BaseItemKind.Audio
                },
                OrderBy = new[] { (ItemSortBy.Random, SortOrder.Ascending) },
                Limit = 4,
                Recursive = true,
                ImageTypes = new[] { ImageType.Primary },
                DtoOptions = new DtoOptions(false)
            });
        }
    }
}
