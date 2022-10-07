using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
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
        public static (string, SortOrder)[] GetOrderBy(IReadOnlyList<string> sortBy, IReadOnlyList<SortOrder> requestedSortOrder)
        {
            if (sortBy.Count == 0)
            {
                return Array.Empty<(string, SortOrder)>();
            }

            var result = new (string, SortOrder)[sortBy.Count];
            var i = 0;
            // Add elements which have a SortOrder specified
            for (; i < requestedSortOrder.Count; i++)
            {
                result[i] = (sortBy[i], requestedSortOrder[i]);
            }

            // Add remaining elements with the first specified SortOrder
            // or the default one if no SortOrders are specified
            var order = requestedSortOrder.Count > 0 ? requestedSortOrder[0] : SortOrder.Ascending;
            for (; i < sortBy.Count; i++)
            {
                result[i] = (sortBy[i], order);
            }

            return result;
        }

        /// <summary>
        /// Checks if the user can update an entry.
        /// </summary>
        /// <param name="userManager">An instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="claimsPrincipal">The <see cref="ClaimsPrincipal"/> for the current request.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="restrictUserPreferences">Whether to restrict the user preferences.</param>
        /// <returns>A <see cref="bool"/> whether the user can update the entry.</returns>
        internal static bool AssertCanUpdateUser(IUserManager userManager, ClaimsPrincipal claimsPrincipal, Guid userId, bool restrictUserPreferences)
        {
            var authenticatedUserId = claimsPrincipal.GetUserId();
            var isAdministrator = claimsPrincipal.IsInRole(UserRoles.Administrator);

            // If they're going to update the record of another user, they must be an administrator
            if (!userId.Equals(authenticatedUserId) && !isAdministrator)
            {
                return false;
            }

            // TODO the EnableUserPreferenceAccess policy does not seem to be used elsewhere
            if (!restrictUserPreferences || isAdministrator)
            {
                return true;
            }

            var user = userManager.GetUserById(userId);
            return user.EnableUserPreferenceAccess;
        }

        internal static async Task<SessionInfo> GetSession(ISessionManager sessionManager, IUserManager userManager, HttpContext httpContext)
        {
            var userId = httpContext.User.GetUserId();
            var user = userManager.GetUserById(userId);
            var session = await sessionManager.LogSessionActivity(
                httpContext.User.GetClient(),
                httpContext.User.GetVersion(),
                httpContext.User.GetDeviceId(),
                httpContext.User.GetDevice(),
                httpContext.GetNormalizedRemoteIp().ToString(),
                user).ConfigureAwait(false);

            if (session == null)
            {
                throw new ArgumentException("Session not found.");
            }

            return session;
        }

        internal static async Task<string> GetSessionId(ISessionManager sessionManager, IUserManager userManager, HttpContext httpContext)
        {
            var session = await GetSession(sessionManager, userManager, httpContext).ConfigureAwait(false);

            return session.Id;
        }

        internal static QueryResult<BaseItemDto> CreateQueryResult(
            QueryResult<(BaseItem Item, ItemCounts ItemCounts)> result,
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

            return new QueryResult<BaseItemDto>(
                result.StartIndex,
                result.TotalRecordCount,
                dtos.ToArray());
        }
    }
}
