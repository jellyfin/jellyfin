using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    [Route("/Items/Filters", "GET", Summary = "Gets branding configuration")]
    public class GetQueryFilters : IReturn<QueryFilters>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }

        [ApiMember(Name = "ParentId", Description = "Specify this to localize the search to a specific item or folder. Omit to use the root", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ParentId { get; set; }

        [ApiMember(Name = "IncludeItemTypes", Description = "Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string IncludeItemTypes { get; set; }

        [ApiMember(Name = "MediaTypes", Description = "Optional filter by MediaType. Allows multiple, comma delimited.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string MediaTypes { get; set; }

        public string[] GetMediaTypes()
        {
            return (MediaTypes ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] GetIncludeItemTypes()
        {
            return (IncludeItemTypes ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }

    [Authenticated]
    public class FilterService : BaseApiService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;

        public FilterService(ILibraryManager libraryManager, IUserManager userManager)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
        }

        public async Task<object> Get(GetQueryFilters request)
        {
            var parentItem = string.IsNullOrEmpty(request.ParentId) ? null : _libraryManager.GetItemById(request.ParentId);
            var user = !string.IsNullOrWhiteSpace(request.UserId) ? _userManager.GetUserById(request.UserId) : null;

            var item = string.IsNullOrEmpty(request.ParentId) ?
               user == null ? _libraryManager.RootFolder : user.RootFolder :
               parentItem;

            var result = await ((Folder)item).GetItems(GetItemsQuery(request, user));

            return ToOptimizedResult(GetFilters(result.Items));
        }

        private QueryFilters GetFilters(BaseItem[] items)
        {
            var result = new QueryFilters();

            result.Years = items.Select(i => i.ProductionYear ?? -1)
                .Where(i => i > 0)
                .Distinct()
                .OrderBy(i => i)
                .ToArray();

            result.Genres = items.SelectMany(i => i.Genres)
                .DistinctNames()
                .OrderBy(i => i)
                .ToArray();

            result.Tags = items
                .SelectMany(i => i.Tags)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(i => i)
                .ToArray();

            result.OfficialRatings = items
                .Select(i => i.OfficialRating)
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(i => i)
                .ToArray();

            return result;
        }

        private InternalItemsQuery GetItemsQuery(GetQueryFilters request, User user)
        {
            var query = new InternalItemsQuery
            {
                User = user,
                MediaTypes = request.GetMediaTypes(),
                IncludeItemTypes = request.GetIncludeItemTypes(),
                Recursive = true,
                EnableTotalRecordCount = false,
                DtoOptions = new Controller.Dto.DtoOptions
                {
                    Fields = new List<ItemFields> { ItemFields.Genres, ItemFields.Tags },
                    EnableImages = false,
                    EnableUserData = false
                }
            };

            return query;
        }
    }
}
