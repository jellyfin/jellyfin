using MediaBrowser.Controller.Entities;
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
        /// Raises the <see cref="E:LibraryChanged" /> event.
        /// </summary>
        /// <param name="args">The <see cref="ChildrenChangedEventArgs" /> instance containing the event data.</param>
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
        /// Saves display preferences for a Folder
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="folder">The folder.</param>
        /// <param name="data">The data.</param>
        /// <returns>Task.</returns>
        Task SaveDisplayPreferencesForFolder(User user, Folder folder, DisplayPreferences data);

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
        /// Gets the item by id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>BaseItem.</returns>
        BaseItem GetItemById(Guid id, Guid userId);

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
        void AddParts(IEnumerable<IResolverIgnoreRule> rules, IEnumerable<IVirtualFolderCreator> pluginFolders,
                      IEnumerable<IItemResolver> resolvers, IEnumerable<IIntroProvider> introProviders, IEnumerable<IBaseItemComparer> itemComparers);

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
    }
}