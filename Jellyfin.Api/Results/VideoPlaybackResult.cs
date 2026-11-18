using System;
using System.Threading.Tasks;
using MediaBrowser.Controller.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Results;

/// <summary>
/// Holds a video playback operation until its response has finished executing.
/// </summary>
public sealed class VideoPlaybackResult : ActionResult
{
    private readonly ActionResult _inner;
    private readonly IVideoPlaybackRequest _request;

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoPlaybackResult"/> class.
    /// </summary>
    /// <param name="inner">The result that prepares and writes the response.</param>
    /// <param name="request">The playback operation owned by this response.</param>
    public VideoPlaybackResult(ActionResult inner, IVideoPlaybackRequest request)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(request);

        _inner = inner;
        _request = request;
    }

    /// <inheritdoc />
    public override async Task ExecuteResultAsync(ActionContext context)
    {
        try
        {
            await _inner.ExecuteResultAsync(context).ConfigureAwait(false);

            // Confirm only after delivery succeeds. A failed or aborted first response
            // rolls back its unconfirmed admission; disposing this operation never
            // releases an admission already confirmed by another response.
            if (context.HttpContext.Response.StatusCode < StatusCodes.Status400BadRequest
                && !context.HttpContext.RequestAborted.IsCancellationRequested)
            {
                _request.Acknowledge();
            }
        }
        finally
        {
            _request.Dispose();
        }
    }
}
