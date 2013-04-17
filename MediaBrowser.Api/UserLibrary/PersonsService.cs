using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
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
    [Route("/Users/{UserId}/Items/{ParentId}/Persons", "GET")]
    [Route("/Users/{UserId}/Items/Root/Persons", "GET")]
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
    [Route("/Users/{UserId}/Persons/{Name}/Counts", "GET")]
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
    /// Class PersonsService
    /// </summary>
    public class PersonsService : BaseItemsByNameService<Person>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PersonsService"/> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        public PersonsService(IUserManager userManager, ILibraryManager libraryManager, IUserDataRepository userDataRepository)
            : base(userManager, libraryManager, userDataRepository)
        {
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
            var user = UserManager.GetUserById(request.UserId);

            var items = user.RootFolder.GetRecursiveChildren(user).Where(i => i.People != null && i.People.Any(p => string.Equals(p.Name, request.Name, StringComparison.OrdinalIgnoreCase))).ToList();

            var counts = new ItemByNameCounts
            {
                TotalCount = items.Count,

                TrailerCount = items.OfType<Trailer>().Count(),

                MovieCount = items.OfType<Movie>().Count(),

                SeriesCount = items.OfType<Series>().Count(),

                GameCount = items.OfType<BaseGame>().Count(),

                SongCount = items.OfType<AudioCodecs>().Count(),

                AlbumCount = items.OfType<MusicAlbum>().Count(),

                EpisodeGuestStarCount = items.OfType<Episode>().Count(i => i.People.Any(p => string.Equals(p.Name, request.Name, StringComparison.OrdinalIgnoreCase) && string.Equals(p.Type, PersonType.GuestStar)))
            };

            return ToOptimizedResult(counts);
        }

        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{Tuple{System.StringFunc{System.Int32}}}.</returns>
        protected override IEnumerable<IbnStub<Person>> GetAllItems(GetItemsByName request, IEnumerable<BaseItem> items, User user)
        {
            var inputPersonTypes = ((GetPersons)request).PersonTypes;
            var personTypes = string.IsNullOrEmpty(inputPersonTypes) ? new string[] { } : inputPersonTypes.Split(',');

            var itemsList = items.Where(i => i.People != null).ToList();

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

                    return itemsList.Where(i => i.People.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && personTypes.Contains(p.Type ?? string.Empty, StringComparer.OrdinalIgnoreCase)));
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

                people.Where(p => personTypes.Contains(p.Type ?? string.Empty, StringComparer.OrdinalIgnoreCase));
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
