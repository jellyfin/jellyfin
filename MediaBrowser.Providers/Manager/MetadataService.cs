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
using MediaBrowser.Controller.Library;
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
        protected MetadataService(IServerConfigurationManager serverConfigurationManager, ILogger<MetadataService<TItemType, TIdType>> logger, IProviderManager providerManager, IFileSystem fileSystem, ILibraryManager libraryManager)
        {
            ServerConfigurationManager = serverConfigurationManager;
            Logger = logger;
            ProviderManager = providerManager;
            FileSystem = fileSystem;
            LibraryManager = libraryManager;
            ImageProvider = new ItemImageProvider(Logger, ProviderManager, FileSystem);
        }

        protected ItemImageProvider ImageProvider { get; }

        protected IServerConfigurationManager ServerConfigurationManager { get; }

        protected ILogger<MetadataService<TItemType, TIdType>> Logger { get; }

        protected IProviderManager ProviderManager { get; }

        protected IFileSystem FileSystem { get; }

        protected ILibraryManager LibraryManager { get; }

        protected virtual bool EnableUpdatingPremiereDateFromChildren => false;

        protected virtual bool EnableUpdatingGenresFromChildren => false;

        protected virtual bool EnableUpdatingStudiosFromChildren => false;

        protected virtual bool EnableUpdatingOfficialRatingFromChildren => false;

        public virtual int Order => 0;

        private FileSystemMetadata TryGetFile(string path, IDirectoryService directoryService)
        {
            try
            {
                return directoryService.GetFile(path);
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
                Item = itemOfType,
                People = LibraryManager.GetPeople(item)
            };

            bool hasRefreshedMetadata = true;
            bool hasRefreshedImages = true;
            var isFirstRefresh = item.DateLastRefreshed == default;

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

            var beforeSaveResult = BeforeSave(itemOfType, isFirstRefresh || refreshOptions.ReplaceAllMetadata || refreshOptions.MetadataRefreshMode == MetadataRefreshMode.FullRefresh || requiresRefresh || refreshOptions.ForceSave, updateType);
            updateType |= beforeSaveResult;

            // Save if changes were made, or it's never been saved before
            if (refreshOptions.ForceSave || updateType > ItemUpdateType.None || isFirstRefresh || refreshOptions.ReplaceAllMetadata || requiresRefresh)
            {
                if (item.IsFileProtocol)
                {
                    var file = TryGetFile(item.Path, refreshOptions.DirectoryService);
                    if (file is not null)
                    {
                        item.DateModified = file.LastWriteTimeUtc;
                    }
                }

                // If any of these properties are set then make sure the updateType is not None, just to force everything to save
                if (refreshOptions.ForceSave || refreshOptions.ReplaceAllMetadata)
                {
                    updateType |= ItemUpdateType.MetadataDownload;
                }

                if (hasRefreshedMetadata && hasRefreshedImages)
                {
                    item.DateLastRefreshed = DateTime.UtcNow;
                }
                else
                {
                    item.DateLastRefreshed = default;
                }

                // Save to database
                await SaveItemAsync(metadataResult, updateType, cancellationToken).ConfigureAwait(false);
            }

            await AfterMetadataRefresh(itemOfType, refreshOptions, cancellationToken).ConfigureAwait(false);

            return updateType;
        }

        private void ApplySearchResult(ItemLookupInfo lookupInfo, RemoteSearchResult result)
        {
            // Episode and Season do not support Identify, so the search results are the Series'
            switch (lookupInfo)
            {
                case EpisodeInfo episodeInfo:
                    episodeInfo.SeriesProviderIds = result.ProviderIds;
                    episodeInfo.ProviderIds.Clear();
                    break;
                case SeasonInfo seasonInfo:
                    seasonInfo.SeriesProviderIds = result.ProviderIds;
                    seasonInfo.ProviderIds.Clear();
                    break;
                default:
                    lookupInfo.ProviderIds = result.ProviderIds;
                    lookupInfo.Name = result.Name;
                    lookupInfo.Year = result.ProductionYear;
                    break;
            }
        }

        protected async Task SaveItemAsync(MetadataResult<TItemType> result, ItemUpdateType reason, CancellationToken cancellationToken)
        {
            if (result.Item.SupportsPeople)
            {
                var baseItem = result.Item;

                await LibraryManager.UpdatePeopleAsync(baseItem, result.People, cancellationToken).ConfigureAwait(false);
            }

            await result.Item.UpdateToRepositoryAsync(reason, cancellationToken).ConfigureAwait(false);
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
        private ItemUpdateType BeforeSave(TItemType item, bool isFullRefresh, ItemUpdateType currentUpdateType)
        {
            var updateType = BeforeSaveInternal(item, isFullRefresh, currentUpdateType);

            updateType |= item.OnMetadataChanged();

            return updateType;
        }

        protected virtual ItemUpdateType BeforeSaveInternal(TItemType item, bool isFullRefresh, ItemUpdateType updateType)
        {
            if (EnableUpdateMetadataFromChildren(item, isFullRefresh, updateType))
            {
                if (isFullRefresh || updateType > ItemUpdateType.None)
                {
                    var children = GetChildrenForMetadataUpdates(item);

                    updateType = UpdateMetadataFromChildren(item, children, isFullRefresh, updateType);
                }
            }

            var presentationUniqueKey = item.CreatePresentationUniqueKey();
            if (!string.Equals(item.PresentationUniqueKey, presentationUniqueKey, StringComparison.Ordinal))
            {
                item.PresentationUniqueKey = presentationUniqueKey;
                updateType |= ItemUpdateType.MetadataImport;
            }

            return updateType;
        }

        protected virtual bool EnableUpdateMetadataFromChildren(TItemType item, bool isFullRefresh, ItemUpdateType currentUpdateType)
        {
            if (isFullRefresh || currentUpdateType > ItemUpdateType.None)
            {
                if (EnableUpdatingPremiereDateFromChildren || EnableUpdatingGenresFromChildren || EnableUpdatingStudiosFromChildren || EnableUpdatingOfficialRatingFromChildren)
                {
                    return true;
                }

                if (item is Folder folder)
                {
                    return folder.SupportsDateLastMediaAdded || folder.SupportsCumulativeRunTimeTicks;
                }
            }

            return false;
        }

        protected virtual IList<BaseItem> GetChildrenForMetadataUpdates(TItemType item)
        {
            if (item is Folder folder)
            {
                return folder.GetRecursiveChildren();
            }

            return Array.Empty<BaseItem>();
        }

        protected virtual ItemUpdateType UpdateMetadataFromChildren(TItemType item, IList<BaseItem> children, bool isFullRefresh, ItemUpdateType currentUpdateType)
        {
            var updateType = ItemUpdateType.None;

            if (isFullRefresh || currentUpdateType > ItemUpdateType.None)
            {
                updateType |= UpdateCumulativeRunTimeTicks(item, children);
                updateType |= UpdateDateLastMediaAdded(item, children);

                // don't update user-changeable metadata for locked items
                if (item.IsLocked)
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
            }

            return updateType;
        }

        private ItemUpdateType UpdateCumulativeRunTimeTicks(TItemType item, IList<BaseItem> children)
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

        private ItemUpdateType UpdateDateLastMediaAdded(TItemType item, IList<BaseItem> children)
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

        private ItemUpdateType UpdatePremiereDate(TItemType item, IList<BaseItem> children)
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

        private ItemUpdateType UpdateGenres(TItemType item, IList<BaseItem> children)
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

        private ItemUpdateType UpdateStudios(TItemType item, IList<BaseItem> children)
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

        private ItemUpdateType UpdateOfficialRating(TItemType item, IList<BaseItem> children)
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

        protected virtual IEnumerable<IImageProvider> GetNonLocalImageProviders(BaseItem item, IEnumerable<IImageProvider> allImageProviders, ImageRefreshOptions options)
        {
            // Get providers to refresh
            var providers = allImageProviders.Where(i => i is not ILocalImageProvider);

            var dateLastImageRefresh = item.DateLastRefreshed;

            // Run all if either of these flags are true
            var runAllProviders = options.ImageRefreshMode == MetadataRefreshMode.FullRefresh || dateLastImageRefresh == default(DateTime);

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
                        Logger.LogError(ex, "Error in {Provider}", provider.Name);

                        // If a local provider fails, consider that a failure
                        refreshResult.ErrorMessage = ex.Message;
                    }
                }
            }

            var isLocalLocked = temp.Item.IsLocked;
            if (!isLocalLocked && (options.ReplaceAllMetadata || options.MetadataRefreshMode > MetadataRefreshMode.ValidationOnly))
            {
                var remoteResult = await ExecuteRemoteProviders(temp, logName, false, id, providers.OfType<IRemoteMetadataProvider<TItemType, TIdType>>(), cancellationToken)
                    .ConfigureAwait(false);

                refreshResult.UpdateType |= remoteResult.UpdateType;
                refreshResult.ErrorMessage = remoteResult.ErrorMessage;
                refreshResult.Failures += remoteResult.Failures;
            }

            if (providers.Any(i => i is not ICustomMetadataProvider))
            {
                if (refreshResult.UpdateType > ItemUpdateType.None)
                {
                    if (!options.RemoveOldMetadata)
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
                        var shouldReplace = options.MetadataRefreshMode > MetadataRefreshMode.ValidationOnly || options.ReplaceAllMetadata;
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
                Logger.LogError(ex, "Error in {Provider}", provider.Name);
            }
        }

        protected virtual TItemType CreateNew()
        {
            return new TItemType();
        }

        private async Task<RefreshResult> ExecuteRemoteProviders(MetadataResult<TItemType> temp, string logName, bool replaceData, TIdType id, IEnumerable<IRemoteMetadataProvider<TItemType, TIdType>> providers, CancellationToken cancellationToken)
        {
            var refreshResult = new RefreshResult();

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

                        MergeData(result, temp, [], replaceData, false);
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
                    Logger.LogError(ex, "Error in {Provider}", provider.Name);
                }
            }

            return refreshResult;
        }

        private void MergeNewData(TItemType source, TIdType lookupInfo)
        {
            // Copy new provider id's that may have been obtained
            foreach (var providerId in source.ProviderIds)
            {
                var key = providerId.Key;

                // Don't replace existing Id's.
                lookupInfo.ProviderIds.TryAdd(key, providerId.Value);
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
                if (replaceData || targetResult.People is null || targetResult.People.Count == 0)
                {
                    targetResult.People = sourceResult.People;
                }
                else if (sourceResult.People is not null && sourceResult.People.Count > 0)
                {
                    MergePeople(sourceResult.People, targetResult.People);
                }
            }

            if (replaceData || !target.PremiereDate.HasValue || (IsYearOnlyDate(target.PremiereDate.Value) && source.PremiereDate.HasValue))
            {
                target.PremiereDate = source.PremiereDate;
            }

            if (replaceData || !target.ProductionYear.HasValue)
            {
                target.ProductionYear = source.ProductionYear;
            }

            if (!lockedFields.Contains(MetadataField.Runtime))
            {
                if (replaceData || !target.RunTimeTicks.HasValue)
                {
                    if (target is not Audio && target is not Video)
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
                    target.Studios = target.Studios.Concat(source.Studios).Distinct().ToArray();
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
                    target.Tags = target.Tags.Concat(source.Tags).Distinct().ToArray();
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
                    target.ProductionLocations = target.ProductionLocations.Concat(source.ProductionLocations).Distinct().ToArray();
                }
            }

            foreach (var id in source.ProviderIds)
            {
                var key = id.Key;

                // Don't replace existing Id's.
                if (replaceData)
                {
                    target.ProviderIds[key] = id.Value;
                }
                else
                {
                    target.ProviderIds.TryAdd(key, id.Value);
                }
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

                if (source.DateCreated != default)
                {
                    target.DateCreated = source.DateCreated;
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

        private static bool IsYearOnlyDate(DateTime date) => date.Month == 1 && date.Day == 1;

        private static void MergePeople(List<PersonInfo> source, List<PersonInfo> target)
        {
            if (target is null)
            {
                target = new List<PersonInfo>();
            }

            var sourceByName = source.ToLookup(p => p.Name.RemoveDiacritics(), StringComparer.OrdinalIgnoreCase);
            var targetByName = target.ToLookup(p => p.Name.RemoveDiacritics(), StringComparer.OrdinalIgnoreCase);

            foreach (var name in targetByName.Select(g => g.Key))
            {
                var targetPeople = targetByName[name].ToArray();
                var sourcePeople = sourceByName[name].ToArray();

                if (sourcePeople.Length == 0)
                {
                    continue;
                }

                for (int i = 0; i < targetPeople.Length; i++)
                {
                    var person = targetPeople[i];
                    var personInSource = i < sourcePeople.Length ? sourcePeople[i] : sourcePeople[0];

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
                    targetHasAlbumArtist.AlbumArtists = targetHasAlbumArtist.AlbumArtists.Concat(sourceHasAlbumArtist.AlbumArtists).Distinct().ToArray();
                }
            }
        }

        private static void MergeVideoInfo(BaseItem source, BaseItem target, bool replaceData)
        {
            if (source is Video sourceCast && target is Video targetCast)
            {
                if (replaceData || !targetCast.Video3DFormat.HasValue)
                {
                    targetCast.Video3DFormat = sourceCast.Video3DFormat;
                }
            }
        }
    }
}
