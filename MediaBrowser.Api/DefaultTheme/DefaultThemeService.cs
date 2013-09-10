using MediaBrowser.Controller;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
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
    }
    
    public class DefaultThemeService : BaseApiService
    {
        private readonly IUserManager _userManager;
        private readonly IDtoService _dtoService;
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;

        public DefaultThemeService(IUserManager userManager, IDtoService dtoService, ILogger logger, ILibraryManager libraryManager)
        {
            _userManager = userManager;
            _dtoService = dtoService;
            _logger = logger;
            _libraryManager = libraryManager;
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

            view.BackdropItems = seriesWithBackdrops
                .OrderBy(i => Guid.NewGuid())
                .Select(i => GetItemStub(i, ImageType.Backdrop))
                .Where(i => i != null)
                .Take(30)
                .ToArray();

            view.SpotlightItems = seriesWithBackdrops
               .OrderBy(i => Guid.NewGuid())
               .Select(i => GetItemStub(i, ImageType.Backdrop))
               .Where(i => i != null)
               .Take(30)
               .ToArray();

            view.ShowsItems = series
               .Where(i => !string.IsNullOrEmpty(i.PrimaryImagePath))
               .OrderBy(i => Guid.NewGuid())
               .Select(i => GetItemStub(i, ImageType.Primary))
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

            // Exclude trailers from backdrops because they're not always 1080p
            var itemsWithBackdrops = items.Where(i => i.BackdropImagePaths.Count > 0 && !(i is Trailer))
                .ToList();

            var movies = items.OfType<Movie>().ToList();

            var moviesWithImages = movies
                .Where(i => !string.IsNullOrEmpty(i.PrimaryImagePath))
                .ToList();

            var view = new MoviesView();

            view.BackdropItems = itemsWithBackdrops
                .OrderBy(i => Guid.NewGuid())
                .Select(i => GetItemStub(i, ImageType.Backdrop))
                .Where(i => i != null)
                .Take(30)
                .ToArray();

            view.SpotlightItems = itemsWithBackdrops
               .OrderBy(i => Guid.NewGuid())
               .Select(i => GetItemStub(i, ImageType.Backdrop))
               .Where(i => i != null)
               .Take(30)
               .ToArray();

            view.MovieItems = moviesWithImages
               .OrderBy(i => Guid.NewGuid())
               .Select(i => GetItemStub(i, ImageType.Primary))
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
             .Where(i => !string.IsNullOrEmpty(i.PrimaryImagePath))
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Primary))
             .Where(i => i != null)
             .Take(3)
             .ToArray();

            view.PeopleItems = await GetActors(items).ConfigureAwait(false);

            view.ThreeDItems = moviesWithImages
             .Where(i => i.Is3D)
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Primary))
             .Where(i => i != null)
             .Take(3)
             .ToArray();

            view.HDItems = moviesWithImages
             .Where(i => i.IsHd)
             .OrderBy(i => Guid.NewGuid())
             .Select(i => GetItemStub(i, ImageType.Primary))
             .Where(i => i != null)
             .Take(3)
             .ToArray();

            return view;
        }

        private async Task<ItemStub[]> GetActors(IEnumerable<BaseItem> mediaItems)
        {
            var actorStubs = new List<ItemStub>();

            var actors = mediaItems.SelectMany(i => i.People)
                .Select(i => i.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
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
                Name = item.Name
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
