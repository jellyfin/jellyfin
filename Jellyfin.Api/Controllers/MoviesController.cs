using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library.Recommendations;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Movies controller.
/// </summary>
[Authorize]
[Tags("Movie")]
public class MoviesController : BaseJellyfinApiController
{
    private readonly IRecommendationsService _recommendationsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoviesController"/> class.
    /// </summary>
    /// <param name="recommendationsService">Instance of <see cref="IRecommendationsService"/>.</param>
    public MoviesController(IRecommendationsService recommendationsService)
    {
        _recommendationsService = recommendationsService;
    }

    /// <summary>
    /// Gets movie recommendations.
    /// </summary>
    /// <param name="userId">Optional. Filter by user id, and attach user data.</param>
    /// <param name="parentId">Specify this to localize the search to a specific item or folder. Omit to use the root.</param>
    /// <param name="fields">Optional. The fields to return.</param>
    /// <param name="categoryLimit">The max number of categories to return.</param>
    /// <param name="itemLimit">The max number of items to return per category.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Movie recommendations returned.</response>
    /// <returns>The list of movie recommendations.</returns>
    [HttpGet("Recommendations")]
    public async Task<ActionResult<IEnumerable<RecommendationDto>>> GetMovieRecommendations(
        [FromQuery] Guid? userId,
        [FromQuery] Guid? parentId,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemFields[] fields,
        [FromQuery] int categoryLimit = 5,
        [FromQuery] int itemLimit = 8,
        CancellationToken cancellationToken = default)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        if (userId.IsNullOrEmpty())
        {
            return Ok((IEnumerable<RecommendationDto>)Array.Empty<RecommendationDto>());
        }

        var request = new RecommendationRequest(
            userId.Value,
            BaseItemKind.Movie,
            parentId,
            categoryLimit,
            itemLimit,
            new DtoOptions { Fields = fields });

        var result = await _recommendationsService.GetRecommendationsAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok((IEnumerable<RecommendationDto>)result);
    }
}
