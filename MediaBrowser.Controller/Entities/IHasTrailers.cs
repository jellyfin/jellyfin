using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Controller.Entities
{
    public interface IHasTrailers : IHasProviderIds
    {
        /// <summary>
        /// Gets or sets the remote trailers.
        /// </summary>
        /// <value>The remote trailers.</value>
        List<MediaUrl> RemoteTrailers { get; set; }

        /// <summary>
        /// Gets or sets the local trailer ids.
        /// </summary>
        /// <value>The local trailer ids.</value>
        List<Guid> LocalTrailerIds { get; set; }
        List<Guid> RemoteTrailerIds { get; set; }
    }

    public static class HasTrailerExtensions
    {
        /// <summary>
        /// Gets the trailer ids.
        /// </summary>
        /// <returns>List&lt;Guid&gt;.</returns>
        public static List<Guid> GetTrailerIds(this IHasTrailers item)
        {
            var list = item.LocalTrailerIds.ToList();
            list.AddRange(item.RemoteTrailerIds);
            return list;
        }

    }
}
