using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Library.Recommendations;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// The suggestions controller.
/// </summary>
[Route("")]
[Authorize]
[Tags("Suggestion")]
public class SuggestionsController : BaseJellyfinApiController
{
    private readonly IDtoService _dtoService;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly IRecommendationsService _recommendationsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SuggestionsController"/> class.
    /// </summary>
    /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="recommendationsService">Instance of the <see cref="IRecommendationsService"/> interface.</param>
    public SuggestionsController(
        IDtoService dtoService,
        IUserManager userManager,
        ILibraryManager libraryManager,
        IRecommendationsService recommendationsService)
    {
        _dtoService = dtoService;
        _userManager = userManager;
        _libraryManager = libraryManager;
        _recommendationsService = recommendationsService;
    }

    /// <summary>
    /// Gets suggestions.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="mediaType">The media types.</param>
    /// <param name="type">The type.</param>
    /// <param name="startIndex">Optional. The start index.</param>
    /// <param name="limit">Optional. The limit.</param>
    /// <param name="enableTotalRecordCount">Whether to enable the total record count.</param>
    /// <response code="200">Suggestions returned.</response>
    /// <returns>A <see cref="QueryResult{BaseItemDto}"/> with the suggestions.</returns>
    [HttpGet("Items/Suggestions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<QueryResult<BaseItemDto>>> GetSuggestions(
        [FromQuery] Guid? userId,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] MediaType[] mediaType,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] BaseItemKind[] type,
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery] bool enableTotalRecordCount = false)
    {
        var dtoOptions = new DtoOptions();

        // Try profile-ranked path when userId is present and types resolve to a single recommendable kind.
        if (!userId.IsNullOrEmpty()
            && RecommendableKindResolver.TryGetRecommendableKind(
                type ?? Array.Empty<BaseItemKind>(),
                mediaType ?? Array.Empty<MediaType>(),
                out var kind))
        {
            var resolvedUserId = RequestHelpers.GetUserId(User, userId);
            if (!resolvedUserId.IsEmpty())
            {
                var ranked = await _recommendationsService
                    .GetRankedItemsAsync(resolvedUserId, kind, parentId: null, startIndex, limit, enableTotalRecordCount, dtoOptions, CancellationToken.None)
                    .ConfigureAwait(false);
                if (ranked is not null)
                {
                    return ranked;
                }
            }
        }

        // Fallback: existing random behavior.
        User? user;
        if (userId.IsNullOrEmpty())
        {
            user = null;
        }
        else
        {
            var requestUserId = RequestHelpers.GetUserId(User, userId);
            user = _userManager.GetUserById(requestUserId);
        }

        var result = _libraryManager.GetItemsResult(new InternalItemsQuery(user)
        {
            OrderBy = new[] { (ItemSortBy.Random, SortOrder.Descending) },
            MediaTypes = mediaType ?? Array.Empty<MediaType>(),
            IncludeItemTypes = type ?? Array.Empty<BaseItemKind>(),
            IsVirtualItem = false,
            StartIndex = startIndex,
            Limit = limit,
            DtoOptions = dtoOptions,
            EnableTotalRecordCount = enableTotalRecordCount,
            Recursive = true
        });

        return new QueryResult<BaseItemDto>(
            startIndex,
            result.TotalRecordCount,
            _dtoService.GetBaseItemDtos(result.Items, dtoOptions, user));
    }

    /// <summary>
    /// Gets suggestions (legacy route).
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="mediaType">The media types.</param>
    /// <param name="type">The type.</param>
    /// <param name="startIndex">Optional. The start index.</param>
    /// <param name="limit">Optional. The limit.</param>
    /// <param name="enableTotalRecordCount">Whether to enable the total record count.</param>
    /// <response code="200">Suggestions returned.</response>
    /// <returns>A <see cref="QueryResult{BaseItemDto}"/> with the suggestions.</returns>
    [HttpGet("Users/{userId}/Suggestions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Obsolete("Kept for backwards compatibility")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<ActionResult<QueryResult<BaseItemDto>>> GetSuggestionsLegacy(
        [FromRoute, Required] Guid userId,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] MediaType[] mediaType,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] BaseItemKind[] type,
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery] bool enableTotalRecordCount = false)
        => GetSuggestions(userId, mediaType, type, startIndex, limit, enableTotalRecordCount);
}
