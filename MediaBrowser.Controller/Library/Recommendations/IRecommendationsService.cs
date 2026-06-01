using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Library.Recommendations;

/// <summary>
/// Builds per-user metadata-based recommendations from the items they have watched and favorited.
/// </summary>
public interface IRecommendationsService
{
    /// <summary>
    /// Returns categorized recommendations (e.g. "Because you watched X").
    /// Returns an empty list when the user has no watch / favorite history (cold start).
    /// </summary>
    /// <param name="request">The recommendation request parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a read-only list of recommendation DTOs.</returns>
    Task<IReadOnlyList<RecommendationDto>> GetRecommendationsAsync(
        RecommendationRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns a flat list of items ranked by alignment with the user's taste profile
    /// for the given kind. Returns null if the user has no history (caller should fall
    /// back to its existing behavior — random ordering for the Suggestions endpoint).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="kind">The item kind to retrieve.</param>
    /// <param name="parentId">The parent item ID, if any.</param>
    /// <param name="startIndex">The start index for pagination.</param>
    /// <param name="limit">The maximum number of items to return.</param>
    /// <param name="enableTotalRecordCount">Whether to include the total record count.</param>
    /// <param name="dtoOptions">The DTO options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a QueryResult of BaseItemDto, or null if the user has no history.</returns>
    Task<QueryResult<BaseItemDto>?> GetRankedItemsAsync(
        Guid userId,
        BaseItemKind kind,
        Guid? parentId,
        int? startIndex,
        int? limit,
        bool enableTotalRecordCount,
        DtoOptions dtoOptions,
        CancellationToken cancellationToken);
}
