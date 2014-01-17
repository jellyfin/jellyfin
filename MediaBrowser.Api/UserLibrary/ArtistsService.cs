using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class GetArtists
    /// </summary>
    [Route("/Artists", "GET")]
    [Api(Description = "Gets all artists from a given item, folder, or the entire library")]
    public class GetArtists : GetItemsByName
    {
    }

    [Route("/Artists/{Name}", "GET")]
    [Api(Description = "Gets an artist, by name")]
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
        public Guid? UserId { get; set; }
    }

    /// <summary>
    /// Class ArtistsService
    /// </summary>
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

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true));

            if (request.UserId.HasValue)
            {
                var user = UserManager.GetUserById(request.UserId.Value);

                return DtoService.GetBaseItemDto(item, fields.ToList(), user);
            }

            return DtoService.GetBaseItemDto(item, fields.ToList());
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetArtists request)
        {
            var result = GetResult(request);

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable{Tuple{System.StringFunc{System.Int32}}}.</returns>
        protected override IEnumerable<MusicArtist> GetAllItems(GetItemsByName request, IEnumerable<BaseItem> items)
        {
            return LibraryManager.GetAllArtists(items)
                .Select(name =>
                {
                    try
                    {
                        return LibraryManager.GetArtist(name);
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorException("Error getting artist {0}", ex, name);
                        return null;
                    }

                }).Where(i => i != null);
        }

        protected override IEnumerable<BaseItem> GetLibraryItems(MusicArtist item, IEnumerable<BaseItem> libraryItems)
        {
            return libraryItems.OfType<IHasArtist>().Where(i => i.HasArtist(item.Name)).Cast<BaseItem>();
        }
    }
}
