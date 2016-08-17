using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class BaseGetSimilarItemsFromItem
    /// </summary>
    public class BaseGetSimilarItemsFromItem : BaseGetSimilarItems
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        public string ExcludeArtistIds { get; set; }
    }

    public class BaseGetSimilarItems : IReturn<ItemsResult>, IHasDtoOptions
    {
        [ApiMember(Name = "EnableImages", Description = "Optional, include image information in output", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableImages { get; set; }

        [ApiMember(Name = "EnableUserData", Description = "Optional, include user data", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableUserData { get; set; }

        [ApiMember(Name = "ImageTypeLimit", Description = "Optional, the max number of images to return, per image type", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? ImageTypeLimit { get; set; }

        [ApiMember(Name = "EnableImageTypes", Description = "Optional. The image types to include in the output.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string EnableImageTypes { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }

        /// <summary>
        /// The maximum number of items to return
        /// </summary>
        /// <value>The limit.</value>
        [ApiMember(Name = "Limit", Description = "Optional. The maximum number of records to return", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? Limit { get; set; }

        /// <summary>
        /// Fields to return within the items, in addition to basic information
        /// </summary>
        /// <value>The fields.</value>
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, CriticRatingSummary, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines, TrailerUrls", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }
    }

    /// <summary>
    /// Class SimilarItemsHelper
    /// </summary>
    public static class SimilarItemsHelper
    {
        internal static async Task<QueryResult<BaseItemDto>> GetSimilarItemsResult(DtoOptions dtoOptions, IUserManager userManager, IItemRepository itemRepository, ILibraryManager libraryManager, IUserDataManager userDataRepository, IDtoService dtoService, ILogger logger, BaseGetSimilarItemsFromItem request, Type[] includeTypes, Func<BaseItem, List<PersonInfo>, List<PersonInfo>, BaseItem, int> getSimilarityScore)
        {
            var user = !string.IsNullOrWhiteSpace(request.UserId) ? userManager.GetUserById(request.UserId) : null;

            var item = string.IsNullOrEmpty(request.Id) ?
                (!string.IsNullOrWhiteSpace(request.UserId) ? user.RootFolder :
                libraryManager.RootFolder) : libraryManager.GetItemById(request.Id);

            var query = new InternalItemsQuery(user)
            {
                IncludeItemTypes = includeTypes.Select(i => i.Name).ToArray(),
                Recursive = true
            };

            // ExcludeArtistIds
            if (!string.IsNullOrEmpty(request.ExcludeArtistIds))
            {
                query.ExcludeArtistIds = request.ExcludeArtistIds.Split('|');
            }

            var inputItems = libraryManager.GetItemList(query);

            var items = GetSimilaritems(item, libraryManager, inputItems, getSimilarityScore)
                .ToList();

            IEnumerable<BaseItem> returnItems = items;

            if (request.Limit.HasValue)
            {
                returnItems = returnItems.Take(request.Limit.Value);
            }

            var dtos = await dtoService.GetBaseItemDtos(returnItems, dtoOptions, user).ConfigureAwait(false);

            return new QueryResult<BaseItemDto>
            {
                Items = dtos.ToArray(),

                TotalRecordCount = items.Count
            };
        }

        /// <summary>
        /// Gets the similaritems.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="inputItems">The input items.</param>
        /// <param name="getSimilarityScore">The get similarity score.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        internal static IEnumerable<BaseItem> GetSimilaritems(BaseItem item, ILibraryManager libraryManager, IEnumerable<BaseItem> inputItems, Func<BaseItem, List<PersonInfo>, List<PersonInfo>, BaseItem, int> getSimilarityScore)
        {
            var itemId = item.Id;
            inputItems = inputItems.Where(i => i.Id != itemId);
            var itemPeople = libraryManager.GetPeople(item);
            var allPeople = libraryManager.GetPeople(new InternalPeopleQuery
            {
                AppearsInItemId = item.Id
            });

            return inputItems.Select(i => new Tuple<BaseItem, int>(i, getSimilarityScore(item, itemPeople, allPeople, i)))
                .Where(i => i.Item2 > 2)
                .OrderByDescending(i => i.Item2)
                .Select(i => i.Item1);
        }

        private static IEnumerable<string> GetTags(BaseItem item)
        {
            return item.Tags;
        }

        private static IEnumerable<string> GetKeywords(BaseItem item)
        {
            return item.Keywords;
        }

        /// <summary>
        /// Gets the similiarity score.
        /// </summary>
        /// <param name="item1">The item1.</param>
        /// <param name="item1People">The item1 people.</param>
        /// <param name="allPeople">All people.</param>
        /// <param name="item2">The item2.</param>
        /// <returns>System.Int32.</returns>
        internal static int GetSimiliarityScore(BaseItem item1, List<PersonInfo> item1People, List<PersonInfo> allPeople, BaseItem item2)
        {
            var points = 0;

            if (!string.IsNullOrEmpty(item1.OfficialRating) && string.Equals(item1.OfficialRating, item2.OfficialRating, StringComparison.OrdinalIgnoreCase))
            {
                points += 10;
            }

            // Find common genres
            points += item1.Genres.Where(i => item2.Genres.Contains(i, StringComparer.OrdinalIgnoreCase)).Sum(i => 10);

            // Find common tags
            points += GetTags(item1).Where(i => GetTags(item2).Contains(i, StringComparer.OrdinalIgnoreCase)).Sum(i => 10);

            // Find common keywords
            points += GetKeywords(item1).Where(i => GetKeywords(item2).Contains(i, StringComparer.OrdinalIgnoreCase)).Sum(i => 10);

            // Find common studios
            points += item1.Studios.Where(i => item2.Studios.Contains(i, StringComparer.OrdinalIgnoreCase)).Sum(i => 3);

            var item2PeopleNames = allPeople.Where(i => i.ItemId == item2.Id)
                .Select(i => i.Name)
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .DistinctNames()
                .ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

            points += item1People.Where(i => item2PeopleNames.ContainsKey(i.Name)).Sum(i =>
            {
                if (string.Equals(i.Type, PersonType.Director, StringComparison.OrdinalIgnoreCase) || string.Equals(i.Role, PersonType.Director, StringComparison.OrdinalIgnoreCase))
                {
                    return 5;
                }
                if (string.Equals(i.Type, PersonType.Actor, StringComparison.OrdinalIgnoreCase) || string.Equals(i.Role, PersonType.Actor, StringComparison.OrdinalIgnoreCase))
                {
                    return 3;
                }
                if (string.Equals(i.Type, PersonType.Composer, StringComparison.OrdinalIgnoreCase) || string.Equals(i.Role, PersonType.Composer, StringComparison.OrdinalIgnoreCase))
                {
                    return 3;
                }
                if (string.Equals(i.Type, PersonType.GuestStar, StringComparison.OrdinalIgnoreCase) || string.Equals(i.Role, PersonType.GuestStar, StringComparison.OrdinalIgnoreCase))
                {
                    return 3;
                }
                if (string.Equals(i.Type, PersonType.Writer, StringComparison.OrdinalIgnoreCase) || string.Equals(i.Role, PersonType.Writer, StringComparison.OrdinalIgnoreCase))
                {
                    return 2;
                }

                return 1;
            });

            if (item1.ProductionYear.HasValue && item2.ProductionYear.HasValue)
            {
                var diff = Math.Abs(item1.ProductionYear.Value - item2.ProductionYear.Value);

                // Add if they came out within the same decade
                if (diff < 10)
                {
                    points += 2;
                }

                // And more if within five years
                if (diff < 5)
                {
                    points += 2;
                }
            }

            return points;
        }

    }
}
