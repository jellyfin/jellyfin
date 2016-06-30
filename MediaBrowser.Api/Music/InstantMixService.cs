using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Music
{
    [Route("/Songs/{Id}/InstantMix", "GET", Summary = "Creates an instant playlist based on a given song")]
    public class GetInstantMixFromSong : BaseGetSimilarItemsFromItem
    {
    }

    [Route("/Albums/{Id}/InstantMix", "GET", Summary = "Creates an instant playlist based on a given album")]
    public class GetInstantMixFromAlbum : BaseGetSimilarItemsFromItem
    {
    }

    [Route("/Playlists/{Id}/InstantMix", "GET", Summary = "Creates an instant playlist based on a given playlist")]
    public class GetInstantMixFromPlaylist : BaseGetSimilarItemsFromItem
    {
    }

    [Route("/Artists/{Name}/InstantMix", "GET", Summary = "Creates an instant playlist based on a given artist")]
    public class GetInstantMixFromArtist : BaseGetSimilarItems
    {
        [ApiMember(Name = "Name", Description = "The artist name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
    }

    [Route("/MusicGenres/{Name}/InstantMix", "GET", Summary = "Creates an instant playlist based on a music genre")]
    public class GetInstantMixFromMusicGenre : BaseGetSimilarItems
    {
        [ApiMember(Name = "Name", Description = "The genre name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
    }

    [Route("/Artists/InstantMix", "GET", Summary = "Creates an instant playlist based on a given artist")]
    public class GetInstantMixFromArtistId : BaseGetSimilarItems
    {
        [ApiMember(Name = "Id", Description = "The artist Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/MusicGenres/InstantMix", "GET", Summary = "Creates an instant playlist based on a music genre")]
    public class GetInstantMixFromMusicGenreId : BaseGetSimilarItems
    {
        [ApiMember(Name = "Id", Description = "The genre Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Items/{Id}/InstantMix", "GET", Summary = "Creates an instant playlist based on a given item")]
    public class GetInstantMixFromItem : BaseGetSimilarItemsFromItem
    {
    }

    [Authenticated]
    public class InstantMixService : BaseApiService
    {
        private readonly IUserManager _userManager;

        private readonly IDtoService _dtoService;
        private readonly ILibraryManager _libraryManager;
        private readonly IMusicManager _musicManager;

        public InstantMixService(IUserManager userManager, IDtoService dtoService, IMusicManager musicManager, ILibraryManager libraryManager)
        {
            _userManager = userManager;
            _dtoService = dtoService;
            _musicManager = musicManager;
            _libraryManager = libraryManager;
        }

        public Task<object> Get(GetInstantMixFromItem request)
        {
            var item = _libraryManager.GetItemById(request.Id);

            var user = _userManager.GetUserById(request.UserId);

            var items = _musicManager.GetInstantMixFromItem(item, user);

            return GetResult(items, user, request);
        }

        public Task<object> Get(GetInstantMixFromArtistId request)
        {
            var item = _libraryManager.GetItemById(request.Id);

            var user = _userManager.GetUserById(request.UserId);

            var items = _musicManager.GetInstantMixFromItem(item, user);

            return GetResult(items, user, request);
        }

        public Task<object> Get(GetInstantMixFromMusicGenreId request)
        {
            var item = _libraryManager.GetItemById(request.Id);

            var user = _userManager.GetUserById(request.UserId);

            var items = _musicManager.GetInstantMixFromItem(item, user);

            return GetResult(items, user, request);
        }

        public Task<object> Get(GetInstantMixFromSong request)
        {
            var item = _libraryManager.GetItemById(request.Id);

            var user = _userManager.GetUserById(request.UserId);

            var items = _musicManager.GetInstantMixFromItem(item, user);

            return GetResult(items, user, request);
        }

        public Task<object> Get(GetInstantMixFromAlbum request)
        {
            var album = _libraryManager.GetItemById(request.Id);

            var user = _userManager.GetUserById(request.UserId);

            var items = _musicManager.GetInstantMixFromItem(album, user);

            return GetResult(items, user, request);
        }

        public Task<object> Get(GetInstantMixFromPlaylist request)
        {
            var playlist = (Playlist)_libraryManager.GetItemById(request.Id);

            var user = _userManager.GetUserById(request.UserId);

            var items = _musicManager.GetInstantMixFromItem(playlist, user);

            return GetResult(items, user, request);
        }

        public Task<object> Get(GetInstantMixFromMusicGenre request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var items = _musicManager.GetInstantMixFromGenres(new[] { request.Name }, user);

            return GetResult(items, user, request);
        }

        public Task<object> Get(GetInstantMixFromArtist request)
        {
            var user = _userManager.GetUserById(request.UserId);
            var artist = _libraryManager.GetArtist(request.Name);

            var items = _musicManager.GetInstantMixFromArtist(artist, user);

            return GetResult(items, user, request);
        }

        private async Task<object> GetResult(IEnumerable<Audio> items, User user, BaseGetSimilarItems request)
        {
            var list = items.ToList();

            var result = new ItemsResult
            {
                TotalRecordCount = list.Count
            };

            var dtoOptions = GetDtoOptions(request);

            result.Items = (await _dtoService.GetBaseItemDtos(list.Take(request.Limit ?? list.Count), dtoOptions, user).ConfigureAwait(false)).ToArray();

            return ToOptimizedResult(result);
        }

    }
}
