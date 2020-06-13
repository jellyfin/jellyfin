#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Folder
    /// </summary>
    public class Folder : BaseItem
    {
        public static IUserViewManager UserViewManager { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is root.
        /// </summary>
        /// <value><c>true</c> if this instance is root; otherwise, <c>false</c>.</value>
        public bool IsRoot { get; set; }

        public LinkedChild[] LinkedChildren { get; set; }

        [JsonIgnore]
        public DateTime? DateLastMediaAdded { get; set; }

        public Folder()
        {
            LinkedChildren = Array.Empty<LinkedChild>();
        }

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

        protected override bool IsAllowTagFilterEnforced()
        {
            if (this is ICollectionFolder)
            {
                return false;
            }
            if (this is UserView)
            {
                return false;
            }
            return true;
        }

        [JsonIgnore]
        protected virtual bool SupportsShortcutChildren => false;

        /// <summary>
        /// Adds the child.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="InvalidOperationException">Unable to add  + item.Name</exception>
        public void AddChild(BaseItem item, CancellationToken cancellationToken)
        {
            item.SetParent(this);

            if (item.Id.Equals(Guid.Empty))
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

        /// <summary>
        /// Gets the actual children.
        /// </summary>
        /// <value>The actual children.</value>
        [JsonIgnore]
        public virtual IEnumerable<BaseItem> Children => LoadChildren();

        /// <summary>
        /// thread-safe access to all recursive children of this folder - without regard to user
        /// </summary>
        /// <value>The recursive children.</value>
        [JsonIgnore]
        public IEnumerable<BaseItem> RecursiveChildren => GetRecursiveChildren();

        public override bool IsVisible(User user)
        {
            if (this is ICollectionFolder && !(this is BasePluginFolder))
            {
                if (user.Policy.BlockedMediaFolders != null)
                {
                    if (user.Policy.BlockedMediaFolders.Contains(Id.ToString("N", CultureInfo.InvariantCulture), StringComparer.OrdinalIgnoreCase) ||

                        // Backwards compatibility
                        user.Policy.BlockedMediaFolders.Contains(Name, StringComparer.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!user.Policy.EnableAllFolders && !user.Policy.EnabledFolders.Contains(Id.ToString("N", CultureInfo.InvariantCulture), StringComparer.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return base.IsVisible(user);
        }

        /// <summary>
        /// Loads our children.  Validation will occur externally.
        /// We want this sychronous.
        /// </summary>
        protected virtual List<BaseItem> LoadChildren()
        {
            //logger.LogDebug("Loading children from {0} {1} {2}", GetType().Name, Id, Path);
            //just load our children from the repo - the library will be validated and maintained in other processes
            return GetCachedChildren();
        }

        public override double? GetRefreshProgress()
        {
            return ProviderManager.GetRefreshProgress(Id);
        }

        public Task ValidateChildren(IProgress<double> progress, CancellationToken cancellationToken)
        {
            return ValidateChildren(progress, cancellationToken, new MetadataRefreshOptions(new DirectoryService(FileSystem)));
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
            return ValidateChildrenInternal(progress, cancellationToken, recursive, true, metadataRefreshOptions, metadataRefreshOptions.DirectoryService);
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
                    Logger.LogError("Found folder containing items with duplicate id. Path: {path}, Child Name: {ChildName}",
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

        protected override void TriggerOnRefreshStart()
        {
        }

        protected override void TriggerOnRefreshComplete()
        {
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
        protected virtual async Task ValidateChildrenInternal(IProgress<double> progress, CancellationToken cancellationToken, bool recursive, bool refreshChildMetadata, MetadataRefreshOptions refreshOptions, IDirectoryService directoryService)
        {
            if (recursive)
            {
                ProviderManager.OnRefreshStart(this);
            }

            try
            {
                await ValidateChildrenInternal2(progress, cancellationToken, recursive, refreshChildMetadata, refreshOptions, directoryService).ConfigureAwait(false);
            }
            finally
            {
                if (recursive)
                {
                    ProviderManager.OnRefreshComplete(this);
                }
            }
        }

        private async Task ValidateChildrenInternal2(IProgress<double> progress, CancellationToken cancellationToken, bool recursive, bool refreshChildMetadata, MetadataRefreshOptions refreshOptions, IDirectoryService directoryService)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var validChildren = new List<BaseItem>();
            var validChildrenNeedGeneration = false;

            if (IsFileProtocol)
            {
                IEnumerable<BaseItem> nonCachedChildren;

                try
                {
                    nonCachedChildren = GetNonCachedChildren(directoryService);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error retrieving children folder");
                    return;
                }

                progress.Report(5);

                if (recursive)
                {
                    ProviderManager.OnRefreshProgress(this, 5);
                }

                // Build a dictionary of the current children we have now by Id so we can compare quickly and easily
                var currentChildren = GetActualChildrenDictionary();

                // Create a list for our validated children
                var newItems = new List<BaseItem>();

                cancellationToken.ThrowIfCancellationRequested();

                foreach (var child in nonCachedChildren)
                {
                    if (currentChildren.TryGetValue(child.Id, out BaseItem currentChild))
                    {
                        validChildren.Add(currentChild);

                        if (currentChild.UpdateFromResolvedItem(child) > ItemUpdateType.None)
                        {
                            currentChild.UpdateToRepository(ItemUpdateType.MetadataImport, cancellationToken);
                        }
                        else
                        {
                            // metadata is up-to-date; make sure DB has correct images dimensions and hash
                            LibraryManager.UpdateImages(currentChild);
                        }

                        continue;
                    }

                    // Brand new item - needs to be added
                    child.SetParent(this);
                    newItems.Add(child);
                    validChildren.Add(child);
                }

                // If any items were added or removed....
                if (newItems.Count > 0 || currentChildren.Count != validChildren.Count)
                {
                    // That's all the new and changed ones - now see if there are any that are missing
                    var itemsRemoved = currentChildren.Values.Except(validChildren).ToList();

                    foreach (var item in itemsRemoved)
                    {
                        if (item.IsFileProtocol)
                        {
                            Logger.LogDebug("Removed item: " + item.Path);

                            item.SetParent(null);
                            LibraryManager.DeleteItem(item, new DeleteOptions { DeleteFileLocation = false }, this, false);
                        }
                    }

                    LibraryManager.CreateItems(newItems, this, cancellationToken);
                }
            }
            else
            {
                validChildrenNeedGeneration = true;
            }

            progress.Report(10);

            if (recursive)
            {
                ProviderManager.OnRefreshProgress(this, 10);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (recursive)
            {
                var innerProgress = new ActionableProgress<double>();

                var folder = this;
                innerProgress.RegisterAction(p =>
                {
                    double newPct = 0.80 * p + 10;
                    progress.Report(newPct);
                    ProviderManager.OnRefreshProgress(folder, newPct);
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
                progress.Report(90);

                if (recursive)
                {
                    ProviderManager.OnRefreshProgress(this, 90);
                }

                var container = this as IMetadataContainer;

                var innerProgress = new ActionableProgress<double>();

                var folder = this;
                innerProgress.RegisterAction(p =>
                {
                    double newPct = 0.10 * p + 90;
                    progress.Report(newPct);
                    if (recursive)
                    {
                        ProviderManager.OnRefreshProgress(folder, newPct);
                    }
                });

                if (container != null)
                {
                    await RefreshAllMetadataForContainer(container, refreshOptions, innerProgress, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    if (validChildrenNeedGeneration)
                    {
                        validChildren = Children.ToList();
                    }

                    await RefreshMetadataRecursive(validChildren, refreshOptions, recursive, innerProgress, cancellationToken);
                }
            }
        }

        private async Task RefreshMetadataRecursive(List<BaseItem> children, MetadataRefreshOptions refreshOptions, bool recursive, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var numComplete = 0;
            var count = children.Count;
            double currentPercent = 0;

            foreach (var child in children)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var innerProgress = new ActionableProgress<double>();

                // Avoid implicitly captured closure
                var currentInnerPercent = currentPercent;

                innerProgress.RegisterAction(p =>
                {
                    double innerPercent = currentInnerPercent;
                    innerPercent += p / (count);
                    progress.Report(innerPercent);
                });

                await RefreshChildMetadata(child, refreshOptions, recursive && child.IsFolder, innerProgress, cancellationToken)
                    .ConfigureAwait(false);

                numComplete++;
                double percent = numComplete;
                percent /= count;
                percent *= 100;
                currentPercent = percent;

                progress.Report(percent);
            }
        }

        private async Task RefreshAllMetadataForContainer(IMetadataContainer container, MetadataRefreshOptions refreshOptions, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var series = container as Series;
            if (series != null)
            {
                await series.RefreshMetadata(refreshOptions, cancellationToken).ConfigureAwait(false);

            }
            await container.RefreshAllMetadata(refreshOptions, progress, cancellationToken).ConfigureAwait(false);
        }

        private async Task RefreshChildMetadata(BaseItem child, MetadataRefreshOptions refreshOptions, bool recursive, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var container = child as IMetadataContainer;

            if (container != null)
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
                    await folder.RefreshMetadataRecursive(folder.Children.ToList(), refreshOptions, true, progress, cancellationToken);
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
        private async Task ValidateSubFolders(IList<Folder> children, IDirectoryService directoryService, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var numComplete = 0;
            var count = children.Count;
            double currentPercent = 0;

            foreach (var child in children)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var innerProgress = new ActionableProgress<double>();

                // Avoid implicitly captured closure
                var currentInnerPercent = currentPercent;

                innerProgress.RegisterAction(p =>
                {
                    double innerPercent = currentInnerPercent;
                    innerPercent += p / (count);
                    progress.Report(innerPercent);
                });

                await child.ValidateChildrenInternal(innerProgress, cancellationToken, true, false, null, directoryService)
                        .ConfigureAwait(false);

                numComplete++;
                double percent = numComplete;
                percent /= count;
                percent *= 100;
                currentPercent = percent;

                progress.Report(percent);
            }
        }

        /// <summary>
        /// Get the children of this folder from the actual file system
        /// </summary>
        /// <returns>IEnumerable{BaseItem}.</returns>
        protected virtual IEnumerable<BaseItem> GetNonCachedChildren(IDirectoryService directoryService)
        {
            var collectionType = LibraryManager.GetContentType(this);
            var libraryOptions = LibraryManager.GetLibraryOptions(this);

            return LibraryManager.ResolvePaths(GetFileSystemChildren(directoryService), directoryService, this, libraryOptions, collectionType);
        }

        /// <summary>
        /// Get our children from the repo - stubbed for now
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
                if (!(this is ICollectionFolder))
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

                if (query.User == null)
                {
                    items = GetRecursiveChildren(filter);
                }
                else
                {
                    items = GetRecursiveChildren(user, query);
                }

                return PostFilterAndSort(items, query, true);
            }

            if (!(this is UserRootFolder) && !(this is AggregateFolder) && query.ParentId == Guid.Empty)
            {
                query.Parent = this;
            }

            if (RequiresPostFiltering2(query))
            {
                return QueryWithPostFiltering2(query);
            }

            return LibraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> QueryWithPostFiltering2(InternalItemsQuery query)
        {
            var startIndex = query.StartIndex;
            var limit = query.Limit;

            query.StartIndex = null;
            query.Limit = null;

            IEnumerable<BaseItem> itemsList = LibraryManager.GetItemList(query);
            var user = query.User;

            if (user != null)
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

            return new QueryResult<BaseItem>
            {
                TotalRecordCount = totalCount,
                Items = returnItems.ToArray()
            };
        }

        private bool RequiresPostFiltering2(InternalItemsQuery query)
        {
            if (query.IncludeItemTypes.Length == 1 && string.Equals(query.IncludeItemTypes[0], typeof(BoxSet).Name, StringComparison.OrdinalIgnoreCase))
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
                if (!(this is ICollectionFolder))
                {
                    Logger.LogDebug("Query requires post-filtering due to LinkedChildren. Type: " + GetType().Name);
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

            if (!string.IsNullOrEmpty(query.AdjacentTo))
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
                if (query.IncludeItemTypes.Length == 1 && query.IncludeItemTypes.Contains(typeof(Series).Name))
                {
                    Logger.LogDebug("Query requires post-filtering due to IsPlayed");
                    return true;
                }
            }

            return false;
        }

        private static BaseItem[] SortItemsByRequest(InternalItemsQuery query, IReadOnlyList<BaseItem> items)
        {
            var ids = query.ItemIds;
            int size = items.Count;

            // ids can potentially contain non-unique guids, but query result cannot,
            // so we include only first occurrence of each guid
            var positions = new Dictionary<Guid, int>(size);
            int index = 0;
            for (int i = 0; i < ids.Length; i++)
            {
                if (positions.TryAdd(ids[i], index))
                {
                    index++;
                }
            }

            var newItems = new BaseItem[size];
            for (int i = 0; i < size; i++)
            {
                var item = items[i];
                newItems[positions[item.Id]] = item;
            }

            return newItems;
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
                    query.ChannelIds = new Guid[] { ChannelId };

                    // Don't blow up here because it could cause parent screens with other content to fail
                    return ChannelManager.GetChannelItemsInternal(query, new SimpleProgress<double>(), CancellationToken.None).Result;
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

            if (query.User == null)
            {
                items = Children.Where(filter);
            }
            else
            {
                items = GetChildren(user, true).Where(filter);
            }

            return PostFilterAndSort(items, query, true);
        }

        public static ICollectionManager CollectionManager { get; set; }

        protected QueryResult<BaseItem> PostFilterAndSort(IEnumerable<BaseItem> items, InternalItemsQuery query, bool enableSorting)
        {
            var user = query.User;

            // Check recursive - don't substitute in plain folder views
            if (user != null)
            {
                items = CollapseBoxSetItemsIfNeeded(items, query, this, user, ConfigurationManager, CollectionManager);
            }

            if (!string.IsNullOrEmpty(query.NameStartsWithOrGreater))
            {
                items = items.Where(i => string.Compare(query.NameStartsWithOrGreater, i.SortName, StringComparison.CurrentCultureIgnoreCase) < 1);
            }
            if (!string.IsNullOrEmpty(query.NameStartsWith))
            {
                items = items.Where(i => i.SortName.StartsWith(query.NameStartsWith, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(query.NameLessThan))
            {
                items = items.Where(i => string.Compare(query.NameLessThan, i.SortName, StringComparison.CurrentCultureIgnoreCase) == 1);
            }

            // This must be the last filter
            if (!string.IsNullOrEmpty(query.AdjacentTo))
            {
                items = UserViewBuilder.FilterForAdjacency(items.ToList(), query.AdjacentTo);
            }

            return UserViewBuilder.SortAndPage(items, null, query, LibraryManager, enableSorting);
        }

        private static IEnumerable<BaseItem> CollapseBoxSetItemsIfNeeded(IEnumerable<BaseItem> items,
            InternalItemsQuery query,
            BaseItem queryParent,
            User user,
            IServerConfigurationManager configurationManager, ICollectionManager collectionManager)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (CollapseBoxSetItems(query, queryParent, user, configurationManager))
            {
                items = collectionManager.CollapseItemsWithinBoxSets(items, user);
            }

            return items;
        }

        private static bool CollapseBoxSetItems(InternalItemsQuery query,
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
                if (user != null && !configurationManager.Configuration.EnableGroupingIntoCollections)
                {
                    return false;
                }

                if (query.IncludeItemTypes.Length == 0 || query.IncludeItemTypes.Contains("Movie", StringComparer.OrdinalIgnoreCase))
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

            if (request.Genres.Length > 0)
            {
                return false;
            }

            if (request.GenreIds.Length > 0)
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

            if (request.GenreIds.Length > 0)
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
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return GetChildren(user, includeLinkedChildren, null);
        }

        public virtual List<BaseItem> GetChildren(User user, bool includeLinkedChildren, InternalItemsQuery query)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            //the true root should return our users root folder children
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
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private void AddChildren(User user, bool includeLinkedChildren, Dictionary<Guid, BaseItem> result, bool recursive, InternalItemsQuery query)
        {
            foreach (var child in GetEligibleChildrenForRecursiveChildren(user))
            {
                bool? isVisibleToUser = null;

                if (query == null || UserViewBuilder.FilterItem(child, query))
                {
                    isVisibleToUser = child.IsVisible(user);

                    if (isVisibleToUser.Value)
                    {
                        result[child.Id] = child;
                    }
                }

                if (isVisibleToUser ?? child.IsVisible(user))
                {
                    if (recursive && child.IsFolder)
                    {
                        var folder = (Folder)child;

                        folder.AddChildren(user, includeLinkedChildren, result, true, query);
                    }
                }
            }

            if (includeLinkedChildren)
            {
                foreach (var child in GetLinkedChildren(user))
                {
                    if (query == null || UserViewBuilder.FilterItem(child, query))
                    {
                        if (child.IsVisible(user))
                        {
                            result[child.Id] = child;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets allowed recursive children of an item
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="includeLinkedChildren">if set to <c>true</c> [include linked children].</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public IEnumerable<BaseItem> GetRecursiveChildren(User user, bool includeLinkedChildren = true)
        {
            return GetRecursiveChildren(user, null);
        }

        public virtual IEnumerable<BaseItem> GetRecursiveChildren(User user, InternalItemsQuery query)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

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
                if (filter == null || filter(child))
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
                    if (filter == null || filter(child))
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

                if (child != null)
                {
                    list.Add(child);
                }
            }
            return list;
        }

        protected virtual bool FilterLinkedChildrenPerUser => false;

        public bool ContainsLinkedChildByItemId(Guid itemId)
        {
            var linkedChildren = LinkedChildren;
            foreach (var i in linkedChildren)
            {
                if (i.ItemId.HasValue && i.ItemId.Value == itemId)
                {
                    return true;
                }

                var child = GetLinkedChild(i);

                if (child != null && child.Id == itemId)
                {
                    return true;
                }
            }
            return false;
        }

        public List<BaseItem> GetLinkedChildren(User user)
        {
            if (!FilterLinkedChildrenPerUser || user == null)
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

                if (child == null)
                {
                    continue;
                }

                var childOwner = child.GetOwner() ?? child;

                if (childOwner != null && !(child is IItemByName))
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
                .Where(i => i.Item2 != null);
        }

        [JsonIgnore]
        protected override bool SupportsOwnedItems => base.SupportsOwnedItems || SupportsShortcutChildren;

        protected override async Task<bool> RefreshedOwnedItems(MetadataRefreshOptions options, List<FileSystemMetadata> fileSystemChildren, CancellationToken cancellationToken)
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
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
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
                    .Where(i => i != null)
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
        /// <returns>Task.</returns>
        public override void MarkPlayed(User user,
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

            if (!user.Configuration.DisplayMissingEpisodes)
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
                    if (episode != null && episode.IsUnaired)
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
        /// <returns>Task.</returns>
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
                var iItemByName = this as IItemByName;
                if (iItemByName != null)
                {
                    var hasDualAccess = this as IHasDualAccess;
                    if (hasDualAccess == null || hasDualAccess.IsAccessedByName)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public override void FillUserDataDtoValues(UserItemDataDto dto, UserItemData userData, BaseItemDto itemDto, User user, DtoOptions fields)
        {
            if (!SupportsUserDataFromChildren)
            {
                return;
            }

            if (itemDto != null)
            {
                if (fields.ContainsField(ItemFields.RecursiveItemCount))
                {
                    itemDto.RecursiveItemCount = GetRecursiveChildCount(user);
                }
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
                });

                double unplayedCount = unplayedQueryResult.TotalRecordCount;

                dto.UnplayedItemCount = unplayedQueryResult.TotalRecordCount;

                if (itemDto != null && itemDto.RecursiveItemCount.HasValue)
                {
                    if (itemDto.RecursiveItemCount.Value > 0)
                    {
                        var unplayedPercentage = (unplayedCount / itemDto.RecursiveItemCount.Value) * 100;
                        dto.PlayedPercentage = 100 - unplayedPercentage;
                        dto.Played = dto.PlayedPercentage.Value >= 100;
                    }
                }
                else
                {
                    dto.Played = (dto.UnplayedItemCount ?? 0) == 0;
                }
            }
        }
    }
}
