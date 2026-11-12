#nullable disable

#pragma warning disable CS1591

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
        /// Gets the local trailers.
        /// </summary>
        /// <value>The local trailers.</value>
        IReadOnlyList<BaseItem> LocalTrailers { get; }
    }
}
