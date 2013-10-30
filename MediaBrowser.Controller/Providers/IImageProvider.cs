using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Interface IImageProvider
    /// </summary>
    public interface IImageProvider
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool Supports(BaseItem item, ImageType imageType);

        /// <summary>
        /// Gets the available images.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{RemoteImageInfo}}.</returns>
        Task<IEnumerable<RemoteImageInfo>> GetAvailableImages(BaseItem item, ImageType imageType, CancellationToken cancellationToken);
    }
}
