using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Library;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// Provides library items that are not backed by local filesystem paths.
/// Implementations are discovered by the plugin system and called during every library scan.
/// Items no longer returned by <see cref="GetItemsAsync"/> are removed from the library.
/// </summary>
public interface IExternalItemProvider
{
    /// <summary>
    /// Gets the unique, stable name for this provider.
    /// Used to namespace <see cref="ExternalItemInfo.ExternalId"/> values in the database.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Checks whether the remote source is reachable.
    /// Called before <see cref="GetItemsAsync"/> during each library scan.
    /// Return <c>false</c> to skip the sync pass without removing existing items.
    /// The default implementation always returns <c>true</c>.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c> if the provider is available.</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken) => Task.FromResult(true);

    /// <summary>
    /// Returns all items this provider currently owns.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The full set of items for this provider.</returns>
    Task<IReadOnlyList<ExternalItemInfo>> GetItemsAsync(CancellationToken cancellationToken);
}
