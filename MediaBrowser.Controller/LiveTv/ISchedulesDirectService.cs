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
    /// <returns>The raw JSON response bytes.</returns>
    Task<byte[]> GetAvailableCountries(CancellationToken cancellationToken);
}
