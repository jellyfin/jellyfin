using System;
using System.Collections.Generic;
using System.Linq;
using Emby.Server.Implementations.Images;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Images
{
    public class ArtistImageProvider : BaseDynamicImageProvider<MusicArtist>
    {
        private readonly ILibraryManager _libraryManager;

        public ArtistImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor, ILibraryManager libraryManager) : base(fileSystem, providerManager, applicationPaths, imageProcessor)
        {
            _libraryManager = libraryManager;
        }

        protected override IReadOnlyList<BaseItem> GetItemsWithImages(BaseItem item)
        {
            return Array.Empty<BaseItem>();

            // TODO enable this when BaseDynamicImageProvider objects are configurable
            // return _libraryManager.GetItemList(new InternalItemsQuery
            // {
            //    ArtistIds = new[] { item.Id },
            //    IncludeItemTypes = new[] { typeof(MusicAlbum).Name },
            //    OrderBy = new[] { (ItemSortBy.Random, SortOrder.Ascending) },
            //    Limit = 4,
            //    Recursive = true,
            //    ImageTypes = new[] { ImageType.Primary },
            //    DtoOptions = new DtoOptions(false)
            // });
        }
    }
}
