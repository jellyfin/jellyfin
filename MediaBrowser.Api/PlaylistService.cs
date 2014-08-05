using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Playlists;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    [Route("/Playlists", "POST", Summary = "Creates a new playlist")]
    public class CreatePlaylist : IReturn<PlaylistCreationResult>
    {
        [ApiMember(Name = "Name", Description = "The name of the new playlist.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Name { get; set; }

        [ApiMember(Name = "Ids", Description = "Item Ids to add to the playlist", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST", AllowMultiple = true)]
        public string Ids { get; set; }

        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserId { get; set; }
    }

    [Route("/Playlists/{Id}/Items", "POST", Summary = "Adds items to a playlist")]
    public class AddToPlaylist : IReturnVoid
    {
        [ApiMember(Name = "Ids", Description = "Item id, comma delimited", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Ids { get; set; }

        [ApiMember(Name = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }
    }

    [Route("/Playlists/{Id}/Items", "DELETE", Summary = "Removes items from a playlist")]
    public class RemoveFromPlaylist : IReturnVoid
    {
        [ApiMember(Name = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }
    }

    [Route("/Playlists/{Id}/Items", "GET", Summary = "Gets the original items of a playlist")]
    public class GetPlaylistItems : IReturn<QueryResult<BaseItemDto>>, IHasItemFields
    {
        [ApiMember(Name = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid? UserId { get; set; }

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
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, CriticRatingSummary, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }
    }

    [Authenticated]
    public class PlaylistService : BaseApiService
    {
        private readonly IPlaylistManager _playlistManager;
        private readonly IDtoService _dtoService;
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;

        public PlaylistService(IDtoService dtoService, IPlaylistManager playlistManager, IUserManager userManager, ILibraryManager libraryManager)
        {
            _dtoService = dtoService;
            _playlistManager = playlistManager;
            _userManager = userManager;
            _libraryManager = libraryManager;
        }

        public object Post(CreatePlaylist request)
        {
            var task = _playlistManager.CreatePlaylist(new PlaylistCreationOptions
            {
                Name = request.Name,
                ItemIdList = (request.Ids ?? string.Empty).Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToList(),
                UserId = request.UserId
            });

            var item = task.Result;

            var dto = _dtoService.GetBaseItemDto(item, new List<ItemFields>());

            return ToOptimizedResult(new PlaylistCreationResult
            {
                Id = dto.Id
            });
        }

        public void Post(AddToPlaylist request)
        {
            var task = _playlistManager.AddToPlaylist(request.Id, request.Ids.Split(','));

            Task.WaitAll(task);
        }

        public void Delete(RemoveFromPlaylist request)
        {
            //var task = _playlistManager.RemoveFromPlaylist(request.Id, request.Ids.Split(',').Select(i => new Guid(i)));

            //Task.WaitAll(task);
        }

        public object Get(GetPlaylistItems request)
        {
            var playlist = (Playlist)_libraryManager.GetItemById(request.Id);
            var user = request.UserId.HasValue ? _userManager.GetUserById(request.UserId.Value) : null;
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

            var dtos = items
                   .Select(i => _dtoService.GetBaseItemDto(i, request.GetItemFields().ToList(), user))
                   .ToArray();

            var result = new ItemsResult
            {
                Items = dtos,
                TotalRecordCount = count
            };

            return ToOptimizedResult(result);
        }
    }
}
