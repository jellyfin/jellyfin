using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.UserLibrary
{
    [Route("/MusicGenres", "GET", Summary = "Gets all music genres from a given item, folder, or the entire library")]
    public class GetMusicGenres : GetItemsByName
    {
    }

    [Route("/MusicGenres/{Name}", "GET", Summary = "Gets a music genre, by name")]
    public class GetMusicGenre : IReturn<BaseItemDto>
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The genre name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }
    }

    [Authenticated]
    public class MusicGenresService : BaseItemsByNameService<MusicGenre>
    {
        public MusicGenresService(
            ILogger<MusicGenresService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IUserManager userManager,
            ILibraryManager libraryManager,
            IUserDataManager userDataRepository,
            IDtoService dtoService,
            IAuthorizationContext authorizationContext)
            : base(
                logger,
                serverConfigurationManager,
                httpResultFactory,
                userManager,
                libraryManager,
                userDataRepository,
                dtoService,
                authorizationContext)
        {
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetMusicGenre request)
        {
            var result = GetItem(request);

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the item.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        private BaseItemDto GetItem(GetMusicGenre request)
        {
            var dtoOptions = GetDtoOptions(AuthorizationContext, request);

            var item = GetMusicGenre(request.Name, LibraryManager, dtoOptions);

            if (!request.UserId.Equals(Guid.Empty))
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
        public object Get(GetMusicGenres request)
        {
            var result = GetResultSlim(request);

            return ToOptimizedResult(result);
        }

        protected override QueryResult<(BaseItem, ItemCounts)> GetItems(GetItemsByName request, InternalItemsQuery query)
        {
            return LibraryManager.GetMusicGenres(query);
        }

        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable{Tuple{System.StringFunc{System.Int32}}}.</returns>
        protected override IEnumerable<BaseItem> GetAllItems(GetItemsByName request, IList<BaseItem> items)
        {
            throw new NotImplementedException();
        }
    }
}
