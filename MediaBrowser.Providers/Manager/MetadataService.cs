using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
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
        /// <param name="result">The result.</param>
        /// <returns>Task.</returns>
        protected Task SaveProviderResult(MetadataStatus result)
        {
            return ProviderRepo.SaveMetadataStatus(result, CancellationToken.None);
        }

        /// <summary>
        /// Gets the last result.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <returns>ProviderResult.</returns>
        protected MetadataStatus GetLastResult(Guid itemId)
        {
            return ProviderRepo.GetMetadataStatus(itemId) ?? new MetadataStatus { ItemId = itemId };
        }

        public async Task RefreshMetadata(IHasMetadata item, MetadataRefreshOptions refreshOptions, CancellationToken cancellationToken)
        {
            var itemOfType = (TItemType)item;
            var config = GetMetadataOptions(itemOfType);

            var updateType = ItemUpdateType.Unspecified;
            var refreshResult = GetLastResult(item.Id);
            refreshResult.LastErrorMessage = string.Empty;
            refreshResult.LastStatus = ProviderRefreshStatus.Success;

            var itemImageProvider = new ItemImageProvider(Logger, ProviderManager, ServerConfigurationManager, FileSystem);
            var localImagesFailed = false;

            var allImageProviders = ((ProviderManager)ProviderManager).GetImageProviders(item).ToList();

            // Start by validating images
            try
            {
                // Always validate images and check for new locally stored ones.
                if (itemImageProvider.ValidateImages(item, allImageProviders.OfType<ILocalImageProvider>()))
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

            // Next run metadata providers
            if (refreshOptions.MetadataRefreshMode != MetadataRefreshMode.None)
            {
                var providers = GetProviders(item, refreshResult.DateLastMetadataRefresh.HasValue, refreshOptions).ToList();

                if (providers.Count > 0 || !refreshResult.DateLastMetadataRefresh.HasValue)
                {
                    updateType = updateType | BeforeMetadataRefresh(itemOfType);
                }

                if (providers.Count > 0)
                {

                    var result = await RefreshWithProviders(itemOfType, refreshOptions, providers, cancellationToken).ConfigureAwait(false);

                    updateType = updateType | result.UpdateType;
                    refreshResult.AddStatus(result.Status, result.ErrorMessage);
                    refreshResult.SetDateLastMetadataRefresh(DateTime.UtcNow);
                    refreshResult.AddImageProvidersRefreshed(result.Providers);
                }
            }

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

            var providersHadChanges = updateType > ItemUpdateType.Unspecified;

            if (refreshOptions.ForceSave || providersHadChanges)
            {
                if (string.IsNullOrEmpty(item.Name))
                {
                    throw new InvalidOperationException(item.GetType().Name + " has no name: " + item.Path);
                }

                // Save to database
                await SaveItem(itemOfType, updateType, cancellationToken);
            }

            if (providersHadChanges || refreshResult.IsDirty)
            {
                await SaveProviderResult(refreshResult).ConfigureAwait(false);
            }
        }

        private readonly MetadataOptions _defaultOptions = new MetadataOptions();
        protected MetadataOptions GetMetadataOptions(TItemType item)
        {
            var type = item.GetType().Name;
            return ServerConfigurationManager.Configuration.MetadataOptions
                .FirstOrDefault(i => string.Equals(i.ItemType, type, StringComparison.OrdinalIgnoreCase)) ??
                _defaultOptions;
        }

        /// <summary>
        /// Befores the metadata refresh.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>ItemUpdateType.</returns>
        protected virtual ItemUpdateType BeforeMetadataRefresh(TItemType item)
        {
            return ItemUpdateType.Unspecified;
        }

        /// <summary>
        /// Befores the save.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>ItemUpdateType.</returns>
        protected virtual ItemUpdateType BeforeSave(TItemType item)
        {
            return ItemUpdateType.Unspecified;
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
                    .Where(i => i.HasChanged(currentItem, currentItem.DateLastSaved))
                    .ToList();

                if (providersWithChanges.Count > 0)
                {
                    var b = true;
                }

                // If local providers are the only ones with changes, then just run those
                if (providersWithChanges.All(i => i is ILocalMetadataProvider))
                {
                    providers = providersWithChanges.Count == 0 ?
                        new List<IMetadataProvider<TItemType>>() :
                        providers.Where(i => i is ILocalMetadataProvider).ToList();
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
                // Avoid implicitly captured closure
                var currentItem = item;

                providers = providers.OfType<IHasChangeMonitor>()
                    .Where(i => i.HasChanged(currentItem, dateLastImageRefresh.Value))
                    .Cast<IImageProvider>()
                    .ToList();
            }

            return providers;
        }

        protected abstract Task SaveItem(TItemType item, ItemUpdateType reason, CancellationToken cancellationToken);

        public bool CanRefresh(IHasMetadata item)
        {
            return item is TItemType;
        }

        protected virtual async Task<RefreshResult> RefreshWithProviders(TItemType item, MetadataRefreshOptions options, List<IMetadataProvider> providers, CancellationToken cancellationToken)
        {
            var refreshResult = new RefreshResult
            {
                UpdateType = ItemUpdateType.Unspecified,
                Providers = providers.Select(i => i.GetType().FullName.GetMD5()).ToList()
            };

            var temp = CreateNew();
            temp.Path = item.Path;

            // If replacing all metadata, run internet providers first
            if (options.ReplaceAllMetadata)
            {
                await ExecuteRemoteProviders(item, temp, providers.OfType<IRemoteMetadataProvider<TItemType, TIdType>>(), refreshResult, cancellationToken).ConfigureAwait(false);
            }

            var hasLocalMetadata = false;

            foreach (var provider in providers.OfType<ILocalMetadataProvider<TItemType>>())
            {
                Logger.Debug("Running {0} for {1}", provider.GetType().Name, item.Path ?? item.Name);

                var itemInfo = new ItemInfo { Path = item.Path, IsInMixedFolder = item.IsInMixedFolder };

                try
                {
                    var localItem = await provider.GetMetadata(itemInfo, cancellationToken).ConfigureAwait(false);

                    if (localItem.HasMetadata)
                    {
                        if (!string.IsNullOrEmpty(localItem.Item.Name))
                        {
                            MergeData(localItem.Item, temp, new List<MetadataFields>(), !options.ReplaceAllMetadata, true);
                            refreshResult.UpdateType = refreshResult.UpdateType | ItemUpdateType.MetadataImport;

                            // Only one local provider allowed per item
                            hasLocalMetadata = true;
                            break;
                        }

                        Logger.Error("Invalid local metadata found for: " + item.Path);
                    }
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

            if (!options.ReplaceAllMetadata && !hasLocalMetadata)
            {
                await ExecuteRemoteProviders(item, temp, providers.OfType<IRemoteMetadataProvider<TItemType, TIdType>>(), refreshResult, cancellationToken).ConfigureAwait(false);
            }

            if (refreshResult.UpdateType > ItemUpdateType.Unspecified)
            {
                MergeData(temp, item, item.LockedFields, true, true);
            }

            foreach (var provider in providers.OfType<ICustomMetadataProvider<TItemType>>())
            {
                await RunCustomProvider(provider, item, refreshResult, cancellationToken).ConfigureAwait(false);
            }

            return refreshResult;
        }

        private async Task RunCustomProvider(ICustomMetadataProvider<TItemType> provider, TItemType item, RefreshResult refreshResult, CancellationToken cancellationToken)
        {
            Logger.Debug("Running {0} for {1}", provider.GetType().Name, item.Path ?? item.Name);

            try
            {
                refreshResult.UpdateType = refreshResult.UpdateType | await provider.FetchAsync(item, cancellationToken).ConfigureAwait(false);
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

        private async Task ExecuteRemoteProviders(TItemType item, TItemType temp, IEnumerable<IRemoteMetadataProvider<TItemType, TIdType>> providers, RefreshResult refreshResult, CancellationToken cancellationToken)
        {
            TIdType id = null;

            foreach (var provider in providers)
            {
                Logger.Debug("Running {0} for {1}", provider.GetType().Name, item.Path ?? item.Name);

                id = id ?? item.GetLookupInfo();

                try
                {
                    var result = await provider.GetMetadata(id, cancellationToken).ConfigureAwait(false);

                    if (result.HasMetadata)
                    {
                        MergeData(result.Item, temp, new List<MetadataFields>(), false, false);

                        refreshResult.UpdateType = refreshResult.UpdateType | ItemUpdateType.MetadataDownload;
                    }
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
        }

        protected abstract void MergeData(TItemType source, TItemType target, List<MetadataFields> lockedFields, bool replaceData, bool mergeMetadataSettings);

        public virtual int Order
        {
            get
            {
                return 0;
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
