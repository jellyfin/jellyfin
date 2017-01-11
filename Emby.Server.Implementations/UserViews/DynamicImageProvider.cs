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

        public override IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            var view = (UserView)item;
            if (IsUsingCollectionStrip(view))
            {
                return new List<ImageType>
                {
                    ImageType.Primary
                };
            }

            return new List<ImageType>
            {
                ImageType.Primary
            };
        }

        protected override async Task<List<BaseItem>> GetItemsWithImages(IHasImages item)
        {
            var view = (UserView)item;

            if (string.Equals(view.ViewType, CollectionType.LiveTv, StringComparison.OrdinalIgnoreCase))
            {
                var programs = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { typeof(LiveTvProgram).Name },
                    ImageTypes = new[] { ImageType.Primary },
                    Limit = 30,
                    IsMovie = true
                }).ToList();

                return GetFinalItems(programs).ToList();
            }

            if (string.Equals(view.ViewType, SpecialFolder.MovieGenre, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(view.ViewType, SpecialFolder.TvGenre, StringComparison.OrdinalIgnoreCase))
            {
                var userItemsResult = await view.GetItems(new InternalItemsQuery
                {
                    CollapseBoxSetItems = false
                });

                return userItemsResult.Items.ToList();
            }

            var isUsingCollectionStrip = IsUsingCollectionStrip(view);
            var recursive = isUsingCollectionStrip && !new[] { CollectionType.Channels, CollectionType.BoxSets, CollectionType.Playlists }.Contains(view.ViewType ?? string.Empty, StringComparer.OrdinalIgnoreCase);

            var result = await view.GetItems(new InternalItemsQuery
            {
                User = view.UserId.HasValue ? _userManager.GetUserById(view.UserId.Value) : null,
                CollapseBoxSetItems = false,
                Recursive = recursive,
                ExcludeItemTypes = new[] { "UserView", "CollectionFolder", "Person" },

            }).ConfigureAwait(false);

            var items = result.Items.Select(i =>
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
                return GetFinalItems(items.Where(i => i.HasImage(ImageType.Primary) || i.HasImage(ImageType.Thumb)).ToList(), 8);
            }

            return GetFinalItems(items.Where(i => i.HasImage(ImageType.Primary)).ToList());
        }

        protected override bool Supports(IHasImages item)
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
                CollectionType.Playlists,
                CollectionType.Photos
            };

            return collectionStripViewTypes.Contains(view.ViewType ?? string.Empty);
        }

        protected override async Task<string> CreateImage(IHasImages item, List<BaseItem> itemsWithImages, string outputPathWithoutExtension, ImageType imageType, int imageIndex)
        {
            if (itemsWithImages.Count == 0)
            {
                return null;
            }

            var outputPath = Path.ChangeExtension(outputPathWithoutExtension, ".png");

            return await CreateThumbCollage(item, itemsWithImages, outputPath, 960, 540).ConfigureAwait(false);
        }
    }
}
