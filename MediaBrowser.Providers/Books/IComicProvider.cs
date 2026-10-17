using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;

namespace MediaBrowser.Providers.Books;

/// <summary>
/// Comic provider interface.
/// </summary>
public interface IComicProvider
{
    /// <summary>
    /// Read the item metadata.
    /// </summary>
    /// <param name="info">The item information.</param>
    /// <param name="directoryService">Instance of the <see cref="IDirectoryService"/> interface.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The metadata result.</returns>
    ValueTask<MetadataResult<Book>> ReadMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken);

    /// <summary>
    /// Determine whether the item has changed.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>Item change status.</returns>
    bool HasItemChanged(BaseItem item);
}
