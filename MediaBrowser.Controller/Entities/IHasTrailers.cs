using System;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Entities
{
    public interface IHasTrailers : IHasProviderIds
    {
        /// <summary>
        /// Gets or sets the remote trailers.
        /// </summary>
        /// <value>The remote trailers.</value>
        IReadOnlyList<MediaUrl> RemoteTrailers { get; set; }

        /// <summary>
        /// Gets or sets the local trailer ids.
        /// </summary>
        /// <value>The local trailer ids.</value>
        IReadOnlyList<Guid> LocalTrailerIds { get; set; }

        /// <summary>
        /// Gets or sets the remote trailer ids.
        /// </summary>
        /// <value>The remote trailer ids.</value>
        IReadOnlyList<Guid> RemoteTrailerIds { get; set; }

        Guid Id { get; set; }
    }

    /// <summary>
    /// Class providing extension methods for working with <see cref="IHasTrailers" />.
    /// </summary>
    public static class HasTrailerExtensions
    {
        /// <summary>
        /// Gets the trailer count.
        /// </summary>
        /// <returns><see cref="IReadOnlyList{Guid}" />.</returns>
        public static int GetTrailerCount(this IHasTrailers item)
            => item.LocalTrailerIds.Count + item.RemoteTrailerIds.Count;

        /// <summary>
        /// Gets the trailer ids.
        /// </summary>
        /// <returns><see cref="IReadOnlyList{Guid}" />.</returns>
        public static IReadOnlyList<Guid> GetTrailerIds(this IHasTrailers item)
        {
            var localIds = item.LocalTrailerIds;
            var remoteIds = item.RemoteTrailerIds;

            var all = new Guid[localIds.Count + remoteIds.Count];
            var index = 0;
            foreach (var id in localIds)
            {
                all[index++] = id;
            }

            foreach (var id in remoteIds)
            {
                all[index++] = id;
            }

            return all;
        }

        /// <summary>
        /// Gets the trailers.
        /// </summary>
        /// <returns><see cref="IReadOnlyList{BaseItem}" />.</returns>
        public static IReadOnlyList<BaseItem> GetTrailers(this IHasTrailers item)
        {
            var localIds = item.LocalTrailerIds;
            var remoteIds = item.RemoteTrailerIds;
            var libraryManager = BaseItem.LibraryManager;

            var all = new BaseItem[localIds.Count + remoteIds.Count];
            var index = 0;
            foreach (var id in localIds)
            {
                all[index++] = libraryManager.GetItemById(id);
            }

            foreach (var id in remoteIds)
            {
                all[index++] = libraryManager.GetItemById(id);
            }

            return all;
        }
    }
}
