using System.IO;
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
    /// Gets a value indicating whether the Schedules Direct daily image download limit is currently active.
    /// </summary>
    /// <returns><c>true</c> if the image limit has been hit and has not yet reset; otherwise <c>false</c>.</returns>
    bool IsImageDailyLimitActive();
}
