using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.CustomNetflix;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
    /// <param name="claimsPrincipal">The <see cref="ClaimsPrincipal"/> for the current request.</param>
    /// <param name="user">The user id.</param>
    /// <param name="restrictUserPreferences">Whether to restrict the user preferences.</param>
    /// <returns>A <see cref="bool"/> whether the user can update the entry.</returns>
    internal static bool AssertCanUpdateUser(ClaimsPrincipal claimsPrincipal, User user, bool restrictUserPreferences)
    {
        var authenticatedUserId = claimsPrincipal.GetUserId();
        var isAdministrator = claimsPrincipal.IsInRole(UserRoles.Administrator);

        // If they're going to update the record of another user, they must be an administrator
        if (!user.Id.Equals(authenticatedUserId) && !isAdministrator)
        {
            return false;
        }

        // TODO the EnableUserPreferenceAccess policy does not seem to be used elsewhere
        if (!restrictUserPreferences || isAdministrator)
        {
            return true;
        }

        return user.EnableUserPreferenceAccess;
    }

    /// <summary>
    /// Get the session based on http request.
    /// </summary>
    /// <param name="sessionManager">The session manager.</param>
    /// <param name="userManager">The user manager.</param>
    /// <param name="httpContext">The http context.</param>
    /// <param name="userId">The optional userid.</param>
    /// <returns>The session.</returns>
    /// <exception cref="ResourceNotFoundException">Session not found.</exception>
    public static async Task<SessionInfo> GetSession(ISessionManager sessionManager, IUserManager userManager, HttpContext httpContext, Guid? userId = null)
    {
        userId ??= httpContext.User.GetUserId();
        User? user = null;
        if (!userId.IsNullOrEmpty())
        {
            user = userManager.GetUserById(userId.Value);
        }

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

    internal static async Task<(SessionInfo Session, bool? IsEnabled, long Generation)> GetCustomNetflixNativeUserDataSession(
        ISessionManager sessionManager,
        IUserManager userManager,
        ICustomNetflixActiveProfileService activeProfileService,
        HttpContext httpContext,
        Guid? userId = null)
    {
        var profileUserId = userId ?? httpContext.User.GetUserId();
        var session = await GetSession(sessionManager, userManager, httpContext, profileUserId).ConfigureAwait(false);
        var token = httpContext.User.GetToken();
        var tokenHash = CustomNetflixNativePlaystateSyncPolicy.HashToken(token);
        var cachedResolution = session.SynchronizeCustomNetflixProfile(() =>
        {
            if (session.CustomNetflixProfileUserId.Equals(profileUserId)
                && string.Equals(session.CustomNetflixTokenHash, tokenHash, StringComparison.Ordinal)
                && session.CustomNetflixNativeUserDataEnabled.HasValue)
            {
                return (
                    IsCached: true,
                    IsEnabled: session.CustomNetflixNativeUserDataEnabled.Value,
                    Generation: session.CustomNetflixProfileGeneration);
            }

            return (
                IsCached: false,
                IsEnabled: false,
                Generation: BeginCustomNetflixProfileResolutionUnsafe(session));
        });
        if (cachedResolution.IsCached)
        {
            return (session, cachedResolution.IsEnabled, cachedResolution.Generation);
        }

        var generation = cachedResolution.Generation;
        if (!activeProfileService.IsEnabled)
        {
            var published = TrySetCustomNetflixProfileResolution(session, generation, profileUserId, token, true);
            return (session, published, generation);
        }

        try
        {
            var activeProfile = await activeProfileService.GetActiveProfileAsync(
                profileUserId,
                token,
                httpContext.RequestAborted).ConfigureAwait(false);
            var enabled = CustomNetflixNativePlaystateSyncPolicy.ShouldSync(activeProfile.Profile);
            var published = TrySetCustomNetflixProfileResolution(session, generation, profileUserId, token, enabled);
            return (session, published && enabled, generation);
        }
        catch (Exception ex) when (ex is CustomNetflixUnavailableException or DbException)
        {
            // Retry on the next request, but never contaminate the default profile meanwhile.
            return (session, null, generation);
        }
    }

    internal static long BeginCustomNetflixProfileResolution(SessionInfo session)
        => session.SynchronizeCustomNetflixProfile(
            () => BeginCustomNetflixProfileResolutionUnsafe(session));

    internal static long BeginCustomNetflixProfileChange(SessionInfo session)
        => session.SynchronizeCustomNetflixProfile(
            () => BeginCustomNetflixProfileResolutionUnsafe(session));

    internal static bool TrySetCustomNetflixProfileResolution(
        SessionInfo session,
        long generation,
        Guid userId,
        string? token,
        bool enabled)
        => session.SynchronizeCustomNetflixProfile(() =>
        {
            if (session.CustomNetflixProfileGeneration != generation)
            {
                return false;
            }

            session.CustomNetflixProfileUserId = userId;
            session.CustomNetflixNativeUserDataEnabled = enabled;
            session.CustomNetflixTokenHash = CustomNetflixNativePlaystateSyncPolicy.HashToken(token);
            return true;
        });

    internal static ObjectResult? GetCustomNetflixNativeUserDataWriteError(bool? enabled)
        => enabled switch
        {
            true => null,
            false => new ConflictObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "CustomNetflix profile isolation",
                Detail = "Native Jellyfin user data is disabled while a non-default CustomNetflix profile is active."
            }),
            null => new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "CustomNetflix profile state unavailable",
                Detail = "The active profile could not be verified, so native Jellyfin user data was not changed."
            })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            }
        };

    internal static bool IsCustomNetflixProfileResolutionCurrentUnsafe(
        SessionInfo session,
        long generation,
        Guid userId,
        string? token,
        bool enabled)
        => session.CustomNetflixProfileGeneration == generation
            && session.CustomNetflixProfileUserId.Equals(userId)
            && string.Equals(
                session.CustomNetflixTokenHash,
                CustomNetflixNativePlaystateSyncPolicy.HashToken(token),
                StringComparison.Ordinal)
            && session.CustomNetflixNativeUserDataEnabled == enabled;

    private static long BeginCustomNetflixProfileResolutionUnsafe(SessionInfo session)
    {
        session.CustomNetflixProfileGeneration++;
        session.CustomNetflixTokenHash = null;
        session.CustomNetflixProfileUserId = null;
        session.CustomNetflixNativeUserDataEnabled = false;
        return session.CustomNetflixProfileGeneration;
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
