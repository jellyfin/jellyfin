using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Retrieves the item.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="type">The type.</param>
        /// <returns>BaseItem.</returns>
        BaseItem RetrieveItem(Guid id, Type type);

        /// <summary>
        /// Gets chapters for an item
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        IEnumerable<ChapterInfo> GetChapters(Guid id);

        /// <summary>
        /// Gets a single chapter for an item
        /// </summary>
        /// <param name="id"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        ChapterInfo GetChapter(Guid id, int index);

        /// <summary>
        /// Saves the chapters.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="chapters">The chapters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveChapters(Guid id, IEnumerable<ChapterInfo> chapters, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Class ItemRepositoryExtensions
    /// </summary>
    public static class ItemRepositoryExtensions
    {
        /// <summary>
        /// Retrieves the item.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repository">The repository.</param>
        /// <param name="id">The id.</param>
        /// <returns>``0.</returns>
        public static T RetrieveItem<T>(this IItemRepository repository, Guid id) 
            where T : BaseItem, new()
        {
            return repository.RetrieveItem(id, typeof(T)) as T;
        }

        /// <summary>
        /// Retrieves the item.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repository">The repository.</param>
        /// <param name="idList">The id list.</param>
        /// <returns>IEnumerable{``0}.</returns>
        public static IEnumerable<T> RetrieveItems<T>(this IItemRepository repository, IEnumerable<Guid> idList) 
            where T : BaseItem, new()
        {
            return idList.Select(repository.RetrieveItem<T>).Where(i => i != null);
        }
    }
}

