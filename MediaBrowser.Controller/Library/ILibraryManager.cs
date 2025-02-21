#pragma warning disable CA1002, CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Querying;
using Episode = MediaBrowser.Controller.Entities.TV.Episode;
using Genre = MediaBrowser.Controller.Entities.Genre;
using Person = MediaBrowser.Controller.Entities.Person;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Interface ILibraryManager.
    /// </summary>
    public interface ILibraryManager
    {
        /// <summary>
        /// Occurs when [item added].
        /// </summary>
        event EventHandler<ItemChangeEventArgs>? ItemAdded;

        /// <summary>
        /// Occurs when [item updated].
        /// </summary>
        event EventHandler<ItemChangeEventArgs>? ItemUpdated;

        /// <summary>
        /// Occurs when [item removed].
        /// </summary>
        event EventHandler<ItemChangeEventArgs>? ItemRemoved;

        /// <summary>
        /// Gets the root folder.
        /// </summary>
        /// <value>The root folder.</value>
        AggregateFolder RootFolder { get; }

        bool IsScanRunning { get; }

        /// <summary>
        /// Resolves the path.
        /// </summary>
        /// <param name="fileInfo">The file information.</param>
        /// <param name="parent">The parent.</param>
        /// <param name="directoryService">An instance of <see cref="IDirectoryService"/>.</param>
        /// <returns>BaseItem.</returns>
        BaseItem? ResolvePath(
            FileSystemMetadata fileInfo,
            Folder? parent = null,
            IDirectoryService? directoryService = null);

        /// <summary>
        /// Resolves a set of files into a list of BaseItem.
        /// </summary>
        /// <param name="files">The list of tiles.</param>
        /// <param name="directoryService">Instance of the <see cref="IDirectoryService"/> interface.</param>
        /// <param name="parent">The parent folder.</param>
        /// <param name="libraryOptions">The library options.</param>
        /// <param name="collectionType">The collection type.</param>
        /// <returns>The items resolved from the paths.</returns>
        IEnumerable<BaseItem> ResolvePaths(
            IEnumerable<FileSystemMetadata> files,
            IDirectoryService directoryService,
            Folder parent,
            LibraryOptions libraryOptions,
            CollectionType? collectionType = null);

        /// <summary>
        /// Gets a Person.
        /// </summary>
        /// <param name="name">The name of the person.</param>
        /// <returns>Task{Person}.</returns>
        Person? GetPerson(string name);

        /// <summary>
        /// Finds the by path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="isFolder"><c>true</c> is the path is a directory; otherwise <c>false</c>.</param>
        /// <returns>BaseItem.</returns>
        BaseItem? FindByPath(string path, bool? isFolder);

        /// <summary>
        /// Gets the artist.
        /// </summary>
        /// <param name="name">The name of the artist.</param>
        /// <returns>Task{Artist}.</returns>
        MusicArtist GetArtist(string name);

        MusicArtist GetArtist(string name, DtoOptions options);

        /// <summary>
        /// Gets a Studio.
        /// </summary>
        /// <param name="name">The name of the studio.</param>
        /// <returns>Task{Studio}.</returns>
        Studio GetStudio(string name);

        /// <summary>
        /// Gets a Genre.
        /// </summary>
        /// <param name="name">The name of the genre.</param>
        /// <returns>Task{Genre}.</returns>
        Genre GetGenre(string name);

        /// <summary>
        /// Gets the genre.
        /// </summary>
        /// <param name="name">The name of the music genre.</param>
        /// <returns>Task{MusicGenre}.</returns>
        MusicGenre GetMusicGenre(string name);

        /// <summary>
        /// Gets a Year.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Task{Year}.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Throws if year is invalid.</exception>
        Year GetYear(int value);

        /// <summary>
        /// Validate and refresh the People sub-set of the IBN.
        /// The items are stored in the db but not loaded into memory until actually requested by an operation.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task ValidatePeopleAsync(IProgress<double> progress, CancellationToken cancellationToken);

        /// <summary>
        /// Reloads the root media folder.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task ValidateMediaLibrary(IProgress<double> progress, CancellationToken cancellationToken);

        /// <summary>
        /// Reloads the root media folder.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="removeRoot">Is remove the library itself allowed.</param>
        /// <returns>Task.</returns>
        Task ValidateTopLibraryFolders(CancellationToken cancellationToken, bool removeRoot = false);

        Task UpdateImagesAsync(BaseItem item, bool forceUpdate = false);

        /// <summary>
        /// Gets the default view.
        /// </summary>
        /// <returns>IEnumerable{VirtualFolderInfo}.</returns>
        List<VirtualFolderInfo> GetVirtualFolders();

        List<VirtualFolderInfo> GetVirtualFolders(bool includeRefreshState);

        /// <summary>
        /// Gets the item by id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>BaseItem.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="id"/> is <c>null</c>.</exception>
        BaseItem? GetItemById(Guid id);

        /// <summary>
        /// Gets the item by id, as T.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <typeparam name="T">The type of item.</typeparam>
        /// <returns>The item.</returns>
        T? GetItemById<T>(Guid id)
            where T : BaseItem;

        /// <summary>
        /// Gets the item by id, as T, and validates user access.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <param name="userId">The user id to validate against.</param>
        /// <typeparam name="T">The type of item.</typeparam>
        /// <returns>The item if found.</returns>
        public T? GetItemById<T>(Guid id, Guid userId)
            where T : BaseItem;

        /// <summary>
        /// Gets the item by id, as T, and validates user access.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <param name="user">The user to validate against.</param>
        /// <typeparam name="T">The type of item.</typeparam>
        /// <returns>The item if found.</returns>
        public T? GetItemById<T>(Guid id, User? user)
            where T : BaseItem;

        /// <summary>
        /// Gets the intros.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        Task<IEnumerable<Video>> GetIntros(BaseItem item, User user);

        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="rules">The rules.</param>
        /// <param name="resolvers">The resolvers.</param>
        /// <param name="introProviders">The intro providers.</param>
        /// <param name="itemComparers">The item comparers.</param>
        /// <param name="postscanTasks">The postscan tasks.</param>
        void AddParts(
            IEnumerable<IResolverIgnoreRule> rules,
            IEnumerable<IItemResolver> resolvers,
            IEnumerable<IIntroProvider> introProviders,
            IEnumerable<IBaseItemComparer> itemComparers,
            IEnumerable<ILibraryPostScanTask> postscanTasks);

        /// <summary>
        /// Sorts the specified items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="user">The user.</param>
        /// <param name="sortBy">The sort by.</param>
        /// <param name="sortOrder">The sort order.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        IEnumerable<BaseItem> Sort(IEnumerable<BaseItem> items, User? user, IEnumerable<ItemSortBy> sortBy, SortOrder sortOrder);

        IEnumerable<BaseItem> Sort(IEnumerable<BaseItem> items, User? user, IEnumerable<(ItemSortBy OrderBy, SortOrder SortOrder)> orderBy);

        /// <summary>
        /// Gets the user root folder.
        /// </summary>
        /// <returns>UserRootFolder.</returns>
        Folder GetUserRootFolder();

        /// <summary>
        /// Creates the item.
        /// </summary>
        /// <param name="item">Item to create.</param>
        /// <param name="parent">Parent of new item.</param>
        void CreateItem(BaseItem item, BaseItem? parent);

        /// <summary>
        /// Creates the items.
        /// </summary>
        /// <param name="items">Items to create.</param>
        /// <param name="parent">Parent of new items.</param>
        /// <param name="cancellationToken">CancellationToken to use for operation.</param>
        void CreateItems(IReadOnlyList<BaseItem> items, BaseItem? parent, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the item.
        /// </summary>
        /// <param name="items">Items to update.</param>
        /// <param name="parent">Parent of updated items.</param>
        /// <param name="updateReason">Reason for update.</param>
        /// <param name="cancellationToken">CancellationToken to use for operation.</param>
        /// <returns>Returns a Task that can be awaited.</returns>
        Task UpdateItemsAsync(IReadOnlyList<BaseItem> items, BaseItem parent, ItemUpdateType updateReason, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="parent">The parent item.</param>
        /// <param name="updateReason">The update reason.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Returns a Task that can be awaited.</returns>
        Task UpdateItemAsync(BaseItem item, BaseItem parent, ItemUpdateType updateReason, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves the item.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>BaseItem.</returns>
        BaseItem RetrieveItem(Guid id);

        /// <summary>
        /// Finds the type of the collection.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        CollectionType? GetContentType(BaseItem item);

        /// <summary>
        /// Gets the type of the inherited content.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        CollectionType? GetInheritedContentType(BaseItem item);

        /// <summary>
        /// Gets the type of the configured content.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        CollectionType? GetConfiguredContentType(BaseItem item);

        /// <summary>
        /// Gets the type of the configured content.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        CollectionType? GetConfiguredContentType(string path);

        /// <summary>
        /// Normalizes the root path list.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        List<FileSystemMetadata> NormalizeRootPathList(IEnumerable<FileSystemMetadata> paths);

        /// <summary>
        /// Registers the item.
        /// </summary>
        /// <param name="item">The item.</param>
        void RegisterItem(BaseItem item);

        /// <summary>
        /// Deletes the item.
        /// </summary>
        /// <param name="item">Item to delete.</param>
        /// <param name="options">Options to use for deletion.</param>
        void DeleteItem(BaseItem item, DeleteOptions options);

        /// <summary>
        /// Deletes the item.
        /// </summary>
        /// <param name="item">Item to delete.</param>
        /// <param name="options">Options to use for deletion.</param>
        /// <param name="notifyParentItem">Notify parent of deletion.</param>
        void DeleteItem(BaseItem item, DeleteOptions options, bool notifyParentItem);

        /// <summary>
        /// Deletes the item.
        /// </summary>
        /// <param name="item">Item to delete.</param>
        /// <param name="options">Options to use for deletion.</param>
        /// <param name="parent">Parent of item.</param>
        /// <param name="notifyParentItem">Notify parent of deletion.</param>
        void DeleteItem(BaseItem item, DeleteOptions options, BaseItem parent, bool notifyParentItem);

        /// <summary>
        /// Gets the named view.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="name">The name.</param>
        /// <param name="parentId">The parent identifier.</param>
        /// <param name="viewType">Type of the view.</param>
        /// <param name="sortName">Name of the sort.</param>
        /// <returns>The named view.</returns>
        UserView GetNamedView(
            User user,
            string name,
            Guid parentId,
            CollectionType? viewType,
            string sortName);

        /// <summary>
        /// Gets the named view.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="name">The name.</param>
        /// <param name="viewType">Type of the view.</param>
        /// <param name="sortName">Name of the sort.</param>
        /// <returns>The named view.</returns>
        UserView GetNamedView(
            User user,
            string name,
            CollectionType? viewType,
            string sortName);

        /// <summary>
        /// Gets the named view.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="viewType">Type of the view.</param>
        /// <param name="sortName">Name of the sort.</param>
        /// <returns>The named view.</returns>
        UserView GetNamedView(
            string name,
            CollectionType viewType,
            string sortName);

        /// <summary>
        /// Gets the named view.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="parentId">The parent identifier.</param>
        /// <param name="viewType">Type of the view.</param>
        /// <param name="sortName">Name of the sort.</param>
        /// <param name="uniqueId">The unique identifier.</param>
        /// <returns>The named view.</returns>
        UserView GetNamedView(
            string name,
            Guid parentId,
            CollectionType? viewType,
            string sortName,
            string uniqueId);

        /// <summary>
        /// Gets the shadow view.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="viewType">Type of the view.</param>
        /// <param name="sortName">Name of the sort.</param>
        /// <returns>The shadow view.</returns>
        UserView GetShadowView(
            BaseItem parent,
            CollectionType? viewType,
            string sortName);

        /// <summary>
        /// Gets the season number from path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.Nullable&lt;System.Int32&gt;.</returns>
        int? GetSeasonNumberFromPath(string path);

        /// <summary>
        /// Fills the missing episode numbers from path.
        /// </summary>
        /// <param name="episode">Episode to use.</param>
        /// <param name="forceRefresh">Option to force refresh of episode numbers.</param>
        /// <returns>True if successful.</returns>
        bool FillMissingEpisodeNumbersFromPath(Episode episode, bool forceRefresh);

        /// <summary>
        /// Parses the name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>ItemInfo.</returns>
        ItemLookupInfo ParseName(string name);

        /// <summary>
        /// Gets the new item identifier.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="type">The type.</param>
        /// <returns>Guid.</returns>
        Guid GetNewItemId(string key, Type type);

        /// <summary>
        /// Finds the extras.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="fileSystemChildren">The file system children.</param>
        /// <param name="directoryService">An instance of <see cref="IDirectoryService"/>.</param>
        /// <returns>IEnumerable&lt;BaseItem&gt;.</returns>
        IEnumerable<BaseItem> FindExtras(BaseItem owner, IReadOnlyList<FileSystemMetadata> fileSystemChildren, IDirectoryService directoryService);

        /// <summary>
        /// Gets the collection folders.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The folders that contain the item.</returns>
        List<Folder> GetCollectionFolders(BaseItem item);

        /// <summary>
        /// Gets the collection folders.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="allUserRootChildren">The root folders to consider.</param>
        /// <returns>The folders that contain the item.</returns>
        List<Folder> GetCollectionFolders(BaseItem item, IEnumerable<Folder> allUserRootChildren);

        LibraryOptions GetLibraryOptions(BaseItem item);

        /// <summary>
        /// Gets the people.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>List&lt;PersonInfo&gt;.</returns>
        IReadOnlyList<PersonInfo> GetPeople(BaseItem item);

        /// <summary>
        /// Gets the people.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>List&lt;PersonInfo&gt;.</returns>
        IReadOnlyList<PersonInfo> GetPeople(InternalPeopleQuery query);

        /// <summary>
        /// Gets the people items.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>List&lt;Person&gt;.</returns>
        IReadOnlyList<Person> GetPeopleItems(InternalPeopleQuery query);

        /// <summary>
        /// Updates the people.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="people">The people.</param>
        void UpdatePeople(BaseItem item, List<PersonInfo> people);

        /// <summary>
        /// Asynchronously updates the people.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="people">The people.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The async task.</returns>
        Task UpdatePeopleAsync(BaseItem item, IReadOnlyList<PersonInfo> people, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the item ids.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>List&lt;Guid&gt;.</returns>
        IReadOnlyList<Guid> GetItemIds(InternalItemsQuery query);

        /// <summary>
        /// Gets the people names.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>List&lt;System.String&gt;.</returns>
        IReadOnlyList<string> GetPeopleNames(InternalPeopleQuery query);

        /// <summary>
        /// Queries the items.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>QueryResult&lt;BaseItem&gt;.</returns>
        QueryResult<BaseItem> QueryItems(InternalItemsQuery query);

        string GetPathAfterNetworkSubstitution(string path, BaseItem? ownerItem = null);

        /// <summary>
        /// Converts the image to local.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="image">The image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <param name="removeOnFailure">Whether to remove the image from the item on failure.</param>
        /// <returns>Task.</returns>
        Task<ItemImageInfo> ConvertImageToLocal(BaseItem item, ItemImageInfo image, int imageIndex, bool removeOnFailure = true);

        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>QueryResult&lt;BaseItem&gt;.</returns>
        IReadOnlyList<BaseItem> GetItemList(InternalItemsQuery query);

        IReadOnlyList<BaseItem> GetItemList(InternalItemsQuery query, bool allowExternalContent);

        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <param name="query">The query to use.</param>
        /// <param name="parents">Items to use for query.</param>
        /// <returns>List of items.</returns>
        IReadOnlyList<BaseItem> GetItemList(InternalItemsQuery query, List<BaseItem> parents);

        /// <summary>
        /// Gets the items result.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>QueryResult&lt;BaseItem&gt;.</returns>
        QueryResult<BaseItem> GetItemsResult(InternalItemsQuery query);

        /// <summary>
        /// Ignores the file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="parent">The parent.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool IgnoreFile(FileSystemMetadata file, BaseItem parent);

        Guid GetStudioId(string name);

        Guid GetGenreId(string name);

        Guid GetMusicGenreId(string name);

        Task AddVirtualFolder(string name, CollectionTypeOptions? collectionType, LibraryOptions options, bool refreshLibrary);

        Task RemoveVirtualFolder(string name, bool refreshLibrary);

        void AddMediaPath(string virtualFolderName, MediaPathInfo mediaPath);

        void UpdateMediaPath(string virtualFolderName, MediaPathInfo mediaPath);

        void RemoveMediaPath(string virtualFolderName, string mediaPath);

        QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetGenres(InternalItemsQuery query);

        QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetMusicGenres(InternalItemsQuery query);

        QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetStudios(InternalItemsQuery query);

        QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetArtists(InternalItemsQuery query);

        QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetAlbumArtists(InternalItemsQuery query);

        QueryResult<(BaseItem Item, ItemCounts ItemCounts)> GetAllArtists(InternalItemsQuery query);

        int GetCount(InternalItemsQuery query);

        Task RunMetadataSavers(BaseItem item, ItemUpdateType updateReason);

        BaseItem GetParentItem(Guid? parentId, Guid? userId);

        /// <summary>
        /// Queue a library scan.
        /// </summary>
        /// <remarks>
        /// This exists so plugins can trigger a library scan.
        /// </remarks>
        void QueueLibraryScan();
    }
}
