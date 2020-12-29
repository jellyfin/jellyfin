using System;
using System.Threading;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Controller.BaseItemManager
{
    /// <summary>
    /// The <c>BaseItem</c> manager.
    /// </summary>
    public interface IBaseItemManager
    {
        /// <summary>
        /// Gets the semaphore used to limit the amount of concurrent metadata refreshes.
        /// </summary>
        SemaphoreSlim MetadataRefreshThrottler { get; }

        /// <summary>
        /// Is metadata fetcher enabled.
        /// </summary>
        /// <param name="baseItem">The base item.</param>
        /// <param name="libraryOptions">The library options.</param>
        /// <param name="name">The metadata fetcher name.</param>
        /// <returns><c>true</c> if metadata fetcher is enabled, else false.</returns>
        bool IsMetadataFetcherEnabled(BaseItem baseItem, LibraryOptions libraryOptions, string name);

        /// <summary>
        /// Is image fetcher enabled.
        /// </summary>
        /// <param name="baseItem">The base item.</param>
        /// <param name="libraryOptions">The library options.</param>
        /// <param name="name">The image fetcher name.</param>
        /// <returns><c>true</c> if image fetcher is enabled, else false.</returns>
        bool IsImageFetcherEnabled(BaseItem baseItem, LibraryOptions libraryOptions, string name);
    }
}