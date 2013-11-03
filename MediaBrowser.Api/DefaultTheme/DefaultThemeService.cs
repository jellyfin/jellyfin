using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using ServiceStack.ServiceHost;
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

        public DefaultThemeService(IUserManager userManager, IDtoService dtoService, ILogger logger, ILibraryManager libraryManager, IImageProcessor imageProcessor, IUserDataManager userDataManager)
        {
            _userManager = userManager;
            _dtoService = dtoService;
            _logger = logger;
            _libraryManager = libraryManager;
            _imageProcessor = imageProcessor;
            _userDataManager = userDataManager;
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

            var itemsWithBackdrops = allFavoriteItems.Where(i => i.BackdropImagePaths.Count > 0)
                .ToList();

            var view = new FavoritesView();

            var fields = new List<ItemFields>();

            view.BackdropItems = FilterItemsForBackdropDisplay(itemsWithBackdrops.OrderBy(i => Guid.NewGuid()))
                .Take(10)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            var spotlightItems = itemsWithBackdrops.OrderBy(i => Guid.NewGuid())
                                                   .Take(10)
                                                   .ToList();

            view.SpotlightItems = spotlightItems
              .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
              .ToList();

            view.Albums = itemsWithImages
                .OfType<MusicAlbum>()
                .OrderBy(i => Guid.NewGuid())
                .Take(4)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.Books = itemsWithImages
                .OfType<Book>()
                .OrderBy(i => Guid.NewGuid())
                .Take(6)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.Episodes = itemsWithImages
                .OfType<Episode>()
                .OrderBy(i => Guid.NewGuid())
                .Take(6)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.Games = itemsWithImages
                .OfType<Game>()
                .OrderBy(i => Guid.NewGuid())
                .Take(6)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.Movies = itemsWithImages
                .OfType<Movie>()
                .OrderBy(i => Guid.NewGuid())
                .Take(6)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.Series = itemsWithImages
                .OfType<Series>()
                .OrderBy(i => Guid.NewGuid())
                .Take(6)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.Songs = itemsWithImages
                .OfType<Audio>()
                .OrderBy(i => Guid.NewGuid())
                .Take(4)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.MiniSpotlights = itemsWithBackdrops
                .Except(spotlightItems)
                .OrderBy(i => Guid.NewGuid())
                .Take(5)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            var artists = allItems.OfType<Audio>()
                .SelectMany(i =>
            {
                var list = new List<string>();

                if (!string.IsNullOrEmpty(i.AlbumArtist))
                {
                    list.Add(i.AlbumArtist);
                }
                list.AddRange(i.Artists);

                return list;
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(i => Guid.NewGuid())
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

            return ToOptimizedResult(view);
        }

        public object Get(GetGamesView request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var items = user.RootFolder.GetRecursiveChildren(user, i => i is Game || i is GameSystem)
                .ToList();

            var gamesWithImages = items.OfType<Game>().Where(i => !string.IsNullOrEmpty(i.PrimaryImagePath)).ToList();

            var itemsWithBackdrops = FilterItemsForBackdropDisplay(items.Where(i => i.BackdropImagePaths.Count > 0)).ToList();

            var gamesWithBackdrops = itemsWithBackdrops.OfType<Game>().ToList();

            var view = new GamesView();

            var fields = new List<ItemFields>();

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
            .OrderBy(i => Guid.NewGuid())
            .Select(i => GetItemStub(i, ImageType.Primary))
            .Where(i => i != null)
            .Take(1)
            .ToList();

            view.MiniSpotlights = gamesWithBackdrops
                .OrderBy(i => Guid.NewGuid())
                .Take(5)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            return ToOptimizedResult(view);
        }

        public object Get(GetTvView request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var series = user.RootFolder.GetRecursiveChildren(user)
                .OfType<Series>()
                .ToList();

            var seriesWithBackdrops = series.Where(i => i.BackdropImagePaths.Count > 0).ToList();

            var view = new TvView
            {
                SeriesCount = series.Count,

                FavoriteSeriesCount = series.Count(i => _userDataManager.GetUserData(user.Id, i.GetUserDataKey()).IsFavorite),

                TopCommunityRatedSeriesCount = series.Count(i => i.CommunityRating.HasValue && i.CommunityRating.Value >= request.TopCommunityRating)
            };

            var fields = new List<ItemFields>();

            var seriesWithBestBackdrops = FilterItemsForBackdropDisplay(seriesWithBackdrops).ToList();

            view.BackdropItems = seriesWithBestBackdrops
                .OrderBy(i => Guid.NewGuid())
                .Take(10)
                .AsParallel()
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.ShowsItems = series
               .Where(i => i.BackdropImagePaths.Count > 0)
               .OrderBy(i => Guid.NewGuid())
               .Select(i => GetItemStub(i, ImageType.Backdrop))
               .Where(i => i != null)
               .Take(1)
               .ToList();

            var romanceGenres = request.RomanceGenre.Split(',').ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);
            var comedyGenres = request.ComedyGenre.Split(',').ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

            view.RomanceItems = seriesWithBackdrops
             .Where(i => i.Genres.Any(romanceGenres.ContainsKey))
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(1)
             .ToList();

            view.ComedyItems = seriesWithBackdrops
             .Where(i => i.Genres.Any(comedyGenres.ContainsKey))
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(1)
             .ToList();

            view.ActorItems = GetActors(series, user.Id);

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
              .OrderBy(i => Guid.NewGuid())
              .Take(5)
              .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
              .ToList();

            return ToOptimizedResult(view);
        }

        public object Get(GetMovieView request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var items = user.RootFolder.GetRecursiveChildren(user, i => i is Movie || i is Trailer || i is BoxSet)
                .ToList();

            // Exclude trailers from backdrops because they're not always 1080p
            var itemsWithBackdrops = items.Where(i => i.BackdropImagePaths.Count > 0)
                .ToList();

            var view = new MoviesView();

            var movies = items.OfType<Movie>()
                .ToList();

            var hdMovies = movies.Where(i => i.IsHD).ToList();

            var familyGenres = request.FamilyGenre.Split(',').ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

            var familyMovies = movies.Where(i => i.Genres.Any(familyGenres.ContainsKey)).ToList();

            view.HDMoviePercentage = 100 * hdMovies.Count;
            view.HDMoviePercentage /= movies.Count;

            view.FamilyMoviePercentage = 100 * familyMovies.Count;
            view.FamilyMoviePercentage /= movies.Count;

            var moviesWithBackdrops = movies
               .Where(i => i.BackdropImagePaths.Count > 0)
               .ToList();

            var fields = new List<ItemFields>();

            var itemsWithTopBackdrops = FilterItemsForBackdropDisplay(itemsWithBackdrops).ToList();

            view.BackdropItems = itemsWithTopBackdrops
                .OrderBy(i => Guid.NewGuid())
                .Take(10)
                .AsParallel()
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
                .ToList();

            view.MovieItems = moviesWithBackdrops
               .OrderBy(i => Guid.NewGuid())
               .Select(i => GetItemStub(i, ImageType.Backdrop))
               .Where(i => i != null)
               .Take(1)
               .ToList();

            view.TrailerItems = items
             .OfType<Trailer>()
             .Where(i => !string.IsNullOrEmpty(i.PrimaryImagePath))
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Primary))
             .Where(i => i != null)
             .Take(1)
             .ToList();

            view.BoxSetItems = items
             .OfType<BoxSet>()
             .Where(i => i.BackdropImagePaths.Count > 0)
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(1)
             .ToList();

            view.ThreeDItems = moviesWithBackdrops
             .Where(i => i.Is3D)
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(1)
             .ToList();

            var romanceGenres = request.RomanceGenre.Split(',').ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);
            var comedyGenres = request.ComedyGenre.Split(',').ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

            view.RomanceItems = moviesWithBackdrops
             .Where(i => i.Genres.Any(romanceGenres.ContainsKey))
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(1)
             .ToList();

            view.ComedyItems = moviesWithBackdrops
             .Where(i => i.Genres.Any(comedyGenres.ContainsKey))
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(1)
             .ToList();

            view.HDItems = hdMovies
             .Where(i => i.BackdropImagePaths.Count > 0)
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(1)
             .ToList();

            view.FamilyMovies = familyMovies
             .Where(i => i.BackdropImagePaths.Count > 0)
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(1)
             .ToList();

            view.PeopleItems = GetActors(items, user.Id);

            var spotlightItems = itemsWithTopBackdrops
                .Where(i => i.CommunityRating.HasValue && i.CommunityRating >= 8)
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
              .OrderBy(i => Guid.NewGuid())
              .ToList();

            // Avoid implicitly captured closure
            var currentUserId = user.Id;
            miniSpotlightItems.InsertRange(2, moviesWithBackdrops
                .Where(i => _userDataManager.GetUserData(currentUserId, i.GetUserDataKey()).PlaybackPositionTicks > 0)
                .OrderByDescending(i => _userDataManager.GetUserData(currentUserId, i.GetUserDataKey()).LastPlayedDate ?? DateTime.MaxValue)
                .Take(3));

            view.MiniSpotlights = miniSpotlightItems
              .Take(5)
              .Select(i => _dtoService.GetBaseItemDto(i, fields, user))
              .ToList();

            return ToOptimizedResult(view);
        }

        private IEnumerable<BaseItem> FilterItemsForBackdropDisplay(IEnumerable<BaseItem> items)
        {
            var tuples = items
                .Select(i => new Tuple<BaseItem, double>(i, GetResolution(i, i.BackdropImagePaths[0])))
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

        private double GetResolution(BaseItem item, string path)
        {
            try
            {
                var date = item.GetImageDateModified(path);

                var size = _imageProcessor.GetImageSize(path, date);

                return size.Width;
            }
            catch
            {
                return 0;
            }
        }

        private List<ItemStub> GetActors(IEnumerable<BaseItem> mediaItems, Guid userId)
        {
            var actors = mediaItems.SelectMany(i => i.People)
                .Select(i => i.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(i => Guid.NewGuid())
                .ToList();

            var result = actors.Select(actor =>
            {
                try
                {
                    var person = _libraryManager.GetPerson(actor);

                    if (!string.IsNullOrEmpty(person.PrimaryImagePath))
                    {
                        var userdata = _userDataManager.GetUserData(userId, person.GetUserDataKey());

                        if (userdata.IsFavorite || (userdata.Likes ?? false))
                        {
                            return GetItemStub(person, ImageType.Primary);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error getting person {0}", ex, actor);
                }

                return null;
            })
            .Where(i => i != null)
            .Take(1)
            .ToList();

            if (result.Count == 0)
            {
                result = actors.Select(actor =>
                {
                    try
                    {
                        var person = _libraryManager.GetPerson(actor);

                        if (!string.IsNullOrEmpty(person.PrimaryImagePath))
                        {
                            return GetItemStub(person, ImageType.Primary);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error getting person {0}", ex, actor);
                    }

                    return null;
                })
                            .Where(i => i != null)
                            .Take(1)
                            .ToList();
            }

            return result;
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
                var imagePath = item.GetImagePath(imageType, 0);

                stub.ImageTag = _imageProcessor.GetImageCacheTag(item, imageType, imagePath);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error getting image tag for {0}", ex, item.Path);
                return null;
            }

            return stub;
        }
    }
}
