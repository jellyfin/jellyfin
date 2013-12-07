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
    }

    public class BaseGetSimilarItems : IReturn<ItemsResult>, IHasItemFields
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid? UserId { get; set; }

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
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, CriticRatingSummary, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, OverviewHtml, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines, TrailerUrls", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }
    }

    /// <summary>
    /// Class SimilarItemsHelper
    /// </summary>
    public static class SimilarItemsHelper
    {
        /// <summary>
        /// Gets the similar items.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="itemRepository">The item repository.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="dtoService">The dto service.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="request">The request.</param>
        /// <param name="includeInSearch">The include in search.</param>
        /// <param name="getSimilarityScore">The get similarity score.</param>
        /// <returns>ItemsResult.</returns>
        internal static ItemsResult GetSimilarItemsResult(IUserManager userManager, IItemRepository itemRepository, ILibraryManager libraryManager, IUserDataManager userDataRepository, IDtoService dtoService, ILogger logger, BaseGetSimilarItemsFromItem request, Func<BaseItem, bool> includeInSearch, Func<BaseItem, BaseItem, int> getSimilarityScore)
        {
            var user = request.UserId.HasValue ? userManager.GetUserById(request.UserId.Value) : null;

            var item = string.IsNullOrEmpty(request.Id) ?
                (request.UserId.HasValue ? user.RootFolder :
                (Folder)libraryManager.RootFolder) : dtoService.GetItemByDtoId(request.Id, request.UserId);

            var fields = request.GetItemFields().ToList();

            var inputItems = user == null
                                 ? libraryManager.RootFolder.GetRecursiveChildren(i => i.Id != item.Id)
                                 : user.RootFolder.GetRecursiveChildren(user, i => i.Id != item.Id);

            var items = GetSimilaritems(item, inputItems, includeInSearch, getSimilarityScore)
                .ToList();

            IEnumerable<BaseItem> returnItems = items;

            if (request.Limit.HasValue)
            {
                returnItems = returnItems.Take(request.Limit.Value);
            }

            var result = new ItemsResult
            {
                Items = returnItems.Select(i => dtoService.GetBaseItemDto(i, fields, user)).ToArray(),

                TotalRecordCount = items.Count
            };

            return result;
        }

        /// <summary>
        /// Gets the similaritems.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="inputItems">The input items.</param>
        /// <param name="includeInSearch">The include in search.</param>
        /// <param name="getSimilarityScore">The get similarity score.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        internal static IEnumerable<BaseItem> GetSimilaritems(BaseItem item, IEnumerable<BaseItem> inputItems, Func<BaseItem, bool> includeInSearch, Func<BaseItem, BaseItem, int> getSimilarityScore)
        {
            inputItems = inputItems.Where(includeInSearch);

            return inputItems.Select(i => new Tuple<BaseItem, int>(i, getSimilarityScore(item, i)))
                .Where(i => i.Item2 > 2)
                .OrderByDescending(i => i.Item2)
                .Select(i => i.Item1);
        }

        private static IEnumerable<string> GetTags(BaseItem item)
        {
            var hasTags = item as IHasTags;
            if (hasTags != null)
            {
                return hasTags.Tags;
            }

            return new List<string>();
        }

        /// <summary>
        /// Gets the similiarity score.
        /// </summary>
        /// <param name="item1">The item1.</param>
        /// <param name="item2">The item2.</param>
        /// <returns>System.Int32.</returns>
        internal static int GetSimiliarityScore(BaseItem item1, BaseItem item2)
        {
            var points = 0;

            if (!string.IsNullOrEmpty(item1.OfficialRating) && string.Equals(item1.OfficialRating, item2.OfficialRating, StringComparison.OrdinalIgnoreCase))
            {
                points += 1;
            }

            // Find common genres
            points += item1.Genres.Where(i => item2.Genres.Contains(i, StringComparer.OrdinalIgnoreCase)).Sum(i => 10);

            // Find common tags
            points += GetTags(item1).Where(i => GetTags(item2).Contains(i, StringComparer.OrdinalIgnoreCase)).Sum(i => 10);

            // Find common studios
            points += item1.Studios.Where(i => item2.Studios.Contains(i, StringComparer.OrdinalIgnoreCase)).Sum(i => 3);

            var item2PeopleNames = item2.People.Select(i => i.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

            points += item1.People.Where(i => item2PeopleNames.ContainsKey(i.Name)).Sum(i =>
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
