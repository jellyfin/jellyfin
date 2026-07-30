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
[Route("CustomNetflix/v1/home")]
[Tags("CustomNetflix")]
public sealed class CustomNetflixHomeController : BaseJellyfinApiController
{
    private readonly ICustomNetflixHomeRowsService _homeRowsService;

    public CustomNetflixHomeController(ICustomNetflixHomeRowsService homeRowsService)
    {
        _homeRowsService = homeRowsService;
    }

    [HttpGet]
    public async Task<ActionResult<CustomNetflixHomeResponseDto>> GetHome(
        [FromQuery] Guid profileId,
        [FromQuery] int itemLimit,
        CancellationToken cancellationToken)
    {
        itemLimit = itemLimit <= 0 ? 20 : itemLimit;
        var home = await _homeRowsService.GetHomeAsync(User.GetUserId(), profileId, itemLimit, cancellationToken).ConfigureAwait(false);
        return Ok(home);
    }
}
