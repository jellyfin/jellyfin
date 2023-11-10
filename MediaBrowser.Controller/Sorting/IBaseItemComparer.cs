using System.Collections.Generic;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Sorting
{
    /// <summary>
    /// Interface IBaseItemComparer.
    /// </summary>
    public interface IBaseItemComparer : IComparer<BaseItem?>
    {
        /// <summary>
        /// Gets the comparer type.
        /// </summary>
        ItemSortBy Type { get; }
    }
}
