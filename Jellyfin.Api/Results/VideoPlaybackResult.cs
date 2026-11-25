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
        var completed = false;
        try
        {
            await _inner.ExecuteResultAsync(context).ConfigureAwait(false);
            completed = true;
        }
        finally
        {
            try
            {
                var response = context.HttpContext.Response;
                // Once a successful response has started, a canceled transfer or seek
                // must preserve the logical playback until stop or inactivity expiry.
                // Failures before the response starts still roll back the admission.
                if (response.StatusCode < StatusCodes.Status400BadRequest
                    && (response.HasStarted || (completed && !context.HttpContext.RequestAborted.IsCancellationRequested)))
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
}
