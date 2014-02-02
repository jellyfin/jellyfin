using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Manager
{
    /// <summary>
    /// Class ProviderManager
    /// </summary>
    public class ProviderManager : IProviderManager
    {
        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The _HTTP client
        /// </summary>
        private readonly IHttpClient _httpClient;

        /// <summary>
        /// The _directory watchers
        /// </summary>
        private readonly ILibraryMonitor _libraryMonitor;

        /// <summary>
        /// Gets or sets the configuration manager.
        /// </summary>
        /// <value>The configuration manager.</value>
        private IServerConfigurationManager ConfigurationManager { get; set; }

        /// <summary>
        /// Gets the list of currently registered metadata prvoiders
        /// </summary>
        /// <value>The metadata providers enumerable.</value>
        private BaseMetadataProvider[] MetadataProviders { get; set; }

        private IImageProvider[] ImageProviders { get; set; }

        private readonly IFileSystem _fileSystem;

        private readonly IProviderRepository _providerRepo;

        private IMetadataService[] _metadataServices = { };
        private IMetadataProvider[] _metadataProviders = { };
        private IEnumerable<IMetadataSaver> _savers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderManager" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="libraryMonitor">The directory watchers.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="providerRepo">The provider repo.</param>
        public ProviderManager(IHttpClient httpClient, IServerConfigurationManager configurationManager, ILibraryMonitor libraryMonitor, ILogManager logManager, IFileSystem fileSystem, IProviderRepository providerRepo)
        {
            _logger = logManager.GetLogger("ProviderManager");
            _httpClient = httpClient;
            ConfigurationManager = configurationManager;
            _libraryMonitor = libraryMonitor;
            _fileSystem = fileSystem;
            _providerRepo = providerRepo;
        }

        /// <summary>
        /// Adds the metadata providers.
        /// </summary>
        /// <param name="providers">The providers.</param>
        /// <param name="imageProviders">The image providers.</param>
        /// <param name="metadataServices">The metadata services.</param>
        /// <param name="metadataProviders">The metadata providers.</param>
        /// <param name="metadataSavers">The metadata savers.</param>
        public void AddParts(IEnumerable<BaseMetadataProvider> providers, IEnumerable<IImageProvider> imageProviders, IEnumerable<IMetadataService> metadataServices, IEnumerable<IMetadataProvider> metadataProviders, IEnumerable<IMetadataSaver> metadataSavers)
        {
            MetadataProviders = providers.OrderBy(e => e.Priority).ToArray();

            ImageProviders = imageProviders.ToArray();

            _metadataServices = metadataServices.OrderBy(i => i.Order).ToArray();
            _metadataProviders = metadataProviders.ToArray();
            _savers = metadataSavers.ToArray();
        }

        public Task RefreshMetadata(IHasMetadata item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            var service = _metadataServices.FirstOrDefault(i => i.CanRefresh(item));

            if (service != null)
            {
                return service.RefreshMetadata(item, options, cancellationToken);
            }

            return ((BaseItem)item).RefreshMetadataDirect(cancellationToken, options.ForceSave, options.ReplaceAllMetadata);
        }

        /// <summary>
        /// Runs all metadata providers for an entity, and returns true or false indicating if at least one was refreshed and requires persistence
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <returns>Task{System.Boolean}.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public async Task<ItemUpdateType?> ExecuteMetadataProviders(BaseItem item, CancellationToken cancellationToken, bool force = false)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            ItemUpdateType? result = null;

            cancellationToken.ThrowIfCancellationRequested();

            var enableInternetProviders = ConfigurationManager.Configuration.EnableInternetProviders;

            var providerHistories = item.DateLastSaved == default(DateTime) ?
                new List<BaseProviderInfo>() :
                _providerRepo.GetProviderHistory(item.Id).ToList();

            // Run the normal providers sequentially in order of priority
            foreach (var provider in MetadataProviders)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!ProviderSupportsItem(provider, item))
                {
                    continue;
                }

                // Skip if internet providers are currently disabled
                if (provider.RequiresInternet && !enableInternetProviders)
                {
                    continue;
                }

                // Put this check below the await because the needs refresh of the next tier of providers may depend on the previous ones running
                //  This is the case for the fan art provider which depends on the movie and tv providers having run before them
                if (provider.RequiresInternet && item.DontFetchMeta && provider.EnforceDontFetchMetadata)
                {
                    continue;
                }

                var providerInfo = providerHistories.FirstOrDefault(i => i.ProviderId == provider.Id);

                if (providerInfo == null)
                {
                    providerInfo = new BaseProviderInfo
                    {
                        ProviderId = provider.Id
                    };
                    providerHistories.Add(providerInfo);
                }

                try
                {
                    if (!force && !provider.NeedsRefresh(item, providerInfo))
                    {
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Error determining NeedsRefresh for {0}", ex, item.Path);
                }

                var updateType = await FetchAsync(provider, item, providerInfo, force, cancellationToken).ConfigureAwait(false);

                if (updateType.HasValue)
                {
                    if (result.HasValue)
                    {
                        result = result.Value | updateType.Value;
                    }
                    else
                    {
                        result = updateType;
                    }
                }
            }

            if (result.HasValue || force)
            {
                await _providerRepo.SaveProviderHistory(item.Id, providerHistories, cancellationToken);
            }

            return result;
        }

        /// <summary>
        /// Providers the supports item.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private bool ProviderSupportsItem(BaseMetadataProvider provider, BaseItem item)
        {
            try
            {
                return provider.Supports(item);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("{0} failed in Supports for type {1}", ex, provider.GetType().Name, item.GetType().Name);
                return false;
            }
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider information.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        private async Task<ItemUpdateType?> FetchAsync(BaseMetadataProvider provider, BaseItem item, BaseProviderInfo providerInfo, bool force, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Don't clog up the log with these providers
            if (!(provider is IDynamicInfoProvider))
            {
                _logger.Debug("Running {0} for {1}", provider.GetType().Name, item.Path ?? item.Name ?? "--Unknown--");
            }

            try
            {
                var changed = await provider.FetchAsync(item, force, providerInfo, cancellationToken).ConfigureAwait(false);

                if (changed)
                {
                    return provider.ItemUpdateType;
                }

                return null;
            }
            catch (OperationCanceledException ex)
            {
                _logger.Debug("{0} canceled for {1}", provider.GetType().Name, item.Name);

                // If the outer cancellation token is the one that caused the cancellation, throw it
                if (cancellationToken.IsCancellationRequested && ex.CancellationToken == cancellationToken)
                {
                    throw;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("{0} failed refreshing {1} {2}", ex, provider.GetType().Name, item.Name, item.Path ?? string.Empty);

                provider.SetLastRefreshed(item, DateTime.UtcNow, providerInfo, ProviderRefreshStatus.Failure);

                return ItemUpdateType.Unspecified;
            }
        }

        /// <summary>
        /// Saves to library filesystem.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="path">The path.</param>
        /// <param name="dataToSave">The data to save.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public async Task SaveToLibraryFilesystem(BaseItem item, string path, Stream dataToSave, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException();
            }
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException();
            }
            if (dataToSave == null)
            {
                throw new ArgumentNullException();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                dataToSave.Dispose();
                cancellationToken.ThrowIfCancellationRequested();
            }

            //Tell the watchers to ignore
            _libraryMonitor.ReportFileSystemChangeBeginning(path);

            if (dataToSave.CanSeek)
            {
                dataToSave.Position = 0;
            }

            try
            {
                using (dataToSave)
                {
                    using (var fs = _fileSystem.GetFileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                    {
                        await dataToSave.CopyToAsync(fs, StreamDefaults.DefaultCopyToBufferSize, cancellationToken).ConfigureAwait(false);
                    }
                }

                // If this is ever used for something other than metadata we can add a file type param
                item.ResolveArgs.AddMetadataFile(path);
            }
            finally
            {
                //Remove the ignore
                _libraryMonitor.ReportFileSystemChangeComplete(path, false);
            }
        }


        /// <summary>
        /// Saves the image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="url">The URL.</param>
        /// <param name="resourcePool">The resource pool.</param>
        /// <param name="type">The type.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task SaveImage(BaseItem item, string url, SemaphoreSlim resourcePool, ImageType type, int? imageIndex, CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                ResourcePool = resourcePool,
                Url = url

            }).ConfigureAwait(false);

            await SaveImage(item, response.Content, response.ContentType, type, imageIndex, url, cancellationToken)
                    .ConfigureAwait(false);
        }

        /// <summary>
        /// Saves the image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="source">The source.</param>
        /// <param name="mimeType">Type of the MIME.</param>
        /// <param name="type">The type.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <param name="sourceUrl">The source URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SaveImage(BaseItem item, Stream source, string mimeType, ImageType type, int? imageIndex, string sourceUrl, CancellationToken cancellationToken)
        {
            return new ImageSaver(ConfigurationManager, _libraryMonitor, _fileSystem, _logger).SaveImage(item, source, mimeType, type, imageIndex, sourceUrl, cancellationToken);
        }

        /// <summary>
        /// Gets the available remote images.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="providerName">Name of the provider.</param>
        /// <param name="type">The type.</param>
        /// <returns>Task{IEnumerable{RemoteImageInfo}}.</returns>
        public async Task<IEnumerable<RemoteImageInfo>> GetAvailableRemoteImages(IHasImages item, CancellationToken cancellationToken, string providerName = null, ImageType? type = null)
        {
            var providers = GetRemoteImageProviders(item);

            if (!string.IsNullOrEmpty(providerName))
            {
                providers = providers.Where(i => string.Equals(i.Name, providerName, StringComparison.OrdinalIgnoreCase));
            }

            var preferredLanguage = item.GetPreferredMetadataLanguage();

            var tasks = providers.Select(i => GetImages(item, cancellationToken, i, preferredLanguage, type));

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            return results.SelectMany(i => i);
        }

        /// <summary>
        /// Gets the images.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="i">The i.</param>
        /// <param name="preferredLanguage">The preferred language.</param>
        /// <param name="type">The type.</param>
        /// <returns>Task{IEnumerable{RemoteImageInfo}}.</returns>
        private async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, CancellationToken cancellationToken, IRemoteImageProvider i, string preferredLanguage, ImageType? type = null)
        {
            try
            {
                if (type.HasValue)
                {
                    var result = await i.GetImages(item, type.Value, cancellationToken).ConfigureAwait(false);

                    return FilterImages(result, preferredLanguage);
                }
                else
                {
                    var result = await i.GetAllImages(item, cancellationToken).ConfigureAwait(false);
                    return FilterImages(result, preferredLanguage);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("{0} failed in GetImageInfos for type {1}", ex, i.GetType().Name, item.GetType().Name);
                return new List<RemoteImageInfo>();
            }
        }

        private IEnumerable<RemoteImageInfo> FilterImages(IEnumerable<RemoteImageInfo> images, string preferredLanguage)
        {
            if (string.Equals(preferredLanguage, "en", StringComparison.OrdinalIgnoreCase))
            {
                images = images.Where(i => string.IsNullOrEmpty(i.Language) ||
                                           string.Equals(i.Language, "en", StringComparison.OrdinalIgnoreCase));
            }

            return images;
        }

        /// <summary>
        /// Gets the supported image providers.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>IEnumerable{IImageProvider}.</returns>
        public IEnumerable<ImageProviderInfo> GetImageProviderInfo(IHasImages item)
        {
            return GetRemoteImageProviders(item).Select(i => new ImageProviderInfo
            {
                Name = i.Name,
                Order = GetOrder(item, i)
            });
        }

        public IEnumerable<IImageProvider> GetImageProviders(IHasImages item)
        {
            return ImageProviders.Where(i =>
            {
                try
                {
                    return i.Supports(item);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("{0} failed in Supports for type {1}", ex, i.GetType().Name, item.GetType().Name);
                    return false;
                }

            }).OrderBy(i => GetOrder(item, i));
        }

        public IEnumerable<IMetadataProvider<T>> GetMetadataProviders<T>(IHasMetadata item)
            where T : IHasMetadata
        {
            return GetMetadataProvidersInternal<T>(item, false);
        }

        private IEnumerable<IMetadataProvider<T>> GetMetadataProvidersInternal<T>(IHasMetadata item, bool includeDisabled)
            where T : IHasMetadata
        {
            return _metadataProviders.OfType<IMetadataProvider<T>>()
                .Where(i => CanRefresh(i, item, includeDisabled))
                .OrderBy(i => GetOrder(item, i));
        }

        private IEnumerable<IRemoteImageProvider> GetRemoteImageProviders(IHasImages item)
        {
            return GetImageProviders(item).OfType<IRemoteImageProvider>();
        }

        /// <summary>
        /// Determines whether this instance can refresh the specified provider.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="item">The item.</param>
        /// <param name="includeDisabled">if set to <c>true</c> [include disabled].</param>
        /// <returns><c>true</c> if this instance can refresh the specified provider; otherwise, <c>false</c>.</returns>
        private bool CanRefresh(IMetadataProvider provider, IHasMetadata item, bool includeDisabled)
        {
            if (!includeDisabled && !ConfigurationManager.Configuration.EnableInternetProviders && provider is IRemoteMetadataProvider)
            {
                return false;
            }

            if (item.LocationType != LocationType.FileSystem && provider is ILocalMetadataProvider)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the order.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="provider">The provider.</param>
        /// <returns>System.Int32.</returns>
        private int GetOrder(IHasImages item, IImageProvider provider)
        {
            var hasOrder = provider as IHasOrder;

            if (hasOrder == null)
            {
                return 0;
            }

            return hasOrder.Order;
        }

        /// <summary>
        /// Gets the order.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="provider">The provider.</param>
        /// <returns>System.Int32.</returns>
        private int GetOrder(IHasMetadata item, IMetadataProvider provider)
        {
            var hasOrder = provider as IHasOrder;

            if (hasOrder == null)
            {
                return 0;
            }

            return hasOrder.Order;
        }

        public IEnumerable<MetadataPluginSummary> GetAllMetadataPlugins()
        {
            var list = new List<MetadataPluginSummary>();

            list.Add(GetPluginSummary<Game>());
            list.Add(GetPluginSummary<GameSystem>());
            list.Add(GetPluginSummary<Movie>());
            list.Add(GetPluginSummary<Trailer>());
            list.Add(GetPluginSummary<BoxSet>());
            list.Add(GetPluginSummary<Book>());
            list.Add(GetPluginSummary<Series>());
            list.Add(GetPluginSummary<Season>());
            list.Add(GetPluginSummary<Episode>());
            list.Add(GetPluginSummary<Person>());
            list.Add(GetPluginSummary<MusicAlbum>());
            list.Add(GetPluginSummary<MusicArtist>());
            list.Add(GetPluginSummary<Audio>());

            list.Add(GetPluginSummary<Genre>());
            list.Add(GetPluginSummary<Studio>());
            list.Add(GetPluginSummary<GameGenre>());
            list.Add(GetPluginSummary<MusicGenre>());
            
            return list;
        }

        private MetadataPluginSummary GetPluginSummary<T>()
            where T : BaseItem, new()
        {
            // Give it a dummy path just so that it looks like a file system item
            var dummy = new T()
            {
                Path = "C:\\",

                // Dummy this up to fool the local trailer check
                Parent = new Folder()
            };

            var summary = new MetadataPluginSummary
            {
                ItemType = typeof(T).Name
            };

            var imageProviders = GetImageProviders(dummy).ToList();

            AddMetadataPlugins(summary.Plugins, dummy);
            AddImagePlugins(summary.Plugins, dummy, imageProviders);

            summary.SupportedImageTypes = imageProviders.OfType<IRemoteImageProvider>()
                .SelectMany(i => i.GetSupportedImages(dummy))
                .Distinct()
                .ToList();

            return summary;
        }

        private void AddMetadataPlugins<T>(List<MetadataPlugin> list, T item)
            where T : IHasMetadata
        {
            var providers = GetMetadataProvidersInternal<T>(item, true).ToList();

            // Locals
            list.AddRange(providers.Where(i => (i is ILocalMetadataProvider)).Select(i => new MetadataPlugin
            {
                Name = i.Name,
                Type = MetadataPluginType.LocalMetadataProvider
            }));

            // Fetchers
            list.AddRange(providers.Where(i => !(i is ILocalMetadataProvider)).Select(i => new MetadataPlugin
            {
                Name = i.Name,
                Type = MetadataPluginType.MetadataFetcher
            }));

            // Savers
            list.AddRange(_savers.Where(i => i.IsEnabledFor(item, ItemUpdateType.MetadataEdit)).OrderBy(i => i.Name).Select(i => new MetadataPlugin
            {
                Name = i.Name,
                Type = MetadataPluginType.MetadataSaver
            }));
        }

        private void AddImagePlugins<T>(List<MetadataPlugin> list, T item, List<IImageProvider> imageProviders)
            where T : IHasImages
        {

            // Locals
            list.AddRange(imageProviders.Where(i => (i is ILocalImageProvider)).Select(i => new MetadataPlugin
            {
                Name = i.Name,
                Type = MetadataPluginType.LocalImageProvider
            }));

            // Fetchers
            list.AddRange(imageProviders.Where(i => !(i is ILocalImageProvider)).Select(i => new MetadataPlugin
            {
                Name = i.Name,
                Type = MetadataPluginType.ImageFetcher
            }));
        }

        private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        /// Saves the metadata.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <returns>Task.</returns>
        public async Task SaveMetadata(IHasMetadata item, ItemUpdateType updateType)
        {
            var locationType = item.LocationType;
            if (locationType == LocationType.Remote || locationType == LocationType.Virtual)
            {
                throw new ArgumentException("Only file-system based items can save metadata.");
            }

            foreach (var saver in _savers.Where(i => i.IsEnabledFor(item, updateType)))
            {
                var path = saver.GetSavePath(item);

                var semaphore = _fileLocks.GetOrAdd(path, key => new SemaphoreSlim(1, 1));

                await semaphore.WaitAsync().ConfigureAwait(false);

                try
                {
                    _libraryMonitor.ReportFileSystemChangeBeginning(path);
                    saver.Save(item, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in metadata saver", ex);
                }
                finally
                {
                    _libraryMonitor.ReportFileSystemChangeComplete(path, false);
                    semaphore.Release();
                }
            }
        }
    }
}
