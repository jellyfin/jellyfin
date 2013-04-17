using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class GetStudios
    /// </summary>
    [Route("/Users/{UserId}/Items/{ParentId}/Studios", "GET")]
    [Route("/Users/{UserId}/Items/Root/Studios", "GET")]
    [Api(Description = "Gets all studios from a given item, folder, or the entire library")]
    public class GetStudios : GetItemsByName
    {
    }

    [Route("/Users/{UserId}/Studios/{Name}/Counts", "GET")]
    [Api(Description = "Gets item counts of library items that a studio appears in")]
    public class GetStudioItemCounts : IReturn<ItemByNameCounts>
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
        [ApiMember(Name = "Name", Description = "The studio name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Class StudiosService
    /// </summary>
    public class StudiosService : BaseItemsByNameService<Studio>
    {
        public StudiosService(IUserManager userManager, ILibraryManager libraryManager, IUserDataRepository userDataRepository)
            : base(userManager, libraryManager, userDataRepository)
        {
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetStudioItemCounts request)
        {
            var user = UserManager.GetUserById(request.UserId);

            var items = user.RootFolder.GetRecursiveChildren(user).Where(i => i.Studios != null && i.Studios.Contains(request.Name, StringComparer.OrdinalIgnoreCase)).ToList();

            var counts = new ItemByNameCounts
            {
                TotalCount = items.Count,

                TrailerCount = items.OfType<Trailer>().Count(),

                MovieCount = items.OfType<Movie>().Count(),

                SeriesCount = items.OfType<Series>().Count(),

                GameCount = items.OfType<BaseGame>().Count(),

                SongCount = items.OfType<AudioCodecs>().Count(),

                AlbumCount = items.OfType<MusicAlbum>().Count()
            };

            return ToOptimizedResult(counts);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetStudios request)
        {
            var result = GetResult(request).Result;

            return ToOptimizedResult(result);
        }
        
        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{Tuple{System.StringFunc{System.Int32}}}.</returns>
        protected override IEnumerable<IbnStub<Studio>> GetAllItems(GetItemsByName request, IEnumerable<BaseItem> items, User user)
        {
            var itemsList = items.Where(i => i.Studios != null).ToList();

            return itemsList
                .SelectMany(i => i.Studios)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(name => new IbnStub<Studio>(name, () => itemsList.Where(i => i.Studios.Contains(name, StringComparer.OrdinalIgnoreCase)), GetEntity));
        }

        /// <summary>
        /// Gets the entity.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Studio}.</returns>
        protected Task<Studio> GetEntity(string name)
        {
            return LibraryManager.GetStudio(name);
        }
    }
}
