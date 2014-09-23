using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MoreLinq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Folder
    /// </summary>
    public class Folder : BaseItem, IHasThemeMedia, IHasTags
    {
        public static IUserManager UserManager { get; set; }
        public static IUserViewManager UserViewManager { get; set; }

        public List<Guid> ThemeSongIds { get; set; }
        public List<Guid> ThemeVideoIds { get; set; }
        public List<string> Tags { get; set; }

        public Folder()
        {
            LinkedChildren = new List<LinkedChild>();

            ThemeSongIds = new List<Guid>();
            ThemeVideoIds = new List<Guid>();
            Tags = new List<string>();
        }

        [IgnoreDataMember]
        public virtual bool IsPreSorted
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is folder.
        /// </summary>
        /// <value><c>true</c> if this instance is folder; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public override bool IsFolder
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is physical root.
        /// </summary>
        /// <value><c>true</c> if this instance is physical root; otherwise, <c>false</c>.</value>
        public bool IsPhysicalRoot { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is root.
        /// </summary>
        /// <value><c>true</c> if this instance is root; otherwise, <c>false</c>.</value>
        public bool IsRoot { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is virtual folder.
        /// </summary>
        /// <value><c>true</c> if this instance is virtual folder; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public virtual bool IsVirtualFolder
        {
            get
            {
                return false;
            }
        }

        public virtual List<LinkedChild> LinkedChildren { get; set; }

        protected virtual bool SupportsShortcutChildren
        {
            get { return true; }
        }

        /// <summary>
        /// Adds the child.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.InvalidOperationException">Unable to add  + item.Name</exception>
        public async Task AddChild(BaseItem item, CancellationToken cancellationToken)
        {
            item.Parent = this;

            if (item.Id == Guid.Empty)
            {
                item.Id = item.Path.GetMBId(item.GetType());
            }

            if (ActualChildren.Any(i => i.Id == item.Id))
            {
                throw new ArgumentException(string.Format("A child with the Id {0} already exists.", item.Id));
            }

            if (item.DateCreated == DateTime.MinValue)
            {
                item.DateCreated = DateTime.UtcNow;
            }
            if (item.DateModified == DateTime.MinValue)
            {
                item.DateModified = DateTime.UtcNow;
            }

            AddChildInternal(item);

            await LibraryManager.CreateItem(item, cancellationToken).ConfigureAwait(false);

            await ItemRepository.SaveChildren(Id, ActualChildren.Select(i => i.Id).ToList(), cancellationToken).ConfigureAwait(false);
        }

        protected void AddChildrenInternal(IEnumerable<BaseItem> children)
        {
            lock (_childrenSyncLock)
            {
                var newChildren = ActualChildren.ToList();
                newChildren.AddRange(children);
                _children = newChildren;
            }
        }
        protected void AddChildInternal(BaseItem child)
        {
            lock (_childrenSyncLock)
            {
                var newChildren = ActualChildren.ToList();
                newChildren.Add(child);
                _children = newChildren;
            }
        }

        protected void RemoveChildrenInternal(IEnumerable<BaseItem> children)
        {
            var ids = children.Select(i => i.Id).ToList();

            lock (_childrenSyncLock)
            {
                _children = ActualChildren.Where(i => !ids.Contains(i.Id)).ToList();
            }
        }

        protected void ClearChildrenInternal()
        {
            lock (_childrenSyncLock)
            {
                _children = new List<BaseItem>();
            }
        }

        [IgnoreDataMember]
        public override string OfficialRatingForComparison
        {
            get
            {
                // Never want folders to be blocked by "BlockNotRated"
                if (this is Series)
                {
                    return base.OfficialRatingForComparison;
                }

                return !string.IsNullOrEmpty(base.OfficialRatingForComparison) ? base.OfficialRatingForComparison : "None";
            }
        }

        /// <summary>
        /// Removes the child.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.InvalidOperationException">Unable to remove  + item.Name</exception>
        public Task RemoveChild(BaseItem item, CancellationToken cancellationToken)
        {
            RemoveChildrenInternal(new[] { item });

            item.Parent = null;

            return ItemRepository.SaveChildren(Id, ActualChildren.Select(i => i.Id).ToList(), cancellationToken);
        }

        /// <summary>
        /// Clears the children.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task ClearChildren(CancellationToken cancellationToken)
        {
            var items = ActualChildren.ToList();

            ClearChildrenInternal();

            foreach (var item in items)
            {
                LibraryManager.ReportItemRemoved(item);
            }

            return ItemRepository.SaveChildren(Id, ActualChildren.Select(i => i.Id).ToList(), cancellationToken);
        }

        #region Indexing

        /// <summary>
        /// Returns the valid set of index by options for this folder type.
        /// Override or extend to modify.
        /// </summary>
        /// <returns>Dictionary{System.StringFunc{UserIEnumerable{BaseItem}}}.</returns>
        protected virtual IEnumerable<string> GetIndexByOptions()
        {
            return new List<string> {            
                {LocalizedStrings.Instance.GetString("NoneDispPref")}, 
                {LocalizedStrings.Instance.GetString("PerformerDispPref")},
                {LocalizedStrings.Instance.GetString("GenreDispPref")},
                {LocalizedStrings.Instance.GetString("DirectorDispPref")},
                {LocalizedStrings.Instance.GetString("YearDispPref")},
                {LocalizedStrings.Instance.GetString("StudioDispPref")}
            };

        }

        /// <summary>
        /// Get the list of indexy by choices for this folder (localized).
        /// </summary>
        /// <value>The index by option strings.</value>
        [IgnoreDataMember]
        public IEnumerable<string> IndexByOptionStrings
        {
            get { return GetIndexByOptions(); }
        }

        #endregion

        /// <summary>
        /// The children
        /// </summary>
        private IReadOnlyList<BaseItem> _children;
        /// <summary>
        /// The _children sync lock
        /// </summary>
        private readonly object _childrenSyncLock = new object();
        /// <summary>
        /// Gets or sets the actual children.
        /// </summary>
        /// <value>The actual children.</value>
        protected virtual IEnumerable<BaseItem> ActualChildren
        {
            get
            {
                return _children ?? (_children = LoadChildrenInternal());
            }
        }

        /// <summary>
        /// thread-safe access to the actual children of this folder - without regard to user
        /// </summary>
        /// <value>The children.</value>
        [IgnoreDataMember]
        public IEnumerable<BaseItem> Children
        {
            get { return ActualChildren; }
        }

        /// <summary>
        /// thread-safe access to all recursive children of this folder - without regard to user
        /// </summary>
        /// <value>The recursive children.</value>
        [IgnoreDataMember]
        public IEnumerable<BaseItem> RecursiveChildren
        {
            get { return GetRecursiveChildren(); }
        }

        public override bool IsVisible(User user)
        {
            if (this is ICollectionFolder)
            {
                if (user.Configuration.BlockedMediaFolders.Contains(Id.ToString("N"), StringComparer.OrdinalIgnoreCase) ||

                    // Backwards compatibility
                    user.Configuration.BlockedMediaFolders.Contains(Name, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return base.IsVisible(user);
        }

        private List<BaseItem> LoadChildrenInternal()
        {
            return LoadChildren().ToList();
        }

        /// <summary>
        /// Loads our children.  Validation will occur externally.
        /// We want this sychronous.
        /// </summary>
        protected virtual IEnumerable<BaseItem> LoadChildren()
        {
            //just load our children from the repo - the library will be validated and maintained in other processes
            return GetCachedChildren();
        }

        public Task ValidateChildren(IProgress<double> progress, CancellationToken cancellationToken)
        {
            return ValidateChildren(progress, cancellationToken, new MetadataRefreshOptions());
        }

        /// <summary>
        /// Validates that the children of the folder still exist
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="metadataRefreshOptions">The metadata refresh options.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <returns>Task.</returns>
        public Task ValidateChildren(IProgress<double> progress, CancellationToken cancellationToken, MetadataRefreshOptions metadataRefreshOptions, bool recursive = true)
        {
            metadataRefreshOptions.DirectoryService = metadataRefreshOptions.DirectoryService ?? new DirectoryService(Logger);

            return ValidateChildrenWithCancellationSupport(progress, cancellationToken, recursive, true, metadataRefreshOptions, metadataRefreshOptions.DirectoryService);
        }

        private Task ValidateChildrenWithCancellationSupport(IProgress<double> progress, CancellationToken cancellationToken, bool recursive, bool refreshChildMetadata, MetadataRefreshOptions refreshOptions, IDirectoryService directoryService)
        {
            return ValidateChildrenInternal(progress, cancellationToken, recursive, refreshChildMetadata, refreshOptions, directoryService);
        }

        private Dictionary<Guid, BaseItem> GetActualChildrenDictionary()
        {
            var dictionary = new Dictionary<Guid, BaseItem>();

            foreach (var child in ActualChildren)
            {
                var id = child.Id;
                if (dictionary.ContainsKey(id))
                {
                    Logger.Error("Found folder containing items with duplicate id. Path: {0}, Child Name: {1}",
                        Path ?? Name,
                        child.Path ?? child.Name);
                }
                else
                {
                    dictionary[id] = child;
                }
            }

            return dictionary;
        }

        private bool IsValidFromResolver(BaseItem current, BaseItem newItem)
        {
            var currentAsVideo = current as Video;

            if (currentAsVideo != null)
            {
                var newAsVideo = newItem as Video;

                if (newAsVideo != null)
                {
                    if (currentAsVideo.IsPlaceHolder != newAsVideo.IsPlaceHolder)
                    {
                        return false;
                    }
                    if (currentAsVideo.IsMultiPart != newAsVideo.IsMultiPart)
                    {
                        return false;
                    }
                    if (currentAsVideo.HasLocalAlternateVersions != newAsVideo.HasLocalAlternateVersions)
                    {
                        return false;
                    }
                }
            }
            else
            {
                var currentAsPlaceHolder = current as ISupportsPlaceHolders;

                if (currentAsPlaceHolder != null)
                {
                    var newHasPlaceHolder = newItem as ISupportsPlaceHolders;

                    if (newHasPlaceHolder != null)
                    {
                        if (currentAsPlaceHolder.IsPlaceHolder != newHasPlaceHolder.IsPlaceHolder)
                        {
                            return false;
                        }
                    }
                }
            }

            return current.IsInMixedFolder == newItem.IsInMixedFolder;
        }

        /// <summary>
        /// Validates the children internal.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <param name="refreshChildMetadata">if set to <c>true</c> [refresh child metadata].</param>
        /// <param name="refreshOptions">The refresh options.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <returns>Task.</returns>
        protected async virtual Task ValidateChildrenInternal(IProgress<double> progress, CancellationToken cancellationToken, bool recursive, bool refreshChildMetadata, MetadataRefreshOptions refreshOptions, IDirectoryService directoryService)
        {
            var locationType = LocationType;

            cancellationToken.ThrowIfCancellationRequested();

            var validChildren = new List<BaseItem>();

            if (locationType != LocationType.Remote && locationType != LocationType.Virtual)
            {
                IEnumerable<BaseItem> nonCachedChildren;

                try
                {
                    nonCachedChildren = GetNonCachedChildren(directoryService);
                }
                catch (IOException ex)
                {
                    nonCachedChildren = new BaseItem[] { };

                    Logger.ErrorException("Error getting file system entries for {0}", ex, Path);
                }

                if (nonCachedChildren == null) return; //nothing to validate

                progress.Report(5);

                //build a dictionary of the current children we have now by Id so we can compare quickly and easily
                var currentChildren = GetActualChildrenDictionary();

                //create a list for our validated children
                var newItems = new List<BaseItem>();

                cancellationToken.ThrowIfCancellationRequested();

                foreach (var child in nonCachedChildren)
                {
                    BaseItem currentChild;

                    if (currentChildren.TryGetValue(child.Id, out currentChild))
                    {
                        if (IsValidFromResolver(currentChild, child))
                        {
                            var currentChildLocationType = currentChild.LocationType;
                            if (currentChildLocationType != LocationType.Remote &&
                                currentChildLocationType != LocationType.Virtual)
                            {
                                currentChild.DateModified = child.DateModified;
                            }

                            currentChild.IsOffline = false;
                            validChildren.Add(currentChild);
                        }
                        else
                        {
                            validChildren.Add(child);
                        }
                    }
                    else
                    {
                        // Brand new item - needs to be added
                        newItems.Add(child);
                        validChildren.Add(child);
                    }
                }

                // If any items were added or removed....
                if (newItems.Count > 0 || currentChildren.Count != validChildren.Count)
                {
                    // That's all the new and changed ones - now see if there are any that are missing
                    var itemsRemoved = currentChildren.Values.Except(validChildren).ToList();
                    var actualRemovals = new List<BaseItem>();

                    foreach (var item in itemsRemoved)
                    {
                        if (item.LocationType == LocationType.Virtual ||
                            item.LocationType == LocationType.Remote)
                        {
                            // Don't remove these because there's no way to accurately validate them.
                            validChildren.Add(item);
                        }

                        else if (!string.IsNullOrEmpty(item.Path) && IsPathOffline(item.Path))
                        {
                            item.IsOffline = true;
                            validChildren.Add(item);
                        }
                        else
                        {
                            item.IsOffline = false;
                            actualRemovals.Add(item);
                        }
                    }

                    if (actualRemovals.Count > 0)
                    {
                        RemoveChildrenInternal(actualRemovals);

                        foreach (var item in actualRemovals)
                        {
                            LibraryManager.ReportItemRemoved(item);
                        }
                    }

                    await LibraryManager.CreateItems(newItems, cancellationToken).ConfigureAwait(false);

                    AddChildrenInternal(newItems);

                    await ItemRepository.SaveChildren(Id, ActualChildren.Select(i => i.Id).ToList(), cancellationToken).ConfigureAwait(false);
                }
            }

            progress.Report(10);

            cancellationToken.ThrowIfCancellationRequested();

            if (recursive)
            {
                await ValidateSubFolders(ActualChildren.OfType<Folder>().ToList(), directoryService, progress, cancellationToken).ConfigureAwait(false);
            }

            progress.Report(20);

            if (refreshChildMetadata)
            {
                var container = this as IMetadataContainer;

                var innerProgress = new ActionableProgress<double>();

                innerProgress.RegisterAction(p => progress.Report((.80 * p) + 20));

                if (container != null)
                {
                    await container.RefreshAllMetadata(refreshOptions, innerProgress, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await RefreshMetadataRecursive(refreshOptions, recursive, innerProgress, cancellationToken);
                }
            }

            progress.Report(100);
        }

        private async Task RefreshMetadataRecursive(MetadataRefreshOptions refreshOptions, bool recursive, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var children = ActualChildren.ToList();

            var percentages = new Dictionary<Guid, double>(children.Count);

            var tasks = new List<Task>();

            foreach (var child in children)
            {
                if (tasks.Count >= 3)
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                    tasks.Clear();
                }

                cancellationToken.ThrowIfCancellationRequested();
                var innerProgress = new ActionableProgress<double>();

                // Avoid implicitly captured closure
                var currentChild = child;
                innerProgress.RegisterAction(p =>
                {
                    lock (percentages)
                    {
                        percentages[currentChild.Id] = p / 100;

                        var percent = percentages.Values.Sum();
                        percent /= children.Count;
                        percent *= 100;
                        progress.Report(percent);
                    }
                });

                if (child.IsFolder)
                {
                    await RefreshChildMetadata(child, refreshOptions, recursive, innerProgress, cancellationToken)
                      .ConfigureAwait(false);
                }
                else
                {
                    // Avoid implicitly captured closure
                    var taskChild = child;

                    tasks.Add(Task.Run(async () => await RefreshChildMetadata(taskChild, refreshOptions, false, innerProgress, cancellationToken).ConfigureAwait(false), cancellationToken));
                }
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            progress.Report(100);
        }

        private async Task RefreshChildMetadata(BaseItem child, MetadataRefreshOptions refreshOptions, bool recursive, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var container = child as IMetadataContainer;

            if (container != null)
            {
                await container.RefreshAllMetadata(refreshOptions, progress, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await child.RefreshMetadata(refreshOptions, cancellationToken).ConfigureAwait(false);

                if (recursive)
                {
                    var folder = child as Folder;

                    if (folder != null)
                    {
                        await folder.RefreshMetadataRecursive(refreshOptions, true, progress, cancellationToken);
                    }
                }
            }
            progress.Report(100);
        }

        /// <summary>
        /// Refreshes the children.
        /// </summary>
        /// <param name="children">The children.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task ValidateSubFolders(IList<Folder> children, IDirectoryService directoryService, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var list = children;
            var childCount = list.Count;

            var percentages = new Dictionary<Guid, double>(list.Count);

            foreach (var item in list)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var child = item;

                var innerProgress = new ActionableProgress<double>();

                innerProgress.RegisterAction(p =>
                {
                    lock (percentages)
                    {
                        percentages[child.Id] = p / 100;

                        var percent = percentages.Values.Sum();
                        percent /= childCount;

                        progress.Report((10 * percent) + 10);
                    }
                });

                await child.ValidateChildrenWithCancellationSupport(innerProgress, cancellationToken, true, false, null, directoryService)
                        .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Determines whether the specified path is offline.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if the specified path is offline; otherwise, <c>false</c>.</returns>
        private bool IsPathOffline(string path)
        {
            if (File.Exists(path))
            {
                return false;
            }

            var originalPath = path;

            // Depending on whether the path is local or unc, it may return either null or '\' at the top
            while (!string.IsNullOrEmpty(path) && path.Length > 1)
            {
                if (Directory.Exists(path))
                {
                    return false;
                }

                path = System.IO.Path.GetDirectoryName(path);
            }

            if (ContainsPath(LibraryManager.GetDefaultVirtualFolders(), originalPath))
            {
                return true;
            }

            return UserManager.Users.Any(user => ContainsPath(LibraryManager.GetVirtualFolders(user), originalPath));
        }

        /// <summary>
        /// Determines whether the specified folders contains path.
        /// </summary>
        /// <param name="folders">The folders.</param>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if the specified folders contains path; otherwise, <c>false</c>.</returns>
        private bool ContainsPath(IEnumerable<VirtualFolderInfo> folders, string path)
        {
            return folders.SelectMany(i => i.Locations).Any(i => ContainsPath(i, path));
        }

        private bool ContainsPath(string parent, string path)
        {
            return string.Equals(parent, path, StringComparison.OrdinalIgnoreCase) || FileSystem.ContainsSubPath(parent, path);
        }

        /// <summary>
        /// Get the children of this folder from the actual file system
        /// </summary>
        /// <returns>IEnumerable{BaseItem}.</returns>
        protected virtual IEnumerable<BaseItem> GetNonCachedChildren(IDirectoryService directoryService)
        {
            return LibraryManager.ResolvePaths<BaseItem>(GetFileSystemChildren(directoryService), directoryService, this);
        }

        /// <summary>
        /// Get our children from the repo - stubbed for now
        /// </summary>
        /// <returns>IEnumerable{BaseItem}.</returns>
        protected IEnumerable<BaseItem> GetCachedChildren()
        {
            return ItemRepository.GetChildren(Id).Select(RetrieveChild).Where(i => i != null);
        }

        /// <summary>
        /// Retrieves the child.
        /// </summary>
        /// <param name="child">The child.</param>
        /// <returns>BaseItem.</returns>
        private BaseItem RetrieveChild(Guid child)
        {
            var item = LibraryManager.GetItemById(child);

            if (item != null)
            {
                if (item is IByReferenceItem)
                {
                    return LibraryManager.GetOrAddByReferenceItem(item);
                }

                item.Parent = this;
            }

            return item;
        }

        public virtual Task<QueryResult<BaseItem>> GetUserItems(UserItemsQuery query)
        {
            var user = query.User;

            var items = query.Recursive
                ? GetRecursiveChildren(user)
                : GetChildren(user, true);

            var result = SortAndFilter(items, query);

            return Task.FromResult(result);
        }

        protected QueryResult<BaseItem> SortAndFilter(IEnumerable<BaseItem> items, UserItemsQuery query)
        {
            return UserViewBuilder.SortAndFilter(items, null, query, LibraryManager, UserDataManager);
        }

        /// <summary>
        /// Gets allowed children of an item
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="includeLinkedChildren">if set to <c>true</c> [include linked children].</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public virtual IEnumerable<BaseItem> GetChildren(User user, bool includeLinkedChildren)
        {
            return GetChildren(user, includeLinkedChildren, false);
        }

        internal IEnumerable<BaseItem> GetChildren(User user, bool includeLinkedChildren, bool includeHidden)
        {
            if (user == null)
            {
                throw new ArgumentNullException();
            }

            //the true root should return our users root folder children
            if (IsPhysicalRoot) return user.RootFolder.GetChildren(user, includeLinkedChildren);

            var list = new List<BaseItem>();

            var hasLinkedChildren = AddChildrenToList(user, includeLinkedChildren, list, includeHidden, false);

            return hasLinkedChildren ? list.DistinctBy(i => i.Id).ToList() : list;
        }

        protected virtual IEnumerable<BaseItem> GetEligibleChildrenForRecursiveChildren(User user)
        {
            return Children;
        }

        /// <summary>
        /// Adds the children to list.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="includeLinkedChildren">if set to <c>true</c> [include linked children].</param>
        /// <param name="list">The list.</param>
        /// <param name="includeHidden">if set to <c>true</c> [include hidden].</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private bool AddChildrenToList(User user, bool includeLinkedChildren, List<BaseItem> list, bool includeHidden, bool recursive)
        {
            var hasLinkedChildren = false;

            foreach (var child in GetEligibleChildrenForRecursiveChildren(user))
            {
                if (child.IsVisible(user))
                {
                    if (includeHidden || !child.IsHiddenFromUser(user))
                    {
                        list.Add(child);
                    }

                    if (recursive && child.IsFolder)
                    {
                        var folder = (Folder)child;

                        if (folder.AddChildrenToList(user, includeLinkedChildren, list, includeHidden, true))
                        {
                            hasLinkedChildren = true;
                        }
                    }
                }
            }

            if (includeLinkedChildren)
            {
                foreach (var child in GetLinkedChildren(user))
                {
                    if (child.IsVisible(user))
                    {
                        hasLinkedChildren = true;

                        list.Add(child);
                    }
                }
            }

            return hasLinkedChildren;
        }

        /// <summary>
        /// Gets allowed recursive children of an item
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="includeLinkedChildren">if set to <c>true</c> [include linked children].</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public virtual IEnumerable<BaseItem> GetRecursiveChildren(User user, bool includeLinkedChildren = true)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            var list = new List<BaseItem>();

            var hasLinkedChildren = AddChildrenToList(user, includeLinkedChildren, list, false, true);

            return hasLinkedChildren ? list.DistinctBy(i => i.Id).ToList() : list;
        }

        /// <summary>
        /// Gets the recursive children.
        /// </summary>
        /// <returns>IList{BaseItem}.</returns>
        public IList<BaseItem> GetRecursiveChildren()
        {
            var list = new List<BaseItem>();

            AddChildrenToList(list, true, null);

            return list;
        }

        /// <summary>
        /// Adds the children to list.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <param name="filter">The filter.</param>
        private void AddChildrenToList(List<BaseItem> list, bool recursive, Func<BaseItem, bool> filter)
        {
            foreach (var child in Children)
            {
                if (filter == null || filter(child))
                {
                    list.Add(child);
                }

                if (recursive && child.IsFolder)
                {
                    var folder = (Folder)child;

                    folder.AddChildrenToList(list, true, filter);
                }
            }
        }


        /// <summary>
        /// Gets the linked children.
        /// </summary>
        /// <returns>IEnumerable{BaseItem}.</returns>
        public IEnumerable<BaseItem> GetLinkedChildren()
        {
            return LinkedChildren
                .Select(GetLinkedChild)
                .Where(i => i != null);
        }

        protected virtual bool FilterLinkedChildrenPerUser
        {
            get
            {
                return false;
            }
        }

        public IEnumerable<BaseItem> GetLinkedChildren(User user)
        {
            if (!FilterLinkedChildrenPerUser || user == null)
            {
                return GetLinkedChildren();
            }

            var locations = user.RootFolder
                .GetChildren(user, true)
                .OfType<CollectionFolder>()
                .SelectMany(i => i.PhysicalLocations)
                .ToList();

            return LinkedChildren
                .Select(i =>
                {
                    var requiresPostFilter = true;

                    if (!string.IsNullOrWhiteSpace(i.Path))
                    {
                        requiresPostFilter = false;

                        if (!locations.Any(l => FileSystem.ContainsSubPath(l, i.Path)))
                        {
                            return null;
                        }
                    }

                    var child = GetLinkedChild(i);

                    if (requiresPostFilter && child != null)
                    {
                        if (string.IsNullOrWhiteSpace(child.Path))
                        {
                            Logger.Debug("Found LinkedChild with null path: {0}", child.Name);
                            return child;
                        }

                        if (!locations.Any(l => FileSystem.ContainsSubPath(l, child.Path)))
                        {
                            return null;
                        }
                    }

                    return child;
                })
                .Where(i => i != null);
        }

        /// <summary>
        /// Gets the linked children.
        /// </summary>
        /// <returns>IEnumerable{BaseItem}.</returns>
        public IEnumerable<Tuple<LinkedChild, BaseItem>> GetLinkedChildrenInfos()
        {
            return LinkedChildren
                .Select(i => new Tuple<LinkedChild, BaseItem>(i, GetLinkedChild(i)))
                .Where(i => i.Item2 != null);
        }

        protected override async Task<bool> RefreshedOwnedItems(MetadataRefreshOptions options, List<FileSystemInfo> fileSystemChildren, CancellationToken cancellationToken)
        {
            var changesFound = false;

            if (SupportsShortcutChildren && LocationType == LocationType.FileSystem)
            {
                if (RefreshLinkedChildren(fileSystemChildren))
                {
                    changesFound = true;
                }
            }

            var baseHasChanges = await base.RefreshedOwnedItems(options, fileSystemChildren, cancellationToken).ConfigureAwait(false);

            return baseHasChanges || changesFound;
        }

        /// <summary>
        /// Refreshes the linked children.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private bool RefreshLinkedChildren(IEnumerable<FileSystemInfo> fileSystemChildren)
        {
            var currentManualLinks = LinkedChildren.Where(i => i.Type == LinkedChildType.Manual).ToList();
            var currentShortcutLinks = LinkedChildren.Where(i => i.Type == LinkedChildType.Shortcut).ToList();

            var newShortcutLinks = fileSystemChildren
                .Where(i => (i.Attributes & FileAttributes.Directory) != FileAttributes.Directory && FileSystem.IsShortcut(i.FullName))
                .Select(i =>
                {
                    try
                    {
                        Logger.Debug("Found shortcut at {0}", i.FullName);

                        var resolvedPath = FileSystem.ResolveShortcut(i.FullName);

                        if (!string.IsNullOrEmpty(resolvedPath))
                        {
                            return new LinkedChild
                            {
                                Path = resolvedPath,
                                Type = LinkedChildType.Shortcut
                            };
                        }

                        Logger.Error("Error resolving shortcut {0}", i.FullName);

                        return null;
                    }
                    catch (IOException ex)
                    {
                        Logger.ErrorException("Error resolving shortcut {0}", ex, i.FullName);
                        return null;
                    }
                })
                .Where(i => i != null)
                .ToList();

            if (!newShortcutLinks.SequenceEqual(currentShortcutLinks, new LinkedChildComparer()))
            {
                Logger.Info("Shortcut links have changed for {0}", Path);

                newShortcutLinks.AddRange(currentManualLinks);
                LinkedChildren = newShortcutLinks;
                return true;
            }

            foreach (var child in LinkedChildren)
            {
                // Reset the cached value
                if (child.ItemId.HasValue && child.ItemId.Value == Guid.Empty)
                {
                    child.ItemId = null;
                }
            }

            return false;
        }

        /// <summary>
        /// Folders need to validate and refresh
        /// </summary>
        /// <returns>Task.</returns>
        public override async Task ChangedExternally()
        {
            var progress = new Progress<double>();

            await ValidateChildren(progress, CancellationToken.None).ConfigureAwait(false);

            await base.ChangedExternally().ConfigureAwait(false);
        }

        /// <summary>
        /// Marks the played.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="datePlayed">The date played.</param>
        /// <param name="userManager">The user manager.</param>
        /// <returns>Task.</returns>
        public override async Task MarkPlayed(User user, DateTime? datePlayed, IUserDataManager userManager)
        {
            // Sweep through recursively and update status
            var tasks = GetRecursiveChildren(user, true).Where(i => !i.IsFolder && i.LocationType != LocationType.Virtual).Select(c => c.MarkPlayed(user, datePlayed, userManager));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Marks the unplayed.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="userManager">The user manager.</param>
        /// <returns>Task.</returns>
        public override async Task MarkUnplayed(User user, IUserDataManager userManager)
        {
            // Sweep through recursively and update status
            var tasks = GetRecursiveChildren(user, true).Where(i => !i.IsFolder && i.LocationType != LocationType.Virtual).Select(c => c.MarkUnplayed(user, userManager));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Finds an item by path, recursively
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>BaseItem.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public BaseItem FindByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException();
            }

            if (string.Equals(Path, path, StringComparison.OrdinalIgnoreCase))
            {
                return this;
            }

            if (PhysicalLocations.Contains(path, StringComparer.OrdinalIgnoreCase))
            {
                return this;
            }

            return RecursiveChildren.FirstOrDefault(i => string.Equals(i.Path, path, StringComparison.OrdinalIgnoreCase) ||
                (!i.IsFolder && !i.IsInMixedFolder && string.Equals(i.ContainingFolderPath, path, StringComparison.OrdinalIgnoreCase)) ||
                i.PhysicalLocations.Contains(path, StringComparer.OrdinalIgnoreCase));
        }

        public override bool IsPlayed(User user)
        {
            return GetRecursiveChildren(user).Where(i => !i.IsFolder && i.LocationType != LocationType.Virtual)
                .All(i => i.IsPlayed(user));
        }

        public override bool IsUnplayed(User user)
        {
            return GetRecursiveChildren(user).Where(i => !i.IsFolder && i.LocationType != LocationType.Virtual)
                .All(i => i.IsUnplayed(user));
        }

        public override void FillUserDataDtoValues(UserItemDataDto dto, UserItemData userData, User user)
        {
            var recursiveItemCount = 0;
            var unplayed = 0;

            double totalPercentPlayed = 0;

            IEnumerable<BaseItem> children;
            var folder = this;

            var season = folder as Season;

            if (season != null)
            {
                children = season.GetEpisodes(user).Where(i => i.LocationType != LocationType.Virtual);
            }
            else
            {
                children = folder.GetRecursiveChildren(user)
                    .Where(i => !i.IsFolder && i.LocationType != LocationType.Virtual);
            }

            // Loop through each recursive child
            foreach (var child in children)
            {
                recursiveItemCount++;

                var isUnplayed = true;

                var itemUserData = UserDataManager.GetUserData(user.Id, child.GetUserDataKey());

                // Incrememt totalPercentPlayed
                if (itemUserData != null)
                {
                    if (itemUserData.Played)
                    {
                        totalPercentPlayed += 100;

                        isUnplayed = false;
                    }
                    else if (itemUserData.PlaybackPositionTicks > 0 && child.RunTimeTicks.HasValue && child.RunTimeTicks.Value > 0)
                    {
                        double itemPercent = itemUserData.PlaybackPositionTicks;
                        itemPercent /= child.RunTimeTicks.Value;
                        totalPercentPlayed += itemPercent;
                    }
                }

                if (isUnplayed)
                {
                    unplayed++;
                }
            }

            dto.UnplayedItemCount = unplayed;

            if (recursiveItemCount > 0)
            {
                dto.PlayedPercentage = totalPercentPlayed / recursiveItemCount;
                dto.Played = dto.PlayedPercentage.Value >= 100;
            }
        }
    }
}
