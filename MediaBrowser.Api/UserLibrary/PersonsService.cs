using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Services;
using System;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class GetPersons
    /// </summary>
    [Route("/Persons", "GET", Summary = "Gets all persons from a given item, folder, or the entire library")]
    public class GetPersons : GetItemsByName
    {
    }

    /// <summary>
    /// Class GetPerson
    /// </summary>
    [Route("/Persons/{Name}", "GET", Summary = "Gets a person, by name")]
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
        public Guid UserId { get; set; }
    }

    /// <summary>
    /// Class PersonsService
    /// </summary>
    [Authenticated]
    public class PersonsService : BaseItemsByNameService<Person>
    {
        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetPerson request)
        {
            var result = GetItem(request);

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the item.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        private BaseItemDto GetItem(GetPerson request)
        {
            var dtoOptions = GetDtoOptions(AuthorizationContext, request);

            var item = GetPerson(request.Name, LibraryManager, dtoOptions);

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
        public object Get(GetPersons request)
        {
            return GetResultSlim(request);
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

        protected override QueryResult<Tuple<BaseItem, ItemCounts>> GetItems(GetItemsByName request, InternalItemsQuery query)
        {
            var items = LibraryManager.GetPeopleItems(new InternalPeopleQuery
            {
                PersonTypes = query.PersonTypes,
                NameContains = query.NameContains ?? query.SearchTerm
            });

            return new QueryResult<Tuple<BaseItem, ItemCounts>>
            {
                TotalRecordCount = items.Count,
                Items = items.Take(query.Limit ?? int.MaxValue).Select(i => new Tuple<BaseItem, ItemCounts>(i, new ItemCounts())).ToArray()
            };
        }

        public PersonsService(IUserManager userManager, ILibraryManager libraryManager, IUserDataManager userDataRepository, IItemRepository itemRepository, IDtoService dtoService, IAuthorizationContext authorizationContext) : base(userManager, libraryManager, userDataRepository, itemRepository, dtoService, authorizationContext)
        {
        }
    }
}
