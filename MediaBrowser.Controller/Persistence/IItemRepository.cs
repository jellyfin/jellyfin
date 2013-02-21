using MediaBrowser.Controller.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Persistence
{
    /// <summary>
    /// Provides an interface to implement an Item repository
    /// </summary>
    public interface IItemRepository : IRepository
    {
        /// <summary>
        /// Saves an item
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveItem(BaseItem item, CancellationToken cancellationToken);

        /// <summary>
        /// Gets an item
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>BaseItem.</returns>
        BaseItem RetrieveItem(Guid id);

        /// <summary>
        /// Gets children of a given Folder
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        IEnumerable<BaseItem> RetrieveChildren(Folder parent);

        /// <summary>
        /// Saves children of a given Folder
        /// </summary>
        /// <param name="parentId">The parent id.</param>
        /// <param name="children">The children.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveChildren(Guid parentId, IEnumerable<BaseItem> children, CancellationToken cancellationToken);
    }
}
