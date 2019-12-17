using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

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
        private readonly IAuthorizationContext _authContext;

        public InstantMixService(
            ILogger<InstantMixService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IUserManager userManager,
            IDtoService dtoService,
            IMusicManager musicManager,
            ILibraryManager libraryManager,
            IAuthorizationContext authContext)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _userManager = userManager;
            _dtoService = dtoService;
            _musicManager = musicManager;
            _libraryManager = libraryManager;
            _authContext = authContext;
        }

        public object Get(GetInstantMixFromItem request)
        {
            var item = _libraryManager.GetItemById(request.Id);

            var user = _userManager.GetUserById(request.UserId);

            var dtoOptions = GetDtoOptions(_authContext, request);

            var items = _musicManager.GetInstantMixFromItem(item, user, dtoOptions);

            return GetResult(items, user, request, dtoOptions);
        }

        public object Get(GetInstantMixFromArtistId request)
        {
            var item = _libraryManager.GetItemById(request.Id);

            var user = _userManager.GetUserById(request.UserId);

            var dtoOptions = GetDtoOptions(_authContext, request);

            var items = _musicManager.GetInstantMixFromItem(item, user, dtoOptions);

            return GetResult(items, user, request, dtoOptions);
        }

        public object Get(GetInstantMixFromMusicGenreId request)
        {
            var item = _libraryManager.GetItemById(request.Id);

            var user = _userManager.GetUserById(request.UserId);

            var dtoOptions = GetDtoOptions(_authContext, request);

            var items = _musicManager.GetInstantMixFromItem(item, user, dtoOptions);

            return GetResult(items, user, request, dtoOptions);
        }

        public object Get(GetInstantMixFromSong request)
        {
            var item = _libraryManager.GetItemById(request.Id);

            var user = _userManager.GetUserById(request.UserId);

            var dtoOptions = GetDtoOptions(_authContext, request);

            var items = _musicManager.GetInstantMixFromItem(item, user, dtoOptions);

            return GetResult(items, user, request, dtoOptions);
        }

        public object Get(GetInstantMixFromAlbum request)
        {
            var album = _libraryManager.GetItemById(request.Id);

            var user = _userManager.GetUserById(request.UserId);

            var dtoOptions = GetDtoOptions(_authContext, request);

            var items = _musicManager.GetInstantMixFromItem(album, user, dtoOptions);

            return GetResult(items, user, request, dtoOptions);
        }

        public object Get(GetInstantMixFromPlaylist request)
        {
            var playlist = (Playlist)_libraryManager.GetItemById(request.Id);

            var user = _userManager.GetUserById(request.UserId);

            var dtoOptions = GetDtoOptions(_authContext, request);

            var items = _musicManager.GetInstantMixFromItem(playlist, user, dtoOptions);

            return GetResult(items, user, request, dtoOptions);
        }

        public object Get(GetInstantMixFromMusicGenre request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var dtoOptions = GetDtoOptions(_authContext, request);

            var items = _musicManager.GetInstantMixFromGenres(new[] { request.Name }, user, dtoOptions);

            return GetResult(items, user, request, dtoOptions);
        }

        private object GetResult(List<BaseItem> items, User user, BaseGetSimilarItems request, DtoOptions dtoOptions)
        {
            var list = items;

            var result = new QueryResult<BaseItemDto>
            {
                TotalRecordCount = list.Count
            };

            if (request.Limit.HasValue)
            {
                list = list.Take(request.Limit.Value).ToList();
            }

            var returnList = _dtoService.GetBaseItemDtos(list, dtoOptions, user);

            result.Items = returnList;

            return result;
        }

    }
}
