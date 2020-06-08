using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Interface ILibraryManager
    /// </summary>
    public interface ILibraryManager
    {
        /// <summary>
        /// Resolves the path.
        /// </summary>
        /// <param name="fileInfo">The file information.</param>
        /// <param name="parent">The parent.</param>
        /// <returns>BaseItem.</returns>
        BaseItem ResolvePath(FileSystemMetadata fileInfo,
            Folder parent = null);

        /// <summary>
        /// Resolves a set of files into a list of BaseItem
        /// </summary>
        IEnumerable<BaseItem> ResolvePaths(IEnumerable<FileSystemMetadata> files,
            IDirectoryService directoryService,
            Folder parent,
            LibraryOptions libraryOptions,
            string collectionType = null);

        /// <summary>
        /// Gets the root folder.
        /// </summary>
        /// <value>The root folder.</value>
        AggregateFolder RootFolder { get; }

        /// <summary>
        /// Gets a Person
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Person}.</returns>
        Person GetPerson(string name);

        /// <summary>
        /// Finds the by path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>BaseItem.</returns>
        BaseItem FindByPath(string path, bool? isFolder);

        /// <summary>
        /// Gets the artist.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Artist}.</returns>
        MusicArtist GetArtist(string name);
        MusicArtist GetArtist(string name, DtoOptions options);
        /// <summary>
        /// Gets a Studio
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Studio}.</returns>
        Studio GetStudio(string name);

        /// <summary>
        /// Gets a Genre
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Genre}.</returns>
        Genre GetGenre(string name);

        /// <summary>
        /// Gets the genre.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{MusicGenre}.</returns>
        MusicGenre GetMusicGenre(string name);

        /// <summary>
        /// Gets a Year
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Task{Year}.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        Year GetYear(int value);

        /// <summary>
        /// Validate and refresh the People sub-set of the IBN.
        /// The items are stored in the db but not loaded into memory until actually requested by an operation.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        Task ValidatePeople(CancellationToken cancellationToken, IProgress<double> progress);

        /// <summary>
        /// Reloads the root media folder
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task ValidateMediaLibrary(IProgress<double> progress, CancellationToken cancellationToken);

        /// <summary>
        /// Queues the library scan.
        /// </summary>
        void QueueLibraryScan();

        void UpdateImages(BaseItem item, bool forceUpdate = false);

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
        BaseItem GetItemById(Guid id);

        /// <summary>
        /// Gets the intros.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        Task<IEnumerable<Video>> GetIntros(BaseItem item, User user);

        /// <summary>
        /// Gets all intro files.
        /// </summary>
        /// <returns>IEnumerable{System.String}.</returns>
        IEnumerable<string> GetAllIntroFiles();

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
        IEnumerable<BaseItem> Sort(IEnumerable<BaseItem> items, User user, IEnumerable<string> sortBy, SortOrder sortOrder);
        IEnumerable<BaseItem> Sort(IEnumerable<BaseItem> items, User user, IEnumerable<ValueTuple<string, SortOrder>> orderBy);

        /// <summary>
        /// Gets the user root folder.
        /// </summary>
        /// <returns>UserRootFolder.</returns>
        Folder GetUserRootFolder();

        /// <summary>
        /// Creates the item.
        /// </summary>
        void CreateItem(BaseItem item, BaseItem parent);

        /// <summary>
        /// Creates the items.
        /// </summary>
        void CreateItems(IEnumerable<BaseItem> items, BaseItem parent, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the item.
        /// </summary>
        void UpdateItems(IEnumerable<BaseItem> items, BaseItem parent, ItemUpdateType updateReason, CancellationToken cancellationToken);

        void UpdateItem(BaseItem item, BaseItem parent, ItemUpdateType updateReason, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves the item.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>BaseItem.</returns>
        BaseItem RetrieveItem(Guid id);

        bool IsScanRunning { get; }

        /// <summary>
        /// Occurs when [item added].
        /// </summary>
        event EventHandler<ItemChangeEventArgs> ItemAdded;

        /// <summary>
        /// Occurs when [item updated].
        /// </summary>
        event EventHandler<ItemChangeEventArgs> ItemUpdated;
        /// <summary>
        /// Occurs when [item removed].
        /// </summary>
        event EventHandler<ItemChangeEventArgs> ItemRemoved;

        /// <summary>
        /// Finds the type of the collection.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        string GetContentType(BaseItem item);

        /// <summary>
        /// Gets the type of the inherited content.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        string GetInheritedContentType(BaseItem item);

        /// <summary>
        /// Gets the type of the configured content.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        string GetConfiguredContentType(BaseItem item);

        /// <summary>
        /// Gets the type of the configured content.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        string GetConfiguredContentType(string path);

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
        void DeleteItem(BaseItem item, DeleteOptions options);

        /// <summary>
        /// Deletes the item.
        /// </summary>
        void DeleteItem(BaseItem item, DeleteOptions options, bool notifyParentItem);

        /// <summary>
        /// Deletes the item.
        /// </summary>
        void DeleteItem(BaseItem item, DeleteOptions options, BaseItem parent, bool notifyParentItem);

        /// <summary>
        /// Gets the named view.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="name">The name.</param>
        /// <param name="parentId">The parent identifier.</param>
        /// <param name="viewType">Type of the view.</param>
        /// <param name="sortName">Name of the sort.</param>
        UserView GetNamedView(User user,
            string name,
            Guid parentId,
            string viewType,
            string sortName);

        /// <summary>
        /// Gets the named view.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="name">The name.</param>
        /// <param name="viewType">Type of the view.</param>
        /// <param name="sortName">Name of the sort.</param>
        UserView GetNamedView(User user,
            string name,
            string viewType,
            string sortName);

        /// <summary>
        /// Gets the named view.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="viewType">Type of the view.</param>
        /// <param name="sortName">Name of the sort.</param>
        UserView GetNamedView(string name,
            string viewType,
            string sortName);

        /// <summary>
        /// Gets the named view.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="parentId">The parent identifier.</param>
        /// <param name="viewType">Type of the view.</param>
        /// <param name="sortName">Name of the sort.</param>
        /// <param name="uniqueId">The unique identifier.</param>
        UserView GetNamedView(string name,
            Guid parentId,
            string viewType,
            string sortName,
            string uniqueId);

        /// <summary>
        /// Gets the shadow view.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="viewType">Type of the view.</param>
        /// <param name="sortName">Name of the sort.</param>
        UserView GetShadowView(BaseItem parent,
          string viewType,
          string sortName);

        /// <summary>
        /// Determines whether [is video file] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is video file] [the specified path]; otherwise, <c>false</c>.</returns>
        bool IsVideoFile(string path);

        /// <summary>
        /// Determines whether [is audio file] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is audio file] [the specified path]; otherwise, <c>false</c>.</returns>
        bool IsAudioFile(string path);

        /// <summary>
        /// Gets the season number from path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.Nullable&lt;System.Int32&gt;.</returns>
        int? GetSeasonNumberFromPath(string path);

        /// <summary>
        /// Fills the missing episode numbers from path.
        /// </summary>
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
        /// Finds the trailers.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="fileSystemChildren">The file system children.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <returns>IEnumerable&lt;Trailer&gt;.</returns>
        IEnumerable<Video> FindTrailers(BaseItem owner, List<FileSystemMetadata> fileSystemChildren,
            IDirectoryService directoryService);

        /// <summary>
        /// Finds the extras.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="fileSystemChildren">The file system children.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <returns>IEnumerable&lt;Video&gt;.</returns>
        IEnumerable<Video> FindExtras(BaseItem owner, List<FileSystemMetadata> fileSystemChildren,
            IDirectoryService directoryService);

        /// <summary>
        /// Gets the collection folders.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>IEnumerable&lt;Folder&gt;.</returns>
        List<Folder> GetCollectionFolders(BaseItem item);

        List<Folder> GetCollectionFolders(BaseItem item, List<Folder> allUserRootChildren);

        LibraryOptions GetLibraryOptions(BaseItem item);

        /// <summary>
        /// Gets the people.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>List&lt;PersonInfo&gt;.</returns>
        List<PersonInfo> GetPeople(BaseItem item);

        /// <summary>
        /// Gets the people.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>List&lt;PersonInfo&gt;.</returns>
        List<PersonInfo> GetPeople(InternalPeopleQuery query);

        /// <summary>
        /// Gets the people items.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>List&lt;Person&gt;.</returns>
        List<Person> GetPeopleItems(InternalPeopleQuery query);

        /// <summary>
        /// Updates the people.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="people">The people.</param>
        void UpdatePeople(BaseItem item, List<PersonInfo> people);

        /// <summary>
        /// Gets the item ids.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>List&lt;Guid&gt;.</returns>
        List<Guid> GetItemIds(InternalItemsQuery query);

        /// <summary>
        /// Gets the people names.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>List&lt;System.String&gt;.</returns>
        List<string> GetPeopleNames(InternalPeopleQuery query);

        /// <summary>
        /// Queries the items.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>QueryResult&lt;BaseItem&gt;.</returns>
        QueryResult<BaseItem> QueryItems(InternalItemsQuery query);

        string GetPathAfterNetworkSubstitution(string path, BaseItem ownerItem = null);

        /// <summary>
        /// Substitutes the path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <returns>System.String.</returns>
        string SubstitutePath(string path, string from, string to);

        /// <summary>
        /// Converts the image to local.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="image">The image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns>Task.</returns>
        Task<ItemImageInfo> ConvertImageToLocal(BaseItem item, ItemImageInfo image, int imageIndex);

        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>QueryResult&lt;BaseItem&gt;.</returns>
        List<BaseItem> GetItemList(InternalItemsQuery query);

        List<BaseItem> GetItemList(InternalItemsQuery query, bool allowExternalContent);

        /// <summary>
        /// Gets the items.
        /// </summary>
        List<BaseItem> GetItemList(InternalItemsQuery query, List<BaseItem> parents);

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

        Task AddVirtualFolder(string name, string collectionType, LibraryOptions options, bool refreshLibrary);
        Task RemoveVirtualFolder(string name, bool refreshLibrary);
        void AddMediaPath(string virtualFolderName, MediaPathInfo path);
        void UpdateMediaPath(string virtualFolderName, MediaPathInfo path);
        void RemoveMediaPath(string virtualFolderName, string path);

        QueryResult<(BaseItem, ItemCounts)> GetGenres(InternalItemsQuery query);
        QueryResult<(BaseItem, ItemCounts)> GetMusicGenres(InternalItemsQuery query);
        QueryResult<(BaseItem, ItemCounts)> GetStudios(InternalItemsQuery query);
        QueryResult<(BaseItem, ItemCounts)> GetArtists(InternalItemsQuery query);
        QueryResult<(BaseItem, ItemCounts)> GetAlbumArtists(InternalItemsQuery query);
        QueryResult<(BaseItem, ItemCounts)> GetAllArtists(InternalItemsQuery query);

        int GetCount(InternalItemsQuery query);

        void AddExternalSubtitleStreams(List<MediaStream> streams,
            string videoPath,
            string[] files);
    }
}
