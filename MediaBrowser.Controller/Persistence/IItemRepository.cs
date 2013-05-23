using MediaBrowser.Controller.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Entities;

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
        BaseItem GetItem(Guid id);

        /// <summary>
        /// Gets children of a given Folder
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        IEnumerable<BaseItem> RetrieveChildren(Folder parent);

        /// <summary>
        /// Retrieves the items.
        /// </summary>
        /// <param name="ids">The ids.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        IEnumerable<BaseItem> GetItems(IEnumerable<Guid> ids);

        /// <summary>
        /// Saves children of a given Folder
        /// </summary>
        /// <param name="parentId">The parent id.</param>
        /// <param name="children">The children.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveChildren(Guid parentId, IEnumerable<BaseItem> children, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the critic reviews.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <returns>Task{IEnumerable{ItemReview}}.</returns>
        Task<IEnumerable<ItemReview>> GetCriticReviews(Guid itemId);

        /// <summary>
        /// Saves the critic reviews.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="criticReviews">The critic reviews.</param>
        /// <returns>Task.</returns>
        Task SaveCriticReviews(Guid itemId, IEnumerable<ItemReview> criticReviews);

        /// <summary>
        /// Saves the items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveItems(IEnumerable<BaseItem> items, CancellationToken cancellationToken);
    }
}
