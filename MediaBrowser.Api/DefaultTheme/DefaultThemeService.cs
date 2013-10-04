using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
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

    [Route("/MBT/DefaultTheme/Home", "GET")]
    public class GetHomeView : IReturn<HomeView>
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

        private readonly IImageProcessor _imageProcessor;

        public DefaultThemeService(IUserManager userManager, IDtoService dtoService, ILogger logger, ILibraryManager libraryManager, IImageProcessor imageProcessor)
        {
            _userManager = userManager;
            _dtoService = dtoService;
            _logger = logger;
            _libraryManager = libraryManager;
            _imageProcessor = imageProcessor;
        }

        public object Get(GetHomeView request)
        {
            var result = GetHomeView(request);

            return ToOptimizedResult(result);
        }

        private HomeView GetHomeView(GetHomeView request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var allItems = user.RootFolder.GetRecursiveChildren(user)
                .ToList();

            var itemsWithBackdrops = allItems.Where(i => i.BackdropImagePaths.Count > 0).ToList();

            var view = new HomeView();

            var fields = new List<ItemFields>();

            var eligibleSpotlightItems = itemsWithBackdrops
                .Where(i => i is Game || i is Movie || i is Series || i is MusicArtist);

            var dtos = FilterItemsForBackdropDisplay(eligibleSpotlightItems)
                .OrderBy(i => Guid.NewGuid())
                .Take(50)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user));

            view.SpotlightItems = dtos.ToList();

            return view;
        }

        public object Get(GetGamesView request)
        {
            var result = GetGamesView(request);

            return ToOptimizedResult(result);
        }

        private GamesView GetGamesView(GetGamesView request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var items = user.RootFolder.GetRecursiveChildren(user)
                .Where(i => i is Game || i is GameSystem)
                .ToList();

            var itemsWithBackdrops = FilterItemsForBackdropDisplay(items.Where(i => i.BackdropImagePaths.Count > 0)).ToList();

            var view = new GamesView();

            var fields = new List<ItemFields>();

            var dtos = itemsWithBackdrops
                .OfType<Game>()
                .OrderBy(i => Guid.NewGuid())
                .Take(50)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user));

            view.SpotlightItems = dtos.ToList();

            return view;
        }

        public object Get(GetMovieView request)
        {
            var result = GetMovieView(request);

            return ToOptimizedResult(result);
        }

        public object Get(GetTvView request)
        {
            var result = GetTvView(request);

            return ToOptimizedResult(result);
        }

        private TvView GetTvView(GetTvView request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var series = user.RootFolder.GetRecursiveChildren(user)
                .OfType<Series>()
                .ToList();

            var seriesWithBackdrops = series.Where(i => i.BackdropImagePaths.Count > 0).ToList();

            var view = new TvView();

            var fields = new List<ItemFields>();

            var dtos = FilterItemsForBackdropDisplay(seriesWithBackdrops)
                .OrderBy(i => Guid.NewGuid())
                .Take(50)
                .AsParallel()
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user));

            view.SpotlightItems = dtos.ToList();

            view.ShowsItems = series
               .Where(i => i.BackdropImagePaths.Count > 0)
               .OrderBy(i => Guid.NewGuid())
               .Select(i => GetItemStub(i, ImageType.Backdrop))
               .Where(i => i != null)
               .Take(3)
               .ToList();

            var romanceGenres = request.RomanceGenre.Split(',').ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);
            var comedyGenres = request.ComedyGenre.Split(',').ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

            view.RomanceItems = seriesWithBackdrops
             .Where(i => i.Genres.Any(romanceGenres.ContainsKey))
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(3)
             .ToList();

            view.ComedyItems = seriesWithBackdrops
             .Where(i => i.Genres.Any(comedyGenres.ContainsKey))
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(3)
             .ToList();

            view.ActorItems = GetActors(series);

            return view;
        }

        private MoviesView GetMovieView(GetMovieView request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var items = user.RootFolder.GetRecursiveChildren(user)
                .Where(i => i is Movie || i is Trailer || i is BoxSet)
                .ToList();

            // Exclude trailers from backdrops because they're not always 1080p
            var itemsWithBackdrops = items.Where(i => i.BackdropImagePaths.Count > 0 && !(i is Trailer))
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

            var dtos = FilterItemsForBackdropDisplay(itemsWithBackdrops)
                .OrderBy(i => Guid.NewGuid())
                .Take(50)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user));

            view.SpotlightItems = dtos.ToList();

            view.MovieItems = moviesWithBackdrops
               .OrderBy(i => Guid.NewGuid())
               .Select(i => GetItemStub(i, ImageType.Backdrop))
               .Where(i => i != null)
               .Take(3)
               .ToList();

            view.TrailerItems = items
             .OfType<Trailer>()
             .Where(i => !string.IsNullOrEmpty(i.PrimaryImagePath))
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Primary))
             .Where(i => i != null)
             .Take(3)
             .ToList();

            view.BoxSetItems = items
             .OfType<BoxSet>()
             .Where(i => i.BackdropImagePaths.Count > 0)
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(3)
             .ToList();

            view.ThreeDItems = moviesWithBackdrops
             .Where(i => i.Is3D)
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(3)
             .ToList();

            var romanceGenres = request.RomanceGenre.Split(',').ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);
            var comedyGenres = request.ComedyGenre.Split(',').ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

            view.RomanceItems = moviesWithBackdrops
             .Where(i => i.Genres.Any(romanceGenres.ContainsKey))
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(3)
             .ToList();

            view.ComedyItems = moviesWithBackdrops
             .Where(i => i.Genres.Any(comedyGenres.ContainsKey))
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(3)
             .ToList();

            view.HDItems = hdMovies
             .Where(i => i.BackdropImagePaths.Count > 0)
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(3)
             .ToList();

            view.FamilyMovies = familyMovies
             .Where(i => i.BackdropImagePaths.Count > 0)
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(3)
             .ToList();

            view.PeopleItems = GetActors(items);

            return view;
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

        private List<ItemStub> GetActors(IEnumerable<BaseItem> mediaItems)
        {
            var actors = mediaItems.SelectMany(i => i.People)
                .Select(i => i.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(i => Guid.NewGuid())
                .ToList();

            return actors.Select(actor =>
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
            .Take(3)
            .ToList();
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
