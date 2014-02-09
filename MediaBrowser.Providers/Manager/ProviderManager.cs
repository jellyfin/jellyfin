using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
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
        /// <param name="imageProviders">The image providers.</param>
        /// <param name="metadataServices">The metadata services.</param>
        /// <param name="metadataProviders">The metadata providers.</param>
        /// <param name="metadataSavers">The metadata savers.</param>
        public void AddParts(IEnumerable<IImageProvider> imageProviders, IEnumerable<IMetadataService> metadataServices, IEnumerable<IMetadataProvider> metadataProviders, IEnumerable<IMetadataSaver> metadataSavers)
        {
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

            _logger.Error("Unable to find a metadata service for item of type " + item.GetType().Name);
            return Task.FromResult(true);
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

            await SaveImage(item, response.Content, response.ContentType, type, imageIndex, cancellationToken)
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
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task SaveImage(BaseItem item, Stream source, string mimeType, ImageType type, int? imageIndex, CancellationToken cancellationToken)
        {
            return new ImageSaver(ConfigurationManager, _libraryMonitor, _fileSystem, _logger).SaveImage(item, source, mimeType, type, imageIndex, cancellationToken);
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
        public IEnumerable<ImageProviderInfo> GetRemoteImageProviderInfo(IHasImages item)
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

            // If this restriction is ever lifted, movie xml providers will have to be updated to prevent owned items like trailers from reading those files
            if (item.IsOwnedItem)
            {
                if (provider is ILocalMetadataProvider || provider is IRemoteMetadataProvider)
                {
                    return false;
                }
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
            var list = new List<MetadataPluginSummary>
            {
                GetPluginSummary<Game>(),
                GetPluginSummary<GameSystem>(),
                GetPluginSummary<Movie>(),
                GetPluginSummary<Trailer>(),
                GetPluginSummary<BoxSet>(),
                GetPluginSummary<Book>(),
                GetPluginSummary<Series>(),
                GetPluginSummary<Season>(),
                GetPluginSummary<Episode>(),
                GetPluginSummary<Person>(),
                GetPluginSummary<MusicAlbum>(),
                GetPluginSummary<MusicArtist>(),
                GetPluginSummary<Audio>(),
                GetPluginSummary<Genre>(),
                GetPluginSummary<Studio>(),
                GetPluginSummary<GameGenre>(),
                GetPluginSummary<MusicGenre>(),
                GetPluginSummary<AdultVideo>(),
                GetPluginSummary<MusicVideo>(),
                GetPluginSummary<Video>(),
                GetPluginSummary<LiveTvChannel>(),
                GetPluginSummary<LiveTvProgram>(),
                GetPluginSummary<LiveTvVideoRecording>(),
                GetPluginSummary<LiveTvAudioRecording>()
            };

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
            list.AddRange(providers.Where(i => (i is IRemoteMetadataProvider)).Select(i => new MetadataPlugin
            {
                Name = i.Name,
                Type = MetadataPluginType.MetadataFetcher
            }));

            // Savers
            list.AddRange(_savers.Where(i => IsSaverEnabledForItem(i, item, ItemUpdateType.MetadataEdit, false)).OrderBy(i => i.Name).Select(i => new MetadataPlugin
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

        public MetadataOptions GetMetadataOptions(IHasMetadata item)
        {
            var type = item.GetType().Name;
            return ConfigurationManager.Configuration.MetadataOptions
                .FirstOrDefault(i => string.Equals(i.ItemType, type, StringComparison.OrdinalIgnoreCase)) ??
                new MetadataOptions();
        }
        
        /// <summary>
        /// Saves the metadata.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <returns>Task.</returns>
        public async Task SaveMetadata(IHasMetadata item, ItemUpdateType updateType)
        {
            foreach (var saver in _savers.Where(i => IsSaverEnabledForItem(i, item, updateType, true)))
            {
                _logger.Debug("Saving {0} to {1}.", item.Path ?? item.Name, saver.Name);
                
                var fileSaver = saver as IMetadataFileSaver;

                if (fileSaver != null)
                {
                    string path = null;

                    try
                    {
                        path = fileSaver.GetSavePath(item);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error in {0} GetSavePath", ex, saver.Name);
                        continue;
                    }

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
                else
                {
                    try
                    {
                        saver.Save(item, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error in metadata saver", ex);
                    }
                }
            }
        }

        private bool IsSaverEnabledForItem(IMetadataSaver saver, IHasMetadata item, ItemUpdateType updateType, bool enforceConfiguration)
        {
            var options = GetMetadataOptions(item);

            try
            {
                if (enforceConfiguration && options.DisabledMetadataSavers.Contains(saver.Name, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }

                return saver.IsEnabledFor(item, updateType);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error in {0}.IsEnabledFor", ex, saver.Name);
                return false;
            }
        }
    }
}
