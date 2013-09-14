using MediaBrowser.Controller;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
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
using System.Threading.Tasks;

namespace MediaBrowser.Api.DefaultTheme
{
    [Route("/MBT/DefaultTheme/TV", "GET")]
    public class GetTvView : IReturn<TvView>
    {
        [ApiMember(Name = "UserId", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }
    }

    [Route("/MBT/DefaultTheme/Movies", "GET")]
    public class GetMovieView : IReturn<MoviesView>
    {
        [ApiMember(Name = "UserId", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }

        [ApiMember(Name = "FamilyRating", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string FamilyRating { get; set; }
    }

    public class DefaultThemeService : BaseApiService
    {
        private readonly IUserManager _userManager;
        private readonly IDtoService _dtoService;
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;

        private readonly ILocalizationManager _localization;

        public DefaultThemeService(IUserManager userManager, IDtoService dtoService, ILogger logger, ILibraryManager libraryManager, ILocalizationManager localization)
        {
            _userManager = userManager;
            _dtoService = dtoService;
            _logger = logger;
            _libraryManager = libraryManager;
            _localization = localization;
        }

        public object Get(GetTvView request)
        {
            var result = GetTvView(request).Result;

            return ToOptimizedResult(result);
        }

        private async Task<TvView> GetTvView(GetTvView request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var series = user.RootFolder.GetRecursiveChildren(user)
                .OfType<Series>()
                .ToList();

            var seriesWithBackdrops = series.Where(i => i.BackdropImagePaths.Count > 0).ToList();

            var view = new TvView();

            var fields = new List<ItemFields>();

            var spotlightItemTasks = seriesWithBackdrops
                .OrderByDescending(i => GetResolution(i, i.BackdropImagePaths[0]))
                .Take(60)
                .OrderBy(i => Guid.NewGuid())
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user));

            view.SpotlightItems = await Task.WhenAll(spotlightItemTasks).ConfigureAwait(false);

            view.ShowsItems = series
               .Where(i => i.BackdropImagePaths.Count > 0)
               .OrderBy(i => Guid.NewGuid())
               .Select(i => GetItemStub(i, ImageType.Backdrop))
               .Where(i => i != null)
               .Take(3)
               .ToArray();

            view.ActorItems = await GetActors(series).ConfigureAwait(false);

            return view;
        }

        public object Get(GetMovieView request)
        {
            var result = GetMovieView(request).Result;

            return ToOptimizedResult(result);
        }

        private async Task<MoviesView> GetMovieView(GetMovieView request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var items = user.RootFolder.GetRecursiveChildren(user)
                .Where(i => i is Movie || i is Trailer || i is BoxSet)
                .ToList();

            var actorsTask = GetActors(items);

            // Exclude trailers from backdrops because they're not always 1080p
            var itemsWithBackdrops = items.Where(i => i.BackdropImagePaths.Count > 0 && !(i is Trailer))
                .ToList();

            var baselineRating = _localization.GetRatingLevel(request.FamilyRating ?? "PG");

            var view = new MoviesView();

            var movies = items.OfType<Movie>()
                .ToList();

            var hdMovies = movies.Where(i => i.IsHd).ToList();

            var familyMovies = movies.Where(i => IsFamilyMovie(i, baselineRating)).ToList();

            view.HDMoviePercentage = 100 * hdMovies.Count;
            view.HDMoviePercentage /= movies.Count;

            view.FamilyMoviePercentage = 100 * familyMovies.Count;
            view.FamilyMoviePercentage /= movies.Count;

            var moviesWithBackdrops = movies
               .Where(i => i.BackdropImagePaths.Count > 0)
               .ToList();

            var fields = new List<ItemFields>();

            var spotlightItemTasks = itemsWithBackdrops
                .OrderByDescending(i => GetResolution(i, i.BackdropImagePaths[0]))
                .Take(60)
                .OrderBy(i => Guid.NewGuid())
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user));

            view.SpotlightItems = await Task.WhenAll(spotlightItemTasks).ConfigureAwait(false);

            view.MovieItems = moviesWithBackdrops
               .OrderBy(i => Guid.NewGuid())
               .Select(i => GetItemStub(i, ImageType.Backdrop))
               .Where(i => i != null)
               .Take(3)
               .ToArray();

            view.TrailerItems = items
             .OfType<Trailer>()
             .Where(i => !string.IsNullOrEmpty(i.PrimaryImagePath))
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Primary))
             .Where(i => i != null)
             .Take(3)
             .ToArray();

            view.BoxSetItems = items
             .OfType<BoxSet>()
             .Where(i => i.BackdropImagePaths.Count > 0)
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(3)
             .ToArray();

            view.ThreeDItems = moviesWithBackdrops
             .Where(i => i.Is3D)
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(3)
             .ToArray();

            var romanceGenres = new[] { "romance" }.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

            view.RomanticItems = moviesWithBackdrops
             .Where(i => i.Genres.Any(romanceGenres.ContainsKey))
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(3)
             .ToArray();

            view.HDItems = hdMovies
             .Where(i => i.BackdropImagePaths.Count > 0)
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(3)
             .ToArray();

            view.FamilyMovies = familyMovies
             .Where(i => i.BackdropImagePaths.Count > 0)
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Backdrop))
             .Where(i => i != null)
             .Take(3)
             .ToArray();

            view.PeopleItems = await actorsTask.ConfigureAwait(false);

            return view;
        }

        private double GetResolution(BaseItem item, string path)
        {
            try
            {
                var date = Kernel.Instance.ImageManager.GetImageDateModified(item, path);

                var size = Kernel.Instance.ImageManager.GetImageSize(path, date).Result;

                return size.Width;
            }
            catch
            {
                return 0;
            }
        }

        private bool IsFamilyMovie(BaseItem item, int? baselineRating)
        {
            var ratingString = item.CustomRating;

            if (string.IsNullOrEmpty(ratingString))
            {
                ratingString = item.OfficialRating;
            }

            if (string.IsNullOrEmpty(ratingString))
            {
                return false;
            }

            var rating = _localization.GetRatingLevel(ratingString);

            if (!baselineRating.HasValue || !rating.HasValue)
            {
                return false;
            }

            return rating.Value <= baselineRating.Value;
        }

        private async Task<ItemStub[]> GetActors(IEnumerable<BaseItem> mediaItems)
        {
            var actorStubs = new List<ItemStub>();

            var actors = mediaItems.SelectMany(i => i.People)
                .Select(i => i.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(i => Guid.NewGuid())
                .ToList();

            foreach (var actor in actors)
            {
                if (actorStubs.Count >= 3)
                {
                    break;
                }

                try
                {
                    var person = await _libraryManager.GetPerson(actor).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(person.PrimaryImagePath))
                    {
                        var stub = GetItemStub(person, ImageType.Primary);

                        if (stub != null)
                        {
                            actorStubs.Add(stub);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error getting person {0}", ex, actor);
                }
            }

            return actorStubs.ToArray();
        }

        private ItemStub GetItemStub(BaseItem item, ImageType imageType)
        {
            var stub = new ItemStub
            {
                Id = _dtoService.GetDtoId(item),
                Name = item.Name,
                ImageType = imageType
            };

            var imageManager = Kernel.Instance.ImageManager;

            try
            {
                var imagePath = imageManager.GetImagePath(item, imageType, 0);

                stub.ImageTag = imageManager.GetImageCacheTag(item, imageType, imagePath);
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
