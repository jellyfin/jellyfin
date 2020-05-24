using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Extensions
{
    /// <summary>
    /// Request Extensions.
    /// </summary>
    public static class RequestExtensions
    {
        /// <summary>
        /// Get Order By.
        /// </summary>
        /// <param name="sortBy">Sort By. Comma delimited string.</param>
        /// <param name="requestedSortOrder">Sort Order. Comma delimited string.</param>
        /// <returns>Order By.</returns>
        public static ValueTuple<string, SortOrder>[] GetOrderBy(string sortBy, string requestedSortOrder)
        {
            var val = sortBy;

            if (string.IsNullOrEmpty(val))
            {
                return Array.Empty<ValueTuple<string, SortOrder>>();
            }

            var vals = val.Split(',');
            if (string.IsNullOrWhiteSpace(requestedSortOrder))
            {
                requestedSortOrder = "Ascending";
            }

            var sortOrders = requestedSortOrder.Split(',');

            var result = new ValueTuple<string, SortOrder>[vals.Length];

            for (var i = 0; i < vals.Length; i++)
            {
                var sortOrderIndex = sortOrders.Length > i ? i : 0;

                var sortOrderValue = sortOrders.Length > sortOrderIndex ? sortOrders[sortOrderIndex] : null;
                var sortOrder = string.Equals(sortOrderValue, "Descending", StringComparison.OrdinalIgnoreCase)
                    ? SortOrder.Descending
                    : SortOrder.Ascending;

                result[i] = new ValueTuple<string, SortOrder>(vals[i], sortOrder);
            }

            return result;
        }

        /// <summary>
        /// Gets the item fields.
        /// </summary>
        /// <param name="fields">The fields.</param>
        /// <returns>IEnumerable{ItemFields}.</returns>
        public static ItemFields[] GetItemFields(string fields)
        {
            if (string.IsNullOrEmpty(fields))
            {
                return Array.Empty<ItemFields>();
            }

            return fields.Split(',').Select(v =>
            {
                if (Enum.TryParse(v, true, out ItemFields value))
                {
                    return (ItemFields?)value;
                }

                return null;
            }).Where(i => i.HasValue).Select(i => i.Value).ToArray();
        }

        /// <summary>
        /// Get parsed filters.
        /// </summary>
        /// <param name="filters">The filters.</param>
        /// <returns>Item filters.</returns>
        public static IEnumerable<ItemFilter> GetFilters(string filters)
        {
            return string.IsNullOrEmpty(filters)
                ? Array.Empty<ItemFilter>()
                : filters.Split(',').Select(v => Enum.Parse<ItemFilter>(v, true));
        }
    }
}
