using System;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Sorting;

namespace Emby.Server.Implementations.Sorting;

/// <summary>
/// Class RuntimeComparer.
/// </summary>
public class RuntimeComparer : IBaseItemComparer
{
    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <value>The name.</value>
    public ItemSortBy Type => ItemSortBy.Runtime;

    /// <summary>
    /// Compares the specified x.
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    /// <returns>System.Int32.</returns>
    public int Compare(BaseItem? x, BaseItem? y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        return (x.RunTimeTicks ?? 0).CompareTo(y.RunTimeTicks ?? 0);
    }
}
