#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Persistence
{
    /// <summary>
    /// Provides an interface to implement an Item repository.
    /// </summary>
    public interface IItemRepository : IDisposable
    {
        /// <summary>
        /// Deletes the item.
        /// </summary>
        /// <param name="id">The identifier.</param>
        void DeleteItem(Guid id);

        /// <summary>
        /// Saves the items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void SaveItems(IReadOnlyList<BaseItem> items, CancellationToken cancellationToken);

        void SaveImages(BaseItem item);

        /// <summary>
        /// Retrieves the item.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>BaseItem.</returns>
        BaseItem RetrieveItem(Guid id);

        /// <summary>
        /// Gets chapters for an item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The list of chapter info.</returns>
        List<ChapterInfo> GetChapters(BaseItem item);

        /// <summary>
        /// Gets a single chapter for an item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="index">The chapter index.</param>
        /// <returns>The chapter info at the specified index.</returns>
        ChapterInfo GetChapter(BaseItem item, int index);

        /// <summary>
        /// Saves the chapters.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <param name="chapters">The list of chapters to save.</param>
        void SaveChapters(Guid id, IReadOnlyList<ChapterInfo> chapters);

        /// <summary>
        /// Gets the media streams.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>IEnumerable{MediaStream}.</returns>
        List<MediaStream> GetMediaStreams(MediaStreamQuery query);

        /// <summary>
        /// Saves the media streams.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="streams">The streams.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void SaveMediaStreams(Guid id, IReadOnlyList<MediaStream> streams, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the media attachments.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>IEnumerable{MediaAttachment}.</returns>
        List<MediaAttachment> GetMediaAttachments(MediaAttachmentQuery query);

        /// <summary>
        /// Saves the media attachments.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="attachments">The attachments.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void SaveMediaAttachments(Guid id, IReadOnlyList<MediaAttachment> attachments, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>QueryResult&lt;BaseItem&gt;.</returns>
        QueryResult<BaseItem> GetItems(InternalItemsQuery query);

        /// <summary>
        /// Gets the item ids list.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>List&lt;Guid&gt;.</returns>
        List<Guid> GetItemIdsList(InternalItemsQuery query);

        /// <summary>
        /// Gets the people.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>List&lt;PersonInfo&gt;.</returns>
        List<PersonInfo> GetPeople(InternalPeopleQuery query);

        /// <summary>
        /// Updates the people.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <param name="people">The people.</param>
        void UpdatePeople(Guid itemId, List<PersonInfo> people);

        /// <summary>
        /// Gets the people names.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>List&lt;System.String&gt;.</returns>
        List<string> GetPeopleNames(InternalPeopleQuery query);

        /// <summary>
        /// Gets the item list.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>List&lt;BaseItem&gt;.</returns>
        List<BaseItem> GetItemList(InternalItemsQuery query);

        /// <summary>
        /// Updates the inherited values.
        /// </summary>
        void UpdateInheritedValues();

        int GetCount(InternalItemsQuery query);

        QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetGenres(InternalItemsQuery query);

        QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetMusicGenres(InternalItemsQuery query);

        QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetStudios(InternalItemsQuery query);

        QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetArtists(InternalItemsQuery query);

        QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetAlbumArtists(InternalItemsQuery query);

        QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetAllArtists(InternalItemsQuery query);

        List<string> GetMusicGenreNames();

        List<string> GetStudioNames();

        List<string> GetGenreNames();

        List<string> GetAllArtistNames();
    }
}
