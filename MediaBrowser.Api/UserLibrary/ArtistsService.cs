using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

    /// <summary>
    /// Class GetArtistsItemCounts
    /// </summary>
    [Route("/Artists/{Name}/Counts", "GET")]
    [Api(Description = "Gets item counts of library items that an artist appears in")]
    public class GetArtistsItemCounts : IReturn<ItemByNameCounts>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The artist name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
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
    public class ArtistsService : BaseItemsByNameService<Artist>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ArtistsService" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="itemRepo">The item repo.</param>
        public ArtistsService(IUserManager userManager, ILibraryManager libraryManager, IUserDataRepository userDataRepository, IItemRepository itemRepo, IDtoService dtoService)
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
            var result = GetItem(request).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the item.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        private async Task<BaseItemDto> GetItem(GetArtist request)
        {
            var item = await GetArtist(request.Name, LibraryManager).ConfigureAwait(false);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true));

            if (request.UserId.HasValue)
            {
                var user = UserManager.GetUserById(request.UserId.Value);

                return await DtoService.GetBaseItemDto(item, fields.ToList(), user).ConfigureAwait(false);
            }

            return await DtoService.GetBaseItemDto(item, fields.ToList()).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetArtistsItemCounts request)
        {
            var name = DeSlugArtistName(request.Name, LibraryManager);

            var items = GetItems(request.UserId).Where(i =>
            {
                var song = i as Audio;

                if (song != null)
                {
                    return song.HasArtist(name);
                }

                var musicVideo = i as MusicVideo;

                if (musicVideo != null)
                {
                    return musicVideo.HasArtist(name);
                }
                
                return false;

            }).ToList();

            var counts = new ItemByNameCounts
            {
                TotalCount = items.Count,

                SongCount = items.OfType<Audio>().Count(),

                AlbumCount = items.Select(i => i.Parent).OfType<MusicAlbum>().Distinct().Count(),

                MusicVideoCount = items.OfType<MusicVideo>().Count(i => i.HasArtist(name))
            };

            return ToOptimizedResult(counts);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetArtists request)
        {
            var result = GetResult(request).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable{Tuple{System.StringFunc{System.Int32}}}.</returns>
        protected override IEnumerable<IbnStub<Artist>> GetAllItems(GetItemsByName request, IEnumerable<BaseItem> items)
        {
            var itemsList = items.OfType<Audio>().ToList();

            return itemsList
                .SelectMany(i =>
                {
                    var list = new List<string>();

                    if (!string.IsNullOrEmpty(i.AlbumArtist))
                    {
                        list.Add(i.AlbumArtist);
                    }
                    list.AddRange(i.Artists);

                    return list;
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(name => new IbnStub<Artist>(name, () => itemsList.Where(i => i.HasArtist(name)), GetEntity));
        }

        /// <summary>
        /// Gets the entity.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Artist}.</returns>
        protected Task<Artist> GetEntity(string name)
        {
            return LibraryManager.GetArtist(name);
        }
    }
}
