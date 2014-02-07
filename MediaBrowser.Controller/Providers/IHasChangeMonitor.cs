using System;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Providers
{
    public interface IHasChangeMonitor
    {
        /// <summary>
        /// Determines whether the specified item has changed.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="date">The date.</param>
        /// <returns><c>true</c> if the specified item has changed; otherwise, <c>false</c>.</returns>
        bool HasChanged(IHasMetadata item, DateTime date);
    }
}
