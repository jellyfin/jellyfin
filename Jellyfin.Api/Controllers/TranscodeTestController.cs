using System.ComponentModel.DataAnnotations;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.TranscodeTestDtos;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Tests the server's transcoding settings.
/// </summary>
[Route("Transcoding/Test")]
[Authorize(Policy = Policies.RequiresElevation)]
public class TranscodeTestController : BaseJellyfinApiController
{
    private readonly TranscodeTestManager _transcodeTestManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranscodeTestController"/> class.
    /// </summary>
    /// <param name="transcodeTestManager">Instance of <see cref="TranscodeTestManager"/>.</param>
    public TranscodeTestController(TranscodeTestManager transcodeTestManager)
    {
        _transcodeTestManager = transcodeTestManager;
    }

    /// <summary>
    /// Starts a transcoding test.
    /// </summary>
    /// <param name="request">What to test.</param>
    /// <response code="202">The test started.</response>
    /// <response code="400">The request is not valid.</response>
    /// <response code="409">A test is already running.</response>
    /// <returns>The status of the test that started.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TranscodeTestStatusDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult<TranscodeTestStatusDto> StartTest([FromBody, Required] TranscodeTestRequestDto request)
    {
        if (!_transcodeTestManager.TryStart(request))
        {
            return Conflict("A transcoding test is already running.");
        }

        return Accepted(_transcodeTestManager.GetStatus());
    }

    /// <summary>
    /// Gets the state and results of the current or last transcoding test.
    /// </summary>
    /// <response code="200">The test status.</response>
    /// <returns>The state, console lines and results.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<TranscodeTestStatusDto> GetTestStatus()
        => _transcodeTestManager.GetStatus();

    /// <summary>
    /// Stops the running transcoding test, or clears the results of the last one.
    /// </summary>
    /// <response code="204">The test is stopping, or the results were cleared.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult StopTest()
    {
        _transcodeTestManager.Cancel();
        return NoContent();
    }
}
