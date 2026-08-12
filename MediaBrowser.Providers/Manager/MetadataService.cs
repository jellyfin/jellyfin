#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Manager
{
    public abstract class MetadataService<TItemType, TIdType> : IMetadataService
        where TItemType : BaseItem, IHasLookupInfo<TIdType>, new()
        where TIdType : ItemLookupInfo, new()
    {
        protected MetadataService(
            IServerConfigurationManager serverConfigurationManager,
            ILogger<MetadataService<TItemType, TIdType>> logger,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILibraryManager libraryManager,
            IExternalDataManager externalDataManager,
            IItemRepository itemRepository)
        {
            ServerConfigurationManager = serverConfigurationManager;
            Logger = logger;
            ProviderManager = providerManager;
            FileSystem = fileSystem;
            LibraryManager = libraryManager;
            ExternalDataManager = externalDataManager;
            ItemRepository = itemRepository;
            ImageProvider = new ItemImageProvider(Logger, ProviderManager, FileSystem);
        }

        protected ItemImageProvider ImageProvider { get; }

        protected IServerConfigurationManager ServerConfigurationManager { get; }

        protected ILogger<MetadataService<TItemType, TIdType>> Logger { get; }

        protected IProviderManager ProviderManager { get; }

        protected IFileSystem FileSystem { get; }

        protected ILibraryManager LibraryManager { get; }

        protected IExternalDataManager ExternalDataManager { get; }

        protected IItemRepository ItemRepository { get; }

        protected virtual bool EnableUpdatingPremiereDateFromChildren => false;

        protected virtual bool EnableUpdatingGenresFromChildren => false;

        protected virtual bool EnableUpdatingStudiosFromChildren => false;

        protected virtual bool EnableUpdatingOfficialRatingFromChildren => false;

        public virtual int Order => 0;

        private FileSystemMetadata TryGetFileSystemMetadata(string path, IDirectoryService directoryService)
        {
            try
            {
                return directoryService.GetFileSystemEntry(path);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error getting file {Path}", path);
                return null;
            }
        }

        public virtual async Task<ItemUpdateType> RefreshMetadata(BaseItem item, MetadataRefreshOptions refreshOptions, CancellationToken cancellationToken)
        {
            var itemOfType = (TItemType)item;
            var updateType = ItemUpdateType.None;

            var libraryOptions = LibraryManager.GetLibraryOptions(item);
            var isFirstRefresh = item.DateLastRefreshed == DateTime.MinValue;
            var hasRefreshedMetadata = true;
            var hasRefreshedImages = true;

            var requiresRefresh = libraryOptions.AutomaticRefreshIntervalDays > 0 && (DateTime.UtcNow - item.DateLastRefreshed).TotalDays >= libraryOptions.AutomaticRefreshIntervalDays;

            if (!requiresRefresh && refreshOptions.MetadataRefreshMode != MetadataRefreshMode.None)
            {
                // TODO: If this returns true, should we instead just change metadata refresh mode to Full?
                requiresRefresh = item.RequiresRefresh();

                if (requiresRefresh)
                {
                    Logger.LogDebug("Refreshing {Type} {Item} because item.RequiresRefresh() returned true", typeof(TItemType).Name, item.Path ?? item.Name);
                }
            }

            if (refreshOptions.RemoveOldMetadata && refreshOptions.ReplaceAllImages)
            {
                if (ImageProvider.RemoveImages(item))
                {
                    updateType |= ItemUpdateType.ImageUpdate;
                }
            }

            var localImagesFailed = false;
            var allImageProviders = ProviderManager.GetImageProviders(item, refreshOptions).ToList();

            // Only validate already registered images if we are replacing and saving locally
            if (item.IsSaveLocalMetadataEnabled() && refreshOptions.ReplaceAllImages)
            {
                item.ValidateImages();
            }
            else
            {
                // Run full image validation and register new local images
                try
                {
                    if (ImageProvider.ValidateImages(item, allImageProviders.OfType<ILocalImageProvider>(), refreshOptions))
                    {
                        updateType |= ItemUpdateType.ImageUpdate;
                    }
                }
                catch (Exception ex)
                {
                    localImagesFailed = true;
                    Logger.LogError(ex, "Error validating images for {Item}", item.Path ?? item.Name ?? "Unknown name");
                }
            }

            var metadataResult = new MetadataResult<TItemType>
            {
                Item = itemOfType
            };

            var beforeSaveResult = await BeforeSave(itemOfType, isFirstRefresh || refreshOptions.ReplaceAllMetadata || refreshOptions.MetadataRefreshMode == MetadataRefreshMode.FullRefresh || requiresRefresh || refreshOptions.ForceSave, updateType)
                .ConfigureAwait(false);
            updateType |= beforeSaveResult;

            if (isFirstRefresh)
            {
                await SaveItemAsync(metadataResult, ItemUpdateType.MetadataImport, false, cancellationToken).ConfigureAwait(false);
            }

            // Next run metadata providers
            if (refreshOptions.MetadataRefreshMode != MetadataRefreshMode.None)
            {
                var providers = GetProviders(item, libraryOptions, refreshOptions, isFirstRefresh, requiresRefresh)
                    .ToList();

                if (providers.Count > 0 || isFirstRefresh || requiresRefresh)
                {
                    if (item.BeforeMetadataRefresh(refreshOptions.ReplaceAllMetadata))
                    {
                        updateType |= ItemUpdateType.MetadataImport;
                    }
                }

                if (providers.Count > 0)
                {
                    var id = itemOfType.GetLookupInfo();

                    if (refreshOptions.SearchResult is not null)
                    {
                        ApplySearchResult(id, refreshOptions.SearchResult);
                    }

                    id.IsAutomated = refreshOptions.IsAutomated;

                    var hasMetadataSavers = ProviderManager.GetMetadataSavers(item, libraryOptions).Any();
                    var result = await RefreshWithProviders(metadataResult, id, refreshOptions, providers, ImageProvider, hasMetadataSavers, cancellationToken).ConfigureAwait(false);

                    updateType |= result.UpdateType;
                    if (result.Failures > 0)
                    {
                        hasRefreshedMetadata = false;
                    }
                }
            }

            // Next run remote image providers, but only if local image providers didn't throw an exception
            if (!localImagesFailed && refreshOptions.ImageRefreshMode > MetadataRefreshMode.ValidationOnly)
            {
                var providers = GetNonLocalImageProviders(item, allImageProviders, refreshOptions).ToList();

                if (providers.Count > 0)
                {
                    var result = await ImageProvider.RefreshImages(itemOfType, libraryOptions, providers, refreshOptions, cancellationToken).ConfigureAwait(false);

                    updateType |= result.UpdateType;
                    if (result.Failures > 0)
                    {
                        hasRefreshedImages = false;
                    }
                }
            }

            var attemptedFetch = refreshOptions.MetadataRefreshMode > MetadataRefreshMode.ValidationOnly
                || refreshOptions.ImageRefreshMode > MetadataRefreshMode.ValidationOnly;

            var refreshStampNeedsSaving = false;

            if (hasRefreshedMetadata && hasRefreshedImages && attemptedFetch)
            {
                item.DateLastRefreshed = DateTime.UtcNow;
                updateType |= item.OnMetadataChanged();

                // A full refresh queries every provider whether or not anything looks stale. When they all
                // come back empty the stamp is the only thing that changed, and without it nothing records
                // that the lookup happened, so the next pass repeats the same fruitless queries forever.
                refreshStampNeedsSaving = refreshOptions.MetadataRefreshMode == MetadataRefreshMode.FullRefresh
                    || refreshOptions.ImageRefreshMode == MetadataRefreshMode.FullRefresh;
            }

            updateType = await SaveInternal(item, refreshOptions, updateType, isFirstRefresh, requiresRefresh, refreshStampNeedsSaving, metadataResult, cancellationToken).ConfigureAwait(false);

            await AfterMetadataRefresh(itemOfType, refreshOptions, cancellationToken).ConfigureAwait(false);

            return updateType;

            async Task<ItemUpdateType> SaveInternal(BaseItem item, MetadataRefreshOptions refreshOptions, ItemUpdateType updateType, bool isFirstRefresh, bool requiresRefresh, bool refreshStampNeedsSaving, MetadataResult<TItemType> metadataResult, CancellationToken cancellationToken)
            {
                // Save if changes were made, or it's never been saved before
                if (refreshOptions.ForceSave || updateType > ItemUpdateType.None || isFirstRefresh || refreshOptions.ReplaceAllMetadata || requiresRefresh || refreshStampNeedsSaving)
                {
                    if (item.IsFileProtocol)
                    {
                        var file = TryGetFileSystemMetadata(item.Path, refreshOptions.DirectoryService);
                        if (file is not null)
                        {
                            item.DateModified = file.LastWriteTimeUtc;

                            if (!file.IsDirectory)
                            {
                                item.Size = file.Length;
                            }
                        }
                    }

                    // If any of these properties are set then make sure the updateType is not None, just to force everything to save
                    if (refreshOptions.ForceSave || refreshOptions.ReplaceAllMetadata)
                    {
                        updateType |= ItemUpdateType.MetadataDownload;
                    }

                    // Save to database
                    await SaveItemAsync(metadataResult, updateType, isFirstRefresh, cancellationToken).ConfigureAwait(false);
                }

                return updateType;
            }
        }

        private void ApplySearchResult(ItemLookupInfo lookupInfo, RemoteSearchResult result)
        {
            // Episode and Season do not support Identify, so the search results are the Series'
            switch (lookupInfo)
            {
                case EpisodeInfo episodeInfo:
                    episodeInfo.SeriesProviderIds = GetValidProviderIds(result.ProviderIds);
                    episodeInfo.ProviderIds.Clear();
                    break;
                case SeasonInfo seasonInfo:
                    seasonInfo.SeriesProviderIds = GetValidProviderIds(result.ProviderIds);
                    seasonInfo.ProviderIds.Clear();
                    break;
                default:
                    lookupInfo.SetProviderIds(result.ProviderIds);
                    lookupInfo.Name = result.Name;
                    lookupInfo.Year = result.ProductionYear;
                    break;
            }
        }

        private static Dictionary<string, string> GetValidProviderIds(IReadOnlyDictionary<string, string> providerIds)
        {
            var validProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (providerIds is null)
            {
                return validProviderIds;
            }

            foreach (var (name, value) in providerIds)
            {
                if (ProviderIdsExtensions.IsValidProviderId(name, value))
                {
                    validProviderIds[name] = value;
                }
            }

            return validProviderIds;
        }

        protected async Task SaveItemAsync(MetadataResult<TItemType> result, ItemUpdateType reason, bool reattachUserData, CancellationToken cancellationToken)
        {
            await result.Item.UpdateToRepositoryAsync(reason, cancellationToken).ConfigureAwait(false);
            if (reattachUserData)
            {
                await result.Item.ReattachUserDataAsync(cancellationToken).ConfigureAwait(false);
            }

            if (result.Item.SupportsPeople && result.People is not null)
            {
                var baseItem = result.Item;

                await LibraryManager.UpdatePeopleAsync(baseItem, result.People, cancellationToken).ConfigureAwait(false);
            }
        }

        protected virtual Task AfterMetadataRefresh(TItemType item, MetadataRefreshOptions refreshOptions, CancellationToken cancellationToken)
        {
            item.AfterMetadataRefresh();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Before the save.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="isFullRefresh">if set to <c>true</c> [is full refresh].</param>
        /// <param name="currentUpdateType">Type of the current update.</param>
        /// <returns>ItemUpdateType.</returns>
        private async Task<ItemUpdateType> BeforeSave(TItemType item, bool isFullRefresh, ItemUpdateType currentUpdateType)
        {
            var updateType = BeforeSaveInternal(item, isFullRefresh, currentUpdateType);

            updateType |= item.OnMetadataChanged();

            if (updateType == ItemUpdateType.None)
            {
                if (!await ItemRepository.ItemExistsAsync(item.Id).ConfigureAwait(false))
                {
                    return ItemUpdateType.MetadataImport;
                }
            }

            return updateType;
        }

        protected virtual ItemUpdateType BeforeSaveInternal(TItemType item, bool isFullRefresh, ItemUpdateType updateType)
        {
            if (EnableUpdateMetadataFromChildren(item, isFullRefresh, updateType))
            {
                var children = GetChildrenForMetadataUpdates(item);
                updateType = UpdateMetadataFromChildren(item, children, isFullRefresh, updateType);
            }

            var presentationUniqueKey = item.CreatePresentationUniqueKey();
            if (!string.Equals(item.PresentationUniqueKey, presentationUniqueKey, StringComparison.Ordinal))
            {
                item.PresentationUniqueKey = presentationUniqueKey;
                updateType |= ItemUpdateType.MetadataImport;
            }

            // Cleanup extracted files if source file was modified
            var itemPath = item.Path;
            if (!string.IsNullOrEmpty(itemPath))
            {
                var info = FileSystem.GetFileSystemInfo(itemPath);
                if (info.Exists && item.HasChanged(info.LastWriteTimeUtc))
                {
                    Logger.LogDebug("File modification time changed from {Then} to {Now}: {Path}", item.DateModified, info.LastWriteTimeUtc, itemPath);

                    item.DateModified = info.LastWriteTimeUtc;
                    if (ServerConfigurationManager.GetMetadataConfiguration().UseFileCreationTimeForDateAdded)
                    {
                        if (info.CreationTimeUtc > DateTime.MinValue)
                        {
                            item.DateCreated = info.CreationTimeUtc;
                        }
                    }

                    if (item is Video video)
                    {
                        Logger.LogInformation("File changed, pruning extracted data: {Path}", item.Path);
                        ExternalDataManager.DeleteExternalItemDataAsync(video, CancellationToken.None).GetAwaiter().GetResult();
                    }

                    updateType |= ItemUpdateType.MetadataImport;
                }
            }

            return updateType;
        }

        protected virtual bool EnableUpdateMetadataFromChildren(TItemType item, bool isFullRefresh, ItemUpdateType currentUpdateType)
        {
            if (item is Folder folder)
            {
                if (!isFullRefresh && currentUpdateType == ItemUpdateType.None)
                {
                    return folder.SupportsDateLastMediaAdded;
                }

                if (isFullRefresh || currentUpdateType > ItemUpdateType.None)
                {
                    if (EnableUpdatingPremiereDateFromChildren || EnableUpdatingGenresFromChildren || EnableUpdatingStudiosFromChildren || EnableUpdatingOfficialRatingFromChildren)
                    {
                        return true;
                    }

                    if (folder.SupportsDateLastMediaAdded || folder.SupportsCumulativeRunTimeTicks)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected virtual IReadOnlyList<BaseItem> GetChildrenForMetadataUpdates(TItemType item)
        {
            if (item is Folder folder)
            {
                return folder.GetRecursiveChildren();
            }

            return [];
        }

        protected virtual ItemUpdateType UpdateMetadataFromChildren(TItemType item, IReadOnlyList<BaseItem> children, bool isFullRefresh, ItemUpdateType currentUpdateType)
        {
            var updateType = ItemUpdateType.None;

            if (item is Folder folder)
            {
                if (folder.SupportsDateLastMediaAdded)
                {
                    updateType |= UpdateDateLastMediaAdded(item, children);
                }

                if ((isFullRefresh || currentUpdateType > ItemUpdateType.None) && folder.SupportsCumulativeRunTimeTicks)
                {
                    updateType |= UpdateCumulativeRunTimeTicks(item, children);
                }
            }

            if (!(isFullRefresh || currentUpdateType > ItemUpdateType.None) || item.IsLocked)
            {
                return updateType;
            }

            if (EnableUpdatingPremiereDateFromChildren)
            {
                updateType |= UpdatePremiereDate(item, children);
            }

            if (EnableUpdatingGenresFromChildren)
            {
                updateType |= UpdateGenres(item, children);
            }

            if (EnableUpdatingStudiosFromChildren)
            {
                updateType |= UpdateStudios(item, children);
            }

            if (EnableUpdatingOfficialRatingFromChildren)
            {
                updateType |= UpdateOfficialRating(item, children);
            }

            return updateType;
        }

        private ItemUpdateType UpdateCumulativeRunTimeTicks(TItemType item, IReadOnlyList<BaseItem> children)
        {
            if (item is Folder folder && folder.SupportsCumulativeRunTimeTicks)
            {
                long ticks = 0;

                foreach (var child in children)
                {
                    if (!child.IsFolder)
                    {
                        ticks += child.RunTimeTicks ?? 0;
                    }
                }

                if (!folder.RunTimeTicks.HasValue || folder.RunTimeTicks.Value != ticks)
                {
                    folder.RunTimeTicks = ticks;
                    return ItemUpdateType.MetadataImport;
                }
            }

            return ItemUpdateType.None;
        }

        private ItemUpdateType UpdateDateLastMediaAdded(TItemType item, IReadOnlyList<BaseItem> children)
        {
            var updateType = ItemUpdateType.None;

            if (item is Folder folder && folder.SupportsDateLastMediaAdded)
            {
                var dateLastMediaAdded = DateTime.MinValue;
                var any = false;

                foreach (var child in children)
                {
                    // Exclude any folders and virtual items since they are only placeholders
                    if (!child.IsFolder && !child.IsVirtualItem)
                    {
                        var childDateCreated = child.DateCreated;
                        if (childDateCreated > dateLastMediaAdded)
                        {
                            dateLastMediaAdded = childDateCreated;
                        }

                        any = true;
                    }
                }

                if ((!folder.DateLastMediaAdded.HasValue && any) || folder.DateLastMediaAdded != dateLastMediaAdded)
                {
                    folder.DateLastMediaAdded = dateLastMediaAdded;
                    updateType = ItemUpdateType.MetadataImport;
                }
            }

            return updateType;
        }

        private ItemUpdateType UpdatePremiereDate(TItemType item, IReadOnlyList<BaseItem> children)
        {
            var updateType = ItemUpdateType.None;

            if (children.Count == 0)
            {
                return updateType;
            }

            var date = children.Select(i => i.PremiereDate ?? DateTime.MaxValue).Min();

            var originalPremiereDate = item.PremiereDate;
            var originalProductionYear = item.ProductionYear;

            if (date > DateTime.MinValue && date < DateTime.MaxValue)
            {
                item.PremiereDate = date;
                item.ProductionYear = date.Year;
            }
            else
            {
                var year = children.Select(i => i.ProductionYear ?? 0).Min();

                if (year > 0)
                {
                    item.ProductionYear = year;
                }
            }

            if ((originalPremiereDate ?? DateTime.MinValue) != (item.PremiereDate ?? DateTime.MinValue)
                || (originalProductionYear ?? -1) != (item.ProductionYear ?? -1))
            {
                updateType |= ItemUpdateType.MetadataEdit;
            }

            return updateType;
        }

        private ItemUpdateType UpdateGenres(TItemType item, IReadOnlyList<BaseItem> children)
        {
            var updateType = ItemUpdateType.None;

            if (!item.LockedFields.Contains(MetadataField.Genres))
            {
                var currentList = item.Genres;

                item.Genres = children.SelectMany(i => i.Genres)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (currentList.Length != item.Genres.Length || !currentList.Order().SequenceEqual(item.Genres.Order(), StringComparer.OrdinalIgnoreCase))
                {
                    updateType |= ItemUpdateType.MetadataEdit;
                }
            }

            return updateType;
        }

        private ItemUpdateType UpdateStudios(TItemType item, IReadOnlyList<BaseItem> children)
        {
            var updateType = ItemUpdateType.None;

            if (!item.LockedFields.Contains(MetadataField.Studios))
            {
                var currentList = item.Studios;

                item.Studios = children.SelectMany(i => i.Studios)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (currentList.Length != item.Studios.Length || !currentList.Order().SequenceEqual(item.Studios.Order(), StringComparer.OrdinalIgnoreCase))
                {
                    updateType |= ItemUpdateType.MetadataEdit;
                }
            }

            return updateType;
        }

        private ItemUpdateType UpdateOfficialRating(TItemType item, IReadOnlyList<BaseItem> children)
        {
            var updateType = ItemUpdateType.None;

            if (!item.LockedFields.Contains(MetadataField.OfficialRating))
            {
                if (item.UpdateRatingToItems(children))
                {
                    updateType |= ItemUpdateType.MetadataEdit;
                }
            }

            return updateType;
        }

        /// <summary>
        /// Gets the providers.
        /// </summary>
        /// <param name="item">A media item.</param>
        /// <param name="libraryOptions">The LibraryOptions to use.</param>
        /// <param name="options">The MetadataRefreshOptions to use.</param>
        /// <param name="isFirstRefresh">Specifies first refresh mode.</param>
        /// <param name="requiresRefresh">Specifies refresh mode.</param>
        /// <returns>IEnumerable{`0}.</returns>
        protected IEnumerable<IMetadataProvider> GetProviders(BaseItem item, LibraryOptions libraryOptions, MetadataRefreshOptions options, bool isFirstRefresh, bool requiresRefresh)
        {
            // Get providers to refresh
            var providers = ProviderManager.GetMetadataProviders<TItemType>(item, libraryOptions).ToList();

            var metadataRefreshMode = options.MetadataRefreshMode;

            // Run all if either of these flags are true
            var runAllProviders = options.ReplaceAllMetadata ||
                metadataRefreshMode == MetadataRefreshMode.FullRefresh ||
                (isFirstRefresh && metadataRefreshMode >= MetadataRefreshMode.Default) ||
                (requiresRefresh && metadataRefreshMode >= MetadataRefreshMode.Default);

            if (!runAllProviders)
            {
                var providersWithChanges = providers
                    .Where(i =>
                    {
                        if (i is IHasItemChangeMonitor hasFileChangeMonitor)
                        {
                            return HasChanged(item, hasFileChangeMonitor, options.DirectoryService);
                        }

                        return false;
                    })
                    .ToList();

                if (providersWithChanges.Count == 0)
                {
                    providers = new List<IMetadataProvider<TItemType>>();
                }
                else
                {
                    var anyRemoteProvidersChanged = providersWithChanges.OfType<IRemoteMetadataProvider>()
                        .Any();

                    var anyLocalProvidersChanged = providersWithChanges.OfType<ILocalMetadataProvider>()
                        .Any();

                    var anyLocalPreRefreshProvidersChanged = providersWithChanges.OfType<IPreRefreshProvider>()
                        .Any();

                    providers = providers.Where(i =>
                    {
                        // If any provider reports a change, always run local ones as well
                        if (i is ILocalMetadataProvider)
                        {
                            return anyRemoteProvidersChanged || anyLocalProvidersChanged || anyLocalPreRefreshProvidersChanged;
                        }

                        // If any remote providers changed, run them all so that priorities can be honored
                        if (i is IRemoteMetadataProvider)
                        {
                            if (options.MetadataRefreshMode == MetadataRefreshMode.ValidationOnly)
                            {
                                return false;
                            }

                            return anyRemoteProvidersChanged;
                        }

                        // Run custom refresh providers if they report a change or any remote providers change
                        return anyRemoteProvidersChanged || providersWithChanges.Contains(i);
                    }).ToList();
                }
            }

            return providers;
        }

        protected virtual IEnumerable<IImageProvider> GetNonLocalImageProviders(BaseItem item, IEnumerable<IImageProvider> allImageProviders, MetadataRefreshOptions options)
        {
            // Get providers to refresh
            var providers = allImageProviders.Where(i => i is not ILocalImageProvider);

            // When identifying, run the provider the user picked first so the correct image is used.
            if (!string.IsNullOrEmpty(options.SearchResult?.SearchProviderName))
            {
                providers = providers
                    .OrderBy(i => string.Equals(i.Name, options.SearchResult.SearchProviderName, StringComparison.OrdinalIgnoreCase) ? 0 : 1);
            }

            var dateLastImageRefresh = item.DateLastRefreshed;

            // Run all if either of these flags are true
            var runAllProviders = options.ImageRefreshMode == MetadataRefreshMode.FullRefresh || dateLastImageRefresh.Date == DateTime.MinValue.Date;

            if (!runAllProviders)
            {
                providers = providers
                    .Where(i =>
                    {
                        if (i is IHasItemChangeMonitor hasFileChangeMonitor)
                        {
                            return HasChanged(item, hasFileChangeMonitor, options.DirectoryService);
                        }

                        return false;
                    });
            }

            return providers;
        }

        public bool CanRefresh(BaseItem item)
        {
            return item is TItemType;
        }

        public bool CanRefreshPrimary(Type type)
        {
            return type == typeof(TItemType);
        }

        protected virtual async Task<RefreshResult> RefreshWithProviders(
            MetadataResult<TItemType> metadata,
            TIdType id,
            MetadataRefreshOptions options,
            ICollection<IMetadataProvider> providers,
            ItemImageProvider imageService,
            bool isSavingMetadata,
            CancellationToken cancellationToken)
        {
            var refreshResult = new RefreshResult
            {
                UpdateType = ItemUpdateType.None
            };

            var item = metadata.Item;

            var customProviders = providers.OfType<ICustomMetadataProvider<TItemType>>().ToList();
            var logName = !item.IsFileProtocol ? item.Name ?? item.Path : item.Path ?? item.Name;

            foreach (var provider in customProviders.Where(i => i is IPreRefreshProvider))
            {
                await RunCustomProvider(provider, item, logName, options, refreshResult, cancellationToken).ConfigureAwait(false);
            }

            if (item.IsLocked)
            {
                return refreshResult;
            }

            var temp = new MetadataResult<TItemType>
            {
                Item = CreateNew()
            };
            temp.Item.Path = item.Path;
            temp.Item.Id = item.Id;
            temp.Item.ParentIndexNumber = item.ParentIndexNumber;
            temp.Item.PreferredMetadataCountryCode = item.PreferredMetadataCountryCode;
            temp.Item.PreferredMetadataLanguage = item.PreferredMetadataLanguage;

            var foundImageTypes = new List<ImageType>();

            // Do not execute local providers if we are identifying or replacing with local metadata saving enabled
            if (options.SearchResult is null && !(isSavingMetadata && options.ReplaceAllMetadata))
            {
                foreach (var provider in providers.OfType<ILocalMetadataProvider<TItemType>>())
                {
                    var providerName = provider.GetType().Name;
                    Logger.LogDebug("Running {Provider} for {Item}", providerName, logName);

                    var itemInfo = new ItemInfo(item);

                    try
                    {
                        var localItem = await provider.GetMetadata(itemInfo, options.DirectoryService, cancellationToken).ConfigureAwait(false);

                        if (localItem.HasMetadata)
                        {
                            foreach (var remoteImage in localItem.RemoteImages)
                            {
                                try
                                {
                                    if (item.ImageInfos.Any(x => x.Type == remoteImage.Type)
                                        && !options.IsReplacingImage(remoteImage.Type))
                                    {
                                        continue;
                                    }

                                    await ProviderManager.SaveImage(item, remoteImage.Url, remoteImage.Type, null, cancellationToken).ConfigureAwait(false);
                                    refreshResult.UpdateType |= ItemUpdateType.ImageUpdate;

                                    // remember imagetype that has just been downloaded
                                    foundImageTypes.Add(remoteImage.Type);
                                }
                                catch (HttpRequestException ex)
                                {
                                    Logger.LogError(ex, "Could not save {ImageType} image: {Url}", Enum.GetName(remoteImage.Type), remoteImage.Url);
                                }
                            }

                            if (foundImageTypes.Count > 0)
                            {
                                imageService.UpdateReplaceImages(options, foundImageTypes);
                            }

                            if (imageService.MergeImages(item, localItem.Images, options))
                            {
                                refreshResult.UpdateType |= ItemUpdateType.ImageUpdate;
                            }

                            MergeData(localItem, temp, [], false, true);
                            refreshResult.UpdateType |= ItemUpdateType.MetadataImport;

                            break;
                        }

                        Logger.LogDebug("{Provider} returned no metadata for {Item}", providerName, logName);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error in {Provider} for {Item}", provider.Name, logName);

                        // If a local provider fails, consider that a failure
                        refreshResult.ErrorMessage = ex.Message;
                    }
                }
            }

            var hasRemoteMetadata = false;
            var isLocalLocked = temp.Item.IsLocked;
            if (!isLocalLocked && (options.ReplaceAllMetadata || options.MetadataRefreshMode > MetadataRefreshMode.ValidationOnly))
            {
                var remoteProviders = providers.OfType<IRemoteMetadataProvider<TItemType, TIdType>>();

                // When identifying, run the provider the user picked first so the correct IDs are used.
                if (!string.IsNullOrEmpty(options.SearchResult?.SearchProviderName))
                {
                    remoteProviders = remoteProviders
                        .OrderBy(i => string.Equals(i.Name, options.SearchResult.SearchProviderName, StringComparison.OrdinalIgnoreCase) ? 0 : 1);
                }

                var remoteResult = await ExecuteRemoteProviders(temp, logName, false, id, remoteProviders, cancellationToken).ConfigureAwait(false);

                hasRemoteMetadata = remoteResult.UpdateType.HasFlag(ItemUpdateType.MetadataDownload);
                refreshResult.UpdateType |= remoteResult.UpdateType;
                refreshResult.ErrorMessage = remoteResult.ErrorMessage;
                refreshResult.Failures += remoteResult.Failures;
            }

            if (providers.Any(i => i is not ICustomMetadataProvider))
            {
                if (refreshResult.UpdateType > ItemUpdateType.None)
                {
                    // Erasing the old values is only safe when a remote provider returned something to
                    // replace them with. If every one of them failed there is no replacement, and wiping the
                    // item would turn a provider being temporarily unreachable into permanent data loss.
                    // A single failure is not enough: Identify asks for the erasure precisely because the
                    // previous match was wrong, and an unrelated provider throwing must not undo that.
                    if (!options.RemoveOldMetadata || (refreshResult.Failures > 0 && !hasRemoteMetadata))
                    {
                        // Add existing metadata to provider result if it does not exist there
                        MergeData(metadata, temp, [], false, false);
                    }

                    if (isLocalLocked)
                    {
                        MergeData(temp, metadata, item.LockedFields, true, true);
                    }
                    else
                    {
                        var shouldReplace = (options.MetadataRefreshMode > MetadataRefreshMode.ValidationOnly && options.ReplaceAllMetadata)
                            // Case for Scan for new and updated files
                            || (options.MetadataRefreshMode == MetadataRefreshMode.Default && !options.ReplaceAllMetadata);
                        MergeData(temp, metadata, item.LockedFields, shouldReplace, true);
                    }
                }
            }

            foreach (var provider in customProviders.Where(i => i is not IPreRefreshProvider))
            {
                await RunCustomProvider(provider, item, logName, options, refreshResult, cancellationToken).ConfigureAwait(false);
            }

            return refreshResult;
        }

        private async Task RunCustomProvider(ICustomMetadataProvider<TItemType> provider, TItemType item, string logName, MetadataRefreshOptions options, RefreshResult refreshResult, CancellationToken cancellationToken)
        {
            Logger.LogDebug("Running {Provider} for {Item}", provider.GetType().Name, logName);

            try
            {
                refreshResult.UpdateType |= await provider.FetchAsync(item, options, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                refreshResult.ErrorMessage = ex.Message;
                Logger.LogError(ex, "Error in {Provider} for {Item}", provider.Name, logName);
            }
        }

        protected virtual TItemType CreateNew()
        {
            return new TItemType();
        }

        private async Task<RefreshResult> ExecuteRemoteProviders(MetadataResult<TItemType> temp, string logName, bool replaceData, TIdType id, IEnumerable<IRemoteMetadataProvider<TItemType, TIdType>> providers, CancellationToken cancellationToken)
        {
            var refreshResult = new RefreshResult();
            var preferredLanguage = id?.MetadataLanguage;

            var overviewIsFallback = false;
            var taglineIsFallback = false;

            if (id is not null)
            {
                MergeNewData(temp.Item, id);
            }

            foreach (var provider in providers)
            {
                var providerName = provider.GetType().Name;
                Logger.LogDebug("Running {Provider} for {Item}", providerName, logName);

                try
                {
                    var result = await provider.GetMetadata(id, cancellationToken).ConfigureAwait(false);

                    if (result.HasMetadata)
                    {
                        result.Provider = provider.Name;

                        if (MetadataLanguageUtils.MatchesPreferredLanguage(result.ResultLanguage, preferredLanguage))
                        {
                            if (overviewIsFallback && !string.IsNullOrEmpty(result.Item.Overview))
                            {
                                temp.Item.Overview = null;
                                overviewIsFallback = false;
                            }

                            if (taglineIsFallback && !string.IsNullOrEmpty(result.Item.Tagline))
                            {
                                temp.Item.Tagline = null;
                                taglineIsFallback = false;
                            }
                        }
                        else
                        {
                            overviewIsFallback |= string.IsNullOrEmpty(temp.Item.Overview) && !string.IsNullOrEmpty(result.Item.Overview);
                            taglineIsFallback |= string.IsNullOrEmpty(temp.Item.Tagline) && !string.IsNullOrEmpty(result.Item.Tagline);
                        }

                        LogInvalidProviderIds(result, providerName, logName);

                        MergeData(result, temp, [], replaceData, false);

                        // A secondary provider regularly knows people the primary missed.
                        if (!replaceData)
                        {
                            temp.People = AddMissingPeople(result.People, temp.People);
                        }

                        MergeNewData(temp.Item, id);

                        refreshResult.UpdateType |= ItemUpdateType.MetadataDownload;
                    }
                    else
                    {
                        Logger.LogDebug("{Provider} returned no metadata for {Item}", providerName, logName);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    refreshResult.Failures++;
                    refreshResult.ErrorMessage = ex.Message;
                    Logger.LogError(ex, "Error in {Provider} for {Item}", provider.Name, logName);
                }
            }

            return refreshResult;
        }

        /// <summary>
        /// Reports the ids a provider returned that cannot belong to the provider they are filed under.
        /// </summary>
        /// <remarks>
        /// The ids are dropped when merging, this names the provider that produced them so the source of a
        /// recurring bad id can be found.
        /// </remarks>
        private void LogInvalidProviderIds(MetadataResult<TItemType> result, string providerName, string logName)
        {
            if (!Logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            LogInvalidProviderIds(result.Item?.ProviderIds, providerName, logName, null);

            if (result.People is null)
            {
                return;
            }

            foreach (var person in result.People)
            {
                LogInvalidProviderIds(person.ProviderIds, providerName, logName, person.Name);
            }
        }

        private void LogInvalidProviderIds(IReadOnlyDictionary<string, string> providerIds, string providerName, string logName, string personName)
        {
            if (providerIds is null)
            {
                return;
            }

            foreach (var (key, value) in providerIds)
            {
                if (ProviderIdsExtensions.IsValidProviderId(key, value))
                {
                    continue;
                }

                if (personName is null)
                {
                    Logger.LogDebug("Discarding {Key} id '{Value}' returned by {Provider} for {Item}", key, value, providerName, logName);
                }
                else
                {
                    Logger.LogDebug("Discarding {Key} id '{Value}' returned by {Provider} for {Person} of {Item}", key, value, providerName, personName, logName);
                }
            }
        }

        private void MergeNewData(TItemType source, TIdType lookupInfo)
        {
            // Copy new provider id's that may have been obtained
            foreach (var providerId in source.ProviderIds)
            {
                var key = providerId.Key;

                if (!ProviderIdsExtensions.IsValidProviderId(key, providerId.Value))
                {
                    continue;
                }

                // Don't replace existing Id's, unless the one already there is unusable - handing that
                // one to the providers that have yet to run is what makes them fail.
                if (!lookupInfo.ProviderIds.TryGetValue(key, out var existingId)
                    || !ProviderIdsExtensions.IsValidProviderId(key, existingId))
                {
                    lookupInfo.ProviderIds[key] = providerId.Value;
                }
            }
        }

        private bool HasChanged(BaseItem item, IHasItemChangeMonitor changeMonitor, IDirectoryService directoryService)
        {
            try
            {
                var hasChanged = changeMonitor.HasChanged(item, directoryService);

                if (hasChanged)
                {
                    Logger.LogDebug("{Monitor} reports change to {Item}", changeMonitor.GetType().Name, item.Path ?? item.Name);
                }

                return hasChanged;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in {Monitor}.HasChanged", changeMonitor.GetType().Name);
                return false;
            }
        }

        /// <summary>
        /// Merges metadata from source into target.
        /// </summary>
        /// <param name="source">The source for new metadata.</param>
        /// <param name="target">The target to insert new metadata into.</param>
        /// <param name="lockedFields">The fields that are locked and should not be updated.</param>
        /// <param name="replaceData"><c>true</c> if existing data should be replaced.</param>
        /// <param name="mergeMetadataSettings"><c>true</c> if the metadata settings in target should be updated to match source.</param>
        /// <exception cref="ArgumentException">Thrown if source or target are null.</exception>
        protected virtual void MergeData(
            MetadataResult<TItemType> source,
            MetadataResult<TItemType> target,
            MetadataField[] lockedFields,
            bool replaceData,
            bool mergeMetadataSettings)
        {
            MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);
        }

        internal static void MergeBaseItemData(
            MetadataResult<TItemType> sourceResult,
            MetadataResult<TItemType> targetResult,
            MetadataField[] lockedFields,
            bool replaceData,
            bool mergeMetadataSettings)
        {
            var source = sourceResult.Item;
            var target = targetResult.Item;

            ArgumentNullException.ThrowIfNull(sourceResult);
            ArgumentNullException.ThrowIfNull(targetResult);

            if (!lockedFields.Contains(MetadataField.Name))
            {
                if (replaceData || string.IsNullOrEmpty(target.Name))
                {
                    // Safeguard against incoming data having an empty name
                    if (!string.IsNullOrWhiteSpace(source.Name))
                    {
                        target.Name = source.Name;
                    }
                }
            }

            if (replaceData || string.IsNullOrEmpty(target.OriginalTitle))
            {
                target.OriginalTitle = source.OriginalTitle;
            }

            if (replaceData || string.IsNullOrEmpty(target.HomePageUrl))
            {
                target.HomePageUrl = source.HomePageUrl;
            }

            if (replaceData || string.IsNullOrEmpty(target.OriginalLanguage))
            {
                target.OriginalLanguage = source.OriginalLanguage;
            }

            if (replaceData || !target.CommunityRating.HasValue)
            {
                target.CommunityRating = source.CommunityRating;
            }

            if (replaceData || !target.EndDate.HasValue)
            {
                target.EndDate = source.EndDate;
            }

            if (!lockedFields.Contains(MetadataField.Genres))
            {
                if (replaceData || target.Genres.Length == 0)
                {
                    target.Genres = source.Genres;
                }
            }

            if (replaceData || !target.IndexNumber.HasValue)
            {
                target.IndexNumber = source.IndexNumber;
            }

            if (!lockedFields.Contains(MetadataField.OfficialRating))
            {
                if (replaceData || string.IsNullOrEmpty(target.OfficialRating))
                {
                    target.OfficialRating = source.OfficialRating;
                }
            }

            if (replaceData || string.IsNullOrEmpty(target.CustomRating))
            {
                target.CustomRating = source.CustomRating;
            }

            if (replaceData || string.IsNullOrEmpty(target.Tagline))
            {
                target.Tagline = source.Tagline;
            }

            if (!lockedFields.Contains(MetadataField.Overview))
            {
                if (replaceData || string.IsNullOrEmpty(target.Overview))
                {
                    target.Overview = source.Overview;
                }
            }

            if (replaceData || !target.ParentIndexNumber.HasValue)
            {
                target.ParentIndexNumber = source.ParentIndexNumber;
            }

            if (!lockedFields.Contains(MetadataField.Cast))
            {
                RemoveInvalidProviderIds(sourceResult.People);
                RemoveInvalidProviderIds(targetResult.People);

                if (replaceData || targetResult.People is null || targetResult.People.Count == 0)
                {
                    // An empty list is how a provider states an item has no cast, and this is the only path
                    // that can drop a credit.
                    targetResult.People = sourceResult.People;
                }
                else if (sourceResult.People is not null && sourceResult.People.Count > 0)
                {
                    MergePeople(sourceResult.People, targetResult.People);
                }
            }

            if (replaceData || !target.PremiereDate.HasValue)
            {
                target.PremiereDate = source.PremiereDate;
            }

            if (replaceData || target.ProductionYear is null)
            {
                target.ProductionYear = source.ProductionYear;
            }

            if (!lockedFields.Contains(MetadataField.Runtime))
            {
                if (replaceData || !target.RunTimeTicks.HasValue)
                {
                    if (target is not Audio && target is not Video && target is not Book)
                    {
                        target.RunTimeTicks = source.RunTimeTicks;
                    }
                }
            }

            if (!lockedFields.Contains(MetadataField.Studios))
            {
                if (replaceData || target.Studios.Length == 0)
                {
                    target.Studios = source.Studios;
                }
                else
                {
                    target.Studios = target.Studios.Concat(source.Studios).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                }
            }

            if (!lockedFields.Contains(MetadataField.Tags))
            {
                if (replaceData || target.Tags.Length == 0)
                {
                    target.Tags = source.Tags;
                }
                else
                {
                    target.Tags = target.Tags.Concat(source.Tags).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                }
            }

            if (!lockedFields.Contains(MetadataField.ProductionLocations))
            {
                if (replaceData || target.ProductionLocations.Length == 0)
                {
                    target.ProductionLocations = source.ProductionLocations;
                }
                else
                {
                    target.ProductionLocations = target.ProductionLocations.Concat(source.ProductionLocations).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                }
            }

            foreach (var id in source.ProviderIds)
            {
                var key = id.Key;

                // An id that cannot belong to the provider it is filed under only breaks that provider on
                // the next refresh, so never let one in - not even when replacing all metadata.
                if (!ProviderIdsExtensions.IsValidProviderId(key, id.Value))
                {
                    continue;
                }

                // Don't replace existing Id's, unless the stored one is unusable - that one is the bad
                // match the refresh is meant to repair.
                if (replaceData
                    || !target.ProviderIds.TryGetValue(key, out var existingId)
                    || !ProviderIdsExtensions.IsValidProviderId(key, existingId))
                {
                    target.ProviderIds[key] = id.Value;
                }
            }

            // A bad id no provider offered a replacement for still has to go, otherwise the item keeps
            // failing the same way on every refresh.
            foreach (var key in target.ProviderIds
                .Where(id => !ProviderIdsExtensions.IsValidProviderId(id.Key, id.Value))
                .Select(id => id.Key)
                .ToArray())
            {
                target.ProviderIds.Remove(key);
            }

            if (replaceData || !target.CriticRating.HasValue)
            {
                target.CriticRating = source.CriticRating;
            }

            if (replaceData || target.RemoteTrailers.Count == 0)
            {
                target.RemoteTrailers = source.RemoteTrailers;
            }
            else
            {
                target.RemoteTrailers = target.RemoteTrailers.Concat(source.RemoteTrailers).DistinctBy(t => t.Url).ToArray();
            }

            MergeAlbumArtist(source, target, replaceData);
            MergeVideoInfo(source, target, replaceData);
            MergeDisplayOrder(source, target, replaceData);

            if (replaceData || string.IsNullOrEmpty(target.ForcedSortName))
            {
                var forcedSortName = source.ForcedSortName;
                if (!string.IsNullOrEmpty(forcedSortName))
                {
                    target.ForcedSortName = forcedSortName;
                }
            }

            if (mergeMetadataSettings)
            {
                if (replaceData || !target.IsLocked)
                {
                    target.IsLocked = target.IsLocked || source.IsLocked;
                }

                if (target.LockedFields.Length == 0)
                {
                    target.LockedFields = source.LockedFields;
                }
                else
                {
                    target.LockedFields = target.LockedFields.Concat(source.LockedFields).Distinct().ToArray();
                }

                if (source.DateCreated != DateTime.MinValue)
                {
                    target.DateCreated = source.DateCreated;
                }

                if (replaceData || source.DateModified != DateTime.MinValue)
                {
                    target.DateModified = source.DateModified;
                }

                if (replaceData || string.IsNullOrEmpty(target.PreferredMetadataCountryCode))
                {
                    target.PreferredMetadataCountryCode = source.PreferredMetadataCountryCode;
                }

                if (replaceData || string.IsNullOrEmpty(target.PreferredMetadataLanguage))
                {
                    target.PreferredMetadataLanguage = source.PreferredMetadataLanguage;
                }
            }
        }

        private static void RemoveInvalidProviderIds(IReadOnlyList<PersonInfo> people)
        {
            if (people is null)
            {
                return;
            }

            foreach (var person in people)
            {
                if (person.ProviderIds is null || person.ProviderIds.Count == 0)
                {
                    continue;
                }

                var invalidKeys = person.ProviderIds
                    .Where(id => !ProviderIdsExtensions.IsValidProviderId(id.Key, id.Value))
                    .Select(id => id.Key)
                    .ToArray();

                foreach (var key in invalidKeys)
                {
                    person.ProviderIds.Remove(key);
                }
            }
        }

        // Only enriches what target already holds: it is about to become the item's cast, so adding
        // here would make a removed credit immortal. AddMissingPeople unions two provider results.
        private static void MergePeople(IReadOnlyList<PersonInfo> source, IReadOnlyList<PersonInfo> target)
        {
            var sourceByName = source.ToLookup(p => p.Name.GetCleanValue(), StringComparer.Ordinal);
            var targetByName = target.ToLookup(p => p.Name.GetCleanValue(), StringComparer.Ordinal);

            foreach (var name in targetByName.Select(g => g.Key))
            {
                var targetPeople = targetByName[name].ToArray();
                var sourcePeople = sourceByName[name].ToArray();

                if (sourcePeople.Length == 0)
                {
                    continue;
                }

                // Paired on the ids rather than on position, taking each source once: the wrong pairing
                // writes one human's data onto the other. Agreement first.
                var matches = new PersonInfo[targetPeople.Length];
                var taken = new bool[sourcePeople.Length];
                for (var pass = 0; pass < 2; pass++)
                {
                    for (var i = 0; i < targetPeople.Length; i++)
                    {
                        if (matches[i] is not null)
                        {
                            continue;
                        }

                        for (var j = 0; j < sourcePeople.Length; j++)
                        {
                            if (taken[j]
                                || targetPeople[i].FindConflictingProvider(sourcePeople[j].ProviderIds) is not null
                                || (pass == 0 && !targetPeople[i].SharesProviderId(sourcePeople[j].ProviderIds)))
                            {
                                continue;
                            }

                            matches[i] = sourcePeople[j];
                            taken[j] = true;
                            break;
                        }
                    }
                }

                for (int i = 0; i < targetPeople.Length; i++)
                {
                    var person = targetPeople[i];
                    var personInSource = matches[i];
                    if (personInSource is null)
                    {
                        continue;
                    }

                    foreach (var providerId in personInSource.ProviderIds)
                    {
                        person.ProviderIds.TryAdd(providerId.Key, providerId.Value);
                    }

                    if (string.IsNullOrWhiteSpace(person.ImageUrl))
                    {
                        person.ImageUrl = personInSource.ImageUrl;
                    }

                    if (!string.IsNullOrWhiteSpace(personInSource.Role) && string.IsNullOrWhiteSpace(person.Role))
                    {
                        person.Role = personInSource.Role;
                    }

                    if (personInSource.SortOrder.HasValue && !person.SortOrder.HasValue)
                    {
                        person.SortOrder = personInSource.SortOrder;
                    }
                }
            }
        }

        /// <summary>
        /// Returns <paramref name="target"/> with the people it does not know at all appended, after its own ordering.
        /// </summary>
        /// <param name="source">The result to take the additional people from.</param>
        /// <param name="target">The result to add them to.</param>
        /// <returns><paramref name="target"/> itself when it already knows everyone, otherwise a new list.</returns>
        internal static IReadOnlyList<PersonInfo> AddMissingPeople(IReadOnlyList<PersonInfo> source, IReadOnlyList<PersonInfo> target)
        {
            if (source is null || source.Count == 0 || target is null || target.Count == 0)
            {
                return target;
            }

            var known = target.Where(p => !string.IsNullOrEmpty(p.Name))
                .ToLookup(p => p.Name.GetCleanValue(), StringComparer.Ordinal);

            List<PersonInfo> merged = null;
            foreach (var person in source)
            {
                // A name the target already holds can still be another human, and only the ids say so.
                if (string.IsNullOrEmpty(person.Name)
                    || known[person.Name.GetCleanValue()].Any(p => p.FindConflictingProvider(person.ProviderIds) is null))
                {
                    continue;
                }

                merged ??= target.ToList();
                PeopleHelper.AddPerson(merged, person);
            }

            return merged ?? target;
        }

        private static void MergeDisplayOrder(BaseItem source, BaseItem target, bool replaceData)
        {
            if (source is IHasDisplayOrder sourceHasDisplayOrder
                && target is IHasDisplayOrder targetHasDisplayOrder)
            {
                if (replaceData || string.IsNullOrEmpty(targetHasDisplayOrder.DisplayOrder))
                {
                    var displayOrder = sourceHasDisplayOrder.DisplayOrder;
                    if (!string.IsNullOrWhiteSpace(displayOrder))
                    {
                        targetHasDisplayOrder.DisplayOrder = displayOrder;
                    }
                }
            }
        }

        private static void MergeAlbumArtist(BaseItem source, BaseItem target, bool replaceData)
        {
            if (source is IHasAlbumArtist sourceHasAlbumArtist
                && target is IHasAlbumArtist targetHasAlbumArtist)
            {
                if (replaceData || targetHasAlbumArtist.AlbumArtists.Count == 0)
                {
                    targetHasAlbumArtist.AlbumArtists = sourceHasAlbumArtist.AlbumArtists;
                }
                else if (sourceHasAlbumArtist.AlbumArtists.Count > 0)
                {
                    targetHasAlbumArtist.AlbumArtists = targetHasAlbumArtist.AlbumArtists.Concat(sourceHasAlbumArtist.AlbumArtists).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                }
            }
        }

        private static void MergeVideoInfo(BaseItem source, BaseItem target, bool replaceData)
        {
            if (source is Video sourceCast && target is Video targetCast)
            {
                if (sourceCast.Video3DFormat.HasValue && (replaceData || !targetCast.Video3DFormat.HasValue))
                {
                    targetCast.Video3DFormat = sourceCast.Video3DFormat;
                }
            }
        }
    }
}
