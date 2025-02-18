#pragma warning disable CS1591

using System;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Sorting;

namespace Emby.Server.Implementations.Sorting;

public class SeriesSortNameComparer : IBaseItemComparer
{
    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <value>The name.</value>
    public ItemSortBy Type => ItemSortBy.SeriesSortName;

    /// <summary>
    /// Compares the specified x.
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    /// <returns>System.Int32.</returns>
    public int Compare(BaseItem? x, BaseItem? y)
    {
        return string.Compare(GetValue(x), GetValue(y), StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetValue(BaseItem? item)
    {
        var hasSeries = item as IHasSeries;
        return hasSeries?.FindSeriesSortName();
    }
}
