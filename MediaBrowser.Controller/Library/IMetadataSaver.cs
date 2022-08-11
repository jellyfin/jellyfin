using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Interface IMetadataSaver.
    /// </summary>
    public interface IMetadataSaver
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Determines whether [is enabled for] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <returns><c>true</c> if [is enabled for] [the specified item]; otherwise, <c>false</c>.</returns>
        bool IsEnabledFor(BaseItem item, ItemUpdateType updateType);

        /// <summary>
        /// Saves the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task SaveAsync(BaseItem item, CancellationToken cancellationToken);
    }
}
