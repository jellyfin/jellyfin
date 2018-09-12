using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Emby.Server.Implementations.Images;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Extensions;

namespace Emby.Server.Implementations.UserViews
{
    public class DynamicImageProvider : BaseDynamicImageProvider<UserView>
    {
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;

        public DynamicImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor, IUserManager userManager, ILibraryManager libraryManager)
            : base(fileSystem, providerManager, applicationPaths, imageProcessor)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
        }

        protected override List<BaseItem> GetItemsWithImages(BaseItem item)
        {
            var view = (UserView)item;

            var isUsingCollectionStrip = IsUsingCollectionStrip(view);
            var recursive = isUsingCollectionStrip && !new[] { CollectionType.BoxSets, CollectionType.Playlists }.Contains(view.ViewType ?? string.Empty, StringComparer.OrdinalIgnoreCase);

            var result = view.GetItemList(new InternalItemsQuery
            {
                User = view.UserId.HasValue ? _userManager.GetUserById(view.UserId.Value) : null,
                CollapseBoxSetItems = false,
                Recursive = recursive,
                ExcludeItemTypes = new[] { "UserView", "CollectionFolder", "Person" },
                DtoOptions = new DtoOptions(false)
            });

            var items = result.Select(i =>
            {
                var episode = i as Episode;
                if (episode != null)
                {
                    var series = episode.Series;
                    if (series != null)
                    {
                        return series;
                    }

                    return episode;
                }

                var season = i as Season;
                if (season != null)
                {
                    var series = season.Series;
                    if (series != null)
                    {
                        return series;
                    }

                    return season;
                }

                var audio = i as Audio;
                if (audio != null)
                {
                    var album = audio.AlbumEntity;
                    if (album != null && album.HasImage(ImageType.Primary))
                    {
                        return album;
                    }
                }

                return i;

            }).DistinctBy(i => i.Id);

            if (isUsingCollectionStrip)
            {
                return items
                    .Where(i => i.HasImage(ImageType.Primary) || i.HasImage(ImageType.Thumb))
                    .OrderBy(i => Guid.NewGuid())
                    .ToList();
            }

            return items
                .Where(i => i.HasImage(ImageType.Primary))
                .OrderBy(i => Guid.NewGuid())
                .ToList();
        }

        protected override bool Supports(BaseItem item)
        {
            var view = item as UserView;
            if (view != null)
            {
                return IsUsingCollectionStrip(view);
            }

            return false;
        }

        private bool IsUsingCollectionStrip(UserView view)
        {
            string[] collectionStripViewTypes =
            {
                CollectionType.Movies,
                CollectionType.TvShows,
                CollectionType.Playlists
            };

            return collectionStripViewTypes.Contains(view.ViewType ?? string.Empty);
        }

        protected override string CreateImage(BaseItem item, List<BaseItem> itemsWithImages, string outputPathWithoutExtension, ImageType imageType, int imageIndex)
        {
            if (itemsWithImages.Count == 0)
            {
                return null;
            }

            var outputPath = Path.ChangeExtension(outputPathWithoutExtension, ".png");

            return CreateThumbCollage(item, itemsWithImages, outputPath, 960, 540);
        }
    }
}
