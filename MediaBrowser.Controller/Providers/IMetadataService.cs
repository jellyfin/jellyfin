#pragma warning disable CS1591

using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace MediaBrowser.Controller.Providers;

public interface IMetadataService
{
    /// <summary>
    /// Gets the order.
    /// </summary>
    /// <value>The order.</value>
    int Order { get; }

    /// <summary>
    /// Determines whether this instance can refresh the specified item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns><c>true</c> if this instance can refresh the specified item.</returns>
    bool CanRefresh(BaseItem item);

    /// <summary>
    /// Determines whether this instance primarily targets the specified type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns><c>true</c> if this instance primarily targets the specified type.</returns>
    bool CanRefreshPrimary(Type type);

    /// <summary>
    /// Refreshes the metadata.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="refreshOptions">The options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task.</returns>
    Task<ItemUpdateType> RefreshMetadata(BaseItem item, MetadataRefreshOptions refreshOptions, CancellationToken cancellationToken);
}
