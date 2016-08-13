using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;

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
        /// <summary>
        /// Gets the album artists.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable&lt;MusicArtist&gt;.</returns>
        IEnumerable<MusicArtist> GetAlbumArtists(IEnumerable<IHasAlbumArtist> items);
        /// <summary>
        /// Gets the artists.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable&lt;MusicArtist&gt;.</returns>
        IEnumerable<MusicArtist> GetArtists(IEnumerable<IHasArtist> items);
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
        /// Gets the game genre.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{GameGenre}.</returns>
        GameGenre GetGameGenre(string name);

        /// <summary>
        /// Gets a Year
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Task{Year}.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
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

        /// <summary>
        /// Gets the default view.
        /// </summary>
        /// <returns>IEnumerable{VirtualFolderInfo}.</returns>
        IEnumerable<VirtualFolderInfo> GetVirtualFolders();

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
        /// <param name="pluginFolders">The plugin folders.</param>
        /// <param name="resolvers">The resolvers.</param>
        /// <param name="introProviders">The intro providers.</param>
        /// <param name="itemComparers">The item comparers.</param>
        /// <param name="postscanTasks">The postscan tasks.</param>
        void AddParts(IEnumerable<IResolverIgnoreRule> rules,
            IEnumerable<IVirtualFolderCreator> pluginFolders,
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
        IEnumerable<BaseItem> Sort(IEnumerable<BaseItem> items, User user, IEnumerable<string> sortBy,
                                   SortOrder sortOrder);

        /// <summary>
        /// Ensure supplied item has only one instance throughout
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The proper instance to the item</returns>
        BaseItem GetOrAddByReferenceItem(BaseItem item);

        /// <summary>
        /// Gets the user root folder.
        /// </summary>
        /// <returns>UserRootFolder.</returns>
        Folder GetUserRootFolder();

        /// <summary>
        /// Creates the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task CreateItem(BaseItem item, CancellationToken cancellationToken);

        /// <summary>
        /// Creates the items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task CreateItems(IEnumerable<BaseItem> items, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateReason">The update reason.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateItem(BaseItem item, ItemUpdateType updateReason, CancellationToken cancellationToken);

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
        /// Reports the item removed.
        /// </summary>
        /// <param name="item">The item.</param>
        void ReportItemRemoved(BaseItem item);

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
        IEnumerable<FileSystemMetadata> NormalizeRootPathList(IEnumerable<FileSystemMetadata> paths);

        /// <summary>
        /// Registers the item.
        /// </summary>
        /// <param name="item">The item.</param>
        void RegisterItem(BaseItem item);

        /// <summary>
        /// Deletes the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>Task.</returns>
        Task DeleteItem(BaseItem item, DeleteOptions options);

        /// <summary>
        /// Gets the named view.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="name">The name.</param>
        /// <param name="parentId">The parent identifier.</param>
        /// <param name="viewType">Type of the view.</param>
        /// <param name="sortName">Name of the sort.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;UserView&gt;.</returns>
        Task<UserView> GetNamedView(User user,
            string name,
            string parentId,
            string viewType,
            string sortName,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the named view.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="name">The name.</param>
        /// <param name="viewType">Type of the view.</param>
        /// <param name="sortName">Name of the sort.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;UserView&gt;.</returns>
        Task<UserView> GetNamedView(User user,
            string name,
            string viewType,
            string sortName,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the named view.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="viewType">Type of the view.</param>
        /// <param name="sortName">Name of the sort.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;UserView&gt;.</returns>
        Task<UserView> GetNamedView(string name,
            string viewType,
            string sortName,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the named view.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="parentId">The parent identifier.</param>
        /// <param name="viewType">Type of the view.</param>
        /// <param name="sortName">Name of the sort.</param>
        /// <param name="uniqueId">The unique identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;UserView&gt;.</returns>
        Task<UserView> GetNamedView(string name,
            string parentId,
            string viewType,
            string sortName,
            string uniqueId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the shadow view.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="viewType">Type of the view.</param>
        /// <param name="sortName">Name of the sort.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;UserView&gt;.</returns>
        Task<UserView> GetShadowView(BaseItem parent,
          string viewType,
          string sortName,
          CancellationToken cancellationToken);

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

        bool IsAudioFile(string path, LibraryOptions libraryOptions);
        bool IsVideoFile(string path, LibraryOptions libraryOptions);

        /// <summary>
        /// Gets the season number from path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.Nullable&lt;System.Int32&gt;.</returns>
        int? GetSeasonNumberFromPath(string path);

        /// <summary>
        /// Fills the missing episode numbers from path.
        /// </summary>
        /// <param name="episode">The episode.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool FillMissingEpisodeNumbersFromPath(Episode episode);

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
        IEnumerable<Folder> GetCollectionFolders(BaseItem item);

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
        /// <returns>Task.</returns>
        Task UpdatePeople(BaseItem item, List<PersonInfo> people);

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
        Task<ItemImageInfo> ConvertImageToLocal(IHasImages item, ItemImageInfo image, int imageIndex);

        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>QueryResult&lt;BaseItem&gt;.</returns>
        IEnumerable<BaseItem> GetItemList(InternalItemsQuery query);

        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="parentIds">The parent ids.</param>
        /// <returns>List&lt;BaseItem&gt;.</returns>
        IEnumerable<BaseItem> GetItemList(InternalItemsQuery query, IEnumerable<string> parentIds);

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

        void AddVirtualFolder(string name, string collectionType, string[] mediaPaths, LibraryOptions options, bool refreshLibrary);
        void RemoveVirtualFolder(string name, bool refreshLibrary);
        void AddMediaPath(string virtualFolderName, string path);
        void RemoveMediaPath(string virtualFolderName, string path);

        QueryResult<Tuple<BaseItem, ItemCounts>> GetGenres(InternalItemsQuery query);
        QueryResult<Tuple<BaseItem, ItemCounts>> GetMusicGenres(InternalItemsQuery query);
        QueryResult<Tuple<BaseItem, ItemCounts>> GetGameGenres(InternalItemsQuery query);
        QueryResult<Tuple<BaseItem, ItemCounts>> GetStudios(InternalItemsQuery query);
        QueryResult<Tuple<BaseItem, ItemCounts>> GetArtists(InternalItemsQuery query);
        QueryResult<Tuple<BaseItem, ItemCounts>> GetAlbumArtists(InternalItemsQuery query);
        QueryResult<Tuple<BaseItem, ItemCounts>> GetAllArtists(InternalItemsQuery query);
    }
}