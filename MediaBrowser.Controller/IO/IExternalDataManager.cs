using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.IO;

/// <summary>
/// Interface IPathManager.
/// </summary>
public interface IExternalDataManager
{
    /// <summary>
    /// Deletes all external item data.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task.</returns>
    Task DeleteExternalItemDataAsync(BaseItem item, CancellationToken cancellationToken);
}
