#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Images
{
    /// <summary>
    /// Class ArtistImageProvider.
    /// </summary>
    public class ArtistImageProvider : BaseDynamicImageProvider<MusicArtist>
    {
        public ArtistImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor)
            : base(fileSystem, providerManager, applicationPaths, imageProcessor)
        {
        }

        /// <summary>
        /// Get children objects used to create an artist image.
        /// </summary>
        /// <param name="item">The artist used to create the image.</param>
        /// <returns>Any relevant children objects.</returns>
        protected override IReadOnlyList<BaseItem> GetItemsWithImages(MusicArtist item)
        {
            return Array.Empty<BaseItem>();

            // TODO enable this when BaseDynamicImageProvider objects are configurable
            // return _libraryManager.GetItemList(new InternalItemsQuery
            // {
            //    ArtistIds = new[] { item.Id },
            //    IncludeItemTypes = new[] { nameof(MusicAlbum) },
            //    OrderBy = new[] { (ItemSortBy.Random, SortOrder.Ascending) },
            //    Limit = 4,
            //    Recursive = true,
            //    ImageTypes = new[] { ImageType.Primary },
            //    DtoOptions = new DtoOptions(false)
            // });
        }
    }
}
