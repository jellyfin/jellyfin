using System.Threading.Tasks;
using MediaBrowser.Controller.Streaming;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Services;

/// <summary>
/// Abstract the Video functionality.
/// </summary>
public interface IVideoService
{
    /// <summary>
    /// Gets the video stream to return.
    /// </summary>
    /// <param name="request">the web request.</param>
    /// <param name="httpRequest">the http request.</param>
    /// <param name="httpContext">the http context.</param>
    /// <returns>web response.</returns>
    Task<ActionResult> GetVideoStreamAsync(VideoRequestDto request, HttpRequest httpRequest, HttpContext httpContext);
}
