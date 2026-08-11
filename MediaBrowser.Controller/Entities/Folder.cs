#nullable disable

#pragma warning disable CA1002, CA1721, CA1819, CS1591

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using J2N.Collections.Generic.Extensions;
using Jellyfin.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LibraryTaskScheduler;
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
        private IEnumerable<BaseItem> _children;
        private LinkedChild[] _linkedChildren = [];

        public static IUserViewManager UserViewManager { get; set; }

        public static ILimitedConcurrencyLibraryScheduler LimitedConcurrencyLibraryScheduler { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is root.
        /// </summary>
        /// <value><c>true</c> if this instance is root; otherwise, <c>false</c>.</value>
        public bool IsRoot { get; set; }

        /// <summary>
        /// Gets or sets the linked children.
        /// </summary>
        [JsonIgnore]
        public LinkedChild[] LinkedChildren
        {
            get => _linkedChildren;
            set
            {
                _linkedChildren = value;

                // Assigning the collection means the caller knows the complete set of links.
                LinkedChildrenLoaded = true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether <see cref="LinkedChildren"/> holds the stored set of links.
        /// </summary>
        /// <remarks>
        /// An unloaded instance carries an empty array that means "unknown", not "no children" —
        /// persisting it would delete every link the item has.
        /// </remarks>
        [JsonIgnore]
        public bool LinkedChildrenLoaded { get; private set; }

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
        /// Gets or Sets the actual children.
        /// </summary>
        /// <value>The actual children.</value>
        [JsonIgnore]
        public virtual IEnumerable<BaseItem> Children
        {
            get => _children ??= LoadChildren();
            set => _children = value;
        }

        /// <summary>
        /// Gets thread-safe access to all recursive children of this folder - without regard to user.
        /// </summary>
        /// <value>The recursive children.</value>
        [JsonIgnore]
        public IEnumerable<BaseItem> RecursiveChildren => GetRecursiveChildren();

        [JsonIgnore]
        protected virtual bool SupportsShortcutChildren => false;

        protected virtual bool FilterLinkedChildrenPerUser => false;

        /// <summary>
        /// Gets a value indicating whether this folder's own directory mtime can be trusted to decide
        /// whether its children need to be re-listed from disk on the next scan. Folders backed by more
        /// than one physical location can't be represented by a single directory's mtime and already have
        /// their own change-detection logic in <see cref="RequiresRefresh"/>.
        /// </summary>
        [JsonIgnore]
        protected virtual bool SupportsDirectoryMtimePruning => true;

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
        protected virtual IReadOnlyList<BaseItem> LoadChildren()
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
            Children = null; // invalidate cached children.
            return ValidateChildrenInternal(progress, recursive, true, allowRemoveRoot, metadataRefreshOptions, metadataRefreshOptions.DirectoryService, cancellationToken);
        }

        private Dictionary<Guid, BaseItem> GetActualChildrenDictionary()
        {
            var dictionary = new Dictionary<Guid, BaseItem>();

            Children = null; // invalidate cached children.
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
                if (GetParents().Any(f => f.Id.Equals(Id)))
                {
                    throw new InvalidOperationException("Recursive datastructure detected abort processing this item.");
                }

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
            var accessibleChildren = new List<BaseItem>();
            var validChildrenNeedGeneration = false;

            if (IsFileProtocol)
            {
                if (CanSkipDiskValidation(refreshOptions, directoryService))
                {
                    Logger.LogDebug("Directory contents unchanged since last scan, skipping child validation: {Path}", Path);

                    var cachedChildren = GetActualChildrenDictionary();
                    validChildren.AddRange(cachedChildren.Values);
                    accessibleChildren.AddRange(cachedChildren.Values);
                }
                else if (!await ValidateChildrenFromDisk(progress, recursive, allowRemoveRoot, directoryService, validChildren, accessibleChildren, cancellationToken).ConfigureAwait(false))
                {
                    return;
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

                await ValidateSubFolders(accessibleChildren.OfType<Folder>().ToList(), directoryService, innerProgress, cancellationToken).ConfigureAwait(false);
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
                        Children = null; // invalidate cached children.
                        validChildren = Children.ToList();
                    }

                    await RefreshMetadataRecursive(accessibleChildren, refreshOptions, recursive, innerProgress, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Determines whether this folder's own directory contents can be assumed unchanged since the
        /// last time they were validated against disk, letting the (comparatively expensive) directory
        /// listing and database diff in <see cref="ValidateChildrenFromDisk"/> be skipped for this pass.
        /// </summary>
        /// <remarks>
        /// This relies on the folder's own directory mtime, which the OS updates whenever an entry is
        /// added, removed, or renamed directly within it. It does not, by itself, tell us whether a
        /// deeper descendant changed - callers still need to recurse into known subfolders so each one
        /// can make the same check against its own directory.
        /// </remarks>
        private bool CanSkipDiskValidation(MetadataRefreshOptions refreshOptions, IDirectoryService directoryService)
        {
            // DirectoryMTime is only ever written by StampDirectoryModifiedTime below, right after a
            // successful full validation - so MinValue unambiguously means "children never listed yet"
            // (e.g. an item created earlier in this very scan pass). It is deliberately not compared
            // against DateModified, which unrelated metadata-refresh code can update independently.
            if (DirectoryMTime == DateTime.MinValue || string.IsNullOrEmpty(Path))
            {
                return false;
            }

            if (!SupportsDirectoryMtimePruning)
            {
                return false;
            }

            // Explicit force-rescan requests must always perform a real, full validation.
            if (refreshOptions is not null
                && (refreshOptions.ReplaceAllMetadata || (refreshOptions.RefreshPaths?.Length ?? 0) > 0))
            {
                return false;
            }

            if (!LibraryManager.GetLibraryOptions(this).EnableDirectoryMtimePruning)
            {
                return false;
            }

            var info = directoryService.GetFileSystemEntry(Path);

            return info is not null && info.Exists && !this.HasDirectoryMTimeChanged(info.LastWriteTimeUtc);
        }

        /// <summary>
        /// Lists this folder's children directly from disk and reconciles them against the database,
        /// creating new items, removing items that are no longer present, and updating anything whose
        /// resolved metadata changed. Newly resolved and still-valid items are appended to
        /// <paramref name="validChildren"/> and <paramref name="accessibleChildren"/> as they're found.
        /// </summary>
        /// <returns><see langword="false"/> if an unrecoverable error occurred and the caller should abort validating this folder entirely; otherwise <see langword="true"/>.</returns>
        private async Task<bool> ValidateChildrenFromDisk(IProgress<double> progress, bool recursive, bool allowRemoveRoot, IDirectoryService directoryService, List<BaseItem> validChildren, List<BaseItem> accessibleChildren, CancellationToken cancellationToken)
        {
            if (!TryGetNonCachedChildren(directoryService, out var nonCachedChildren))
            {
                return false;
            }

            progress.Report(ProgressHelpers.RetrievedChildren);

            if (recursive)
            {
                ProviderManager.OnRefreshProgress(this, ProgressHelpers.RetrievedChildren);
            }

            // Build a dictionary of the current children we have now by Id so we can compare quickly and easily
            var currentChildren = GetActualChildrenDictionary();
            var state = new ChildReconciliationState(validChildren, accessibleChildren, currentChildren, BuildChildrenByPathLookup(currentChildren));

            cancellationToken.ThrowIfCancellationRequested();

            await ReconcileDiskChildren(nonCachedChildren, directoryService, allowRemoveRoot, state, cancellationToken).ConfigureAwait(false);

            // That's all the new and changed ones - now see if any have been removed and need cleanup
            var itemsRemoved = currentChildren.Values.Except(validChildren).ToList();

            // Build a set of paths that are alternate versions of valid children
            // These items should not be deleted - they're managed by their primary video
            var alternateVersionPaths = GetAlternateVersionPaths(validChildren);

            // Collect replaced primaries for deferred deletion (after CreateItems)
            var replacedPrimaries = new List<(Video OldPrimary, Video NewPrimary)>();

            if (itemsRemoved.Count > 0)
            {
                RemoveMissingItems(itemsRemoved, state, alternateVersionPaths, replacedPrimaries);
            }

            if (state.NewItems.Count > 0)
            {
                LibraryManager.CreateItems(state.NewItems, this, cancellationToken);
            }

            // Process deferred replaced-primary deletions now that new primaries exist in DB/cache.
            // This avoids the premature promotion that would occur if DeleteItem ran before CreateItems.
            await ProcessDeferredPrimaryReplacements(replacedPrimaries, cancellationToken).ConfigureAwait(false);

            // Demote old primaries that are now alternate versions of newly created primaries.
            // This handles the case where a new file is added that becomes the new primary
            // (e.g. movie-2 added, movie-3 was primary → movie-3 needs demotion).
            // Items in replacedPrimaries are excluded (already in actuallyRemoved).
            var oldPrimariesToDemote = GetPrimariesToDemote(itemsRemoved.Except(state.ActuallyRemoved), state.NewItems, alternateVersionPaths);
            await DemoteReplacedPrimaries(oldPrimariesToDemote, cancellationToken).ConfigureAwait(false);

            // After removing items, reattach any detached user data to remaining children
            // that share the same user data keys (eg. same episode replaced with a new file).
            await ReattachDetachedUserData(validChildren, state.ActuallyRemoved, cancellationToken).ConfigureAwait(false);

            // Record this folder's current directory mtime so a future scan can tell whether its
            // contents need to be re-listed from disk at all (see CanSkipDiskValidation). This is
            // tracked independently of the generic metadata-refresh pass, which does not reliably
            // keep a folder's own DateModified in sync with its directory's mtime.
            await StampDirectoryModifiedTime(directoryService, cancellationToken).ConfigureAwait(false);

            return true;
        }

        private bool TryGetNonCachedChildren(IDirectoryService directoryService, out IEnumerable<BaseItem> nonCachedChildren)
        {
            nonCachedChildren = [];

            try
            {
                nonCachedChildren = GetNonCachedChildren(directoryService);
                return true;
            }
            catch (IOException ex)
            {
                Logger.LogError(ex, "Error retrieving children from file system");
                return true;
            }
            catch (SecurityException ex)
            {
                Logger.LogError(ex, "Error retrieving children from file system");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error retrieving children");
                return false;
            }
        }

        private static Dictionary<string, BaseItem> BuildChildrenByPathLookup(Dictionary<Guid, BaseItem> currentChildren)
        {
            // Build a reverse path→item lookup for detecting type changes
            var currentChildrenByPath = new Dictionary<string, BaseItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in currentChildren)
            {
                if (!string.IsNullOrEmpty(kvp.Value.Path))
                {
                    currentChildrenByPath.TryAdd(kvp.Value.Path, kvp.Value);
                }
            }

            return currentChildrenByPath;
        }

        private static HashSet<string> GetAlternateVersionPaths(List<BaseItem> validChildren)
        {
            return validChildren
                .OfType<Video>()
                .SelectMany(v => v.LocalAlternateVersions ?? [])
                .Where(p => !string.IsNullOrEmpty(p))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private async Task ReconcileDiskChildren(
            IEnumerable<BaseItem> nonCachedChildren,
            IDirectoryService directoryService,
            bool allowRemoveRoot,
            ChildReconciliationState state,
            CancellationToken cancellationToken)
        {
            foreach (var child in nonCachedChildren)
            {
                if (!IsLibraryFolderAccessible(directoryService, child, allowRemoveRoot))
                {
                    // Preserve inaccessible items so they aren't treated as removed.
                    if (state.CurrentChildren.TryGetValue(child.Id, out var childrenToKeep))
                    {
                        state.ValidChildren.Add(childrenToKeep);
                    }

                    continue;
                }

                if (state.CurrentChildren.TryGetValue(child.Id, out BaseItem currentChild))
                {
                    state.ValidChildren.Add(currentChild);
                    state.AccessibleChildren.Add(currentChild);

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

                RemoveStaleItemAtSamePath(child, state);

                // Brand new item - needs to be added
                child.SetParent(this);
                state.NewItems.Add(child);
                state.ValidChildren.Add(child);
                state.AccessibleChildren.Add(child);
            }
        }

        private void RemoveStaleItemAtSamePath(BaseItem child, ChildReconciliationState state)
        {
            // Check if an existing item occupies the same path with different type/ID
            if (string.IsNullOrEmpty(child.Path)
                || !state.CurrentChildrenByPath.TryGetValue(child.Path, out var staleItem)
                || staleItem.Id.Equals(child.Id))
            {
                return;
            }

            Logger.LogInformation(
                "Item type changed at {Path}: {OldType} -> {NewType}, removing stale entry",
                child.Path,
                staleItem.GetType().Name,
                child.GetType().Name);

            state.CurrentChildren.Remove(staleItem.Id);
            state.CurrentChildrenByPath.Remove(child.Path);
            staleItem.SetParent(null);
            LibraryManager.DeleteItem(staleItem, new DeleteOptions { DeleteFileLocation = false }, this, false);
            state.ActuallyRemoved.Add(staleItem);
        }

        private void RemoveMissingItems(
            List<BaseItem> itemsRemoved,
            ChildReconciliationState state,
            HashSet<string> alternateVersionPaths,
            List<(Video OldPrimary, Video NewPrimary)> replacedPrimaries)
        {
            foreach (var item in itemsRemoved)
            {
                if (!item.CanDelete())
                {
                    Logger.LogDebug("Item marked as non-removable, skipping: {Path}", item.Path ?? item.Name);
                    continue;
                }

                // Skip items that are alternate versions of another video
                if (item is Video && !string.IsNullOrEmpty(item.Path) && alternateVersionPaths.Contains(item.Path))
                {
                    Logger.LogDebug("Item path matches an alternate version, skipping deletion: {Path}", item.Path);
                    continue;
                }

                if (TryDeferReplacedPrimaryDeletion(item, state.NewItems, alternateVersionPaths, state.ActuallyRemoved, replacedPrimaries))
                {
                    continue;
                }

                if (item.IsFileProtocol)
                {
                    Logger.LogDebug("Removed item: {Path}", item.Path);

                    state.ActuallyRemoved.Add(item);
                    item.SetParent(null);
                    LibraryManager.DeleteItem(item, new DeleteOptions { DeleteFileLocation = false }, this, false);
                }
            }
        }

        /// <summary>
        /// Defer deletion if this primary video is being replaced by a new primary that takes over its
        /// alternates. Deleting now would trigger premature promotion inside DeleteItem and write stale
        /// paths to collection NFOs.
        /// </summary>
        /// <returns><see langword="true"/> if deletion of <paramref name="item"/> was deferred.</returns>
        private static bool TryDeferReplacedPrimaryDeletion(
            BaseItem item,
            List<BaseItem> newItems,
            HashSet<string> alternateVersionPaths,
            List<BaseItem> actuallyRemoved,
            List<(Video OldPrimary, Video NewPrimary)> replacedPrimaries)
        {
            if (item is not Video primaryVideo
                || primaryVideo.PrimaryVersionId.HasValue
                || !primaryVideo.OwnerId.IsEmpty()
                || !(primaryVideo.LocalAlternateVersions ?? []).Any(p => alternateVersionPaths.Contains(p)))
            {
                return false;
            }

            var newPrimary = newItems
                .OfType<Video>()
                .FirstOrDefault(v => (v.LocalAlternateVersions ?? [])
                    .Any(p => (primaryVideo.LocalAlternateVersions ?? [])
                        .Any(op => string.Equals(op, p, StringComparison.OrdinalIgnoreCase))));
            if (newPrimary is null)
            {
                return false;
            }

            Logger.LogDebug("Deferring deletion of replaced primary: {Path}", item.Path);
            replacedPrimaries.Add((primaryVideo, newPrimary));
            actuallyRemoved.Add(item);
            item.SetParent(null);
            return true;
        }

        private async Task ProcessDeferredPrimaryReplacements(List<(Video OldPrimary, Video NewPrimary)> replacedPrimaries, CancellationToken cancellationToken)
        {
            foreach (var (oldPrimary, newPrimary) in replacedPrimaries)
            {
                Logger.LogInformation(
                    "Processing deferred deletion of replaced primary {OldName} ({OldId}), new primary {NewName} ({NewId})",
                    oldPrimary.Name,
                    oldPrimary.Id,
                    newPrimary.Name,
                    newPrimary.Id);

                // Reroute collection/playlist references from old primary to new primary
                await LibraryManager.RerouteLinkedChildReferencesAsync(oldPrimary.Id, newPrimary.Id).ConfigureAwait(false);

                // Transfer alternates from old primary to new primary
                var localAlternateIds = LibraryManager.GetLocalAlternateVersionIds(oldPrimary).ToHashSet();
                var allAlternateIds = localAlternateIds
                    .Concat(LibraryManager.GetLinkedAlternateVersions(oldPrimary).Select(v => v.Id))
                    .Distinct()
                    .ToList();

                foreach (var altId in allAlternateIds)
                {
                    if (LibraryManager.GetItemById(altId) is Video altVideo && !altVideo.Id.Equals(newPrimary.Id))
                    {
                        altVideo.SetPrimaryVersionId(newPrimary.Id);
                        altVideo.OwnerId = localAlternateIds.Contains(altVideo.Id) ? newPrimary.Id : Guid.Empty;
                        await altVideo.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
                    }
                }

                // Clear alternate arrays so DeleteItem won't trigger promotion
                oldPrimary.LocalAlternateVersions = [];
                oldPrimary.LinkedAlternateVersions = [];

                // Safe to delete now — no promotion will happen
                LibraryManager.DeleteItem(oldPrimary, new DeleteOptions { DeleteFileLocation = false }, this, false);
            }
        }

        private static List<(Video OldPrimary, Video NewPrimary)> GetPrimariesToDemote(
            IEnumerable<BaseItem> itemsStillRemoved,
            List<BaseItem> newItems,
            HashSet<string> alternateVersionPaths)
        {
            var oldPrimariesToDemote = new List<(Video OldPrimary, Video NewPrimary)>();
            foreach (var item in itemsStillRemoved)
            {
                if (item is not Video video
                    || !video.OwnerId.IsEmpty()
                    || string.IsNullOrEmpty(item.Path)
                    || !alternateVersionPaths.Contains(item.Path))
                {
                    continue;
                }

                var newPrimary = newItems
                    .OfType<Video>()
                    .FirstOrDefault(v => (v.LocalAlternateVersions ?? [])
                        .Any(p => string.Equals(p, item.Path, StringComparison.OrdinalIgnoreCase)));
                if (newPrimary is not null)
                {
                    oldPrimariesToDemote.Add((video, newPrimary));
                }
            }

            return oldPrimariesToDemote;
        }

        private static async Task DemoteReplacedPrimaries(List<(Video OldPrimary, Video NewPrimary)> oldPrimariesToDemote, CancellationToken cancellationToken)
        {
            foreach (var (oldPrimary, newPrimary) in oldPrimariesToDemote)
            {
                Logger.LogInformation(
                    "Demoting old primary {OldName} ({OldId}) to alternate of new primary {NewName} ({NewId})",
                    oldPrimary.Name,
                    oldPrimary.Id,
                    newPrimary.Name,
                    newPrimary.Id);

                // First: update old primary's alternate items to point to new primary.
                // Order matters — update alternates FIRST so they don't get orphan-deleted
                // when old primary's arrays are cleared.
                var oldAlternateIds = LibraryManager.GetLocalAlternateVersionIds(oldPrimary)
                    .Concat(LibraryManager.GetLinkedAlternateVersions(oldPrimary).Select(v => v.Id))
                    .Distinct()
                    .ToList();

                foreach (var altId in oldAlternateIds)
                {
                    if (LibraryManager.GetItemById(altId) is Video altVideo && !altVideo.Id.Equals(newPrimary.Id))
                    {
                        altVideo.SetPrimaryVersionId(newPrimary.Id);
                        altVideo.OwnerId = newPrimary.Id;
                        await altVideo.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
                    }
                }

                // Then: demote old primary — clear its arrays and set it as alternate of new primary
                oldPrimary.LocalAlternateVersions = [];
                oldPrimary.LinkedAlternateVersions = [];
                oldPrimary.SetPrimaryVersionId(newPrimary.Id);
                oldPrimary.OwnerId = newPrimary.Id;
                await oldPrimary.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);

                // Re-route playlist/collection references from old primary to new primary
                await LibraryManager.RerouteLinkedChildReferencesAsync(oldPrimary.Id, newPrimary.Id).ConfigureAwait(false);
            }
        }

        private static async Task ReattachDetachedUserData(List<BaseItem> validChildren, List<BaseItem> actuallyRemoved, CancellationToken cancellationToken)
        {
            if (actuallyRemoved.Count == 0)
            {
                return;
            }

            var removedKeys = actuallyRemoved.SelectMany(i => i.GetUserDataKeys()).ToHashSet();
            foreach (var child in validChildren)
            {
                if (child.GetUserDataKeys().Any(removedKeys.Contains))
                {
                    await child.ReattachUserDataAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task StampDirectoryModifiedTime(IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(Path))
            {
                return;
            }

            var selfInfo = directoryService.GetFileSystemEntry(Path);
            if (selfInfo is null || !selfInfo.Exists || !this.HasDirectoryMTimeChanged(selfInfo.LastWriteTimeUtc))
            {
                return;
            }

            DirectoryMTime = selfInfo.LastWriteTimeUtc;
            await UpdateToRepositoryAsync(ItemUpdateType.MetadataImport, cancellationToken).ConfigureAwait(false);
        }

        private async Task RefreshMetadataRecursive(IList<BaseItem> children, MetadataRefreshOptions refreshOptions, bool recursive, IProgress<double> progress, CancellationToken cancellationToken)
        {
            await RunTasks(
                (baseItem, innerProgress) => RefreshChildMetadata(baseItem, refreshOptions, recursive && baseItem.IsFolder, innerProgress, cancellationToken),
                children,
                progress,
                cancellationToken).ConfigureAwait(false);
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
                    folder.Children = null; // invalidate cached children.
                    await folder.RefreshMetadataRecursive(folder.Children.Except([this, child]).ToList(), refreshOptions, true, progress, cancellationToken).ConfigureAwait(false);
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
            await RunTasks(
                (folder, innerProgress) => folder.ValidateChildrenInternal(innerProgress, true, false, false, null, directoryService, cancellationToken),
                children,
                progress,
                cancellationToken).ConfigureAwait(false);
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
            await LimitedConcurrencyLibraryScheduler
                .Enqueue(
                    children.ToArray(),
                    task,
                    progress,
                    cancellationToken)
                .ConfigureAwait(false);
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
        protected IReadOnlyList<BaseItem> GetCachedChildren()
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
            if (!query.ForceDirect && CollapseBoxSetItems(query, this, query.User, ConfigurationManager))
            {
                query.CollapseBoxSetItems = true;
                SetCollapseBoxSetItemTypes(query);
            }

            if (this is not UserRootFolder
                && this is not AggregateFolder
                && query.ParentId.IsEmpty())
            {
                query.Parent = this;
            }

            // BoxSets and Playlists can have per-user visibility (shares/open access) that is stored in the
            // serialized item data and cannot be evaluated by the database query, so filter them in memory.
            if (query.IncludeItemTypes.Length > 0
                && query.IncludeItemTypes.All(t => t == BaseItemKind.BoxSet || t == BaseItemKind.Playlist))
            {
                return QueryWithPostFiltering(query);
            }

            return LibraryManager.GetItemsResult(query);
        }

        protected QueryResult<BaseItem> QueryWithPostFiltering(InternalItemsQuery query)
        {
            var startIndex = query.StartIndex;
            var limit = query.Limit;

            query.StartIndex = null;
            query.Limit = null;

            IEnumerable<BaseItem> itemsList = LibraryManager.GetItemList(query);
            var user = query.User;

            if (user is not null)
            {
                // needed for boxsets and playlists
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

            IEnumerable<BaseItem> items;

            int totalItemCount = 0;
            if (query.User is null)
            {
                items = UserViewBuilder.Filter(Children, user, query, UserDataManager, LibraryManager);
                totalItemCount = items.Count();
            }
            else
            {
                // need to pass this param to the children.
                // Note: Don't pass Limit/StartIndex here as pagination should happen after sorting in PostFilterAndSort
                var childQuery = new InternalItemsQuery
                {
                    DisplayAlbumFolders = query.DisplayAlbumFolders,
                    NameStartsWith = query.NameStartsWith,
                    NameStartsWithOrGreater = query.NameStartsWithOrGreater,
                    NameLessThan = query.NameLessThan
                };

                items = UserViewBuilder.Filter(
                    GetChildren(user, true, out totalItemCount, childQuery),
                    user,
                    query,
                    UserDataManager,
                    LibraryManager);
            }

            return PostFilterAndSort(items, query);
        }

        protected QueryResult<BaseItem> PostFilterAndSort(IEnumerable<BaseItem> items, InternalItemsQuery query)
        {
            var user = query.User;

            // Check recursive - don't substitute in plain folder views
            if (user is not null)
            {
                items = CollapseBoxSetItemsIfNeeded(items, query, this, user, ConfigurationManager, CollectionManager);

                // After collapse, BoxSets may have replaced items whose names matched the filter
                // but the BoxSet's own name may not match. Re-apply name filtering so BoxSets
                // appear under the correct letter (e.g. "Jump Street" under J, not under #).
                items = ApplyNameFilter(items, query);
            }

            return UserViewBuilder.SortAndPage(items, null, query, LibraryManager);
        }

        private static IEnumerable<BaseItem> ApplyNameFilter(IEnumerable<BaseItem> items, InternalItemsQuery query)
        {
            if (!string.IsNullOrWhiteSpace(query.NameStartsWith))
            {
                items = items.Where(i => i.SortName.StartsWith(query.NameStartsWith, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(query.NameStartsWithOrGreater))
            {
                items = items.Where(i => string.Compare(i.SortName, query.NameStartsWithOrGreater, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (!string.IsNullOrWhiteSpace(query.NameLessThan))
            {
                items = items.Where(i => string.Compare(i.SortName, query.NameLessThan, StringComparison.OrdinalIgnoreCase) < 0);
            }

            return items;
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

            if (!CollapseBoxSetItems(query, queryParent, user, configurationManager))
            {
                return items;
            }

            var config = configurationManager.Configuration;

            bool collapseMovies = config.EnableGroupingMoviesIntoCollections;
            bool collapseSeries = config.EnableGroupingShowsIntoCollections;

            if (user is null || (collapseMovies && collapseSeries))
            {
                return collectionManager.CollapseItemsWithinBoxSets(items, user);
            }

            if (!collapseMovies && !collapseSeries)
            {
                return items;
            }

            var collapsibleItems = new List<BaseItem>();
            var remainingItems = new List<BaseItem>();

            foreach (var item in items)
            {
                if ((collapseMovies && item is Movie) || (collapseSeries && item is Series))
                {
                    collapsibleItems.Add(item);
                }
                else
                {
                    remainingItems.Add(item);
                }
            }

            if (collapsibleItems.Count == 0)
            {
                return remainingItems;
            }

            var collapsedItems = collectionManager.CollapseItemsWithinBoxSets(collapsibleItems, user);

            return collapsedItems.Concat(remainingItems);
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
            if (param.HasValue)
            {
                return param.Value && AllowBoxSetCollapsing(query);
            }

            var config = configurationManager.Configuration;

            bool queryHasMovies = query.IncludeItemTypes.Length == 0 || query.IncludeItemTypes.Contains(BaseItemKind.Movie);
            bool queryHasSeries = query.IncludeItemTypes.Length == 0 || query.IncludeItemTypes.Contains(BaseItemKind.Series);

            bool collapseMovies = config.EnableGroupingMoviesIntoCollections;
            bool collapseSeries = config.EnableGroupingShowsIntoCollections;

            if (user is not null)
            {
                bool canCollapse = (queryHasMovies && collapseMovies) || (queryHasSeries && collapseSeries);
                return canCollapse && AllowBoxSetCollapsing(query);
            }

            return (queryHasMovies || queryHasSeries) && AllowBoxSetCollapsing(query);
        }

        private void SetCollapseBoxSetItemTypes(InternalItemsQuery query)
        {
            var config = ConfigurationManager.Configuration;
            bool collapseMovies = config.EnableGroupingMoviesIntoCollections;
            bool collapseSeries = config.EnableGroupingShowsIntoCollections;

            if (collapseMovies && collapseSeries)
            {
                // Empty means collapse all types
                query.CollapseBoxSetItemTypes = [];
                return;
            }

            var types = new List<BaseItemKind>();
            if (collapseMovies)
            {
                types.Add(BaseItemKind.Movie);
            }

            if (collapseSeries)
            {
                types.Add(BaseItemKind.Series);
            }

            query.CollapseBoxSetItemTypes = types.ToArray();
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

            if (request.MinIndexNumber.HasValue)
            {
                return false;
            }

            if (request.OrderBy.Any(o =>
                o.OrderBy == ItemSortBy.CommunityRating ||
                o.OrderBy == ItemSortBy.CriticRating ||
                o.OrderBy == ItemSortBy.Runtime))
            {
                return false;
            }

            return true;
        }

        public virtual IReadOnlyList<BaseItem> GetChildren(User user, bool includeLinkedChildren, out int totalItemCount, InternalItemsQuery query = null)
        {
            ArgumentNullException.ThrowIfNull(user);
            query ??= new InternalItemsQuery();
            query.User = user;

            // the true root should return our users root folder children
            if (IsPhysicalRoot)
            {
                return LibraryManager.GetUserRootFolder().GetChildren(user, includeLinkedChildren, out totalItemCount);
            }

            var result = new Dictionary<Guid, BaseItem>();

            totalItemCount = AddChildren(user, includeLinkedChildren, result, false, query);

            return result.Values.ToArray();
        }

        public virtual IReadOnlyList<BaseItem> GetChildren(User user, bool includeLinkedChildren, InternalItemsQuery query = null)
        {
            return GetChildren(user, includeLinkedChildren, out _, query);
        }

        protected virtual IEnumerable<BaseItem> GetEligibleChildrenForRecursiveChildren(User user)
        {
            return Children;
        }

        /// <summary>
        /// Adds the children to list.
        /// </summary>
        private int AddChildren(User user, bool includeLinkedChildren, Dictionary<Guid, BaseItem> result, bool recursive, InternalItemsQuery query, HashSet<Folder> visitedFolders = null)
        {
            // Prevent infinite recursion of nested folders
            visitedFolders ??= new HashSet<Folder>();
            if (!visitedFolders.Add(this))
            {
                return 0;
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

            if (includeLinkedChildren)
            {
                children = children.Concat(GetLinkedChildren(user)).ToArray();
            }

            return AddChildrenFromCollection(children, user, includeLinkedChildren, result, recursive, query, visitedFolders);
        }

        private int AddChildrenFromCollection(IEnumerable<BaseItem> children, User user, bool includeLinkedChildren, Dictionary<Guid, BaseItem> result, bool recursive, InternalItemsQuery query, HashSet<Folder> visitedFolders)
        {
            query ??= new InternalItemsQuery();
            var limit = query.Limit > 0 ? query.Limit : int.MaxValue;
            query.Limit = 0;

            var visibleChildren = children
                .Where(e => e.IsVisible(user))
                .ToArray();

            var realChildren = UserViewBuilder.Filter(visibleChildren, query.User, query, UserDataManager, LibraryManager)
                .ToArray();

            var childCount = realChildren.Length;
            if (result.Count < limit)
            {
                var remainingCount = (int)(limit - result.Count);
                foreach (var child in realChildren
                    .Skip(query.StartIndex ?? 0)
                    .Take(remainingCount))
                {
                    result[child.Id] = child;
                }
            }

            if (recursive)
            {
                foreach (var child in visibleChildren
                    .Where(e => e.IsFolder)
                    .OfType<Folder>())
                {
                    childCount += child.AddChildren(user, includeLinkedChildren, result, true, query, visitedFolders);
                }
            }

            return childCount;
        }

        public virtual IReadOnlyList<BaseItem> GetRecursiveChildren(User user, InternalItemsQuery query, out int totalCount)
        {
            ArgumentNullException.ThrowIfNull(user);

            var result = new Dictionary<Guid, BaseItem>();

            totalCount = AddChildren(user, true, result, true, query);

            return result.Values.ToArray();
        }

        /// <summary>
        /// Gets the recursive children.
        /// </summary>
        /// <returns>IList{BaseItem}.</returns>
        public IReadOnlyList<BaseItem> GetRecursiveChildren()
        {
            return GetRecursiveChildren(true);
        }

        public IReadOnlyList<BaseItem> GetRecursiveChildren(bool includeLinkedChildren)
        {
            return GetRecursiveChildren(i => true, includeLinkedChildren);
        }

        public IReadOnlyList<BaseItem> GetRecursiveChildren(Func<BaseItem, bool> filter)
        {
            return GetRecursiveChildren(filter, true);
        }

        public IReadOnlyList<BaseItem> GetRecursiveChildren(Func<BaseItem, bool> filter, bool includeLinkedChildren)
        {
            var result = new Dictionary<Guid, BaseItem>();

            AddChildrenToList(result, includeLinkedChildren, true, filter);

            return result.Values.ToArray();
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
            var resolved = ResolveLinkedChildren(LinkedChildren);
            var list = new List<BaseItem>(resolved.Count);
            foreach (var (_, item) in resolved)
            {
                list.Add(item);
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
        public IReadOnlyList<Tuple<LinkedChild, BaseItem>> GetLinkedChildrenInfos()
        {
            return ResolveLinkedChildren(LinkedChildren)
                .Select(t => new Tuple<LinkedChild, BaseItem>(t.Info, t.Item))
                .ToArray();
        }

        /// <summary>
        /// Resolves a list of <see cref="LinkedChild"/> entries to their <see cref="BaseItem"/> targets,
        /// batching the database lookup across all entries with a known ItemId.
        /// Entries without a usable ItemId fall back to the per-entry <see cref="BaseItem.GetLinkedChild"/>
        /// path (legacy path-based resolution).
        /// </summary>
        /// <param name="linkedChildren">Linked children to resolve.</param>
        /// <returns>Each input entry paired with its resolved item; entries that fail to resolve are dropped.</returns>
        private List<(LinkedChild Info, BaseItem Item)> ResolveLinkedChildren(IReadOnlyList<LinkedChild> linkedChildren)
        {
            var resolved = new List<(LinkedChild Info, BaseItem Item)>(linkedChildren.Count);
            if (linkedChildren.Count == 0)
            {
                return resolved;
            }

            var idsToBatch = new HashSet<Guid>();
            foreach (var info in linkedChildren)
            {
                if (info.ItemId.HasValue && !info.ItemId.Value.IsEmpty())
                {
                    idsToBatch.Add(info.ItemId.Value);
                }
            }

            Dictionary<Guid, BaseItem> byId = null;
            if (idsToBatch.Count > 0)
            {
                var batched = LibraryManager.GetItemList(new InternalItemsQuery
                {
                    ItemIds = [.. idsToBatch]
                });
                byId = new Dictionary<Guid, BaseItem>(batched.Count);
                foreach (var item in batched)
                {
                    byId[item.Id] = item;
                }
            }

            foreach (var info in linkedChildren)
            {
                BaseItem item = null;
                if (byId is not null && info.ItemId.HasValue && byId.TryGetValue(info.ItemId.Value, out var batchedItem))
                {
                    item = batchedItem;
                }
                else
                {
                    // ItemId is missing/empty or the batched query couldn't return the item
                    // (e.g. it has been removed). Fall back to per-entry resolution, which also
                    // handles legacy path-based linked children.
                    item = GetLinkedChild(info);
                }

                if (item is not null)
                {
                    resolved.Add((info, item));
                }
            }

            return resolved;
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
#pragma warning disable CS0618 // Type or member is obsolete - shortcuts require Path for lazy ItemId resolution
                                return new LinkedChild
                                {
                                    Path = resolvedPath,
                                    Type = LinkedChildType.Shortcut
                                };
#pragma warning restore CS0618
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

        public override bool IsPlayed(User user, UserItemData userItemData)
        {
            return ItemRepository.GetIsPlayed(user, Id, true);
        }

        public override bool IsUnplayed(User user, UserItemData userItemData)
        {
            return !IsPlayed(user, userItemData);
        }

        public override void FillUserDataDtoValues(
            UserItemDataDto dto,
            UserItemData userData,
            BaseItemDto itemDto,
            User user,
            DtoOptions fields,
            (int Played, int Total)? precomputedCounts = null)
        {
            if (!SupportsUserDataFromChildren)
            {
                return;
            }

            if (SupportsPlayedStatus || (itemDto is not null && fields.ContainsField(ItemFields.RecursiveItemCount)))
            {
                int playedCount;
                int totalCount;

                if (precomputedCounts.HasValue)
                {
                    // Use batch-fetched counts (avoids N+1 queries)
                    (playedCount, totalCount) = precomputedCounts.Value;
                }
                else
                {
                    // Fall back to per-item query when no batch data is available
                    var query = new InternalItemsQuery(user);

                    if (LinkedChildren.Length > 0)
                    {
                        (playedCount, totalCount) = ItemCountService.GetPlayedAndTotalCountFromLinkedChildren(query, Id);
                    }
                    else
                    {
                        (playedCount, totalCount) = ItemCountService.GetPlayedAndTotalCount(query, Id);
                    }
                }

                if (itemDto is not null && fields.ContainsField(ItemFields.RecursiveItemCount))
                {
                    itemDto.RecursiveItemCount = totalCount;
                }

                if (SupportsPlayedStatus)
                {
                    var unplayedCount = totalCount - playedCount;
                    dto.UnplayedItemCount = unplayedCount;

                    if (totalCount > 0)
                    {
                        dto.PlayedPercentage = playedCount / (double)totalCount * 100;
                        dto.Played = playedCount >= totalCount;
                    }
                    else
                    {
                        dto.Played = true;
                    }
                }
            }
        }

        /// <summary>
        /// Bundles the mutable collections threaded through disk-child reconciliation so helper method
        /// signatures don't keep growing every time another step needs access to one of them.
        /// </summary>
        private sealed class ChildReconciliationState
        {
            public ChildReconciliationState(List<BaseItem> validChildren, List<BaseItem> accessibleChildren, Dictionary<Guid, BaseItem> currentChildren, Dictionary<string, BaseItem> currentChildrenByPath)
            {
                ValidChildren = validChildren;
                AccessibleChildren = accessibleChildren;
                CurrentChildren = currentChildren;
                CurrentChildrenByPath = currentChildrenByPath;
            }

            public List<BaseItem> ValidChildren { get; }

            public List<BaseItem> AccessibleChildren { get; }

            public Dictionary<Guid, BaseItem> CurrentChildren { get; }

            public Dictionary<string, BaseItem> CurrentChildrenByPath { get; }

            public List<BaseItem> NewItems { get; } = [];

            public List<BaseItem> ActuallyRemoved { get; } = [];
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
