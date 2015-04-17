using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Server.Implementations.Photos;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.UserViews
{
    public class DynamicImageProvider : BaseDynamicImageProvider<UserView>, IPreRefreshProvider
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
                ImageType.Primary,
                ImageType.Thumb
            };
        }

        protected override async Task<List<BaseItem>> GetItemsWithImages(IHasImages item)
        {
            var view = (UserView)item;

            if (!view.UserId.HasValue)
            {
                return new List<BaseItem>();
            }

            if (string.Equals(view.ViewType, CollectionType.LiveTv, StringComparison.OrdinalIgnoreCase))
            {
                return new List<BaseItem>();
            }

            if (string.Equals(view.ViewType, SpecialFolder.GameGenre, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(view.ViewType, SpecialFolder.MusicGenre, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(view.ViewType, SpecialFolder.MovieGenre, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(view.ViewType, SpecialFolder.TvGenre, StringComparison.OrdinalIgnoreCase))
            {
                var userItemsResult = await view.GetItems(new InternalItemsQuery
                {
                    User = _userManager.GetUserById(view.UserId.Value),
                    CollapseBoxSetItems = false
                });

                return userItemsResult.Items.ToList();
            }

            var isUsingCollectionStrip = IsUsingCollectionStrip(view);
            var recursive = isUsingCollectionStrip && !new[] { CollectionType.Playlists, CollectionType.Channels }.Contains(view.ViewType ?? string.Empty, StringComparer.OrdinalIgnoreCase);

            var result = await view.GetItems(new InternalItemsQuery
            {
                User = _userManager.GetUserById(view.UserId.Value),
                CollapseBoxSetItems = false,
                Recursive = recursive,
                ExcludeItemTypes = new[] { "UserView", "CollectionFolder", "Playlist" }

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
                    var episodeSeason = episode.Season;
                    if (episodeSeason != null)
                    {
                        return episodeSeason;
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
                    var album = audio.FindParent<MusicAlbum>();
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

        public override bool Supports(IHasImages item)
        {
            var view = item as UserView;

            if (view != null && view.UserId.HasValue)
            {
                var supported = new[]
                {
                    SpecialFolder.TvFavoriteEpisodes,
                    SpecialFolder.TvFavoriteSeries,
                    SpecialFolder.TvGenres,
                    SpecialFolder.TvGenre,
                    SpecialFolder.TvLatest,
                    SpecialFolder.TvNextUp,
                    SpecialFolder.TvResume,
                    SpecialFolder.TvShowSeries,

                    SpecialFolder.MovieCollections,
                    SpecialFolder.MovieFavorites,
                    SpecialFolder.MovieGenres,
                    SpecialFolder.MovieGenre,
                    SpecialFolder.MovieLatest,
                    SpecialFolder.MovieMovies,
                    SpecialFolder.MovieResume,

                    SpecialFolder.GameFavorites,
                    SpecialFolder.GameGenres,
                    SpecialFolder.GameGenre,
                    SpecialFolder.GameSystems,
                    SpecialFolder.LatestGames,
                    SpecialFolder.RecentlyPlayedGames,

                    SpecialFolder.MusicArtists,
                    SpecialFolder.MusicAlbumArtists,
                    SpecialFolder.MusicAlbums,
                    SpecialFolder.MusicGenres,
                    SpecialFolder.MusicGenre,
                    SpecialFolder.MusicLatest,
                    SpecialFolder.MusicPlaylists,
                    SpecialFolder.MusicSongs,
                    SpecialFolder.MusicFavorites,
                    SpecialFolder.MusicFavoriteArtists,
                    SpecialFolder.MusicFavoriteAlbums,
                    SpecialFolder.MusicFavoriteSongs
                };

                return (IsUsingCollectionStrip(view) || supported.Contains(view.ViewType, StringComparer.OrdinalIgnoreCase)) &&
                    _userManager.GetUserById(view.UserId.Value) != null;
            }

            return false;
        }

        private bool IsUsingCollectionStrip(UserView view)
        {
            string[] collectionStripViewTypes =
            {
                CollectionType.Movies,
                CollectionType.TvShows,
                CollectionType.Games,
                CollectionType.Music,
                CollectionType.BoxSets,
                CollectionType.Playlists,
                CollectionType.Channels,
                CollectionType.LiveTv,
                CollectionType.Books,
                CollectionType.Photos,
                CollectionType.HomeVideos,
                CollectionType.MusicVideos,
                string.Empty
            };

            return collectionStripViewTypes.Contains(view.ViewType ?? string.Empty);
        }

        protected override bool CreateImage(IHasImages item, List<BaseItem> itemsWithImages, string outputPath, ImageType imageType, int imageIndex)
        {
            var view = (UserView)item;
            if (imageType == ImageType.Primary && IsUsingCollectionStrip(view))
            {
                if (itemsWithImages.Count == 0 && !string.Equals(view.ViewType, CollectionType.LiveTv, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                CreateThumbCollage(item, itemsWithImages, outputPath, 960, 540, false, item.Name);
                return true;
            }

            return base.CreateImage(item, itemsWithImages, outputPath, imageType, imageIndex);
        }

        protected override IEnumerable<String> GetStripCollageImagePaths(IHasImages primaryItem, IEnumerable<BaseItem> items)
        {
            var userView = primaryItem as UserView;

            if (userView != null && string.Equals(userView.ViewType, CollectionType.LiveTv, StringComparison.OrdinalIgnoreCase))
            {
                var list = new List<string>();
                for (int i = 1; i <= 8; i++)
                {
                    list.Add(ExtractLiveTvResource(i.ToString(CultureInfo.InvariantCulture), ApplicationPaths));
                }
                return list;
            }

            return base.GetStripCollageImagePaths(primaryItem, items);
        }

        private string ExtractLiveTvResource(string name, IApplicationPaths paths)
        {
            var namespacePath = GetType().Namespace + ".livetv." + name + ".jpg";
            var tempPath = Path.Combine(paths.TempDirectory, Guid.NewGuid().ToString("N") + ".jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath));

            using (var stream = GetType().Assembly.GetManifestResourceStream(namespacePath))
            {
                using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    stream.CopyTo(fileStream);
                }
            }

            return tempPath;
        }
    }
}
