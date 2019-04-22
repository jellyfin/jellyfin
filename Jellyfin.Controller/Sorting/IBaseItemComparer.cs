using System.Collections.Generic;
using Jellyfin.Controller.Entities;

namespace Jellyfin.Controller.Sorting
{
    /// <summary>
    /// Interface IBaseItemComparer
    /// </summary>
    public interface IBaseItemComparer : IComparer<BaseItem>
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }
    }
}
