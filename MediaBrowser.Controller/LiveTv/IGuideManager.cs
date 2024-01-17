using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.LiveTv;

namespace MediaBrowser.Controller.LiveTv;

/// <summary>
/// Service responsible for managing the Live TV guide.
/// </summary>
public interface IGuideManager
{
    /// <summary>
    /// Gets the guide information.
    /// </summary>
    /// <returns>The <see cref="GuideInfo"/>.</returns>
    GuideInfo GetGuideInfo();

    /// <summary>
    /// Refresh the guide.
    /// </summary>
    /// <param name="progress">The <see cref="IProgress{T}"/> to use.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use.</param>
    /// <returns>Task representing the refresh operation.</returns>
    Task RefreshGuide(IProgress<double> progress, CancellationToken cancellationToken);
}
