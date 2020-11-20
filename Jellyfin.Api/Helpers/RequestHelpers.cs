using System;
using System.Linq;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Helpers
{
    /// <summary>
    /// Request Extensions.
    /// </summary>
    public static class RequestHelpers
    {
        /// <summary>
        /// Get Order By.
        /// </summary>
        /// <param name="sortBy">Sort By. Comma delimited string.</param>
        /// <param name="requestedSortOrder">Sort Order. Comma delimited string.</param>
        /// <returns>Order By.</returns>
        public static ValueTuple<string, SortOrder>[] GetOrderBy(string? sortBy, string? requestedSortOrder)
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
        /// Splits a string at a separating character into an array of substrings.
        /// </summary>
        /// <param name="value">The string to split.</param>
        /// <param name="separator">The char that separates the substrings.</param>
        /// <param name="removeEmpty">Option to remove empty substrings from the array.</param>
        /// <returns>An array of the substrings.</returns>
        internal static string[] Split(string? value, char separator, bool removeEmpty)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<string>();
            }

            return removeEmpty
                ? value.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                : value.Split(separator);
        }

        /// <summary>
        /// Checks if the user can update an entry.
        /// </summary>
        /// <param name="authContext">Instance of the <see cref="IAuthorizationContext"/> interface.</param>
        /// <param name="requestContext">The <see cref="HttpRequest"/>.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="restrictUserPreferences">Whether to restrict the user preferences.</param>
        /// <returns>A <see cref="bool"/> whether the user can update the entry.</returns>
        internal static bool AssertCanUpdateUser(IAuthorizationContext authContext, HttpRequest requestContext, Guid userId, bool restrictUserPreferences)
        {
            var auth = authContext.GetAuthorizationInfo(requestContext);

            var authenticatedUser = auth.User;

            // If they're going to update the record of another user, they must be an administrator
            if ((!userId.Equals(auth.UserId) && !authenticatedUser.HasPermission(PermissionKind.IsAdministrator))
                || (restrictUserPreferences && !authenticatedUser.EnableUserPreferenceAccess))
            {
                return false;
            }

            return true;
        }

        internal static SessionInfo GetSession(ISessionManager sessionManager, IAuthorizationContext authContext, HttpRequest request)
        {
            var authorization = authContext.GetAuthorizationInfo(request);
            var user = authorization.User;
            var session = sessionManager.LogSessionActivity(
                authorization.Client,
                authorization.Version,
                authorization.DeviceId,
                authorization.Device,
                request.HttpContext.GetNormalizedRemoteIp(),
                user);

            if (session == null)
            {
                throw new ArgumentException("Session not found.");
            }

            return session;
        }

        internal static QueryResult<BaseItemDto> CreateQueryResult(
            QueryResult<(BaseItem, ItemCounts)> result,
            DtoOptions dtoOptions,
            IDtoService dtoService,
            bool includeItemTypes,
            User? user)
        {
            var dtos = result.Items.Select(i =>
            {
                var (baseItem, counts) = i;
                var dto = dtoService.GetItemByNameDto(baseItem, dtoOptions, null, user);

                if (includeItemTypes)
                {
                    dto.ChildCount = counts.ItemCount;
                    dto.ProgramCount = counts.ProgramCount;
                    dto.SeriesCount = counts.SeriesCount;
                    dto.EpisodeCount = counts.EpisodeCount;
                    dto.MovieCount = counts.MovieCount;
                    dto.TrailerCount = counts.TrailerCount;
                    dto.AlbumCount = counts.AlbumCount;
                    dto.SongCount = counts.SongCount;
                    dto.ArtistCount = counts.ArtistCount;
                }

                return dto;
            });

            return new QueryResult<BaseItemDto>
            {
                Items = dtos.ToArray(),
                TotalRecordCount = result.TotalRecordCount
            };
        }
    }
}
