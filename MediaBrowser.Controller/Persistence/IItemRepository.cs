using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
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
        /// Opens the connection to the repository
        /// </summary>
        /// <returns>Task.</returns>
        Task Initialize();

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
        IEnumerable<ItemReview> GetCriticReviews(Guid itemId);

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
        /// <returns>BaseItem.</returns>
        BaseItem RetrieveItem(Guid id);

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

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <param name="parentId">The parent id.</param>
        /// <returns>IEnumerable{ChildDefinition}.</returns>
        IEnumerable<Guid> GetChildren(Guid parentId);

        /// <summary>
        /// Saves the children.
        /// </summary>
        /// <param name="parentId">The parent id.</param>
        /// <param name="children">The children.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveChildren(Guid parentId, IEnumerable<Guid> children, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the media streams.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>IEnumerable{MediaStream}.</returns>
        IEnumerable<MediaStream> GetMediaStreams(MediaStreamQuery query);

        /// <summary>
        /// Saves the media streams.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="streams">The streams.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveMediaStreams(Guid id, IEnumerable<MediaStream> streams, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the provider history.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <returns>IEnumerable{BaseProviderInfo}.</returns>
        IEnumerable<BaseProviderInfo> GetProviderHistory(Guid itemId);

        /// <summary>
        /// Saves the provider history.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="history">The history.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveProviderHistory(Guid id, IEnumerable<BaseProviderInfo> history, CancellationToken cancellationToken);
    }
}

