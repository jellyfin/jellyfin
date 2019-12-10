using System;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Playlists;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api
{
    [Route("/Playlists", "POST", Summary = "Creates a new playlist")]
    public class CreatePlaylist : IReturn<PlaylistCreationResult>
    {
        [ApiMember(Name = "Name", Description = "The name of the new playlist.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Name { get; set; }

        [ApiMember(Name = "Ids", Description = "Item Ids to add to the playlist", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST", AllowMultiple = true)]
        public string Ids { get; set; }

        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid UserId { get; set; }

        [ApiMember(Name = "MediaType", Description = "The playlist media type", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string MediaType { get; set; }
    }

    [Route("/Playlists/{Id}/Items", "POST", Summary = "Adds items to a playlist")]
    public class AddToPlaylist : IReturnVoid
    {
        [ApiMember(Name = "Ids", Description = "Item id, comma delimited", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Ids { get; set; }

        [ApiMember(Name = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public Guid UserId { get; set; }
    }

    [Route("/Playlists/{Id}/Items/{ItemId}/Move/{NewIndex}", "POST", Summary = "Moves a playlist item")]
    public class MoveItem : IReturnVoid
    {
        [ApiMember(Name = "ItemId", Description = "ItemId", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string ItemId { get; set; }

        [ApiMember(Name = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "NewIndex", Description = "NewIndex", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public int NewIndex { get; set; }
    }

    [Route("/Playlists/{Id}/Items", "DELETE", Summary = "Removes items from a playlist")]
    public class RemoveFromPlaylist : IReturnVoid
    {
        [ApiMember(Name = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }

        [ApiMember(Name = "EntryIds", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "DELETE")]
        public string EntryIds { get; set; }
    }

    [Route("/Playlists/{Id}/Items", "GET", Summary = "Gets the original items of a playlist")]
    public class GetPlaylistItems : IReturn<QueryResult<BaseItemDto>>, IHasDtoOptions
    {
        [ApiMember(Name = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Skips over a given number of items within the results. Use for paging.
        /// </summary>
        /// <value>The start index.</value>
        [ApiMember(Name = "StartIndex", Description = "Optional. The record index to start at. All items with a lower index will be dropped from the results.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? StartIndex { get; set; }

        /// <summary>
        /// The maximum number of items to return
        /// </summary>
        /// <value>The limit.</value>
        [ApiMember(Name = "Limit", Description = "Optional. The maximum number of records to return", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? Limit { get; set; }

        /// <summary>
        /// Fields to return within the items, in addition to basic information
        /// </summary>
        /// <value>The fields.</value>
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }

        [ApiMember(Name = "EnableImages", Description = "Optional, include image information in output", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableImages { get; set; }

        [ApiMember(Name = "EnableUserData", Description = "Optional, include user data", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableUserData { get; set; }

        [ApiMember(Name = "ImageTypeLimit", Description = "Optional, the max number of images to return, per image type", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? ImageTypeLimit { get; set; }

        [ApiMember(Name = "EnableImageTypes", Description = "Optional. The image types to include in the output.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string EnableImageTypes { get; set; }
    }

    [Authenticated]
    public class PlaylistService : BaseApiService
    {
        private readonly IPlaylistManager _playlistManager;
        private readonly IDtoService _dtoService;
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IAuthorizationContext _authContext;

        public PlaylistService(
            ILogger<PlaylistService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IDtoService dtoService,
            IPlaylistManager playlistManager,
            IUserManager userManager,
            ILibraryManager libraryManager,
            IAuthorizationContext authContext)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _dtoService = dtoService;
            _playlistManager = playlistManager;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _authContext = authContext;
        }

        public void Post(MoveItem request)
        {
            _playlistManager.MoveItem(request.Id, request.ItemId, request.NewIndex);
        }

        public async Task<object> Post(CreatePlaylist request)
        {
            var result = await _playlistManager.CreatePlaylist(new PlaylistCreationRequest
            {
                Name = request.Name,
                ItemIdList = GetGuids(request.Ids),
                UserId = request.UserId,
                MediaType = request.MediaType

            }).ConfigureAwait(false);

            return ToOptimizedResult(result);
        }

        public void Post(AddToPlaylist request)
        {
            _playlistManager.AddToPlaylist(request.Id, GetGuids(request.Ids), request.UserId);
        }

        public void Delete(RemoveFromPlaylist request)
        {
            _playlistManager.RemoveFromPlaylist(request.Id, request.EntryIds.Split(','));
        }

        public object Get(GetPlaylistItems request)
        {
            var playlist = (Playlist)_libraryManager.GetItemById(request.Id);
            var user = !request.UserId.Equals(Guid.Empty) ? _userManager.GetUserById(request.UserId) : null;

            var items = playlist.GetManageableItems().ToArray();

            var count = items.Length;

            if (request.StartIndex.HasValue)
            {
                items = items.Skip(request.StartIndex.Value).ToArray();
            }

            if (request.Limit.HasValue)
            {
                items = items.Take(request.Limit.Value).ToArray();
            }

            var dtoOptions = GetDtoOptions(_authContext, request);

            var dtos = _dtoService.GetBaseItemDtos(items.Select(i => i.Item2).ToList(), dtoOptions, user);

            for (int index = 0; index < dtos.Count; index++)
            {
                dtos[index].PlaylistItemId = items[index].Item1.Id;
            }

            var result = new QueryResult<BaseItemDto>
            {
                Items = dtos,
                TotalRecordCount = count
            };

            return ToOptimizedResult(result);
        }
    }
}
