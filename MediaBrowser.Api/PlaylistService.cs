using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Playlists;
using MediaBrowser.Model.Querying;
using ServiceStack;
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

    [Authenticated]
    public class PlaylistService : BaseApiService
    {
        private readonly IPlaylistManager _playlistManager;
        private readonly IDtoService _dtoService;

        public PlaylistService(IDtoService dtoService, IPlaylistManager playlistManager)
        {
            _dtoService = dtoService;
            _playlistManager = playlistManager;
        }

        public object Post(CreatePlaylist request)
        {
            var task = _playlistManager.CreatePlaylist(new PlaylistCreationOptions
            {
                Name = request.Name,
                ItemIdList = (request.Ids ?? string.Empty).Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToList()
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
    }
}
