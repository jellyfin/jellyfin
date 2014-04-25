using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Interface ILibraryManager
    /// </summary>
    public interface ILibraryManager
    {
        /// <summary>
        /// Resolves the item.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>BaseItem.</returns>
        BaseItem ResolveItem(ItemResolveArgs args);

        /// <summary>
        /// Resolves a path into a BaseItem
        /// </summary>
        /// <param name="fileInfo">The file info.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <param name="parent">The parent.</param>
        /// <returns>BaseItem.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        BaseItem ResolvePath(FileSystemInfo fileInfo, IDirectoryService directoryService, Folder parent = null);

        /// <summary>
        /// Resolves the path.
        /// </summary>
        /// <param name="fileInfo">The file information.</param>
        /// <param name="parent">The parent.</param>
        /// <returns>BaseItem.</returns>
        BaseItem ResolvePath(FileSystemInfo fileInfo, Folder parent = null);

        /// <summary>
        /// Resolves a set of files into a list of BaseItem
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="files">The files.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <param name="parent">The parent.</param>
        /// <returns>List{``0}.</returns>
        List<T> ResolvePaths<T>(IEnumerable<FileSystemInfo> files, IDirectoryService directoryService, Folder parent)
            where T : BaseItem;

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
        /// Gets the artist.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{Artist}.</returns>
        MusicArtist GetArtist(string name);

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
        IEnumerable<VirtualFolderInfo> GetDefaultVirtualFolders();

        /// <summary>
        /// Gets the view.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{VirtualFolderInfo}.</returns>
        IEnumerable<VirtualFolderInfo> GetVirtualFolders(User user);

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
        IEnumerable<Video> GetIntros(BaseItem item, User user);

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

        /// <summary>
        /// Validates the artists.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        Task ValidateArtists(CancellationToken cancellationToken, IProgress<double> progress);

        /// <summary>
        /// Validates the music genres.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        Task ValidateMusicGenres(CancellationToken cancellationToken, IProgress<double> progress);

        /// <summary>
        /// Validates the game genres.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        Task ValidateGameGenres(CancellationToken cancellationToken, IProgress<double> progress);

        /// <summary>
        /// Validates the genres.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        Task ValidateGenres(CancellationToken cancellationToken, IProgress<double> progress);

        /// <summary>
        /// Validates the studios.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        Task ValidateStudios(CancellationToken cancellationToken, IProgress<double> progress);

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
        string FindCollectionType(BaseItem item);

        /// <summary>
        /// Normalizes the root path list.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        IEnumerable<string> NormalizeRootPathList(IEnumerable<string> paths);

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
        /// Replaces the videos with primary versions.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        IEnumerable<BaseItem> ReplaceVideosWithPrimaryVersions(IEnumerable<BaseItem> items);
    }

    public static class LibraryManagerExtensions
    {
        public static Task DeleteItem(this ILibraryManager manager, BaseItem item)
        {
            return manager.DeleteItem(item, new DeleteOptions
            {
                DeleteFileLocation = true
            });
        }

        public static BaseItem GetItemById(this ILibraryManager manager, string id)
        {
            return manager.GetItemById(new Guid(id));
        }
    }
}