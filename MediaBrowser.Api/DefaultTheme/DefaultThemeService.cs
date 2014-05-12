using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Api.DefaultTheme
{
    [Route("/MBT/DefaultTheme/Games", "GET")]
    public class GetGamesView : IReturn<GamesView>
    {
        [ApiMember(Name = "UserId", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }

        [ApiMember(Name = "RecentlyPlayedGamesLimit", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int RecentlyPlayedGamesLimit { get; set; }

        public string ParentId { get; set; }
    }

    [Route("/MBT/DefaultTheme/TV", "GET")]
    public class GetTvView : IReturn<TvView>
    {
        [ApiMember(Name = "UserId", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }

        [ApiMember(Name = "ComedyGenre", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string ComedyGenre { get; set; }

        [ApiMember(Name = "RomanceGenre", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string RomanceGenre { get; set; }

        [ApiMember(Name = "TopCommunityRating", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public double TopCommunityRating { get; set; }

        [ApiMember(Name = "NextUpEpisodeLimit", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int NextUpEpisodeLimit { get; set; }

        [ApiMember(Name = "ResumableEpisodeLimit", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int ResumableEpisodeLimit { get; set; }

        [ApiMember(Name = "LatestEpisodeLimit", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int LatestEpisodeLimit { get; set; }

        public string ParentId { get; set; }
    }

    [Route("/MBT/DefaultTheme/Movies", "GET")]
    public class GetMovieView : IReturn<MoviesView>
    {
        [ApiMember(Name = "UserId", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }

        [ApiMember(Name = "FamilyGenre", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string FamilyGenre { get; set; }

        [ApiMember(Name = "ComedyGenre", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string ComedyGenre { get; set; }

        [ApiMember(Name = "RomanceGenre", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string RomanceGenre { get; set; }

        [ApiMember(Name = "LatestMoviesLimit", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int LatestMoviesLimit { get; set; }

        [ApiMember(Name = "LatestTrailersLimit", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int LatestTrailersLimit { get; set; }

        public string ParentId { get; set; }
    }

    [Route("/MBT/DefaultTheme/Favorites", "GET")]
    public class GetFavoritesView : IReturn<FavoritesView>
    {
        [ApiMember(Name = "UserId", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }
    }

    public class DefaultThemeService : BaseApiService
    {
        private readonly IUserManager _userManager;
        private readonly IDtoService _dtoService;
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserDataManager _userDataManager;

        private readonly IImageProcessor _imageProcessor;
        private readonly IItemRepository _itemRepo;

        public DefaultThemeService(IUserManager userManager, IDtoService dtoService, ILogger logger, ILibraryManager libraryManager, IImageProcessor imageProcessor, IUserDataManager userDataManager, IItemRepository itemRepo)
        {
            _userManager = userManager;
            _dtoService = dtoService;
            _logger = logger;
            _libraryManager = libraryManager;
            _imageProcessor = imageProcessor;
            _userDataManager = userDataManager;
            _itemRepo = itemRepo;
        }

        public object Get(GetFavoritesView request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var allItems = user.RootFolder.GetRecursiveChildren(user)
                .ToList();

            var allFavoriteItems = allItems.Where(i => _userDataManager.GetUserData(user.Id, i.GetUserDataKey()).IsFavorite)
                .ToList();

            var itemsWithImages = allFavoriteItems.Where(i => !string.IsNullOrEmpty(i.PrimaryImagePath))
                .ToList();

            var itemsWithBackdrops = allFavoriteItems.Where(i => i.GetImages(ImageType.Backdrop).Any())
                .ToList();

            var view = new FavoritesView();

            var fields = new List<ItemFields>();

            view.BackdropItems = FilterItemsForBackdropDisplay(itemsWithBackdrops)
                .Randomize("backdrop")
                .Take(10)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            var spotlightItems = itemsWithBackdrops.Randomize("spotlight")
                                                   .Take(10)
                                                   .ToList();

            view.SpotlightItems = spotlightItems
              .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
              .ToList();

            fields.Add(ItemFields.PrimaryImageAspectRatio);

            view.Albums = itemsWithImages
                .OfType<MusicAlbum>()
                .Randomize()
                .Take(4)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.Books = itemsWithImages
                .OfType<Book>()
                .Randomize()
                .Take(6)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.Episodes = itemsWithImages
                .OfType<Episode>()
                .Randomize()
                .Take(6)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.Games = itemsWithImages
                .OfType<Game>()
                .Randomize()
                .Take(6)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.Movies = itemsWithImages
                .OfType<Movie>()
                .Randomize()
                .Take(6)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.Series = itemsWithImages
                .OfType<Series>()
                .Randomize()
                .Take(6)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.Songs = itemsWithImages
                .OfType<Audio>()
                .Randomize()
                .Take(4)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.MiniSpotlights = itemsWithBackdrops
                .Except(spotlightItems)
                .Randomize()
                .Take(5)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            var artists = allItems.OfType<Audio>()
                .SelectMany(i => i.AllArtists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Randomize()
            .Select(i =>
            {
                try
                {
                    return _libraryManager.GetArtist(i);
                }
                catch
                {
                    return null;
                }
            })
                .Where(i => i != null && _userDataManager.GetUserData(user.Id, i.GetUserDataKey()).IsFavorite)
                .Take(4)
                .ToList();

            view.Artists = artists
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            return ToOptimizedSerializedResultUsingCache(view);
        }

        public object Get(GetGamesView request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var items = GetAllLibraryItems(user.Id, _userManager, _libraryManager, request.ParentId).Where(i => i is Game || i is GameSystem)
                .ToList();

            var gamesWithImages = items.OfType<Game>().Where(i => !string.IsNullOrEmpty(i.PrimaryImagePath)).ToList();

            var itemsWithBackdrops = FilterItemsForBackdropDisplay(items.Where(i => i.GetImages(ImageType.Backdrop).Any())).ToList();

            var gamesWithBackdrops = itemsWithBackdrops.OfType<Game>().ToList();

            var view = new GamesView();

            var fields = new List<ItemFields>();

            view.GameSystems = items
                .OfType<GameSystem>()
                .OrderBy(i => i.SortName)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            var currentUserId = user.Id;
            view.RecentlyPlayedGames = gamesWithImages
                .OrderByDescending(i => _userDataManager.GetUserData(currentUserId, i.GetUserDataKey()).LastPlayedDate ?? DateTime.MinValue)
                .Take(request.RecentlyPlayedGamesLimit)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.BackdropItems = gamesWithBackdrops
                .OrderBy(i => Guid.NewGuid())
                .Take(10)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.SpotlightItems = gamesWithBackdrops
                .OrderBy(i => Guid.NewGuid())
                .Take(10)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.MultiPlayerItems = gamesWithImages
            .Where(i => i.PlayersSupported.HasValue && i.PlayersSupported.Value > 1)
            .Randomize()
            .Select(i => GetItemStub(i, ImageType.Primary))
            .Where(i => i != null)
            .Take(1)
            .ToList();

            return ToOptimizedSerializedResultUsingCache(view);
        }

        public object Get(GetTvView request)
        {
            var romanceGenres = request.RomanceGenre.Split(',').ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);
            var comedyGenres = request.ComedyGenre.Split(',').ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

            var user = _userManager.GetUserById(request.UserId);

            var series = GetAllLibraryItems(user.Id, _userManager, _libraryManager, request.ParentId)
                .OfType<Series>()
                .ToList();

            var seriesWithBackdrops = series.Where(i => i.GetImages(ImageType.Backdrop).Any()).ToList();

            var view = new TvView();

            var fields = new List<ItemFields>();

            var seriesWithBestBackdrops = FilterItemsForBackdropDisplay(seriesWithBackdrops).ToList();

            view.BackdropItems = seriesWithBestBackdrops
                .OrderBy(i => Guid.NewGuid())
                .Take(10)
                .AsParallel()
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.ShowsItems = series
               .Where(i => i.GetImages(ImageType.Backdrop).Any())
               .Randomize("all")
               .Select(i => GetItemStub(i, ImageType.Backdrop))
               .Where(i => i != null)
               .Take(1)
               .ToList();

            view.RomanceItems = seriesWithBackdrops
             .Where(i => i.Genres.Any(romanceGenres.ContainsKey))
             .Randomize("romance")
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(1)
             .ToList();

            view.ComedyItems = seriesWithBackdrops
             .Where(i => i.Genres.Any(comedyGenres.ContainsKey))
             .Randomize("comedy")
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(1)
             .ToList();

            var spotlightSeries = seriesWithBestBackdrops
                .Where(i => i.CommunityRating.HasValue && i.CommunityRating >= 8.5)
                .ToList();

            if (spotlightSeries.Count < 20)
            {
                spotlightSeries = seriesWithBestBackdrops;
            }

            spotlightSeries = spotlightSeries
                .OrderBy(i => Guid.NewGuid())
                .Take(10)
                .ToList();

            view.SpotlightItems = spotlightSeries
                .AsParallel()
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            var miniSpotlightItems = seriesWithBackdrops
                .Except(spotlightSeries.OfType<Series>())
                .Where(i => i.CommunityRating.HasValue && i.CommunityRating >= 8)
                .ToList();

            if (miniSpotlightItems.Count < 15)
            {
                miniSpotlightItems = seriesWithBackdrops;
            }

            view.MiniSpotlights = miniSpotlightItems
              .Randomize("minispotlight")
              .Take(5)
              .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
              .ToList();

            var nextUpEpisodes = new TvShowsService(_userManager, _userDataManager, _libraryManager, _itemRepo, _dtoService)
                .GetNextUpEpisodes(new GetNextUpEpisodes { UserId = user.Id }, series)
                .ToList();

            fields.Add(ItemFields.PrimaryImageAspectRatio);

            view.NextUpEpisodes = nextUpEpisodes
                .Take(request.NextUpEpisodeLimit)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.SeriesIdsInProgress = nextUpEpisodes.Select(i => i.Series.Id.ToString("N")).ToList();

            // Avoid implicitly captured closure
            var currentUser1 = user;

            var ownedEpisodes = series
                .SelectMany(i => i.GetRecursiveChildren(currentUser1, j => j.LocationType != LocationType.Virtual))
                .OfType<Episode>()
                .ToList();

            // Avoid implicitly captured closure
            var currentUser = user;

            view.LatestEpisodes = ownedEpisodes
                .OrderByDescending(i => i.DateCreated)
                .Where(i => !_userDataManager.GetUserData(currentUser.Id, i.GetUserDataKey()).Played)
                .Take(request.LatestEpisodeLimit)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.ResumableEpisodes = ownedEpisodes
                .Where(i => _userDataManager.GetUserData(currentUser.Id, i.GetUserDataKey()).PlaybackPositionTicks > 0)
                .OrderByDescending(i => _userDataManager.GetUserData(currentUser.Id, i.GetUserDataKey()).LastPlayedDate ?? DateTime.MinValue)
                .Take(request.ResumableEpisodeLimit)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            return ToOptimizedSerializedResultUsingCache(view);
        }

        public object Get(GetMovieView request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var items = GetAllLibraryItems(user.Id, _userManager, _libraryManager, request.ParentId)
                .Where(i => i is Movie || i is Trailer || i is BoxSet)
                .ToList();

            var view = new MoviesView();

            var movies = items.OfType<Movie>()
                .ToList();

            var trailers = items.OfType<Trailer>()
               .ToList();

            var hdMovies = movies.Where(i => i.IsHD).ToList();

            var familyGenres = request.FamilyGenre.Split(',').ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

            var familyMovies = movies.Where(i => i.Genres.Any(familyGenres.ContainsKey)).ToList();

            view.HDMoviePercentage = 100 * hdMovies.Count;
            view.HDMoviePercentage /= movies.Count;

            view.FamilyMoviePercentage = 100 * familyMovies.Count;
            view.FamilyMoviePercentage /= movies.Count;

            var moviesWithBackdrops = movies
               .Where(i => i.GetImages(ImageType.Backdrop).Any())
               .ToList();

            var fields = new List<ItemFields>();

            var itemsWithTopBackdrops = FilterItemsForBackdropDisplay(moviesWithBackdrops).ToList();

            view.BackdropItems = itemsWithTopBackdrops
                .OrderBy(i => Guid.NewGuid())
                .Take(10)
                .AsParallel()
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.MovieItems = moviesWithBackdrops
               .Randomize("all")
               .Select(i => GetItemStub(i, ImageType.Backdrop))
               .Where(i => i != null)
               .Take(1)
               .ToList();

            view.TrailerItems = trailers
             .Where(i => !string.IsNullOrEmpty(i.PrimaryImagePath))
             .Randomize()
             .Select(i => GetItemStub(i, ImageType.Primary))
             .Where(i => i != null)
             .Take(1)
             .ToList();

            view.BoxSetItems = items
             .OfType<BoxSet>()
             .Where(i => i.GetImages(ImageType.Backdrop).Any())
             .Randomize()
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(1)
             .ToList();

            view.ThreeDItems = moviesWithBackdrops
             .Where(i => i.Is3D)
             .Randomize("3d")
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(1)
             .ToList();

            var romanceGenres = request.RomanceGenre.Split(',').ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);
            var comedyGenres = request.ComedyGenre.Split(',').ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

            view.RomanceItems = moviesWithBackdrops
             .Where(i => i.Genres.Any(romanceGenres.ContainsKey))
             .Randomize("romance")
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(1)
             .ToList();

            view.ComedyItems = moviesWithBackdrops
             .Where(i => i.Genres.Any(comedyGenres.ContainsKey))
             .Randomize("comedy")
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(1)
             .ToList();

            view.HDItems = hdMovies
             .Where(i => i.GetImages(ImageType.Backdrop).Any())
             .Randomize("hd")
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(1)
             .ToList();

            view.FamilyMovies = familyMovies
             .Where(i => i.GetImages(ImageType.Backdrop).Any())
             .Randomize("family")
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(1)
             .ToList();

            var currentUserId = user.Id;
            var spotlightItems = itemsWithTopBackdrops
                .Where(i => i.CommunityRating.HasValue && i.CommunityRating >= 8)
                .Where(i => !_userDataManager.GetUserData(currentUserId, i.GetUserDataKey()).Played)
                .ToList();

            if (spotlightItems.Count < 20)
            {
                spotlightItems = itemsWithTopBackdrops;
            }

            spotlightItems = spotlightItems
                .OrderBy(i => Guid.NewGuid())
                .Take(10)
                .ToList();

            view.SpotlightItems = spotlightItems
                .AsParallel()
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            var miniSpotlightItems = moviesWithBackdrops
                .Except(spotlightItems)
                .Where(i => i.CommunityRating.HasValue && i.CommunityRating >= 7.5)
                .ToList();

            if (miniSpotlightItems.Count < 15)
            {
                miniSpotlightItems = itemsWithTopBackdrops;
            }

            miniSpotlightItems = miniSpotlightItems
              .Randomize("minispotlight")
              .ToList();

            // Avoid implicitly captured closure
            miniSpotlightItems.InsertRange(0, moviesWithBackdrops
                .Where(i => _userDataManager.GetUserData(currentUserId, i.GetUserDataKey()).PlaybackPositionTicks > 0)
                .OrderByDescending(i => _userDataManager.GetUserData(currentUserId, i.GetUserDataKey()).LastPlayedDate ?? DateTime.MaxValue)
                .Take(3));

            view.MiniSpotlights = miniSpotlightItems
              .Take(3)
              .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
              .ToList();

            // Avoid implicitly captured closure
            var currentUserId1 = user.Id;

            view.LatestMovies = movies
                .OrderByDescending(i => i.DateCreated)
                .Where(i => !_userDataManager.GetUserData(currentUserId1, i.GetUserDataKey()).Played)
                .Take(request.LatestMoviesLimit)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.LatestTrailers = trailers
                .OrderByDescending(i => i.DateCreated)
                .Where(i => !_userDataManager.GetUserData(currentUserId1, i.GetUserDataKey()).Played)
                .Take(request.LatestTrailersLimit)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            return ToOptimizedSerializedResultUsingCache(view);
        }

        private IEnumerable<BaseItem> FilterItemsForBackdropDisplay(IEnumerable<BaseItem> items)
        {
            var tuples = items
                .Select(i => new Tuple<BaseItem, double>(i, GetResolution(i, ImageType.Backdrop, 0)))
                .Where(i => i.Item2 > 0)
                .ToList();

            var topItems = tuples
                .Where(i => i.Item2 >= 1920)
                .ToList();

            if (topItems.Count >= 10)
            {
                return topItems.Select(i => i.Item1);
            }

            return tuples.Select(i => i.Item1);
        }

        private double GetResolution(BaseItem item, ImageType type, int index)
        {
            try
            {
                var info = item.GetImageInfo(type, index);

                var size = _imageProcessor.GetImageSize(info.Path, info.DateModified);

                return size.Width;
            }
            catch
            {
                return 0;
            }
        }

        private ItemStub GetItemStub(BaseItem item, ImageType imageType)
        {
            var stub = new ItemStub
            {
                Id = _dtoService.GetDtoId(item),
                Name = item.Name,
                ImageType = imageType
            };

            try
            {
                var tag = _imageProcessor.GetImageCacheTag(item, imageType);

                if (tag != null)
                {
                    stub.ImageTag = tag;
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error getting image tag for {0}", ex, item.Path);
                return null;
            }

            return stub;
        }
    }

    static class RandomExtension
    {
        public static IEnumerable<T> Randomize<T>(this IEnumerable<T> sequence, string type = "none")
            where T : BaseItem
        {
            var hour = DateTime.Now.Hour + DateTime.Now.Day + 2;

            var typeCode = type.GetHashCode();

            return sequence.OrderBy(i =>
            {
                var val = i.Id.GetHashCode() + i.Genres.Count + i.People.Count + (i.ProductionYear ?? 0) + i.DateCreated.Minute + i.DateModified.Minute + typeCode;

                return val % hour;
            });
        }

        public static IEnumerable<string> Randomize(this IEnumerable<string> sequence)
        {
            var hour = DateTime.Now.Hour + 2;

            return sequence.OrderBy(i => i.GetHashCode() % hour);
        }
    }
}
