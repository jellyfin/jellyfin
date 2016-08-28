using System;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using ServiceStack;
using System.Collections.Generic;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class GetArtists
    /// </summary>
    [Route("/Artists", "GET", Summary = "Gets all artists from a given item, folder, or the entire library")]
    public class GetArtists : GetItemsByName
    {
    }

    [Route("/Artists/AlbumArtists", "GET", Summary = "Gets all album artists from a given item, folder, or the entire library")]
    public class GetAlbumArtists : GetItemsByName
    {
    }

    [Route("/Artists/{Name}", "GET", Summary = "Gets an artist, by name")]
    public class GetArtist : IReturn<BaseItemDto>
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The artist name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }
    }

    /// <summary>
    /// Class ArtistsService
    /// </summary>
    [Authenticated]
    public class ArtistsService : BaseItemsByNameService<MusicArtist>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ArtistsService" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="itemRepo">The item repo.</param>
        public ArtistsService(IUserManager userManager, ILibraryManager libraryManager, IUserDataManager userDataRepository, IItemRepository itemRepo, IDtoService dtoService)
            : base(userManager, libraryManager, userDataRepository, itemRepo, dtoService)
        {
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetArtist request)
        {
            var result = GetItem(request);

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the item.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        private BaseItemDto GetItem(GetArtist request)
        {
            var item = GetArtist(request.Name, LibraryManager);

            var dtoOptions = GetDtoOptions(request);

            if (!string.IsNullOrWhiteSpace(request.UserId))
            {
                var user = UserManager.GetUserById(request.UserId);

                return DtoService.GetBaseItemDto(item, dtoOptions, user);
            }

            return DtoService.GetBaseItemDto(item, dtoOptions);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetArtists request)
        {
            if (string.IsNullOrWhiteSpace(request.IncludeItemTypes))
            {
                //request.IncludeItemTypes = "Audio,MusicVideo";
            }

            var result = GetResultSlim(request);

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetAlbumArtists request)
        {
            if (string.IsNullOrWhiteSpace(request.IncludeItemTypes))
            {
                //request.IncludeItemTypes = "Audio,MusicVideo";
            }

            var result = GetResultSlim(request);

            return ToOptimizedResult(result);
        }

        protected override QueryResult<Tuple<BaseItem, ItemCounts>> GetItems(GetItemsByName request, InternalItemsQuery query)
        {
            if (request is GetAlbumArtists)
            {
                return LibraryManager.GetAlbumArtists(query);
            }

            return LibraryManager.GetArtists(query);
        }

        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable{Tuple{System.StringFunc{System.Int32}}}.</returns>
        protected override IEnumerable<BaseItem> GetAllItems(GetItemsByName request, IEnumerable<BaseItem> items)
        {
            throw new NotImplementedException();
        }
    }
}
