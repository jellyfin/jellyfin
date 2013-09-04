using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
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
    /// Class GetPersons
    /// </summary>
    [Route("/Persons", "GET")]
    [Api(Description = "Gets all persons from a given item, folder, or the entire library")]
    public class GetPersons : GetItemsByName
    {
        /// <summary>
        /// Gets or sets the person types.
        /// </summary>
        /// <value>The person types.</value>
        public string PersonTypes { get; set; }
    }

    /// <summary>
    /// Class GetPersonItemCounts
    /// </summary>
    [Route("/Persons/{Name}/Counts", "GET")]
    [Api(Description = "Gets item counts of library items that a person appears in")]
    public class GetPersonItemCounts : IReturn<ItemByNameCounts>
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
        [ApiMember(Name = "Name", Description = "The person name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Class GetPerson
    /// </summary>
    [Route("/Persons/{Name}", "GET")]
    [Api(Description = "Gets a person, by name")]
    public class GetPerson : IReturn<BaseItemDto>
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "The person name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid? UserId { get; set; }
    }

    /// <summary>
    /// Class PersonsService
    /// </summary>
    public class PersonsService : BaseItemsByNameService<Person>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PersonsService" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="itemRepo">The item repo.</param>
        public PersonsService(IUserManager userManager, ILibraryManager libraryManager, IUserDataRepository userDataRepository, IItemRepository itemRepo, IDtoService dtoService)
            : base(userManager, libraryManager, userDataRepository, itemRepo, dtoService)
        {
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetPerson request)
        {
            var result = GetItem(request).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the item.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        private async Task<BaseItemDto> GetItem(GetPerson request)
        {
            var item = await GetPerson(request.Name, LibraryManager).ConfigureAwait(false);

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
        public object Get(GetPersons request)
        {
            var result = GetResult(request).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetPersonItemCounts request)
        {
            var name = DeSlugPersonName(request.Name, LibraryManager);

            var items = GetItems(request.UserId).Where(i => i.People != null && i.People.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))).ToList();

            var counts = new ItemByNameCounts
            {
                TotalCount = items.Count,

                TrailerCount = items.OfType<Trailer>().Count(),

                MovieCount = items.OfType<Movie>().Count(),

                SeriesCount = items.OfType<Series>().Count(),

                GameCount = items.OfType<Game>().Count(),

                SongCount = items.OfType<Audio>().Count(),

                AlbumCount = items.OfType<MusicAlbum>().Count(),

                EpisodeCount = items.OfType<Episode>().Count(),

                MusicVideoCount = items.OfType<MusicVideo>().Count(),

                AdultVideoCount = items.OfType<AdultVideo>().Count()
            };

            return ToOptimizedResult(counts);
        }

        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable{Tuple{System.StringFunc{System.Int32}}}.</returns>
        protected override IEnumerable<IbnStub<Person>> GetAllItems(GetItemsByName request, IEnumerable<BaseItem> items)
        {
            var inputPersonTypes = ((GetPersons)request).PersonTypes;
            var personTypes = string.IsNullOrEmpty(inputPersonTypes) ? new string[] { } : inputPersonTypes.Split(',');

            var itemsList = items.ToList();

            // Either get all people, or all people filtered by a specific person type
            var allPeople = GetAllPeople(itemsList, personTypes);

            return allPeople
                .Select(i => i.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)

                .Select(name => new IbnStub<Person>(name, () =>
                {
                    if (personTypes.Length == 0)
                    {
                        return itemsList.Where(i => i.People.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
                    }

                    return itemsList.Where(i => i.People.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && (personTypes.Contains(p.Type ?? string.Empty, StringComparer.OrdinalIgnoreCase) || personTypes.Contains(p.Role ?? string.Empty, StringComparer.OrdinalIgnoreCase))));
                }, GetEntity)
            );
        }

        /// <summary>
        /// Gets all people.
        /// </summary>
        /// <param name="itemsList">The items list.</param>
        /// <param name="personTypes">The person types.</param>
        /// <returns>IEnumerable{PersonInfo}.</returns>
        private IEnumerable<PersonInfo> GetAllPeople(IEnumerable<BaseItem> itemsList, string[] personTypes)
        {
            var people = itemsList.SelectMany(i => i.People.OrderBy(p => p.Type));

            return personTypes.Length == 0 ?

                people :

                people.Where(p => personTypes.Contains(p.Type ?? string.Empty, StringComparer.OrdinalIgnoreCase) || personTypes.Contains(p.Role ?? string.Empty, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the entity.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Genre}.</returns>
        protected Task<Person> GetEntity(string name)
        {
            return LibraryManager.GetPerson(name);
        }
    }
}
