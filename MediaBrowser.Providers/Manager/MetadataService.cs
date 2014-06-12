using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Manager
{
    public abstract class MetadataService<TItemType, TIdType> : IMetadataService
        where TItemType : IHasMetadata, IHasLookupInfo<TIdType>, new()
        where TIdType : ItemLookupInfo, new()
    {
        protected readonly IServerConfigurationManager ServerConfigurationManager;
        protected readonly ILogger Logger;
        protected readonly IProviderManager ProviderManager;
        protected readonly IProviderRepository ProviderRepo;
        protected readonly IFileSystem FileSystem;

        protected MetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IProviderRepository providerRepo, IFileSystem fileSystem)
        {
            ServerConfigurationManager = serverConfigurationManager;
            Logger = logger;
            ProviderManager = providerManager;
            ProviderRepo = providerRepo;
            FileSystem = fileSystem;
        }

        /// <summary>
        /// Saves the provider result.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="result">The result.</param>
        /// <returns>Task.</returns>
        protected Task SaveProviderResult(TItemType item, MetadataStatus result)
        {
            result.ItemId = item.Id;
            result.ItemName = item.Name;
            result.ItemType = item.GetType().Name;

            var series = item as IHasSeries;

            result.SeriesName = series == null ? null : series.SeriesName;

            return ProviderRepo.SaveMetadataStatus(result, CancellationToken.None);
        }

        /// <summary>
        /// Gets the last result.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>ProviderResult.</returns>
        protected MetadataStatus GetLastResult(IHasMetadata item)
        {
            if (item.DateLastSaved == default(DateTime))
            {
                return new MetadataStatus { ItemId = item.Id };
            }

            return ProviderRepo.GetMetadataStatus(item.Id) ?? new MetadataStatus { ItemId = item.Id };
        }

        public async Task RefreshMetadata(IHasMetadata item, MetadataRefreshOptions refreshOptions, CancellationToken cancellationToken)
        {
            if (refreshOptions.DirectoryService == null)
            {
                refreshOptions.DirectoryService = new DirectoryService(Logger);
            }

            var itemOfType = (TItemType)item;
            var config = ProviderManager.GetMetadataOptions(item);

            var updateType = ItemUpdateType.None;
            var refreshResult = GetLastResult(item);
            refreshResult.LastErrorMessage = string.Empty;
            refreshResult.LastStatus = ProviderRefreshStatus.Success;

            var itemImageProvider = new ItemImageProvider(Logger, ProviderManager, ServerConfigurationManager, FileSystem);
            var localImagesFailed = false;

            var allImageProviders = ((ProviderManager)ProviderManager).GetImageProviders(item).ToList();

            // Start by validating images
            try
            {
                // Always validate images and check for new locally stored ones.
                if (itemImageProvider.ValidateImages(item, allImageProviders.OfType<ILocalImageProvider>(), refreshOptions.DirectoryService))
                {
                    updateType = updateType | ItemUpdateType.ImageUpdate;
                }
            }
            catch (Exception ex)
            {
                localImagesFailed = true;
                Logger.ErrorException("Error validating images for {0}", ex, item.Path ?? item.Name);
                refreshResult.AddStatus(ProviderRefreshStatus.Failure, ex.Message);
            }

            // Identify item
            TIdType id = null;

            // Next run metadata providers
            if (refreshOptions.MetadataRefreshMode != MetadataRefreshMode.None)
            {
                var providers = GetProviders(item, refreshResult.DateLastMetadataRefresh.HasValue, refreshOptions)
                    .ToList();

                if (providers.Count > 0 || !refreshResult.DateLastMetadataRefresh.HasValue)
                {
                    if (item.BeforeMetadataRefresh())
                    {
                        updateType = updateType | ItemUpdateType.MetadataImport;
                    }
                }

                if (providers.Count > 0)
                {
                    id = await CreateInitialLookupInfo(itemOfType, cancellationToken).ConfigureAwait(false);
                    var result = await RefreshWithProviders(itemOfType, id, refreshOptions, providers, itemImageProvider, cancellationToken).ConfigureAwait(false);

                    updateType = updateType | result.UpdateType;
                    refreshResult.AddStatus(result.Status, result.ErrorMessage);
                    refreshResult.SetDateLastMetadataRefresh(DateTime.UtcNow);
                    refreshResult.AddImageProvidersRefreshed(result.Providers);
                }
            }

            if (id == null)
            {
                id = await CreateInitialLookupInfo(itemOfType, cancellationToken).ConfigureAwait(false);
            }

            MergeIdentities(itemOfType, id);

            // Next run remote image providers, but only if local image providers didn't throw an exception
            if (!localImagesFailed && refreshOptions.ImageRefreshMode != ImageRefreshMode.ValidationOnly)
            {
                var providers = GetNonLocalImageProviders(item, allImageProviders, refreshResult.DateLastImagesRefresh, refreshOptions).ToList();

                if (providers.Count > 0)
                {
                    var result = await itemImageProvider.RefreshImages(itemOfType, providers, refreshOptions, config, cancellationToken).ConfigureAwait(false);

                    updateType = updateType | result.UpdateType;
                    refreshResult.AddStatus(result.Status, result.ErrorMessage);
                    refreshResult.SetDateLastImagesRefresh(DateTime.UtcNow);
                    refreshResult.AddImageProvidersRefreshed(result.Providers);
                }
            }

            updateType = updateType | BeforeSave(itemOfType);

            var providersHadChanges = updateType > ItemUpdateType.None;

            // Save if changes were made, or it's never been saved before
            if (refreshOptions.ForceSave || providersHadChanges || item.DateLastSaved == default(DateTime))
            {
                // Save to database
                await SaveItem(itemOfType, updateType, cancellationToken);
            }

            if (providersHadChanges || refreshResult.IsDirty)
            {
                await SaveProviderResult(itemOfType, refreshResult).ConfigureAwait(false);
            }
        }

        private void MergeIdentities(TItemType item, TIdType id)
        {
            var hasIdentity = id as IHasIdentities<IItemIdentity>;
            if (hasIdentity != null)
            {
                item.Identities = hasIdentity.Identities.ToList();
            }
        }

        /// <summary>
        /// Befores the save.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>ItemUpdateType.</returns>
        protected virtual ItemUpdateType BeforeSave(TItemType item)
        {
            return ItemUpdateType.None;
        }

        /// <summary>
        /// Gets the providers.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="hasRefreshedMetadata">if set to <c>true</c> [has refreshed metadata].</param>
        /// <param name="options">The options.</param>
        /// <returns>IEnumerable{`0}.</returns>
        protected virtual IEnumerable<IMetadataProvider> GetProviders(IHasMetadata item, bool hasRefreshedMetadata, MetadataRefreshOptions options)
        {
            // Get providers to refresh
            var providers = ((ProviderManager)ProviderManager).GetMetadataProviders<TItemType>(item).ToList();

            // Run all if either of these flags are true
            var runAllProviders = options.ReplaceAllMetadata || options.MetadataRefreshMode == MetadataRefreshMode.FullRefresh || !hasRefreshedMetadata;

            if (!runAllProviders)
            {
                // Avoid implicitly captured closure
                var currentItem = item;

                var providersWithChanges = providers.OfType<IHasChangeMonitor>()
                    .Where(i => HasChanged(currentItem, i, currentItem.DateLastSaved, options.DirectoryService))
                    .Cast<IMetadataProvider<TItemType>>()
                    .ToList();

                if (providersWithChanges.Count == 0)
                {
                    providers = new List<IMetadataProvider<TItemType>>();
                }
                else
                {
                    providers = providers.Where(i =>
                    {
                        // If any provider reports a change, always run local ones as well
                        if (i is ILocalMetadataProvider)
                        {
                            return true;
                        }

                        var anyRemoteProvidersChanged = providersWithChanges.OfType<IRemoteMetadataProvider>()
                            .Any();

                        // If any remote providers changed, run them all so that priorities can be honored
                        if (i is IRemoteMetadataProvider)
                        {
                            return anyRemoteProvidersChanged;
                        }

                        // Run custom providers if they report a change or any remote providers change
                        return anyRemoteProvidersChanged || providersWithChanges.Contains(i);

                    }).ToList();
                }
            }

            return providers;
        }

        protected virtual IEnumerable<IImageProvider> GetNonLocalImageProviders(IHasMetadata item, IEnumerable<IImageProvider> allImageProviders, DateTime? dateLastImageRefresh, ImageRefreshOptions options)
        {
            // Get providers to refresh
            var providers = allImageProviders.Where(i => !(i is ILocalImageProvider)).ToList();

            // Run all if either of these flags are true
            var runAllProviders = options.ImageRefreshMode == ImageRefreshMode.FullRefresh || !dateLastImageRefresh.HasValue;

            if (!runAllProviders)
            {
                providers = providers.OfType<IHasChangeMonitor>()
                    .Where(i => HasChanged(item, i, dateLastImageRefresh.Value, options.DirectoryService))
                    .Cast<IImageProvider>()
                    .ToList();
            }

            return providers;
        }

        protected Task SaveItem(TItemType item, ItemUpdateType reason, CancellationToken cancellationToken)
        {
            return item.UpdateToRepository(reason, cancellationToken);
        }

        public bool CanRefresh(IHasMetadata item)
        {
            return item is TItemType;
        }

        protected virtual async Task<RefreshResult> RefreshWithProviders(TItemType item, TIdType id, MetadataRefreshOptions options, List<IMetadataProvider> providers, ItemImageProvider imageService, CancellationToken cancellationToken)
        {
            var refreshResult = new RefreshResult
            {
                UpdateType = ItemUpdateType.None,
                Providers = providers.Select(i => i.GetType().FullName.GetMD5()).ToList()
            };

            var customProviders = providers.OfType<ICustomMetadataProvider<TItemType>>().ToList();

            foreach (var provider in customProviders.Where(i => i is IPreRefreshProvider))
            {
                await RunCustomProvider(provider, item, options, refreshResult, cancellationToken).ConfigureAwait(false);
            }

            var temp = CreateNew();
            temp.Path = item.Path;

            // If replacing all metadata, run internet providers first
            if (options.ReplaceAllMetadata)
            {
                await ExecuteRemoteProviders(item, temp, id, providers.OfType<IRemoteMetadataProvider<TItemType, TIdType>>(), refreshResult, cancellationToken).ConfigureAwait(false);
            }

            var hasLocalMetadata = false;

            foreach (var provider in providers.OfType<ILocalMetadataProvider<TItemType>>())
            {
                var providerName = provider.GetType().Name;
                Logger.Debug("Running {0} for {1}", providerName, item.Path ?? item.Name);

                var itemInfo = new ItemInfo { Path = item.Path, IsInMixedFolder = item.IsInMixedFolder };

                try
                {
                    var localItem = await provider.GetMetadata(itemInfo, cancellationToken).ConfigureAwait(false);

                    if (localItem.HasMetadata)
                    {
                        if (imageService.MergeImages(item, localItem.Images))
                        {
                            refreshResult.UpdateType = refreshResult.UpdateType | ItemUpdateType.ImageUpdate;
                        }

                        MergeData(localItem.Item, temp, new List<MetadataFields>(), !options.ReplaceAllMetadata, true);
                        refreshResult.UpdateType = refreshResult.UpdateType | ItemUpdateType.MetadataImport;

                        // Only one local provider allowed per item
                        hasLocalMetadata = true;
                        item.IsUnidentified = false;
                        break;
                    }

                    Logger.Debug("{0} returned no metadata for {1}", providerName, item.Path ?? item.Name);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // If a local provider fails, consider that a failure
                    refreshResult.Status = ProviderRefreshStatus.Failure;
                    refreshResult.ErrorMessage = ex.Message;
                    Logger.ErrorException("Error in {0}", ex, provider.Name);

                    // If the local provider fails don't continue with remote providers because the user's saved metadata could be lost
                    return refreshResult;
                }
            }

            // Local metadata is king - if any is found don't run remote providers
            if (!options.ReplaceAllMetadata && (!hasLocalMetadata || options.MetadataRefreshMode == MetadataRefreshMode.FullRefresh))
            {
                await ExecuteRemoteProviders(item, temp, id, providers.OfType<IRemoteMetadataProvider<TItemType, TIdType>>(), refreshResult, cancellationToken).ConfigureAwait(false);
            }

            if (refreshResult.UpdateType > ItemUpdateType.None)
            {
                MergeData(temp, item, item.LockedFields, true, true);
            }

            foreach (var provider in customProviders.Where(i => !(i is IPreRefreshProvider)))
            {
                await RunCustomProvider(provider, item, options, refreshResult, cancellationToken).ConfigureAwait(false);
            }

            return refreshResult;
        }

        private async Task RunCustomProvider(ICustomMetadataProvider<TItemType> provider, TItemType item, MetadataRefreshOptions options, RefreshResult refreshResult, CancellationToken cancellationToken)
        {
            Logger.Debug("Running {0} for {1}", provider.GetType().Name, item.Path ?? item.Name);

            try
            {
                refreshResult.UpdateType = refreshResult.UpdateType | await provider.FetchAsync(item, options, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                refreshResult.Status = ProviderRefreshStatus.CompletedWithErrors;
                refreshResult.ErrorMessage = ex.Message;
                Logger.ErrorException("Error in {0}", ex, provider.Name);
            }
        }

        protected virtual TItemType CreateNew()
        {
            return new TItemType();
        }

        private async Task ExecuteRemoteProviders(TItemType item, TItemType temp, TIdType id, IEnumerable<IRemoteMetadataProvider<TItemType, TIdType>> providers, RefreshResult refreshResult, CancellationToken cancellationToken)
        {
            var unidentifiedCount = 0;
            var identifiedCount = 0;

            foreach (var provider in providers)
            {
                var providerName = provider.GetType().Name;
                Logger.Debug("Running {0} for {1}", providerName, item.Path ?? item.Name);

                if (id != null)
                {
                    MergeNewData(temp, id);
                }

                try
                {
                    var result = await provider.GetMetadata(id, cancellationToken).ConfigureAwait(false);

                    if (result.HasMetadata)
                    {
                        MergeData(result.Item, temp, new List<MetadataFields>(), false, false);

                        refreshResult.UpdateType = refreshResult.UpdateType | ItemUpdateType.MetadataDownload;

                        identifiedCount++;
                    }
                    else
                    {
                        unidentifiedCount++; 
                        Logger.Debug("{0} returned no metadata for {1}", providerName, item.Path ?? item.Name);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    unidentifiedCount++; 
                    refreshResult.Status = ProviderRefreshStatus.CompletedWithErrors;
                    refreshResult.ErrorMessage = ex.Message;
                    Logger.ErrorException("Error in {0}", ex, provider.Name);
                }
            }

            var isUnidentified = unidentifiedCount > 0 && identifiedCount == 0;

            if (item.IsUnidentified != isUnidentified)
            {
                item.IsUnidentified = isUnidentified;
                refreshResult.UpdateType = refreshResult.UpdateType | ItemUpdateType.MetadataImport;
            }
        }

        private async Task<TIdType> CreateInitialLookupInfo(TItemType item, CancellationToken cancellationToken)
        {
            var info = item.GetLookupInfo();
            
            var hasIdentity = info as IHasIdentities<IItemIdentity>;
            if (hasIdentity != null)
            {
                await hasIdentity.FindIdentities(ProviderManager, cancellationToken).ConfigureAwait(false);
            }

            return info;
        }

        private void MergeNewData(TItemType source, TIdType lookupInfo)
        {
            // Copy new provider id's that may have been obtained
            foreach (var providerId in source.ProviderIds)
            {
                var key = providerId.Key;

                // Don't replace existing Id's.
                if (!lookupInfo.ProviderIds.ContainsKey(key))
                {
                    lookupInfo.ProviderIds[key] = providerId.Value;
                }
            }
        }

        protected abstract void MergeData(TItemType source, TItemType target, List<MetadataFields> lockedFields, bool replaceData, bool mergeMetadataSettings);

        public virtual int Order
        {
            get
            {
                return 0;
            }
        }

        private bool HasChanged(IHasMetadata item, IHasChangeMonitor changeMonitor, DateTime date, IDirectoryService directoryService)
        {
            try
            {
                return changeMonitor.HasChanged(item, directoryService, date);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error in {0}.HasChanged", ex, changeMonitor.GetType().Name);
                return false;
            }
        }
    }

    public class RefreshResult
    {
        public ItemUpdateType UpdateType { get; set; }
        public ProviderRefreshStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public List<Guid> Providers { get; set; }
    }
}
