using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Library
{
    public interface ILibraryManager
    {
        /// <summary>
        /// Fires whenever any validation routine adds or removes items.  The added and removed items are properties of the args.
        /// *** Will fire asynchronously. ***
        /// </summary>
        event EventHandler<ChildrenChangedEventArgs> LibraryChanged;

        /// <summary>
        /// Reports the library changed.
        /// </summary>
        /// <param name="args">The <see cref="ChildrenChangedEventArgs"/> instance containing the event data.</param>
        void ReportLibraryChanged(ChildrenChangedEventArgs args);

        /// <summary>
        /// Resolves the item.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>BaseItem.</returns>
        BaseItem ResolveItem(ItemResolveArgs args);

        /// <summary>
        /// Resolves a path into a BaseItem
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="parent">The parent.</param>
        /// <param name="fileInfo">The file info.</param>
        /// <returns>BaseItem.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        BaseItem ResolvePath(string path, Folder parent = null, WIN32_FIND_DATA? fileInfo = null);

        /// <summary>
        /// Resolves a set of files into a list of BaseItem
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="files">The files.</param>
        /// <param name="parent">The parent.</param>
        /// <returns>List{``0}.</returns>
        List<T> ResolvePaths<T>(IEnumerable<WIN32_FIND_DATA> files, Folder parent) 
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
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <returns>Task{Person}.</returns>
        Task<Person> GetPerson(string name, bool allowSlowProviders = false);

        /// <summary>
        /// Gets the artist.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <returns>Task{Artist}.</returns>
        Task<Artist> GetArtist(string name, bool allowSlowProviders = false);

        /// <summary>
        /// Gets a Studio
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <returns>Task{Studio}.</returns>
        Task<Studio> GetStudio(string name, bool allowSlowProviders = false);

        /// <summary>
        /// Gets a Genre
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <returns>Task{Genre}.</returns>
        Task<Genre> GetGenre(string name, bool allowSlowProviders = false);

        /// <summary>
        /// Gets a Year
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <returns>Task{Year}.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        Task<Year> GetYear(int value, bool allowSlowProviders = false);

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
        IEnumerable<string> GetIntros(BaseItem item, User user);

        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="rules">The rules.</param>
        /// <param name="pluginFolders">The plugin folders.</param>
        /// <param name="resolvers">The resolvers.</param>
        /// <param name="introProviders">The intro providers.</param>
        /// <param name="itemComparers">The item comparers.</param>
        void AddParts(IEnumerable<IResolverIgnoreRule> rules, 
            IEnumerable<IVirtualFolderCreator> pluginFolders, 
            IEnumerable<IItemResolver> resolvers, 
            IEnumerable<IIntroProvider> introProviders, 
            IEnumerable<IBaseItemComparer> itemComparers);

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
        /// <param name="item"></param>
        /// <returns>The proper instance to the item</returns>
        BaseItem GetOrAddByReferenceItem(BaseItem item);

        /// <summary>
        /// Gets the user root folder.
        /// </summary>
        /// <param name="userRootPath">The user root path.</param>
        /// <returns>UserRootFolder.</returns>
        UserRootFolder GetUserRootFolder(string userRootPath);

        /// <summary>
        /// Saves the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveItem(BaseItem item, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves the item.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>Task{BaseItem}.</returns>
        BaseItem RetrieveItem(Guid id);

        /// <summary>
        /// Saves the children.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="children">The children.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveChildren(Guid id, IEnumerable<BaseItem> children, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves the children.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        IEnumerable<BaseItem> RetrieveChildren(Folder parent);

        /// <summary>
        /// Validates the artists.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        Task ValidateArtists(CancellationToken cancellationToken, IProgress<double> progress);
    }
}