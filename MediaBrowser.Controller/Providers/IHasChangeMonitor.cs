using MediaBrowser.Controller.Entities;
using System;

namespace MediaBrowser.Controller.Providers
{
    public interface IHasChangeMonitor
    {
        /// <summary>
        /// Determines whether the specified item has changed.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <param name="date">The date.</param>
        /// <returns><c>true</c> if the specified item has changed; otherwise, <c>false</c>.</returns>
        bool HasChanged(IHasMetadata item, IDirectoryService directoryService, DateTime date);
    }

    public interface IHasItemChangeMonitor
    {
        /// <summary>
        /// Determines whether the specified item has changed.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="status">The status.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <returns><c>true</c> if the specified item has changed; otherwise, <c>false</c>.</returns>
        bool HasChanged(IHasMetadata item, MetadataStatus status, IDirectoryService directoryService);
    }
}
