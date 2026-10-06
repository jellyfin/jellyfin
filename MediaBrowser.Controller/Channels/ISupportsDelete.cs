using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Channels
{
    /// <summary>
    /// Interface for channels that support deleting items.
    /// </summary>
    public interface ISupportsDelete
    {
        /// <summary>
        /// Gets a value indicating whether the item can be deleted.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if the item can be deleted, <c>false</c> otherwise.</returns>
        bool CanDelete(BaseItem item);

        /// <summary>
        /// Deletes the item with the provided id.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the deletion of the item.</returns>
        Task DeleteItem(string id, CancellationToken cancellationToken);
    }
}
