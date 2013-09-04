using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    [Route("/Songs/{Id}/InstantMix", "GET")]
    [Api(Description = "Creates an instant playlist based on a given song")]
    public class GetInstantMixFromSong : BaseGetSimilarItemsFromItem
    {
    }

    [Route("/Albums/{Id}/InstantMix", "GET")]
    [Api(Description = "Creates an instant playlist based on a given album")]
    public class GetInstantMixFromAlbum : BaseGetSimilarItemsFromItem
    {
    }

    [Route("/Artists/{Name}/InstantMix", "GET")]
    [Api(Description = "Creates an instant playlist based on a given artist")]
    public class GetInstantMixFromArtist : BaseGetSimilarItems
    {
        [ApiMember(Name = "Name", Description = "The artist name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
    }

    [Route("/MusicGenres/{Name}/InstantMix", "GET")]
    [Api(Description = "Creates an instant playlist based on a music genre")]
    public class GetInstantMixFromMusicGenre : BaseGetSimilarItems
    {
        [ApiMember(Name = "Name", Description = "The genre name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
    }

    public class InstantMixService : BaseApiService
    {
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;

        private readonly IDtoService _dtoService;

        public InstantMixService(IUserManager userManager, ILibraryManager libraryManager, IDtoService dtoService)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _dtoService = dtoService;
        }

        public object Get(GetInstantMixFromSong request)
        {
            var item = _dtoService.GetItemByDtoId(request.Id);

            var result = GetInstantMixResult(request, item.Genres).Result;

            return ToOptimizedResult(result);
        }

        public object Get(GetInstantMixFromAlbum request)
        {
            var album = (MusicAlbum)_dtoService.GetItemByDtoId(request.Id);

            var genres = album
               .RecursiveChildren
               .OfType<Audio>()
               .SelectMany(i => i.Genres)
               .Concat(album.Genres)
               .Distinct(StringComparer.OrdinalIgnoreCase);

            var result = GetInstantMixResult(request, genres).Result;

            return ToOptimizedResult(result);
        }

        public object Get(GetInstantMixFromMusicGenre request)
        {
            var genre = GetMusicGenre(request.Name, _libraryManager).Result;

            var result = GetInstantMixResult(request, new[] { genre.Name }).Result;

            return ToOptimizedResult(result);
        }

        public object Get(GetInstantMixFromArtist request)
        {
            var artist = GetArtist(request.Name, _libraryManager).Result;

            var genres = _libraryManager.RootFolder
                .RecursiveChildren
                .OfType<Audio>()
                .Where(i => i.HasArtist(artist.Name))
                .SelectMany(i => i.Genres)
                .Concat(artist.Genres)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            var result = GetInstantMixResult(request, genres).Result;

            return ToOptimizedResult(result);
        }

        private async Task<ItemsResult> GetInstantMixResult(BaseGetSimilarItems request, IEnumerable<string> genres)
        {
            var user = request.UserId.HasValue ? _userManager.GetUserById(request.UserId.Value) : null;

            var fields = request.GetItemFields().ToList();

            var inputItems = user == null
                                 ? _libraryManager.RootFolder.RecursiveChildren
                                 : user.RootFolder.GetRecursiveChildren(user);

            var genresDictionary = genres.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

            var limit = request.Limit.HasValue ? request.Limit.Value * 2 : 100;

            var items = inputItems
                .OfType<Audio>()
                .Select(i => new Tuple<Audio, int>(i, i.Genres.Count(genresDictionary.ContainsKey)))
                .OrderByDescending(i => i.Item2)
                .ThenBy(i => Guid.NewGuid())
                .Select(i => i.Item1)
                .Take(limit)
                .OrderBy(i => Guid.NewGuid())
                .ToArray();

            var result = new ItemsResult
            {
                TotalRecordCount = items.Length
            };

            var tasks = items.Take(request.Limit ?? items.Length)
                .Select(i => _dtoService.GetBaseItemDto(i, fields, user));

            result.Items = await Task.WhenAll(tasks).ConfigureAwait(false);

            return result;
        }

    }
}
