using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Api.Helpers;

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
    public static (ItemSortBy, SortOrder)[] GetOrderBy(IReadOnlyList<ItemSortBy> sortBy, IReadOnlyList<SortOrder> requestedSortOrder)
    {
        if (sortBy.Count == 0)
        {
            return Array.Empty<(ItemSortBy, SortOrder)>();
        }

        var result = new (ItemSortBy, SortOrder)[sortBy.Count];
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
    /// Checks if the user can access a user.
    /// </summary>
    /// <param name="claimsPrincipal">The <see cref="ClaimsPrincipal"/> for the current request.</param>
    /// <param name="userId">The user id.</param>
    /// <returns>A <see cref="bool"/> whether the user can access the user.</returns>
    internal static Guid GetUserId(ClaimsPrincipal claimsPrincipal, Guid? userId)
    {
        var authenticatedUserId = claimsPrincipal.GetUserId();

        // UserId not provided, fall back to authenticated user id.
        if (userId.IsNullOrEmpty())
        {
            return authenticatedUserId;
        }

        // User must be administrator to access another user.
        var isAdministrator = claimsPrincipal.IsInRole(UserRoles.Administrator);
        if (!userId.Value.Equals(authenticatedUserId) && !isAdministrator)
        {
            throw new SecurityException("Forbidden");
        }

        return userId.Value;
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
        if (user is null)
        {
            throw new ResourceNotFoundException();
        }

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
            httpContext.GetNormalizedRemoteIP().ToString(),
            user).ConfigureAwait(false);

        if (session is null)
        {
            throw new ResourceNotFoundException("Session not found.");
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

    /// <summary>
    /// Assess the item's access for a user.
    /// </summary>
    /// <param name="httpContext">The http context.</param>
    /// <param name="itemId">The item id.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="userId">The user id. Defaults to authenticated user.</param>
    /// <param name="userManager">The user manager.</param>
    /// <typeparam name="T">The type of item to return.</typeparam>
    /// <returns>
    /// Item and user if success, otherwise status result.
    /// </returns>
    internal static (T? Item, User? User, ActionResult? Result) AssessItemAccess<T>(
        HttpContext httpContext,
        Guid itemId,
        ILibraryManager? libraryManager,
        Guid? userId = null,
        IUserManager? userManager = null)
        where T : BaseItem
    {
        var (item, user, statusResult) = AssessItemAccess(httpContext, itemId, libraryManager, userId, userManager);
        T? typedItem = item as T;
        if (typedItem is null)
        {
            statusResult ??= new BadRequestResult();
        }

        return (typedItem, user, statusResult);
    }

    /// <summary>
    /// Assess the item's access for a user.
    /// </summary>
    /// <param name="httpContext">The http context.</param>
    /// <param name="itemId">The item id.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="userId">The user id. Defaults to authenticated user.</param>
    /// <param name="userManager">The user manager.</param>
    /// <returns>
    /// Item and user if success, otherwise status result.
    /// </returns>
    internal static (BaseItem? Item, User? User, ActionResult? Result) AssessItemAccess(
        HttpContext httpContext,
        Guid itemId,
        ILibraryManager? libraryManager,
        Guid? userId = null,
        IUserManager? userManager = null)
    {
        userManager ??= httpContext.RequestServices.GetRequiredService<IUserManager>();
        libraryManager ??= httpContext.RequestServices.GetRequiredService<ILibraryManager>();

        BaseItem? item;
        ActionResult? statusResult;

        if (httpContext.User.Identity?.IsAuthenticated != true || httpContext.User.GetIsApiKey())
        {
            /*
             * If this is an unauthenticated or api key request,
             * and the request doesn't contain a user id,
             * then just return the item.
             */
            if (userId is null)
            {
                (item, statusResult) = GetItem(libraryManager, itemId, userId);
                return (item, null, statusResult);
            }
        }
        else
        {
            // Authenticated as user, get the request user id.
            userId = GetUserId(httpContext.User, userId);
        }

        var user = userManager.GetUserById(userId.Value);
        if (user is null)
        {
            // Invalid user id, so no item access.
            return (null, null, new NotFoundResult());
        }

        (item, statusResult) = GetItem(libraryManager, itemId, userId);
        if (item is null)
        {
            return (null, null, statusResult);
        }

        if (item is not UserRootFolder && !item.IsVisibleStandalone(user))
        {
            return (null, null, new UnauthorizedObjectResult($"{user.Username} is not permitted to access item {item.Name}."));
        }

        // All validations passed.
        return (item, user, null);
    }

    private static (BaseItem? Item, ActionResult? StatusResult) GetItem(ILibraryManager libraryManager, Guid itemId, Guid? userId)
    {
        var item = itemId.IsEmpty()
            ? (userId.IsNullOrEmpty()
                ? libraryManager.RootFolder
                : libraryManager.GetUserRootFolder())
            : libraryManager.GetItemById(itemId);

        return item is null
            ? (null, new NotFoundResult())
            : (item, null);
    }
}
