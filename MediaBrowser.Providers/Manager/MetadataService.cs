using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Manager
{
    public abstract class MetadataService<TItemType> : IMetadataService
        where TItemType : IHasMetadata, new()
    {
        protected readonly IServerConfigurationManager ServerConfigurationManager;
        protected readonly ILogger Logger;
        protected readonly IProviderManager ProviderManager;

        private IMetadataProvider<TItemType>[] _providers = { };

        private IImageProvider[] _imageProviders = { };

        protected MetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager)
        {
            ServerConfigurationManager = serverConfigurationManager;
            Logger = logger;
            ProviderManager = providerManager;
        }

        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="providers">The providers.</param>
        /// <param name="imageProviders">The image providers.</param>
        public void AddParts(IEnumerable<IMetadataProvider> providers, IEnumerable<IImageProvider> imageProviders)
        {
            _providers = providers.OfType<IMetadataProvider<TItemType>>()
                .ToArray();

            _imageProviders = imageProviders.OrderBy(i => i.Order).ToArray();
        }

        /// <summary>
        /// Saves the provider result.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>Task.</returns>
        protected Task SaveProviderResult(ProviderResult result)
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Gets the last result.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <returns>ProviderResult.</returns>
        protected ProviderResult GetLastResult(Guid itemId)
        {
            return new ProviderResult
            {
                ItemId = itemId
            };
        }

        public async Task RefreshMetadata(IHasMetadata item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            var itemOfType = (TItemType)item;

            var updateType = ItemUpdateType.Unspecified;
            var lastResult = GetLastResult(item.Id);
            var refreshResult = new ProviderResult { ItemId = item.Id };

            var imageProviders = GetImageProviders(item).ToList();
            var itemImageProvider = new ItemImageProvider(Logger, ProviderManager, ServerConfigurationManager);
            var localImagesFailed = false;

            // Start by validating images
            try
            {
                // Always validate images and check for new locally stored ones.
                if (itemImageProvider.ValidateImages(item, imageProviders))
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
            if (options.MetadataRefreshMode != MetadataRefreshMode.None)
            {
                var providers = GetProviders(item, lastResult.HasRefreshedMetadata, options).ToList();

                if (providers.Count > 0)
                {
                    var result = await RefreshWithProviders(itemOfType, options, providers, cancellationToken).ConfigureAwait(false);

                    updateType = updateType | result.UpdateType;
                    refreshResult.AddStatus(result.Status, result.ErrorMessage);
                }

                refreshResult.HasRefreshedMetadata = true;
            }

            // Next run remote image providers, but only if local image providers didn't throw an exception
            if (!localImagesFailed)
            {
                if ((options.ImageRefreshMode == MetadataRefreshMode.EnsureMetadata && !lastResult.HasRefreshedImages) ||
                                            options.ImageRefreshMode == MetadataRefreshMode.FullRefresh)
                {
                    var imagesReult = await itemImageProvider.RefreshImages(itemOfType, imageProviders, options, cancellationToken).ConfigureAwait(false);

                    updateType = updateType | imagesReult.UpdateType;
                    refreshResult.AddStatus(imagesReult.Status, imagesReult.ErrorMessage);
                    refreshResult.HasRefreshedImages = true;
                }
            }

            var providersHadChanges = updateType > ItemUpdateType.Unspecified;

            if (options.ForceSave || providersHadChanges)
            {
                if (string.IsNullOrEmpty(item.Name))
                {
                    throw new InvalidOperationException("Item has no name");
                }

                // Save to database
                await SaveItem(itemOfType, updateType, cancellationToken);
            }

            if (providersHadChanges)
            {
                refreshResult.DateLastRefreshed = DateTime.UtcNow;
                await SaveProviderResult(refreshResult).ConfigureAwait(false);
            }
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
            var providers = _providers.Where(i => CanRefresh(i, item)).ToList();

            // Run all if either of these flags are true
            var runAllProviders = options.ReplaceAllMetadata || options.MetadataRefreshMode == MetadataRefreshMode.FullRefresh || !hasRefreshedMetadata;

            if (!runAllProviders)
            {
                // Avoid implicitly captured closure
                var currentItem = item;

                var providersWithChanges = providers.OfType<IHasChangeMonitor>()
                    .Where(i => i.HasChanged(currentItem, item.DateLastSaved))
                    .ToList();

                // If local providers are the only ones with changes, then just run those
                if (providersWithChanges.All(i => i is ILocalMetadataProvider))
                {
                    providers = providers.Where(i => i is ILocalMetadataProvider).ToList();
                }
            }

            return providers;
        }

        /// <summary>
        /// Determines whether this instance can refresh the specified provider.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if this instance can refresh the specified provider; otherwise, <c>false</c>.</returns>
        protected bool CanRefresh(IMetadataProvider provider, IHasMetadata item)
        {
            if (!ServerConfigurationManager.Configuration.EnableInternetProviders && provider is IRemoteMetadataProvider)
            {
                return false;
            }

            if (item.LocationType != LocationType.FileSystem && provider is ILocalMetadataProvider)
            {
                return false;
            }

            return true;
        }

        protected abstract Task SaveItem(TItemType item, ItemUpdateType reason, CancellationToken cancellationToken);

        protected virtual ItemId GetId(IHasMetadata item)
        {
            return new ItemId
            {
                MetadataCountryCode = item.GetPreferredMetadataCountryCode(),
                MetadataLanguage = item.GetPreferredMetadataLanguage(),
                Name = item.Name,
                ProviderIds = item.ProviderIds
            };
        }

        public bool CanRefresh(IHasMetadata item)
        {
            return item is TItemType;
        }

        protected virtual async Task<RefreshResult> RefreshWithProviders(TItemType item, MetadataRefreshOptions options, List<IMetadataProvider> providers, CancellationToken cancellationToken)
        {
            var refreshResult = new RefreshResult { UpdateType = ItemUpdateType.Unspecified };

            var temp = new TItemType();

            // If replacing all metadata, run internet providers first
            if (options.ReplaceAllMetadata)
            {
                await ExecuteRemoteProviders(item, temp, providers.OfType<IRemoteMetadataProvider<TItemType>>(), refreshResult, cancellationToken).ConfigureAwait(false);
            }

            var hasLocalMetadata = false;

            foreach (var provider in providers.OfType<ILocalMetadataProvider<TItemType>>())
            {
                Logger.Debug("Running {0} for {1}", provider.GetType().Name, item.Path ?? item.Name);

                try
                {
                    var localItem = await provider.GetMetadata(item.Path, cancellationToken).ConfigureAwait(false);

                    if (localItem.HasMetadata)
                    {
                        MergeData(localItem.Item, temp, new List<MetadataFields>(), false, true);
                        refreshResult.UpdateType = refreshResult.UpdateType | ItemUpdateType.MetadataImport;

                        // Only one local provider allowed per item
                        hasLocalMetadata = true;
                        break;
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
                await ExecuteRemoteProviders(item, temp, providers.OfType<IRemoteMetadataProvider<TItemType>>(), refreshResult, cancellationToken).ConfigureAwait(false);
            }

            MergeData(temp, item, item.LockedFields, true, true);

            return refreshResult;
        }

        private async Task ExecuteRemoteProviders(TItemType item, TItemType temp, IEnumerable<IRemoteMetadataProvider<TItemType>> providers, RefreshResult refreshResult, CancellationToken cancellationToken)
        {
            var id = GetId(item);

            foreach (var provider in providers)
            {
                Logger.Debug("Running {0} for {1}", provider.GetType().Name, item.Path ?? item.Name);

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

        private IEnumerable<IImageProvider> GetImageProviders(IHasImages item)
        {
            var providers = _imageProviders.Where(i =>
            {
                try
                {
                    return i.Supports(item);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error in ImageProvider.Supports", ex, i.Name);

                    return false;
                }
            });

            if (!ServerConfigurationManager.Configuration.EnableInternetProviders)
            {
                providers = providers.Where(i => !(i is IRemoteImageProvider));
            }

            return providers.OrderBy(i => i.Order);
        }
    }

    public class RefreshResult
    {
        public ItemUpdateType UpdateType { get; set; }
        public ProviderRefreshStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
