using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.LiveTv;

/// <summary>
/// Provides Schedules Direct specific operations.
/// </summary>
public interface ISchedulesDirectService
{
    /// <summary>
    /// Gets the available countries from the Schedules Direct API, using a file cache.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A stream containing the raw JSON response.</returns>
    Task<Stream> GetAvailableCountries(CancellationToken cancellationToken);

    /// <summary>
    /// Gets a value indicating whether an image may be downloaded from the given url.
    /// </summary>
    /// <param name="imageUrl">The image url.</param>
    /// <returns><c>false</c> for a Schedules Direct url while the daily image limit is active; otherwise <c>true</c>.</returns>
    bool CanDownloadImage(string imageUrl);

    /// <summary>
    /// Reports a successful image download, so that an unexplained failure streak is reset.
    /// </summary>
    /// <param name="imageUrl">The image url that was downloaded.</param>
    void ReportImageDownloadSuccess(string imageUrl);

    /// <summary>
    /// Reports a failed image download. Schedules Direct answers an exhausted image quota with
    /// an HTTP 200 JSON error body rather than a failure status, so the body is what decides
    /// whether image acquisition has to stop.
    /// </summary>
    /// <param name="imageUrl">The image url that failed.</param>
    /// <param name="statusCode">The HTTP status code of the failure, if any.</param>
    /// <param name="responseBody">The body of the failed response, if it was readable.</param>
    /// <returns>What the caller must do with the image.</returns>
    ImageDownloadFailureAction ReportImageDownloadFailure(string imageUrl, HttpStatusCode? statusCode, string? responseBody);
}
