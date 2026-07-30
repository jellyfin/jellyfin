#pragma warning disable CS1591

using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using MediaBrowser.Controller.CustomNetflix;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

[Authorize]
[Route("CustomNetflix/v1/profiles/{profileId:guid}/recommendations")]
[Tags("CustomNetflix")]
public sealed class CustomNetflixRecommendationsController : BaseJellyfinApiController
{
    private readonly ICustomNetflixRecommendationService _recommendationService;

    public CustomNetflixRecommendationsController(ICustomNetflixRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    [HttpGet]
    public async Task<ActionResult<CustomNetflixRecommendationsResponseDto>> GetRecommendations(
        [FromRoute] Guid profileId,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        var result = await _recommendationService.GetRecommendationsAsync(
            User.GetUserId(),
            profileId,
            limit,
            cancellationToken).ConfigureAwait(false);
        return result is null ? NotFound() : Ok(result);
    }
}
