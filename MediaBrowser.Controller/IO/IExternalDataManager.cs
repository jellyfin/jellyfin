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

    /// <summary>
    /// Deletes only the filesystem-side external item data (attachments, subtitles, trickplay, chapter images).
    /// Use this when DB-side cleanup is already handled by another code path (e.g. <c>IItemPersistenceService.DeleteItem</c>).
    /// </summary>
    /// <param name="item">The item.</param>
    void DeleteExternalItemFiles(BaseItem item);
}
