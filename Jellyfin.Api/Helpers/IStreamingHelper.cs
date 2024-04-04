using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Streaming Helper.
/// </summary>
public interface IStreamingHelper
{
    /// <summary>
    /// Create Streaming State.
    /// </summary>
    /// <param name="streamingRequest">request.</param>
    /// <param name="httpContext">httpcontext.</param>
    /// <param name="encodingHelper">encoding helper.</param>
    /// <param name="transcodingJobType">job type.</param>
    /// <param name="cancellationToken">cancellation token.</param>
    /// <returns>stream state.</returns>
    Task<StreamState> GetStreamingState(
        StreamingRequestDto streamingRequest,
        HttpContext httpContext,
        EncodingHelper encodingHelper,
        TranscodingJobType transcodingJobType,
        CancellationToken cancellationToken);
}
