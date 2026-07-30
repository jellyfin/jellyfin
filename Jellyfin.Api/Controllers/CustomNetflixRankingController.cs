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
[Route("CustomNetflix/v1")]
[Tags("CustomNetflix")]
public sealed class CustomNetflixRankingController : BaseJellyfinApiController
{
    private readonly ICustomNetflixRankingService _rankingService;

    public CustomNetflixRankingController(ICustomNetflixRankingService rankingService)
    {
        _rankingService = rankingService;
    }

    [HttpGet("trending")]
    public async Task<ActionResult<CustomNetflixRankedItemsResponseDto>> GetTrending(
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        var response = await _rankingService.GetTrendingAsync(User.GetUserId(), limit <= 0 ? 20 : limit, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [HttpGet("top10")]
    public async Task<ActionResult<CustomNetflixRankedItemsResponseDto>> GetTopTen(CancellationToken cancellationToken)
    {
        var response = await _rankingService.GetTopTenAsync(User.GetUserId(), 10, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }
}
