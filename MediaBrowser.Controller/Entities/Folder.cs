#nullable disable

#pragma warning disable CA1002, CA1721, CA1819, CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;
using Episode = MediaBrowser.Controller.Entities.TV.Episode;
using MusicAlbum = MediaBrowser.Controller.Entities.Audio.MusicAlbum;
using Season = MediaBrowser.Controller.Entities.TV.Season;
using Series = MediaBrowser.Controller.Entities.TV.Series;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Folder.
    /// </summary>
    public class Folder : BaseItem
    {
        public Folder()
        {
            LinkedChildren = Array.Empty<LinkedChild>();
        }

        public static IUserViewManager UserViewManager { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is root.
        /// </summary>
        /// <value><c>true</c> if this instance is root; otherwise, <c>false</c>.</value>
        public bool IsRoot { get; set; }

        public LinkedChild[] LinkedChildren { get; set; }

        [JsonIgnore]
        public DateTime? DateLastMediaAdded { get; set; }

        [JsonIgnore]
        public override bool SupportsThemeMedia => true;

        [JsonIgnore]
        public virtual bool IsPreSorted => false;

        [JsonIgnore]
        public virtual bool IsPhysicalRoot => false;

        [JsonIgnore]
        public override bool SupportsInheritedParentImages => true;

        [JsonIgnore]
        public override bool SupportsPlayedStatus => true;

        /// <summary>
        /// Gets a value indicating whether this instance is folder.
        /// </summary>
        /// <value><c>true</c> if this instance is folder; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public override bool IsFolder => true;

        [JsonIgnore]
        public override bool IsDisplayedAsFolder => true;

        [JsonIgnore]
        public virtual bool SupportsCumulativeRunTimeTicks => false;

        [JsonIgnore]
        public virtual bool SupportsDateLastMediaAdded => false;

        [JsonIgnore]
        public override string FileNameWithoutExtension
        {
            get
            {
                if (IsFileProtocol)
                {
                    return System.IO.Path.GetFileName(Path);
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the actual children.
        /// </summary>
        /// <value>The actual children.</value>
        [JsonIgnore]
        public virtual IEnumerable<BaseItem> Children => LoadChildren();

        /// <summary>
        /// Gets thread-safe access to all recursive children of this folder - without regard to user.
        /// </summary>
        /// <value>The recursive children.</value>
        [JsonIgnore]
        public IEnumerable<BaseItem> RecursiveChildren => GetRecursiveChildren();

        [JsonIgnore]
        protected virtual bool SupportsShortcutChildren => false;

        protected virtual bool FilterLinkedChildrenPerUser => false;

        [JsonIgnore]
        protected override bool SupportsOwnedItems => base.SupportsOwnedItems || SupportsShortcutChildren;

        [JsonIgnore]
        public virtual bool SupportsUserDataFromChildren
        {
            get
            {
                // These are just far too slow.
                if (this is ICollectionFolder)
                {
                    return false;
                }

                if (this is UserView)
                {
                    return false;
                }

                if (this is UserRootFolder)
                {
                    return false;
                }

                if (this is Channel)
                {
                    return false;
                }

                if (SourceType != SourceType.Library)
                {
                    return false;
                }

                if (this is IItemByName)
                {
                    if (this is not IHasDualAccess hasDualAccess || hasDualAccess.IsAccessedByName)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public static ICollectionManager CollectionManager { get; set; }

        public override bool CanDelete()
        {
            if (IsRoot)
            {
                return false;
            }

            return base.CanDelete();
        }

        public override bool RequiresRefresh()
        {
            var baseResult = base.RequiresRefresh();

            if (SupportsCumulativeRunTimeTicks && !RunTimeTicks.HasValue)
            {
                baseResult = true;
            }

            return baseResult;
        }

        /// <summary>
        /// Adds the child.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <exception cref="InvalidOperationException">Unable to add  + item.Name.</exception>
        public void AddChild(BaseItem item)
        {
            item.SetParent(this);

            if (item.Id.IsEmpty())
            {
                item.Id = LibraryManager.GetNewItemId(item.Path, item.GetType());
            }

            if (item.DateCreated == DateTime.MinValue)
            {
                item.DateCreated = DateTime.UtcNow;
            }

            if (item.DateModified == DateTime.MinValue)
            {
                item.DateModified = DateTime.UtcNow;
            }

            LibraryManager.CreateItem(item, this);
        }

        public override bool IsVisible(User user, bool skipAllowedTagsCheck = false)
        {
            if (this is ICollectionFolder && this is not BasePluginFolder)
            {
                var blockedMediaFolders = user.GetPreferenceValues<Guid>(PreferenceKind.BlockedMediaFolders);
                if (blockedMediaFolders.Length > 0)
                {
                    if (blockedMediaFolders.Contains(Id))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!user.HasPermission(PermissionKind.EnableAllFolders)
                        && !user.GetPreferenceValues<Guid>(PreferenceKind.EnabledFolders).Contains(Id))
                    {
                        return false;
                    }
                }
            }

            return base.IsVisible(user, skipAllowedTagsCheck);
        }

        /// <summary>
        /// Loads our children.  Validation will occur externally.
        /// We want this synchronous.
        /// </summary>
        /// <returns>Returns children.</returns>
        protected virtual List<BaseItem> LoadChildren()
        {
            // logger.LogDebug("Loading children from {0} {1} {2}", GetType().Name, Id, Path);
            // just load our children from the repo - the library will be validated and maintained in other processes
            return GetCachedChildren();
        }

        public override double? GetRefreshProgress()
        {
            return ProviderManager.GetRefreshProgress(Id);
        }

        public Task ValidateChildren(IProgress<double> progress, CancellationToken cancellationToken)
        {
            return ValidateChildren(progress, new MetadataRefreshOptions(new DirectoryService(FileSystem)), cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Validates that the children of the folder still exist.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="metadataRefreshOptions">The metadata refresh options.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <param name="allowRemoveRoot">remove item even this folder is root.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task ValidateChildren(IProgress<double> progress, MetadataRefreshOptions metadataRefreshOptions, bool recursive = true, bool allowRemoveRoot = false, CancellationToken cancellationToken = default)
        {
            return ValidateChildrenInternal(progress, recursive, true, allowRemoveRoot, metadataRefreshOptions, metadataRefreshOptions.DirectoryService, cancellationToken);
        }

        private Dictionary<Guid, BaseItem> GetActualChildrenDictionary()
        {
            var dictionary = new Dictionary<Guid, BaseItem>();

            var childrenList = Children.ToList();

            foreach (var child in childrenList)
            {
                var id = child.Id;
                if (dictionary.ContainsKey(id))
                {
                    Logger.LogError(
                        "Found folder containing items with duplicate id. Path: {Path}, Child Name: {ChildName}",
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

        /// <summary>
        /// Validates the children internal.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <param name="refreshChildMetadata">if set to <c>true</c> [refresh child metadata].</param>
        /// <param name="allowRemoveRoot">remove item even this folder is root.</param>
        /// <param name="refreshOptions">The refresh options.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        protected virtual async Task ValidateChildrenInternal(IProgress<double> progress, bool recursive, bool refreshChildMetadata, bool allowRemoveRoot, MetadataRefreshOptions refreshOptions, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            if (recursive)
            {
                ProviderManager.OnRefreshStart(this);
            }

            try
            {
                await ValidateChildrenInternal2(progress, recursive, refreshChildMetadata, allowRemoveRoot, refreshOptions, directoryService, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (recursive)
                {
                    ProviderManager.OnRefreshComplete(this);
                }
            }
        }

        private static bool IsLibraryFolderAccessible(IDirectoryService directoryService, BaseItem item, bool checkCollection)
        {
            if (!checkCollection && (item is BoxSet || string.Equals(item.FileNameWithoutExtension, "collections", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // For top parents i.e. Library folders, skip the validation if it's empty or inaccessible
            if (item.IsTopParent && !directoryService.IsAccessible(item.ContainingFolderPath))
            {
                Logger.LogWarning("Library folder {LibraryFolderPath} is inaccessible or empty, skipping", item.ContainingFolderPath);
                return false;
            }

            return true;
        }

        private async Task ValidateChildrenInternal2(IProgress<double> progress, bool recursive, bool refreshChildMetadata, bool allowRemoveRoot, MetadataRefreshOptions refreshOptions, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            if (!IsLibraryFolderAccessible(directoryService, this, allowRemoveRoot))
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var validChildren = new List<BaseItem>();
            var validChildrenNeedGeneration = false;

            if (IsFileProtocol)
            {
                IEnumerable<BaseItem> nonCachedChildren = [];

                try
                {
                    nonCachedChildren = GetNonCachedChildren(directoryService);
                }
                catch (IOException ex)
                {
                    Logger.LogError(ex, "Error retrieving children from file system");
                }
                catch (SecurityException ex)
                {
                    Logger.LogError(ex, "Error retrieving children from file system");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error retrieving children");
                    return;
                }

                progress.Report(ProgressHelpers.RetrievedChildren);

                if (recursive)
                {
                    ProviderManager.OnRefreshProgress(this, ProgressHelpers.RetrievedChildren);
                }

                // Build a dictionary of the current children we have now by Id so we can compare quickly and easily
                var currentChildren = GetActualChildrenDictionary();

                // Create a list for our validated children
                var newItems = new List<BaseItem>();

                cancellationToken.ThrowIfCancellationRequested();

                foreach (var child in nonCachedChildren)
                {
                    if (!IsLibraryFolderAccessible(directoryService, child, allowRemoveRoot))
                    {
                        continue;
                    }

                    if (currentChildren.TryGetValue(child.Id, out BaseItem currentChild))
                    {
                        validChildren.Add(currentChild);

                        if (currentChild.UpdateFromResolvedItem(child) > ItemUpdateType.None)
                        {
                            await currentChild.UpdateToRepositoryAsync(ItemUpdateType.MetadataImport, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            // metadata is up-to-date; make sure DB has correct images dimensions and hash
                            await LibraryManager.UpdateImagesAsync(currentChild).ConfigureAwait(false);
                        }

                        continue;
                    }

                    // Brand new item - needs to be added
                    child.SetParent(this);
                    newItems.Add(child);
                    validChildren.Add(child);
                }

                // That's all the new and changed ones - now see if any have been removed and need cleanup
                var itemsRemoved = currentChildren.Values.Except(validChildren).ToList();
                var shouldRemove = !IsRoot || allowRemoveRoot;
                // If it's an AggregateFolder, don't remove
                if (shouldRemove && itemsRemoved.Count > 0)
                {
                    foreach (var item in itemsRemoved)
                    {
                        if (item.IsFileProtocol)
                        {
                            Logger.LogDebug("Removed item: {Path}", item.Path);

                            item.SetParent(null);
                            LibraryManager.DeleteItem(item, new DeleteOptions { DeleteFileLocation = false }, this, false);
                        }
                    }
                }

                if (newItems.Count > 0)
                {
                    LibraryManager.CreateItems(newItems, this, cancellationToken);
                }
            }
            else
            {
                validChildrenNeedGeneration = true;
            }

            progress.Report(ProgressHelpers.UpdatedChildItems);

            if (recursive)
            {
                ProviderManager.OnRefreshProgress(this, ProgressHelpers.UpdatedChildItems);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (recursive)
            {
                var folder = this;
                var innerProgress = new Progress<double>(innerPercent =>
                {
                    var percent = ProgressHelpers.GetProgress(ProgressHelpers.UpdatedChildItems, ProgressHelpers.ScannedSubfolders, innerPercent);

                    progress.Report(percent);

                    ProviderManager.OnRefreshProgress(folder, percent);
                });

                if (validChildrenNeedGeneration)
                {
                    validChildren = Children.ToList();
                    validChildrenNeedGeneration = false;
                }

                await ValidateSubFolders(validChildren.OfType<Folder>().ToList(), directoryService, innerProgress, cancellationToken).ConfigureAwait(false);
            }

            if (refreshChildMetadata)
            {
                progress.Report(ProgressHelpers.ScannedSubfolders);

                if (recursive)
                {
                    ProviderManager.OnRefreshProgress(this, ProgressHelpers.ScannedSubfolders);
                }

                var container = this as IMetadataContainer;

                var folder = this;
                var innerProgress = new Progress<double>(innerPercent =>
                {
                    var percent = ProgressHelpers.GetProgress(ProgressHelpers.ScannedSubfolders, ProgressHelpers.RefreshedMetadata, innerPercent);

                    progress.Report(percent);

                    if (recursive)
                    {
                        ProviderManager.OnRefreshProgress(folder, percent);
                    }
                });

                if (container is not null)
                {
                    await RefreshAllMetadataForContainer(container, refreshOptions, innerProgress, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    if (validChildrenNeedGeneration)
                    {
                        validChildren = Children.ToList();
                    }

                    await RefreshMetadataRecursive(validChildren, refreshOptions, recursive, innerProgress, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private Task RefreshMetadataRecursive(IList<BaseItem> children, MetadataRefreshOptions refreshOptions, bool recursive, IProgress<double> progress, CancellationToken cancellationToken)
        {
            return RunTasks(
                (baseItem, innerProgress) => RefreshChildMetadata(baseItem, refreshOptions, recursive && baseItem.IsFolder, innerProgress, cancellationToken),
                children,
                progress,
                cancellationToken);
        }

        private async Task RefreshAllMetadataForContainer(IMetadataContainer container, MetadataRefreshOptions refreshOptions, IProgress<double> progress, CancellationToken cancellationToken)
        {
            if (container is Series series)
            {
                await series.RefreshMetadata(refreshOptions, cancellationToken).ConfigureAwait(false);
            }

            await container.RefreshAllMetadata(refreshOptions, progress, cancellationToken).ConfigureAwait(false);
        }

        private async Task RefreshChildMetadata(BaseItem child, MetadataRefreshOptions refreshOptions, bool recursive, IProgress<double> progress, CancellationToken cancellationToken)
        {
            if (child is IMetadataContainer container)
            {
                await RefreshAllMetadataForContainer(container, refreshOptions, progress, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                if (refreshOptions.RefreshItem(child))
                {
                    await child.RefreshMetadata(refreshOptions, cancellationToken).ConfigureAwait(false);
                }

                if (recursive && child is Folder folder)
                {
                    await folder.RefreshMetadataRecursive(folder.Children.ToList(), refreshOptions, true, progress, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Refreshes the children.
        /// </summary>
        /// <param name="children">The children.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private Task ValidateSubFolders(IList<Folder> children, IDirectoryService directoryService, IProgress<double> progress, CancellationToken cancellationToken)
        {
            return RunTasks(
                (folder, innerProgress) => folder.ValidateChildrenInternal(innerProgress, true, false, false, null, directoryService, cancellationToken),
                children,
                progress,
                cancellationToken);
        }

        /// <summary>
        /// Runs an action block on a list of children.
        /// </summary>
        /// <param name="task">The task to run for each child.</param>
        /// <param name="children">The list of children.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task RunTasks<T>(Func<T, IProgress<double>, Task> task, IList<T> children, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var childrenCount = children.Count;
            var childrenProgress = new double[childrenCount];

            void UpdateProgress()
            {
                progress.Report(childrenProgress.Average());
            }

            var fanoutConcurrency = ConfigurationManager.Configuration.LibraryScanFanoutConcurrency;
            var parallelism = fanoutConcurrency > 0 ? fanoutConcurrency : Environment.ProcessorCount;

            var actionBlock = new ActionBlock<int>(
                async i =>
                {
                    var innerProgress = new Progress<double>(innerPercent =>
                    {
                        // round the percent and only update progress if it changed to prevent excessive UpdateProgress calls
                        var innerPercentRounded = Math.Round(innerPercent);
                        if (childrenProgress[i] != innerPercentRounded)
                        {
                            childrenProgress[i] = innerPercentRounded;
                            UpdateProgress();
                        }
                    });

                    await task(children[i], innerProgress).ConfigureAwait(false);

                    childrenProgress[i] = 100;

                    UpdateProgress();
                },
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = parallelism,
                    CancellationToken = cancellationToken,
                });

            for (var i = 0; i < childrenCount; i++)
            {
                await actionBlock.SendAsync(i, cancellationToken).ConfigureAwait(false);
            }

            actionBlock.Complete();

            await actionBlock.Completion.ConfigureAwait(false);
        }

        /// <summary>
        /// Get the children of this folder from the actual file system.
        /// </summary>
        /// <returns>IEnumerable{BaseItem}.</returns>
        /// <param name="directoryService">The directory service to use for operation.</param>
        /// <returns>Returns set of base items.</returns>
        protected virtual IEnumerable<BaseItem> GetNonCachedChildren(IDirectoryService directoryService)
        {
            var collectionType = LibraryManager.GetContentType(this);
            var libraryOptions = LibraryManager.GetLibraryOptions(this);

            return LibraryManager.ResolvePaths(GetFileSystemChildren(directoryService), directoryService, this, libraryOptions, collectionType);
        }

        /// <summary>
        /// Get our children from the repo - stubbed for now.
        /// </summary>
        /// <returns>IEnumerable{BaseItem}.</returns>
        protected List<BaseItem> GetCachedChildren()
        {
            return ItemRepository.GetItemList(new InternalItemsQuery
            {
                Parent = this,
                GroupByPresentationUniqueKey = false,
                DtoOptions = new DtoOptions(true)
            });
        }

        public virtual int GetChildCount(User user)
        {
            if (LinkedChildren.Length > 0)
            {
                if (this is not ICollectionFolder)
                {
                    return GetChildren(user, true).Count;
                }
            }

            var result = GetItems(new InternalItemsQuery(user)
            {
                Recursive = false,
                Limit = 0,
                Parent = this,
                DtoOptions = new DtoOptions(false)
                {
                    EnableImages = false
                }
            });

            return result.TotalRecordCount;
        }

        public virtual int GetRecursiveChildCount(User user)
        {
            return GetItems(new InternalItemsQuery(user)
            {
                Recursive = true,
                IsFolder = false,
                IsVirtualItem = false,
                EnableTotalRecordCount = true,
                Limit = 0,
                DtoOptions = new DtoOptions(false)
                {
                    EnableImages = false
                }
            }).TotalRecordCount;
        }

        public QueryResult<BaseItem> QueryRecursive(InternalItemsQuery query)
        {
            var user = query.User;

            if (!query.ForceDirect && RequiresPostFiltering(query))
            {
                IEnumerable<BaseItem> items;
                Func<BaseItem, bool> filter = i => UserViewBuilder.Filter(i, user, query, UserDataManager, LibraryManager);

                if (query.User is null)
                {
                    items = GetRecursiveChildren(filter);
                }
                else
                {
                    items = GetRecursiveChildren(user, query);
                }

                return PostFilterAndSort(items, query, true);
            }

            if (this is not UserRootFolder
                && this is not AggregateFolder
                && query.ParentId.IsEmpty())
            {
                query.Parent = this;
            }

            if (RequiresPostFiltering2(query))
            {
                return QueryWithPostFiltering2(query);
            }

            return LibraryManager.GetItemsResult(query);
        }

        protected QueryResult<BaseItem> QueryWithPostFiltering2(InternalItemsQuery query)
        {
            var startIndex = query.StartIndex;
            var limit = query.Limit;

            query.StartIndex = null;
            query.Limit = null;

            IEnumerable<BaseItem> itemsList = LibraryManager.GetItemList(query);
            var user = query.User;

            if (user is not null)
            {
                // needed for boxsets
                itemsList = itemsList.Where(i => i.IsVisibleStandalone(query.User));
            }

            IEnumerable<BaseItem> returnItems;
            int totalCount = 0;

            if (query.EnableTotalRecordCount)
            {
                var itemArray = itemsList.ToArray();
                totalCount = itemArray.Length;
                returnItems = itemArray;
            }
            else
            {
                returnItems = itemsList;
            }

            if (limit.HasValue)
            {
                returnItems = returnItems.Skip(startIndex ?? 0).Take(limit.Value);
            }
            else if (startIndex.HasValue)
            {
                returnItems = returnItems.Skip(startIndex.Value);
            }

            return new QueryResult<BaseItem>(
                query.StartIndex,
                totalCount,
                returnItems.ToArray());
        }

        private bool RequiresPostFiltering2(InternalItemsQuery query)
        {
            if (query.IncludeItemTypes.Length == 1 && query.IncludeItemTypes[0] == BaseItemKind.BoxSet)
            {
                Logger.LogDebug("Query requires post-filtering due to BoxSet query");
                return true;
            }

            return false;
        }

        private bool RequiresPostFiltering(InternalItemsQuery query)
        {
            if (LinkedChildren.Length > 0)
            {
                if (this is not ICollectionFolder)
                {
                    Logger.LogDebug("{Type}: Query requires post-filtering due to LinkedChildren.", GetType().Name);
                    return true;
                }
            }

            // Filter by Video3DFormat
            if (query.Is3D.HasValue)
            {
                Logger.LogDebug("Query requires post-filtering due to Is3D");
                return true;
            }

            if (query.HasOfficialRating.HasValue)
            {
                Logger.LogDebug("Query requires post-filtering due to HasOfficialRating");
                return true;
            }

            if (query.IsPlaceHolder.HasValue)
            {
                Logger.LogDebug("Query requires post-filtering due to IsPlaceHolder");
                return true;
            }

            if (query.HasSpecialFeature.HasValue)
            {
                Logger.LogDebug("Query requires post-filtering due to HasSpecialFeature");
                return true;
            }

            if (query.HasSubtitles.HasValue)
            {
                Logger.LogDebug("Query requires post-filtering due to HasSubtitles");
                return true;
            }

            if (query.HasTrailer.HasValue)
            {
                Logger.LogDebug("Query requires post-filtering due to HasTrailer");
                return true;
            }

            if (query.HasThemeSong.HasValue)
            {
                Logger.LogDebug("Query requires post-filtering due to HasThemeSong");
                return true;
            }

            if (query.HasThemeVideo.HasValue)
            {
                Logger.LogDebug("Query requires post-filtering due to HasThemeVideo");
                return true;
            }

            // Filter by VideoType
            if (query.VideoTypes.Length > 0)
            {
                Logger.LogDebug("Query requires post-filtering due to VideoTypes");
                return true;
            }

            if (CollapseBoxSetItems(query, this, query.User, ConfigurationManager))
            {
                Logger.LogDebug("Query requires post-filtering due to CollapseBoxSetItems");
                return true;
            }

            if (!query.AdjacentTo.IsNullOrEmpty())
            {
                Logger.LogDebug("Query requires post-filtering due to AdjacentTo");
                return true;
            }

            if (query.SeriesStatuses.Length > 0)
            {
                Logger.LogDebug("Query requires post-filtering due to SeriesStatuses");
                return true;
            }

            if (query.AiredDuringSeason.HasValue)
            {
                Logger.LogDebug("Query requires post-filtering due to AiredDuringSeason");
                return true;
            }

            if (query.IsPlayed.HasValue)
            {
                if (query.IncludeItemTypes.Length == 1 && query.IncludeItemTypes.Contains(BaseItemKind.Series))
                {
                    Logger.LogDebug("Query requires post-filtering due to IsPlayed");
                    return true;
                }
            }

            return false;
        }

        private static BaseItem[] SortItemsByRequest(InternalItemsQuery query, IReadOnlyList<BaseItem> items)
        {
            return items.OrderBy(i => Array.IndexOf(query.ItemIds, i.Id)).ToArray();
        }

        public QueryResult<BaseItem> GetItems(InternalItemsQuery query)
        {
            if (query.ItemIds.Length > 0)
            {
                var result = LibraryManager.GetItemsResult(query);

                if (query.OrderBy.Count == 0 && query.ItemIds.Length > 1)
                {
                    result.Items = SortItemsByRequest(query, result.Items);
                }

                return result;
            }

            return GetItemsInternal(query);
        }

        public IReadOnlyList<BaseItem> GetItemList(InternalItemsQuery query)
        {
            query.EnableTotalRecordCount = false;

            if (query.ItemIds.Length > 0)
            {
                var result = LibraryManager.GetItemList(query);

                if (query.OrderBy.Count == 0 && query.ItemIds.Length > 1)
                {
                    return SortItemsByRequest(query, result);
                }

                return result;
            }

            return GetItemsInternal(query).Items;
        }

        protected virtual QueryResult<BaseItem> GetItemsInternal(InternalItemsQuery query)
        {
            if (SourceType == SourceType.Channel)
            {
                try
                {
                    query.Parent = this;
                    query.ChannelIds = new[] { ChannelId };

                    // Don't blow up here because it could cause parent screens with other content to fail
                    return ChannelManager.GetChannelItemsInternal(query, new Progress<double>(), CancellationToken.None).GetAwaiter().GetResult();
                }
                catch
                {
                    // Already logged at lower levels
                    return new QueryResult<BaseItem>();
                }
            }

            if (query.Recursive)
            {
                return QueryRecursive(query);
            }

            var user = query.User;

            Func<BaseItem, bool> filter = i => UserViewBuilder.Filter(i, user, query, UserDataManager, LibraryManager);

            IEnumerable<BaseItem> items;

            if (query.User is null)
            {
                items = Children.Where(filter);
            }
            else
            {
                // need to pass this param to the children.
                var childQuery = new InternalItemsQuery
                {
                    DisplayAlbumFolders = query.DisplayAlbumFolders
                };

                items = GetChildren(user, true, childQuery).Where(filter);
            }

            return PostFilterAndSort(items, query, true);
        }

        protected QueryResult<BaseItem> PostFilterAndSort(IEnumerable<BaseItem> items, InternalItemsQuery query, bool enableSorting)
        {
            var user = query.User;

            // Check recursive - don't substitute in plain folder views
            if (user is not null)
            {
                items = CollapseBoxSetItemsIfNeeded(items, query, this, user, ConfigurationManager, CollectionManager);
            }

            #pragma warning disable CA1309
            if (!string.IsNullOrEmpty(query.NameStartsWithOrGreater))
            {
                items = items.Where(i => string.Compare(query.NameStartsWithOrGreater, i.SortName, StringComparison.InvariantCultureIgnoreCase) < 1);
            }

            if (!string.IsNullOrEmpty(query.NameStartsWith))
            {
                items = items.Where(i => i.SortName.StartsWith(query.NameStartsWith, StringComparison.InvariantCultureIgnoreCase));
            }

            if (!string.IsNullOrEmpty(query.NameLessThan))
            {
                items = items.Where(i => string.Compare(query.NameLessThan, i.SortName, StringComparison.InvariantCultureIgnoreCase) == 1);
            }
            #pragma warning restore CA1309

            // This must be the last filter
            if (!query.AdjacentTo.IsNullOrEmpty())
            {
                items = UserViewBuilder.FilterForAdjacency(items.ToList(), query.AdjacentTo.Value);
            }

            return UserViewBuilder.SortAndPage(items, null, query, LibraryManager, enableSorting);
        }

        private static IEnumerable<BaseItem> CollapseBoxSetItemsIfNeeded(
            IEnumerable<BaseItem> items,
            InternalItemsQuery query,
            BaseItem queryParent,
            User user,
            IServerConfigurationManager configurationManager,
            ICollectionManager collectionManager)
        {
            ArgumentNullException.ThrowIfNull(items);

            if (CollapseBoxSetItems(query, queryParent, user, configurationManager))
            {
                items = collectionManager.CollapseItemsWithinBoxSets(items, user);
            }

            return items;
        }

        private static bool CollapseBoxSetItems(
            InternalItemsQuery query,
            BaseItem queryParent,
            User user,
            IServerConfigurationManager configurationManager)
        {
            // Could end up stuck in a loop like this
            if (queryParent is BoxSet)
            {
                return false;
            }

            if (queryParent is Series)
            {
                return false;
            }

            if (queryParent is Season)
            {
                return false;
            }

            if (queryParent is MusicAlbum)
            {
                return false;
            }

            if (queryParent is MusicArtist)
            {
                return false;
            }

            var param = query.CollapseBoxSetItems;

            if (!param.HasValue)
            {
                if (user is not null && !configurationManager.Configuration.EnableGroupingIntoCollections)
                {
                    return false;
                }

                if (query.IncludeItemTypes.Length == 0 || query.IncludeItemTypes.Contains(BaseItemKind.Movie))
                {
                    param = true;
                }
            }

            return param.HasValue && param.Value && AllowBoxSetCollapsing(query);
        }

        private static bool AllowBoxSetCollapsing(InternalItemsQuery request)
        {
            if (request.IsFavorite.HasValue)
            {
                return false;
            }

            if (request.IsFavoriteOrLiked.HasValue)
            {
                return false;
            }

            if (request.IsLiked.HasValue)
            {
                return false;
            }

            if (request.IsPlayed.HasValue)
            {
                return false;
            }

            if (request.IsResumable.HasValue)
            {
                return false;
            }

            if (request.IsFolder.HasValue)
            {
                return false;
            }

            if (request.Genres.Count > 0)
            {
                return false;
            }

            if (request.GenreIds.Count > 0)
            {
                return false;
            }

            if (request.HasImdbId.HasValue)
            {
                return false;
            }

            if (request.HasOfficialRating.HasValue)
            {
                return false;
            }

            if (request.HasOverview.HasValue)
            {
                return false;
            }

            if (request.HasParentalRating.HasValue)
            {
                return false;
            }

            if (request.HasSpecialFeature.HasValue)
            {
                return false;
            }

            if (request.HasSubtitles.HasValue)
            {
                return false;
            }

            if (request.HasThemeSong.HasValue)
            {
                return false;
            }

            if (request.HasThemeVideo.HasValue)
            {
                return false;
            }

            if (request.HasTmdbId.HasValue)
            {
                return false;
            }

            if (request.HasTrailer.HasValue)
            {
                return false;
            }

            if (request.ImageTypes.Length > 0)
            {
                return false;
            }

            if (request.Is3D.HasValue)
            {
                return false;
            }

            if (request.Is4K.HasValue)
            {
                return false;
            }

            if (request.IsHD.HasValue)
            {
                return false;
            }

            if (request.IsLocked.HasValue)
            {
                return false;
            }

            if (request.IsPlaceHolder.HasValue)
            {
                return false;
            }

            if (request.IsPlayed.HasValue)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(request.Person))
            {
                return false;
            }

            if (request.PersonIds.Length > 0)
            {
                return false;
            }

            if (request.ItemIds.Length > 0)
            {
                return false;
            }

            if (request.StudioIds.Length > 0)
            {
                return false;
            }

            if (request.GenreIds.Count > 0)
            {
                return false;
            }

            if (request.VideoTypes.Length > 0)
            {
                return false;
            }

            if (request.Years.Length > 0)
            {
                return false;
            }

            if (request.Tags.Length > 0)
            {
                return false;
            }

            if (request.OfficialRatings.Length > 0)
            {
                return false;
            }

            if (request.MinCommunityRating.HasValue)
            {
                return false;
            }

            if (request.MinCriticRating.HasValue)
            {
                return false;
            }

            if (request.MinIndexNumber.HasValue)
            {
                return false;
            }

            return true;
        }

        public List<BaseItem> GetChildren(User user, bool includeLinkedChildren)
        {
            ArgumentNullException.ThrowIfNull(user);

            return GetChildren(user, includeLinkedChildren, new InternalItemsQuery(user));
        }

        public virtual List<BaseItem> GetChildren(User user, bool includeLinkedChildren, InternalItemsQuery query)
        {
            ArgumentNullException.ThrowIfNull(user);

            // the true root should return our users root folder children
            if (IsPhysicalRoot)
            {
                return LibraryManager.GetUserRootFolder().GetChildren(user, includeLinkedChildren);
            }

            var result = new Dictionary<Guid, BaseItem>();

            AddChildren(user, includeLinkedChildren, result, false, query);

            return result.Values.ToList();
        }

        protected virtual IEnumerable<BaseItem> GetEligibleChildrenForRecursiveChildren(User user)
        {
            return Children;
        }

        /// <summary>
        /// Adds the children to list.
        /// </summary>
        private void AddChildren(User user, bool includeLinkedChildren, Dictionary<Guid, BaseItem> result, bool recursive, InternalItemsQuery query, HashSet<Folder> visitedFolders = null)
        {
            // Prevent infinite recursion of nested folders
            visitedFolders ??= new HashSet<Folder>();
            if (!visitedFolders.Add(this))
            {
                return;
            }

            // If Query.AlbumFolders is set, then enforce the format as per the db in that it permits sub-folders in music albums.
            IEnumerable<BaseItem> children = null;
            if ((query?.DisplayAlbumFolders ?? false) && (this is MusicAlbum))
            {
                children = Children;
                query = null;
            }

            // If there are not sub-folders, proceed as normal.
            if (children is null)
            {
                children = GetEligibleChildrenForRecursiveChildren(user);
            }

            AddChildrenFromCollection(children, user, includeLinkedChildren, result, recursive, query, visitedFolders);

            if (includeLinkedChildren)
            {
                AddChildrenFromCollection(GetLinkedChildren(user), user, includeLinkedChildren, result, recursive, query, visitedFolders);
            }
        }

        private void AddChildrenFromCollection(IEnumerable<BaseItem> children, User user, bool includeLinkedChildren, Dictionary<Guid, BaseItem> result, bool recursive, InternalItemsQuery query, HashSet<Folder> visitedFolders)
        {
            foreach (var child in children)
            {
                if (!child.IsVisible(user))
                {
                    continue;
                }

                if (query is null || UserViewBuilder.FilterItem(child, query))
                {
                    result[child.Id] = child;
                }

                if (recursive && child.IsFolder)
                {
                    var folder = (Folder)child;

                    folder.AddChildren(user, includeLinkedChildren, result, true, query, visitedFolders);
                }
            }
        }

        public virtual IEnumerable<BaseItem> GetRecursiveChildren(User user, InternalItemsQuery query)
        {
            ArgumentNullException.ThrowIfNull(user);

            var result = new Dictionary<Guid, BaseItem>();

            AddChildren(user, true, result, true, query);

            return result.Values;
        }

        /// <summary>
        /// Gets the recursive children.
        /// </summary>
        /// <returns>IList{BaseItem}.</returns>
        public IList<BaseItem> GetRecursiveChildren()
        {
            return GetRecursiveChildren(true);
        }

        public IList<BaseItem> GetRecursiveChildren(bool includeLinkedChildren)
        {
            return GetRecursiveChildren(i => true, includeLinkedChildren);
        }

        public IList<BaseItem> GetRecursiveChildren(Func<BaseItem, bool> filter)
        {
            return GetRecursiveChildren(filter, true);
        }

        public IList<BaseItem> GetRecursiveChildren(Func<BaseItem, bool> filter, bool includeLinkedChildren)
        {
            var result = new Dictionary<Guid, BaseItem>();

            AddChildrenToList(result, includeLinkedChildren, true, filter);

            return result.Values.ToList();
        }

        /// <summary>
        /// Adds the children to list.
        /// </summary>
        private void AddChildrenToList(Dictionary<Guid, BaseItem> result, bool includeLinkedChildren, bool recursive, Func<BaseItem, bool> filter)
        {
            foreach (var child in Children)
            {
                if (filter is null || filter(child))
                {
                    result[child.Id] = child;
                }

                if (recursive && child.IsFolder)
                {
                    var folder = (Folder)child;

                    // We can only support includeLinkedChildren for the first folder, or we might end up stuck in a loop of linked items
                    folder.AddChildrenToList(result, false, true, filter);
                }
            }

            if (includeLinkedChildren)
            {
                foreach (var child in GetLinkedChildren())
                {
                    if (filter is null || filter(child))
                    {
                        result[child.Id] = child;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the linked children.
        /// </summary>
        /// <returns>IEnumerable{BaseItem}.</returns>
        public List<BaseItem> GetLinkedChildren()
        {
            var linkedChildren = LinkedChildren;
            var list = new List<BaseItem>(linkedChildren.Length);

            foreach (var i in linkedChildren)
            {
                var child = GetLinkedChild(i);

                if (child is not null)
                {
                    list.Add(child);
                }
            }

            return list;
        }

        public bool ContainsLinkedChildByItemId(Guid itemId)
        {
            var linkedChildren = LinkedChildren;
            foreach (var i in linkedChildren)
            {
                if (i.ItemId.HasValue)
                {
                    if (i.ItemId.Value.Equals(itemId))
                    {
                        return true;
                    }

                    continue;
                }

                var child = GetLinkedChild(i);

                if (child is not null && child.Id.Equals(itemId))
                {
                    return true;
                }
            }

            return false;
        }

        public List<BaseItem> GetLinkedChildren(User user)
        {
            if (!FilterLinkedChildrenPerUser || user is null)
            {
                return GetLinkedChildren();
            }

            var linkedChildren = LinkedChildren;
            var list = new List<BaseItem>(linkedChildren.Length);

            if (linkedChildren.Length == 0)
            {
                return list;
            }

            var allUserRootChildren = LibraryManager.GetUserRootFolder()
                .GetChildren(user, true)
                .OfType<Folder>()
                .ToList();

            var collectionFolderIds = allUserRootChildren
                .Select(i => i.Id)
                .ToList();

            foreach (var i in linkedChildren)
            {
                var child = GetLinkedChild(i);

                if (child is null)
                {
                    continue;
                }

                var childOwner = child.GetOwner() ?? child;

                if (child is not IItemByName)
                {
                    var childProtocol = childOwner.PathProtocol;
                    if (!childProtocol.HasValue || childProtocol.Value != Model.MediaInfo.MediaProtocol.File)
                    {
                        if (!childOwner.IsVisibleStandalone(user))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        var itemCollectionFolderIds =
                            LibraryManager.GetCollectionFolders(childOwner, allUserRootChildren).Select(f => f.Id);

                        if (!itemCollectionFolderIds.Any(collectionFolderIds.Contains))
                        {
                            continue;
                        }
                    }
                }

                list.Add(child);
            }

            return list;
        }

        /// <summary>
        /// Gets the linked children.
        /// </summary>
        /// <returns>IEnumerable{BaseItem}.</returns>
        public IEnumerable<Tuple<LinkedChild, BaseItem>> GetLinkedChildrenInfos()
        {
            return LinkedChildren
                .Select(i => new Tuple<LinkedChild, BaseItem>(i, GetLinkedChild(i)))
                .Where(i => i.Item2 is not null);
        }

        protected override async Task<bool> RefreshedOwnedItems(MetadataRefreshOptions options, IReadOnlyList<FileSystemMetadata> fileSystemChildren, CancellationToken cancellationToken)
        {
            var changesFound = false;

            if (IsFileProtocol)
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
        /// <param name="fileSystemChildren">The enumerable of file system metadata.</param>
        /// <returns><c>true</c> if the linked children were updated, <c>false</c> otherwise.</returns>
        protected virtual bool RefreshLinkedChildren(IEnumerable<FileSystemMetadata> fileSystemChildren)
        {
            if (SupportsShortcutChildren)
            {
                var newShortcutLinks = fileSystemChildren
                    .Where(i => !i.IsDirectory && FileSystem.IsShortcut(i.FullName))
                    .Select(i =>
                    {
                        try
                        {
                            Logger.LogDebug("Found shortcut at {0}", i.FullName);

                            var resolvedPath = CollectionFolder.ApplicationHost.ExpandVirtualPath(FileSystem.ResolveShortcut(i.FullName));

                            if (!string.IsNullOrEmpty(resolvedPath))
                            {
                                return new LinkedChild
                                {
                                    Path = resolvedPath,
                                    Type = LinkedChildType.Shortcut
                                };
                            }

                            Logger.LogError("Error resolving shortcut {0}", i.FullName);

                            return null;
                        }
                        catch (IOException ex)
                        {
                            Logger.LogError(ex, "Error resolving shortcut {0}", i.FullName);
                            return null;
                        }
                    })
                    .Where(i => i is not null)
                    .ToList();

                var currentShortcutLinks = LinkedChildren.Where(i => i.Type == LinkedChildType.Shortcut).ToList();

                if (!newShortcutLinks.SequenceEqual(currentShortcutLinks, new LinkedChildComparer(FileSystem)))
                {
                    Logger.LogInformation("Shortcut links have changed for {0}", Path);

                    newShortcutLinks.AddRange(LinkedChildren.Where(i => i.Type == LinkedChildType.Manual));
                    LinkedChildren = newShortcutLinks.ToArray();
                    return true;
                }
            }

            foreach (var child in LinkedChildren)
            {
                // Reset the cached value
                child.ItemId = null;
            }

            return false;
        }

        /// <summary>
        /// Marks the played.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="datePlayed">The date played.</param>
        /// <param name="resetPosition">if set to <c>true</c> [reset position].</param>
        public override void MarkPlayed(
            User user,
            DateTime? datePlayed,
            bool resetPosition)
        {
            var query = new InternalItemsQuery
            {
                User = user,
                Recursive = true,
                IsFolder = false,
                EnableTotalRecordCount = false
            };

            if (!user.DisplayMissingEpisodes)
            {
                query.IsVirtualItem = false;
            }

            var itemsResult = GetItemList(query);

            // Sweep through recursively and update status
            foreach (var item in itemsResult)
            {
                if (item.IsVirtualItem)
                {
                    // The querying doesn't support virtual unaired
                    var episode = item as Episode;
                    if (episode is not null && episode.IsUnaired)
                    {
                        continue;
                    }
                }

                item.MarkPlayed(user, datePlayed, resetPosition);
            }
        }

        /// <summary>
        /// Marks the unplayed.
        /// </summary>
        /// <param name="user">The user.</param>
        public override void MarkUnplayed(User user)
        {
            var itemsResult = GetItemList(new InternalItemsQuery
            {
                User = user,
                Recursive = true,
                IsFolder = false,
                EnableTotalRecordCount = false
            });

            // Sweep through recursively and update status
            foreach (var item in itemsResult)
            {
                item.MarkUnplayed(user);
            }
        }

        public override bool IsPlayed(User user)
        {
            var itemsResult = GetItemList(new InternalItemsQuery(user)
            {
                Recursive = true,
                IsFolder = false,
                IsVirtualItem = false,
                EnableTotalRecordCount = false
            });

            return itemsResult
                .All(i => i.IsPlayed(user));
        }

        public override bool IsUnplayed(User user)
        {
            return !IsPlayed(user);
        }

        public override void FillUserDataDtoValues(UserItemDataDto dto, UserItemData userData, BaseItemDto itemDto, User user, DtoOptions fields)
        {
            if (!SupportsUserDataFromChildren)
            {
                return;
            }

            if (itemDto is not null && fields.ContainsField(ItemFields.RecursiveItemCount))
            {
                itemDto.RecursiveItemCount = GetRecursiveChildCount(user);
            }

            if (SupportsPlayedStatus)
            {
                var unplayedQueryResult = GetItems(new InternalItemsQuery(user)
                {
                    Recursive = true,
                    IsFolder = false,
                    IsVirtualItem = false,
                    EnableTotalRecordCount = true,
                    Limit = 0,
                    IsPlayed = false,
                    DtoOptions = new DtoOptions(false)
                    {
                        EnableImages = false
                    }
                }).TotalRecordCount;

                dto.UnplayedItemCount = unplayedQueryResult;

                if (itemDto?.RecursiveItemCount > 0)
                {
                    var unplayedPercentage = ((double)unplayedQueryResult / itemDto.RecursiveItemCount.Value) * 100;
                    dto.PlayedPercentage = 100 - unplayedPercentage;
                    dto.Played = dto.PlayedPercentage.Value >= 100;
                }
                else
                {
                    dto.Played = (dto.UnplayedItemCount ?? 0) == 0;
                }
            }
        }

        /// <summary>
        /// Contains constants used when reporting scan progress.
        /// </summary>
        private static class ProgressHelpers
        {
            /// <summary>
            /// Reported after the folders immediate children are retrieved.
            /// </summary>
            public const int RetrievedChildren = 5;

            /// <summary>
            /// Reported after add, updating, or deleting child items from the LibraryManager.
            /// </summary>
            public const int UpdatedChildItems = 10;

            /// <summary>
            /// Reported once subfolders are scanned.
            /// When scanning subfolders, the progress will be between [UpdatedItems, ScannedSubfolders].
            /// </summary>
            public const int ScannedSubfolders = 50;

            /// <summary>
            /// Reported once metadata is refreshed.
            /// When refreshing metadata, the progress will be between [ScannedSubfolders, MetadataRefreshed].
            /// </summary>
            public const int RefreshedMetadata = 100;

            /// <summary>
            /// Gets the current progress given the previous step, next step, and progress in between.
            /// </summary>
            /// <param name="previousProgressStep">The previous progress step.</param>
            /// <param name="nextProgressStep">The next progress step.</param>
            /// <param name="currentProgress">The current progress step.</param>
            /// <returns>The progress.</returns>
            public static double GetProgress(int previousProgressStep, int nextProgressStep, double currentProgress)
            {
                return previousProgressStep + ((nextProgressStep - previousProgressStep) * (currentProgress / 100));
            }
        }
    }
}
