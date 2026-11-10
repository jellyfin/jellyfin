using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class providing extension methods for working with <see cref="IHasTrailers" />.
    /// </summary>
    public static class HasTrailerExtensions
    {
        /// <summary>
        /// Gets the trailer count.
        /// </summary>
        /// <param name="item">Media item.</param>
        /// <returns><see cref="IReadOnlyList{Guid}" />.</returns>
        public static int GetTrailerCount(this IHasTrailers item)
            => item.LocalTrailers.Count + item.RemoteTrailers.Count;
    }
}
