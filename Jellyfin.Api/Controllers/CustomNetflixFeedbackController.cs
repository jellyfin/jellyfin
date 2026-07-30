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
[Route("CustomNetflix/v1/profiles/{profileId:guid}/items/{itemId:guid}/feedback")]
[Tags("CustomNetflix")]
public sealed class CustomNetflixFeedbackController : BaseJellyfinApiController
{
    private readonly ICustomNetflixFeedbackService _feedbackService;

    public CustomNetflixFeedbackController(ICustomNetflixFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    [HttpGet]
    public async Task<ActionResult<CustomNetflixItemFeedbackDto>> GetFeedback(
        [FromRoute] Guid profileId,
        [FromRoute] Guid itemId,
        CancellationToken cancellationToken)
    {
        var result = await _feedbackService.GetAsync(
            User.GetUserId(),
            profileId,
            itemId,
            cancellationToken).ConfigureAwait(false);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut]
    public async Task<ActionResult<CustomNetflixItemFeedbackDto>> SetFeedback(
        [FromRoute] Guid profileId,
        [FromRoute] Guid itemId,
        [FromBody] CustomNetflixItemFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _feedbackService.SetAsync(
                User.GetUserId(),
                profileId,
                itemId,
                request,
                cancellationToken).ConfigureAwait(false);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete]
    public async Task<ActionResult> ClearFeedback(
        [FromRoute] Guid profileId,
        [FromRoute] Guid itemId,
        CancellationToken cancellationToken)
    {
        var result = await _feedbackService.ClearAsync(
            User.GetUserId(),
            profileId,
            itemId,
            cancellationToken).ConfigureAwait(false);
        return result is null ? NotFound() : NoContent();
    }
}
