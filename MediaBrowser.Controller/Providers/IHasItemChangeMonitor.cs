using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Providers
{
    public interface IHasItemChangeMonitor
    {
        /// <summary>
        /// Determines whether the specified item has changed.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <returns><c>true</c> if the specified item has changed; otherwise, <c>false</c>.</returns>
        bool HasChanged(IHasMetadata item, IDirectoryService directoryService);
    }
}