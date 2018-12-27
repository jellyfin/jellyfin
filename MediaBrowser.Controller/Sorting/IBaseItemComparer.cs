using MediaBrowser.Controller.Entities;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Sorting
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
