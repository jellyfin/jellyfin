#pragma warning disable CS1591

using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Sorting;

namespace Emby.Server.Implementations.Sorting;

public class IsFolderComparer : IBaseItemComparer
{
    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <value>The name.</value>
    public ItemSortBy Type => ItemSortBy.IsFolder;

    /// <summary>
    /// Compares the specified x.
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    /// <returns>System.Int32.</returns>
    public int Compare(BaseItem? x, BaseItem? y)
    {
        return GetValue(x).CompareTo(GetValue(y));
    }

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <param name="x">The x.</param>
    /// <returns>System.String.</returns>
    private static int GetValue(BaseItem? x)
    {
        return x?.IsFolder ?? true ? 0 : 1;
    }
}
